using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(category: "Animation Core/Blend Trees", description: "Evaluates a 2D BlendTree based on X and Y blend parameters")]
    public class BlendTree2DNode
        : NodeDefinition<BlendTree2DNode.Data, BlendTree2DNode.SimPorts, BlendTree2DNode.KernelData, BlendTree2DNode.KernelDefs, BlendTree2DNode.Kernel>
        , IMsgHandler<BlobAssetReference<BlendTree2DSimpleDirectional>>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(description: "BlendTree data")]
            public MessageInput<BlendTree2DNode, BlobAssetReference<BlendTree2DSimpleDirectional>> BlendTree;
            [PortDefinition(isHidden: true)]
            public MessageInput<BlendTree2DNode, Rig> Rig;

            [PortDefinition(isHidden: true)]
            public MessageOutput<BlendTree2DNode, Rig> RigOut;
        }

        [Managed]
        public struct Data : INodeData
        {
            // Assets.
            public BlobAssetReference<RigDefinition>                RigDefinition;
            public BlobAssetReference<BlendTree2DSimpleDirectional> BlendTree;

            internal NodeHandle<BlendTree2DNode>                    BlendTree2DNode;
            internal NodeHandle<KernelPassThroughNodeFloat>         NormalizedTimeNode;
            internal NodeHandle<ComputeBlendTree2DWeightsNode>      ComputeBlendTree2DWeightsNode;

            internal NodeHandle<NMixerNode>                         NMixerNode;

            internal List<NodeHandle<UberClipNode>>                 Motions;
            internal List<NodeHandle<GetBufferElementValueNode>>    MotionDurationNodes;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description: "Normalized time")]
            public DataInput<BlendTree2DNode, float> NormalizedTime;
            [PortDefinition(displayName: "Blend X", description: "Blend parameter X value")]
            public DataInput<BlendTree2DNode, float> BlendParameterX;
            [PortDefinition(displayName: "Blend Y", description: "Blend parameter Y value")]
            public DataInput<BlendTree2DNode, float> BlendParameterY;

            [PortDefinition(description: "Resulting animation stream")]
            public DataOutput<BlendTree2DNode, Buffer<AnimatedData>> Output;
            [PortDefinition(description: "Current motion duration, used to compute normalized time")]
            public DataOutput<BlendTree2DNode, float> Duration;
        }

        public struct KernelData : IKernelData
        {
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var data = ref GetNodeData(ctx.Handle);

            data.BlendTree2DNode = Set.CastHandle<BlendTree2DNode>(ctx.Handle);

            data.NormalizedTimeNode = Set.Create<KernelPassThroughNodeFloat>();
            data.NMixerNode = Set.Create<NMixerNode>();
            data.ComputeBlendTree2DWeightsNode = Set.Create<ComputeBlendTree2DWeightsNode>();

            Set.Connect(data.BlendTree2DNode, SimulationPorts.RigOut, data.NMixerNode, NMixerNode.SimulationPorts.Rig);

            ctx.ForwardInput(KernelPorts.NormalizedTime, data.NormalizedTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardInput(KernelPorts.BlendParameterX, data.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.KernelPorts.BlendParameterX);
            ctx.ForwardInput(KernelPorts.BlendParameterY, data.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.KernelPorts.BlendParameterY);
            ctx.ForwardOutput(KernelPorts.Output, data.NMixerNode, NMixerNode.KernelPorts.Output);
            ctx.ForwardOutput(KernelPorts.Duration, data.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.KernelPorts.Duration);

            data.Motions = new List<NodeHandle<UberClipNode>>();
            data.MotionDurationNodes = new List<NodeHandle<GetBufferElementValueNode>>();
        }

        protected override void Destroy(NodeHandle handle)
        {
            var data = GetNodeData(handle);

            Set.Destroy(data.NormalizedTimeNode);
            Set.Destroy(data.NMixerNode);
            Set.Destroy(data.ComputeBlendTree2DWeightsNode);

            for (int i = 0; i < data.Motions.Count; i++)
                Set.Destroy(data.Motions[i]);

            for (int i = 0; i < data.MotionDurationNodes.Count; i++)
                Set.Destroy(data.MotionDurationNodes[i]);
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<BlendTree2DSimpleDirectional> blendTree)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);
            nodeData.BlendTree = blendTree;

            var thisHandle = Set.CastHandle<BlendTree2DNode>(ctx.Handle);

            for (int i = 0; i < nodeData.Motions.Count; i++)
                Set.Destroy(nodeData.Motions[i]);

            for (int i = 0; i < nodeData.MotionDurationNodes.Count; i++)
                Set.Destroy(nodeData.MotionDurationNodes[i]);

            nodeData.Motions.Clear();
            nodeData.MotionDurationNodes.Clear();

            Set.SendMessage(nodeData.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.SimulationPorts.BlendTree, blendTree);

            var length = nodeData.BlendTree.Value.Motions.Length;

            Set.SetPortArraySize(nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, (ushort)length);
            Set.SetPortArraySize(nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, (ushort)length);
            Set.SetPortArraySize(nodeData.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.KernelPorts.MotionDurations, (ushort)length);

            for (int i = 0; i < length; i++)
            {
                var clip = nodeData.BlendTree.Value.Motions[i].Clip;
                if (!clip.IsCreated)
                    throw new InvalidOperationException("Motion in BlendTree2D is not valid");

                var motionNode = Set.Create<UberClipNode>();
                Set.SendMessage(motionNode, UberClipNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.NormalizedTime });
                Set.SendMessage(motionNode, UberClipNode.SimulationPorts.Clip, clip);

                var bufferElementToPortNode = Set.Create<GetBufferElementValueNode>();

                nodeData.Motions.Add(motionNode);
                nodeData.MotionDurationNodes.Add(bufferElementToPortNode);

                Set.Connect(thisHandle, SimulationPorts.RigOut, motionNode, UberClipNode.SimulationPorts.Rig);
                Set.Connect(nodeData.NormalizedTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, motionNode, UberClipNode.KernelPorts.Time);
                Set.Connect(motionNode, UberClipNode.KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, i);
                Set.SetData(nodeData.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.KernelPorts.MotionDurations, i, clip.Value.Duration);

                Set.Connect(nodeData.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.KernelPorts.Weights, bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Input);
                Set.Connect(bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, i);
                Set.SetData(bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Index, i);
            }

            // Forward rig definition to all children.
            ctx.EmitMessage(SimulationPorts.RigOut, new Rig { Value = nodeData.RigDefinition });
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);
            nodeData.RigDefinition = rig;
            // Forward rig definition to all children
            ctx.EmitMessage(SimulationPorts.RigOut, rig);
        }

        // Needed for test purpose to inspect internal node
        internal Data ExposeNodeData(NodeHandle handle) => GetNodeData(handle);

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
