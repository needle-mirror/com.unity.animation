using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "b4535bca721444e69e7ef91c5c2cf640", version: 1, "Animation High-Level")]
    public class SimpleBlendTree1DNode :
        SimulationKernelNodeDefinition<SimpleBlendTree1DNode.MessagePorts, SimpleBlendTree1DNode.DataPorts>,
        IRigContextHandler<SimpleBlendTree1DNode.NodeData>
    {
        public struct MessagePorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "cee8ee0efe0f4630bb5402d4ee5465cf", isHidden: true)] public MessageInput<SimpleBlendTree1DNode, Rig> Context;
            [PortDefinition(guid: "c64cb9cd15f6486b88a764671399aa13", "1D Blend Tree")] public MessageInput<SimpleBlendTree1DNode, BlobAssetReference<BlendTree1D>> BlendTree;
            [PortDefinition(guid: "11a2e6d9ea73448ea41f0e79d7d8fb74", "Parameter Value")] public MessageInput<SimpleBlendTree1DNode, float> Value;

#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            internal MessageOutput<SimpleBlendTree1DNode, float> OutInternalTimeValue;
            internal MessageOutput<SimpleBlendTree1DNode, float> OutInternalDurationValue;
            internal MessageOutput<SimpleBlendTree1DNode, float> OutInternalBlendValue;
#pragma warning restore 649
        }

        public struct DataPorts : IKernelPortDefinition
        {
            [PortDefinition(guid: "35c4053760a942a08aac29e384ec4db4")]
            public DataOutput<SimpleBlendTree1DNode, Buffer<AnimatedData>> Output;
        }

        private struct NodeData : INodeData, IInit, IDestroy,
                                  IMsgHandler<Rig>,
                                  IMsgHandler<BlobAssetReference<BlendTree1D>>,
                                  IMsgHandler<float>
        {
            public NodeHandle<Unity.Animation.BlendTree1DNode> BlendTree1DNode;
#pragma warning disable 0618 // TODO : Remove usage of the Deltatime node in our samples
            public NodeHandle<DeltaTimeNode> DeltaTimeNode;
#pragma warning restore 0618
            public NodeHandle<TimeCounterNode> TimeCounterNode;
            public NodeHandle<TimeLoopNode> TimeLoopNode;
            public NodeHandle<FloatRcpNode> FloatRcpNode;

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<SimpleBlendTree1DNode>(ctx.Handle);

                BlendTree1DNode = ctx.Set.Create<Unity.Animation.BlendTree1DNode>();
                FloatRcpNode = ctx.Set.Create<FloatRcpNode>();
                TimeCounterNode = ctx.Set.Create<TimeCounterNode>();
    #pragma warning disable 0618 // TODO : Remove usage of the Deltatime node in our samples
                DeltaTimeNode = ctx.Set.Create<DeltaTimeNode>();
    #pragma warning restore 0618
                TimeLoopNode = ctx.Set.Create<TimeLoopNode>();

                ctx.Set.Connect(BlendTree1DNode, Unity.Animation.BlendTree1DNode.KernelPorts.Duration, FloatRcpNode, Unity.Animation.FloatRcpNode.KernelPorts.Input);
                ctx.Set.Connect(FloatRcpNode, Unity.Animation.FloatRcpNode.KernelPorts.Output, TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.Speed);
    #pragma warning disable 0618 // TODO : Remove usage of the Deltatime node in our samples
                ctx.Set.Connect(DeltaTimeNode, Unity.Animation.DeltaTimeNode.KernelPorts.DeltaTime, TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.DeltaTime);
    #pragma warning restore 0618
                ctx.Set.Connect(TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.Time, TimeLoopNode, Unity.Animation.TimeLoopNode.KernelPorts.InputTime);
                ctx.Set.Connect(TimeLoopNode, Unity.Animation.TimeLoopNode.KernelPorts.NormalizedTime, BlendTree1DNode, Unity.Animation.BlendTree1DNode.KernelPorts.NormalizedTime);

                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalTimeValue, TimeCounterNode, Unity.Animation.TimeCounterNode.SimulationPorts.Time);
                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalDurationValue, TimeLoopNode, Unity.Animation.TimeLoopNode.SimulationPorts.Duration);

                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalBlendValue, BlendTree1DNode, Unity.Animation.BlendTree1DNode.KernelPorts.BlendParameter);

                ctx.ForwardInput(SimulationPorts.Context, BlendTree1DNode, Unity.Animation.BlendTree1DNode.SimulationPorts.Rig);
                ctx.ForwardInput(SimulationPorts.BlendTree, BlendTree1DNode, Unity.Animation.BlendTree1DNode.SimulationPorts.BlendTree);
                ctx.ForwardOutput(KernelPorts.Output, BlendTree1DNode, Unity.Animation.BlendTree1DNode.KernelPorts.Output);

                ctx.EmitMessage(SimulationPorts.OutInternalTimeValue, 0F);
                ctx.EmitMessage(SimulationPorts.OutInternalDurationValue, 1F);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(BlendTree1DNode);
                ctx.Set.Destroy(FloatRcpNode);
                ctx.Set.Destroy(TimeCounterNode);
                ctx.Set.Destroy(DeltaTimeNode);
                ctx.Set.Destroy(TimeLoopNode);
            }

            public void HandleMessage(MessageContext ctx, in Rig msg)
            {
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<BlendTree1D> msg)
            {
            }

            public void HandleMessage(MessageContext ctx, in float msg)
            {
                ctx.EmitMessage(SimulationPorts.OutInternalBlendValue, msg);
            }
        }

        public struct KernelData : IKernelData
        {
        }

        public struct Kernel : IGraphKernel<KernelData, DataPorts>
        {
            public void Execute(RenderContext ctx, in KernelData data, ref DataPorts ports)
            {
            }
        }

        public InputPortID GetPort(NodeHandle handle)
        {
            return (InputPortID)SimulationPorts.Context;
        }
    }
}
