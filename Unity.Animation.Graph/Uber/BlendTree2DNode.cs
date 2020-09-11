using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "dd965a2ebf994f77ad87f02fce11ce9a", version: 1, category: "Animation Core/Blend Trees", description: "Evaluates a 2D BlendTree based on X and Y blend parameters")]
    public class BlendTree2DNode
        : NodeDefinition<BlendTree2DNode.Data, BlendTree2DNode.SimPorts, BlendTree2DNode.KernelData, BlendTree2DNode.KernelDefs, BlendTree2DNode.Kernel>
        , IMsgHandler<BlobAssetReference<BlendTree2DSimpleDirectional>>
        , IRigContextHandler
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "28cb77b3c65c49fabe33810d9665efa5", description: "BlendTree data")]
            public MessageInput<BlendTree2DNode, BlobAssetReference<BlendTree2DSimpleDirectional>> BlendTree;
            [PortDefinition(guid: "2efe136265d74ee6bf17610df0d2fcbe", isHidden: true)]
            public MessageInput<BlendTree2DNode, Rig> Rig;

            [PortDefinition(guid: "e0f25365515b4009bcf8f99e8208bc68", isHidden: true)]
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
            [PortDefinition(guid: "6d64e4d8dd1a4574ab065ce67c239a15", description: "Normalized time")]
            public DataInput<BlendTree2DNode, float> NormalizedTime;
            [PortDefinition(guid: "20a3ff3a4a99432783ec25b41830a83b", displayName: "Blend X", description: "Blend parameter X value")]
            public DataInput<BlendTree2DNode, float> BlendParameterX;
            [PortDefinition(guid: "ed4275b3425a431ea5e05c3a9e7dc65f", displayName: "Blend Y", description: "Blend parameter Y value")]
            public DataInput<BlendTree2DNode, float> BlendParameterY;

            [PortDefinition(guid: "8279a2f700f14b81bb2f4db467d2fb7a", description: "Resulting animation stream")]
            public DataOutput<BlendTree2DNode, Buffer<AnimatedData>> Output;
            [PortDefinition(guid: "8a3c6c2b60ee493ca8eed9f5488fb39e", description: "Current motion duration, used to compute normalized time")]
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

        protected override void Destroy(DestroyContext ctx)
        {
            var data = GetNodeData(ctx.Handle);

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

#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
            Set.SendMessage(nodeData.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.SimulationPorts.BlendTree, blendTree);
#pragma warning restore 0618

            var length = nodeData.BlendTree.Value.Motions.Length;

            Set.SetPortArraySize(nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, (ushort)length);
            Set.SetPortArraySize(nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, (ushort)length);
            Set.SetPortArraySize(nodeData.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.KernelPorts.MotionDurations, (ushort)length);

            for (int i = 0; i < length; i++)
            {
                var clip = nodeData.BlendTree.Value.Motions[i].Clip;
                Core.ValidateIsCreated(clip);

                var motionNode = Set.Create<UberClipNode>();
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
                Set.SendMessage(motionNode, UberClipNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.NormalizedTime });
                Set.SendMessage(motionNode, UberClipNode.SimulationPorts.Clip, clip);
#pragma warning restore 0618

                var bufferElementToPortNode = Set.Create<GetBufferElementValueNode>();

                nodeData.Motions.Add(motionNode);
                nodeData.MotionDurationNodes.Add(bufferElementToPortNode);

                Set.Connect(thisHandle, SimulationPorts.RigOut, motionNode, UberClipNode.SimulationPorts.Rig);
                Set.Connect(nodeData.NormalizedTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, motionNode, UberClipNode.KernelPorts.Time);
                Set.Connect(motionNode, UberClipNode.KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, i);
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
                Set.SetData(nodeData.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.KernelPorts.MotionDurations, i, clip.Value.Duration);
#pragma warning restore 0618

                Set.Connect(nodeData.ComputeBlendTree2DWeightsNode, ComputeBlendTree2DWeightsNode.KernelPorts.Weights, bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Input);
                Set.Connect(bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, i);
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
                Set.SetData(bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Index, i);
#pragma warning restore 0618
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
