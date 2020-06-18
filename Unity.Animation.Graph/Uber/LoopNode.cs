using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "e995e3e7e5d640e5b6123dfcef20eba7", version: 1, isHidden: true)]
    public class LoopNode
        : NodeDefinition<LoopNode.Data, LoopNode.SimPorts, LoopNode.KernelData, LoopNode.KernelDefs, LoopNode.Kernel>
        , IMsgHandler<int>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<LoopNode, Rig> Rig;
            public MessageInput<LoopNode, int> SkipRoot;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<LoopNode, float> NormalizedTime;
            public DataInput<LoopNode, float> RootWeightMultiplier;
            public DataInput<LoopNode, Buffer<AnimatedData>> Delta;

            public DataInput<LoopNode, Buffer<AnimatedData>> Input;
            public DataOutput<LoopNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
            public NodeHandle<SimPassThroughNode<Rig>> RigNode;

            public NodeHandle<KernelPassThroughNodeFloat> DefaultWeightNode;
            public NodeHandle<WeightBuilderNode> WeightChannelsNode;
            public NodeHandle<FloatMulNode> RootWeightNode;

            public NodeHandle<AddPoseNode>     AddNode;
            public NodeHandle<InversePoseNode> InverseNode;
            public NodeHandle<WeightPoseNode>  WeightNode;
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
            nodeData.DefaultWeightNode = Set.Create<KernelPassThroughNodeFloat>();
            nodeData.WeightChannelsNode = Set.Create<WeightBuilderNode>();
            nodeData.RootWeightNode = Set.Create<FloatMulNode>();
            nodeData.AddNode = Set.Create<AddPoseNode>();
            nodeData.WeightNode = Set.Create<WeightPoseNode>();
            nodeData.InverseNode = Set.Create<InversePoseNode>();

            Set.Connect(nodeData.RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, nodeData.WeightChannelsNode, WeightBuilderNode.SimulationPorts.Rig);
            Set.Connect(nodeData.RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, nodeData.AddNode, AddPoseNode.SimulationPorts.Rig);
            Set.Connect(nodeData.RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, nodeData.InverseNode, InversePoseNode.SimulationPorts.Rig);
            Set.Connect(nodeData.RigNode, SimPassThroughNode<Rig>.SimulationPorts.Output, nodeData.WeightNode, WeightPoseNode.SimulationPorts.Rig);

            ctx.ForwardInput(KernelPorts.NormalizedTime, nodeData.DefaultWeightNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            Set.Connect(nodeData.DefaultWeightNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.WeightChannelsNode, WeightBuilderNode.KernelPorts.DefaultWeight);

            Set.Connect(nodeData.DefaultWeightNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.RootWeightNode, FloatMulNode.KernelPorts.InputA);
            ctx.ForwardInput(KernelPorts.RootWeightMultiplier, nodeData.RootWeightNode, FloatMulNode.KernelPorts.InputB);

            Set.SetPortArraySize(nodeData.WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelIndices, 3);
            Set.SetPortArraySize(nodeData.WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelWeights, 3);
            for (var i = 0; i < 3; ++i)
            {
                Set.Connect(nodeData.RootWeightNode, FloatMulNode.KernelPorts.Output, nodeData.WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelWeights, i);
            }

            ctx.ForwardInput(KernelPorts.Delta, nodeData.WeightNode, WeightPoseNode.KernelPorts.Input);
            Set.Connect(nodeData.WeightChannelsNode, WeightBuilderNode.KernelPorts.Output, nodeData.WeightNode, WeightPoseNode.KernelPorts.WeightMasks);

            Set.Connect(nodeData.WeightNode, WeightPoseNode.KernelPorts.Output, nodeData.InverseNode, InversePoseNode.KernelPorts.Input);

            ctx.ForwardInput(KernelPorts.Input, nodeData.AddNode, AddPoseNode.KernelPorts.InputA);
            Set.Connect(nodeData.InverseNode, InversePoseNode.KernelPorts.Output, nodeData.AddNode, AddPoseNode.KernelPorts.InputB);
            ctx.ForwardOutput(KernelPorts.Output, nodeData.AddNode, AddPoseNode.KernelPorts.Output);
        }

        protected override void Destroy(DestroyContext ctx)
        {
            var nodeData = GetNodeData(ctx.Handle);

            Set.Destroy(nodeData.RigNode);
            Set.Destroy(nodeData.DefaultWeightNode);
            Set.Destroy(nodeData.WeightChannelsNode);
            Set.Destroy(nodeData.RootWeightNode);
            Set.Destroy(nodeData.AddNode);
            Set.Destroy(nodeData.InverseNode);
            Set.Destroy(nodeData.WeightNode);
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            Set.SendMessage(nodeData.RigNode, SimPassThroughNode<Rig>.SimulationPorts.Input, rig);

            Set.SetData(nodeData.WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelIndices, 0, rig.Value.Value.Bindings.TranslationBindingIndex);
            Set.SetData(nodeData.WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelIndices, 1, rig.Value.Value.Bindings.RotationBindingIndex);
            Set.SetData(nodeData.WeightChannelsNode, WeightBuilderNode.KernelPorts.ChannelIndices, 2, rig.Value.Value.Bindings.ScaleBindingIndex);
        }

        public void HandleMessage(in MessageContext ctx, in int msg)
        {
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
