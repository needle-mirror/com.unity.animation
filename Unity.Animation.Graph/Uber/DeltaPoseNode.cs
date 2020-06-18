using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "fd66a28a123c416a9860c6e090650266", version: 1, category: "Animation Core/Utils", description: "Computes the delta animation stream given two input streams")]
    public class DeltaPoseNode
        : NodeDefinition<DeltaPoseNode.Data, DeltaPoseNode.SimPorts, DeltaPoseNode.KernelData, DeltaPoseNode.KernelDefs, DeltaPoseNode.Kernel>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "7e89282e80fa40619ea8e2e3426ab9b7", isHidden: true)]
            public MessageInput<DeltaPoseNode, Rig> Rig;
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

        public struct Data : INodeData
        {
            public NodeHandle<SimPassThroughNode<Rig>> RigNode;
            public NodeHandle<AddPoseNode> AddNode;
            public NodeHandle<InversePoseNode> InverseNode;
        }

        public struct KernelData : IKernelData
        {
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            nodeData.RigNode = Set.Create<SimPassThroughNode<Rig>>();
            nodeData.AddNode = Set.Create<AddPoseNode>();
            nodeData.InverseNode = Set.Create<InversePoseNode>();

            Set.Connect(nodeData.RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, nodeData.AddNode, AddPoseNode.SimulationPorts.Rig);
            Set.Connect(nodeData.RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, nodeData.InverseNode, InversePoseNode.SimulationPorts.Rig);

            Set.Connect(nodeData.InverseNode, InversePoseNode.KernelPorts.Output, nodeData.AddNode, AddPoseNode.KernelPorts.InputA);

            ctx.ForwardInput(KernelPorts.Input, nodeData.AddNode, AddPoseNode.KernelPorts.InputB);
            ctx.ForwardInput(KernelPorts.Subtract, nodeData.InverseNode, InversePoseNode.KernelPorts.Input);
            ctx.ForwardOutput(KernelPorts.Output, nodeData.AddNode, AddPoseNode.KernelPorts.Output);
        }

        protected override void Destroy(DestroyContext ctx)
        {
            var nodeData = GetNodeData(ctx.Handle);

            Set.Destroy(nodeData.RigNode);
            Set.Destroy(nodeData.AddNode);
            Set.Destroy(nodeData.InverseNode);
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);
            Set.SendMessage(nodeData.RigNode, SimPassThroughNode<Rig>.SimulationPorts.Input, rig);
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
