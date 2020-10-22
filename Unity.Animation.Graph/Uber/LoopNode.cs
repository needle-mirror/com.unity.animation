using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "e995e3e7e5d640e5b6123dfcef20eba7", version: 1, isHidden: true)]
    public class LoopNode
        : SimulationKernelNodeDefinition<LoopNode.SimPorts, LoopNode.KernelDefs>
        , IRigContextHandler<LoopNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<LoopNode, Rig> Rig;
            public MessageInput<LoopNode, int> SkipRoot;

            internal MessageOutput<LoopNode, Rig> m_OutRig;
            internal MessageOutput<LoopNode, int> m_OutTranslationBindingIndex;
            internal MessageOutput<LoopNode, int> m_OutRotationBindingIndex;
            internal MessageOutput<LoopNode, int> m_OutScaleBindingIndexRig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<LoopNode, float> NormalizedTime;
            public DataInput<LoopNode, float> RootWeightMultiplier;
            public DataInput<LoopNode, Buffer<AnimatedData>> Delta;

            public DataInput<LoopNode, Buffer<AnimatedData>> Input;
            public DataOutput<LoopNode, Buffer<AnimatedData>> Output;
        }

        struct Data : INodeData, IInit, IDestroy
            , IMsgHandler<Rig>, IMsgHandler<int>
        {
            NodeHandle<SimPassThroughNode<Rig>> m_RigNode;

            NodeHandle<KernelPassThroughNodeFloat> m_DefaultWeightNode;
            NodeHandle<WeightBuilderNode> m_WeightChannelsNode;
            NodeHandle<FloatMulNode> m_RootWeightNode;

            NodeHandle<AddPoseNode>     m_AddNode;
            NodeHandle<InversePoseNode> m_InverseNode;
            NodeHandle<WeightPoseNode>  m_WeightNode;

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<LoopNode>(ctx.Handle);

                m_RigNode = ctx.Set.Create<SimPassThroughNode<Rig>>();
                m_DefaultWeightNode = ctx.Set.Create<KernelPassThroughNodeFloat>();
                m_WeightChannelsNode = ctx.Set.Create<WeightBuilderNode>();
                m_RootWeightNode = ctx.Set.Create<FloatMulNode>();
                m_AddNode = ctx.Set.Create<AddPoseNode>();
                m_WeightNode = ctx.Set.Create<WeightPoseNode>();
                m_InverseNode = ctx.Set.Create<InversePoseNode>();

                ctx.Set.Connect(m_RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, m_WeightChannelsNode, WeightBuilderNode.SimulationPorts.Rig);
                ctx.Set.Connect(m_RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, m_AddNode, AddPoseNode.SimulationPorts.Rig);
                ctx.Set.Connect(m_RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, m_InverseNode, InversePoseNode.SimulationPorts.Rig);
                ctx.Set.Connect(m_RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, m_WeightNode, WeightPoseNode.SimulationPorts.Rig);

                ctx.ForwardInput(KernelPorts.NormalizedTime, m_DefaultWeightNode, KernelPassThroughNodeFloat.KernelPorts.Input);
                ctx.Set.Connect(m_DefaultWeightNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_WeightChannelsNode, WeightBuilderNode.KernelPorts.DefaultWeight);

                ctx.Set.Connect(m_DefaultWeightNode, KernelPassThroughNodeFloat.KernelPorts.Output, m_RootWeightNode, FloatMulNode.KernelPorts.InputA);
                ctx.ForwardInput(KernelPorts.RootWeightMultiplier, m_RootWeightNode, FloatMulNode.KernelPorts.InputB);

                ctx.Set.SetPortArraySize(m_WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelIndices, 3);
                ctx.Set.SetPortArraySize(m_WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelWeights, 3);
                for (var i = 0; i < 3; ++i)
                {
                    ctx.Set.Connect(m_RootWeightNode, FloatMulNode.KernelPorts.Output, m_WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelWeights, i);
                }

                ctx.ForwardInput(KernelPorts.Delta, m_WeightNode, WeightPoseNode.KernelPorts.Input);
                ctx.Set.Connect(m_WeightChannelsNode, WeightBuilderNode.KernelPorts.Output, m_WeightNode, WeightPoseNode.KernelPorts.WeightMasks);

                ctx.Set.Connect(m_WeightNode, WeightPoseNode.KernelPorts.Output, m_InverseNode, InversePoseNode.KernelPorts.Input);

                ctx.ForwardInput(KernelPorts.Input, m_AddNode, AddPoseNode.KernelPorts.InputA);
                ctx.Set.Connect(m_InverseNode, InversePoseNode.KernelPorts.Output, m_AddNode, AddPoseNode.KernelPorts.InputB);
                ctx.ForwardOutput(KernelPorts.Output, m_AddNode, AddPoseNode.KernelPorts.Output);

                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_RigNode, SimPassThroughNode<Rig>.SimulationPorts.Input);
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutTranslationBindingIndex , m_WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelIndices, 0);
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRotationBindingIndex , m_WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelIndices, 1);
                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutScaleBindingIndexRig , m_WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelIndices, 2);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(m_RigNode);
                ctx.Set.Destroy(m_DefaultWeightNode);
                ctx.Set.Destroy(m_WeightChannelsNode);
                ctx.Set.Destroy(m_RootWeightNode);
                ctx.Set.Destroy(m_AddNode);
                ctx.Set.Destroy(m_InverseNode);
                ctx.Set.Destroy(m_WeightNode);
            }

            public void HandleMessage(in MessageContext ctx, in Rig rig)
            {
                ctx.EmitMessage(SimulationPorts.m_OutRig, rig);

                ctx.EmitMessage(SimulationPorts.m_OutTranslationBindingIndex,  rig.Value.Value.Bindings.TranslationBindingIndex);
                ctx.EmitMessage(SimulationPorts.m_OutRotationBindingIndex, rig.Value.Value.Bindings.RotationBindingIndex);
                ctx.EmitMessage(SimulationPorts.m_OutScaleBindingIndexRig, rig.Value.Value.Bindings.ScaleBindingIndex);
            }

            public void HandleMessage(in MessageContext ctx, in int msg)
            {
            }
        }

        struct KernelData : IKernelData
        {
        }

        [BurstCompile]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Rig;
    }
}
