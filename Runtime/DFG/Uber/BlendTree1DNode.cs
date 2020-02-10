using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Blend Trees", description:"Evaluates a 1D BlendTree based on a blend parameter")]
    public class BlendTree1DNode
        : NodeDefinition<BlendTree1DNode.Data, BlendTree1DNode.SimPorts, BlendTree1DNode.KernelData, BlendTree1DNode.KernelDefs, BlendTree1DNode.Kernel>
        , IMsgHandler<BlobAssetReference<BlendTree1D>>
        , IMsgHandler<Rig>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(description:"BlendTree data")]
            public MessageInput<BlendTree1DNode, BlobAssetReference<BlendTree1D>> BlendTree;
            [PortDefinition(isHidden:true)]
            public MessageInput<BlendTree1DNode, Rig> Rig;

            [PortDefinition(isHidden:true)]
            public MessageOutput<BlendTree1DNode, Rig> RigOut;
        }

        [Managed]
        public struct Data : INodeData
        {
            // Assets.
            public BlobAssetReference<RigDefinition>           RigDefinition;
            public BlobAssetReference<BlendTree1D>             BlendTree;

            internal NodeHandle<ComputeBlendTree1DWeightsNode> ComputeBlendTree1DWeightsNode;
            internal NodeHandle<BlendTree1DNode>               BlendTree1DNode;
            internal NodeHandle<KernelPassThroughNodeFloat>    NormalizedTimeNode;
            internal NodeHandle<NMixerNode>                    NMixerNode;

            internal List<NodeHandle<UberClipNode>>              Motions;
            internal List<NodeHandle<GetBufferElementValueNode>> MotionDurationNodes;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Normalized time")]
            public DataInput<BlendTree1DNode, float> NormalizedTime;
            [PortDefinition(displayName:"Blend", description:"Blend parameter value")]
            public DataInput<BlendTree1DNode, float> BlendParameter;

            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<BlendTree1DNode, Buffer<AnimatedData>> Output;
            [PortDefinition(description:"Current motion duration, used to compute normalized time")]
            public DataOutput<BlendTree1DNode, float> Duration;
        }

        public struct KernelData : IKernelData
        {
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var data = ref GetNodeData(ctx.Handle);
            data.BlendTree1DNode = Set.CastHandle<BlendTree1DNode>(ctx.Handle);

            data.NormalizedTimeNode = Set.Create<KernelPassThroughNodeFloat>();
            data.NMixerNode = Set.Create<NMixerNode>();
            data.ComputeBlendTree1DWeightsNode = Set.Create<ComputeBlendTree1DWeightsNode>();

            Set.Connect(data.BlendTree1DNode, SimulationPorts.RigOut, data.NMixerNode, NMixerNode.SimulationPorts.Rig);

            ctx.ForwardInput(KernelPorts.NormalizedTime, data.NormalizedTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardInput(KernelPorts.BlendParameter, data.ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.KernelPorts.BlendParameter);
            ctx.ForwardOutput(KernelPorts.Output, data.NMixerNode, NMixerNode.KernelPorts.Output);
            ctx.ForwardOutput(KernelPorts.Duration, data.ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.KernelPorts.Duration);

            data.Motions = new List<NodeHandle<UberClipNode>>();
            data.MotionDurationNodes = new List<NodeHandle<GetBufferElementValueNode>>();
        }

        protected override void Destroy(NodeHandle handle)
        {
            var data = GetNodeData(handle);

            Set.Destroy(data.NormalizedTimeNode);
            Set.Destroy(data.NMixerNode);
            Set.Destroy(data.ComputeBlendTree1DWeightsNode);

            for(int i=0;i<data.Motions.Count;i++)
                Set.Destroy(data.Motions[i]);

            for(int i=0;i<data.MotionDurationNodes.Count;i++)
                Set.Destroy(data.MotionDurationNodes[i]);
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<BlendTree1D> blendTree)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            var thisHandle = Set.CastHandle<BlendTree1DNode>(ctx.Handle);

            nodeData.BlendTree = blendTree;

            Set.SendMessage(nodeData.ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.SimulationPorts.BlendTree, blendTree);

            for(int i=0;i<nodeData.Motions.Count;i++)
                Set.Destroy(nodeData.Motions[i]);

            for(int i=0;i<nodeData.MotionDurationNodes.Count;i++)
                Set.Destroy(nodeData.MotionDurationNodes[i]);

            nodeData.Motions.Clear();
            nodeData.MotionDurationNodes.Clear();

            var length = nodeData.BlendTree.Value.Motions.Length;

            Set.SetPortArraySize(nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, (ushort)length);
            Set.SetPortArraySize(nodeData.NMixerNode, NMixerNode.KernelPorts.Weights, (ushort)length);
            Set.SetPortArraySize(nodeData.ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.KernelPorts.MotionDurations, (ushort)length);
            
            for(int i=0;i<length;i++)
            {
                var clip = nodeData.BlendTree.Value.Motions[i].Clip;
                if(!clip.IsCreated)
                    throw new InvalidOperationException("Motion in BlendTree1D is not valid");

                var motionNode = Set.Create<UberClipNode>();
                Set.SendMessage(motionNode, UberClipNode.SimulationPorts.Configuration, new ClipConfiguration { Mask = ClipConfigurationMask.NormalizedTime });
                Set.SendMessage(motionNode, UberClipNode.SimulationPorts.Clip, clip);

                var bufferElementToPortNode = Set.Create<GetBufferElementValueNode>();

                nodeData.Motions.Add(motionNode);
                nodeData.MotionDurationNodes.Add(bufferElementToPortNode);

                Set.Connect(thisHandle, SimulationPorts.RigOut, motionNode, UberClipNode.SimulationPorts.Rig);
                Set.Connect(nodeData.NormalizedTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, motionNode, UberClipNode.KernelPorts.Time);
                Set.Connect(motionNode, UberClipNode.KernelPorts.Output, nodeData.NMixerNode, NMixerNode.KernelPorts.Inputs, i );
                Set.SetData(nodeData.ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.KernelPorts.MotionDurations, i, clip.Value.Duration );

                Set.Connect(nodeData.ComputeBlendTree1DWeightsNode, ComputeBlendTree1DWeightsNode.KernelPorts.Weights, bufferElementToPortNode, GetBufferElementValueNode.KernelPorts.Input);
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
