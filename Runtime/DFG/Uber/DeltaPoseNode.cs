using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Utils", description:"Computes the delta animation stream given two input streams")]
    public class DeltaPoseNode
        : NodeDefinition<DeltaPoseNode.Data, DeltaPoseNode.SimPorts, DeltaPoseNode.KernelData, DeltaPoseNode.KernelDefs, DeltaPoseNode.Kernel>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<DeltaPoseNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Input stream")]
            public DataInput<DeltaPoseNode, Buffer<AnimatedData>> Input;

            [PortDefinition(description:"Stream to substract")]
            public DataInput<DeltaPoseNode, Buffer<AnimatedData>> Subtract;

            [PortDefinition(description:"Resulting delta stream")]
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

        protected override void Destroy(NodeHandle handle)
        {
            var nodeData = GetNodeData(handle);

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
