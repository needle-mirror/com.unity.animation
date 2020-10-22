using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "fd66a28a123c416a9860c6e090650266", version: 1, category: "Animation Core/Utils", description: "Computes the delta animation stream given two input streams")]
    public class DeltaPoseNode
        : SimulationKernelNodeDefinition<DeltaPoseNode.SimPorts, DeltaPoseNode.KernelDefs>
        , IRigContextHandler<DeltaPoseNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "7e89282e80fa40619ea8e2e3426ab9b7", isHidden: true)]
            public MessageInput<DeltaPoseNode, Rig> Rig;

            // For internal messages in node data.
            internal MessageOutput<DeltaPoseNode, Rig> m_OutRig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "f6827edbb1414f7aa286e2886447ab77", description: "Input stream")]
            public DataInput<DeltaPoseNode, Buffer<AnimatedData>> Input;

            [PortDefinition(guid: "6cc739a7f24f4b9db9e4b39e399c900f", description: "Stream to subtract")]
            public DataInput<DeltaPoseNode, Buffer<AnimatedData>> Subtract;

            [PortDefinition(guid: "2364845e84e147328da308491cd5b4bd", description: "Resulting delta stream")]
            public DataOutput<DeltaPoseNode, Buffer<AnimatedData>> Output;
        }

        struct Data : INodeData, IInit, IDestroy
            , IMsgHandler<Rig>
        {
            NodeHandle<SimPassThroughNode<Rig>> m_RigNode;
            NodeHandle<AddPoseNode> m_AddNode;
            NodeHandle<InversePoseNode> m_InverseNode;

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<DeltaPoseNode>(ctx.Handle);

                m_RigNode = ctx.Set.Create<SimPassThroughNode<Rig>>();
                m_AddNode = ctx.Set.Create<AddPoseNode>();
                m_InverseNode = ctx.Set.Create<InversePoseNode>();

                ctx.Set.Connect(m_RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, m_AddNode, AddPoseNode.SimulationPorts.Rig);
                ctx.Set.Connect(m_RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, m_InverseNode, InversePoseNode.SimulationPorts.Rig);

                ctx.Set.Connect(m_InverseNode, InversePoseNode.KernelPorts.Output, m_AddNode, AddPoseNode.KernelPorts.InputA);

                ctx.Set.Connect(thisHandle, SimulationPorts.m_OutRig, m_RigNode, SimPassThroughNode<Rig>.SimulationPorts.Input);

                ctx.ForwardInput(KernelPorts.Input, m_AddNode, AddPoseNode.KernelPorts.InputB);
                ctx.ForwardInput(KernelPorts.Subtract, m_InverseNode, InversePoseNode.KernelPorts.Input);
                ctx.ForwardOutput(KernelPorts.Output, m_AddNode, AddPoseNode.KernelPorts.Output);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(m_RigNode);
                ctx.Set.Destroy(m_AddNode);
                ctx.Set.Destroy(m_InverseNode);
            }

            public void HandleMessage(in MessageContext ctx, in Rig rig)
            {
                ctx.EmitMessage(SimulationPorts.m_OutRig, rig);
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
