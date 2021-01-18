using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "0fa3e1906f2d490493fb54df73c64fe9", version: 1, "Animation High-Level")]
    public class SimpleBlendTree2DNode :
        SimulationKernelNodeDefinition<SimpleBlendTree2DNode.MessagePorts, SimpleBlendTree2DNode.DataPorts>,
        IRigContextHandler<SimpleBlendTree2DNode.NodeData>
    {
        public struct MessagePorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "07af14d08653483099fa544656fb5393", isHidden: true)] public MessageInput<SimpleBlendTree2DNode, Rig> Context;
            [PortDefinition(guid: "f5ba27381f2c4b2b9d0fb1a51bdfe456", "2D Simple Directional Blend Tree")] public MessageInput<SimpleBlendTree2DNode, BlobAssetReference<BlendTree2DSimpleDirectional>> BlendTree;
            [PortDefinition(guid: "a95878ef73c245c4854bd25a2320ea85", "Parameter Value X")] public MessageInput<SimpleBlendTree2DNode, float> ValueX;
            [PortDefinition(guid: "74a2f682047d4b8282e068a16831833e", "Parameter Value Y")] public MessageInput<SimpleBlendTree2DNode, float> ValueY;

#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            internal MessageOutput<SimpleBlendTree2DNode, float> OutInternalTimeValue;
            internal MessageOutput<SimpleBlendTree2DNode, float> OutInternalDurationValue;
            internal MessageOutput<SimpleBlendTree2DNode, float> OutInternalBlendXValue;
            internal MessageOutput<SimpleBlendTree2DNode, float> OutInternalBlendYValue;
#pragma warning restore 649
        }

        public struct DataPorts : IKernelPortDefinition
        {
            [PortDefinition(guid: "d9d53f83c0a24e4c977854e400425a1c")]
            public DataOutput<SimpleBlendTree2DNode, Buffer<AnimatedData>> Output;
        }

        private struct NodeData : INodeData, IInit, IDestroy,
                                  IMsgHandler<Rig>,
                                  IMsgHandler<BlobAssetReference<BlendTree2DSimpleDirectional>>,
                                  IMsgHandler<float>
        {
            public NodeHandle<Unity.Animation.BlendTree2DNode> BlendTree2DNode;
#pragma warning disable 0618 // TODO : Remove usage of the Deltatime node in our samples
            public NodeHandle<DeltaTimeNode> DeltaTimeNode;
#pragma warning restore 0618
            public NodeHandle<TimeCounterNode> TimeCounterNode;
            public NodeHandle<TimeLoopNode> TimeLoopNode;
            public NodeHandle<FloatRcpNode> FloatRcpNode;

            public void Init(InitContext ctx)
            {
                var thisHandle = ctx.Set.CastHandle<SimpleBlendTree2DNode>(ctx.Handle);

                BlendTree2DNode = ctx.Set.Create<Unity.Animation.BlendTree2DNode>();
                FloatRcpNode = ctx.Set.Create<FloatRcpNode>();
                TimeCounterNode = ctx.Set.Create<TimeCounterNode>();
    #pragma warning disable 0618 // TODO : Remove usage of the Deltatime node in our samples
                DeltaTimeNode = ctx.Set.Create<DeltaTimeNode>();
    #pragma warning restore 0618
                TimeLoopNode = ctx.Set.Create<TimeLoopNode>();

                ctx.Set.Connect(BlendTree2DNode, Unity.Animation.BlendTree2DNode.KernelPorts.Duration, FloatRcpNode, Unity.Animation.FloatRcpNode.KernelPorts.Input);
                ctx.Set.Connect(FloatRcpNode, Unity.Animation.FloatRcpNode.KernelPorts.Output, TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.Speed);
    #pragma warning disable 0618 // TODO : Remove usage of the Deltatime node in our samples
                ctx.Set.Connect(DeltaTimeNode, Unity.Animation.DeltaTimeNode.KernelPorts.DeltaTime, TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.DeltaTime);
    #pragma warning restore 0618
                ctx.Set.Connect(TimeCounterNode, Unity.Animation.TimeCounterNode.KernelPorts.Time, TimeLoopNode, Unity.Animation.TimeLoopNode.KernelPorts.InputTime);
                ctx.Set.Connect(TimeLoopNode, Unity.Animation.TimeLoopNode.KernelPorts.NormalizedTime, BlendTree2DNode, Unity.Animation.BlendTree2DNode.KernelPorts.NormalizedTime);

                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalTimeValue, TimeCounterNode, Unity.Animation.TimeCounterNode.SimulationPorts.Time);
                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalDurationValue, TimeLoopNode, Unity.Animation.TimeLoopNode.SimulationPorts.Duration);

                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalBlendXValue, BlendTree2DNode, Unity.Animation.BlendTree2DNode.KernelPorts.BlendParameterX);
                ctx.Set.Connect(thisHandle, SimulationPorts.OutInternalBlendYValue, BlendTree2DNode, Unity.Animation.BlendTree2DNode.KernelPorts.BlendParameterY);

                ctx.ForwardInput(SimulationPorts.Context, BlendTree2DNode, Unity.Animation.BlendTree2DNode.SimulationPorts.Rig);
                ctx.ForwardInput(SimulationPorts.BlendTree, BlendTree2DNode, Unity.Animation.BlendTree2DNode.SimulationPorts.BlendTree);
                ctx.ForwardOutput(KernelPorts.Output, BlendTree2DNode, Unity.Animation.BlendTree2DNode.KernelPorts.Output);

                ctx.EmitMessage(SimulationPorts.OutInternalTimeValue, 0F);
                ctx.EmitMessage(SimulationPorts.OutInternalDurationValue, 1F);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(BlendTree2DNode);
                ctx.Set.Destroy(FloatRcpNode);
                ctx.Set.Destroy(TimeCounterNode);
                ctx.Set.Destroy(DeltaTimeNode);
                ctx.Set.Destroy(TimeLoopNode);
            }

            public void HandleMessage(MessageContext ctx, in Rig msg)
            {
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<BlendTree2DSimpleDirectional> msg)
            {
            }

            public void HandleMessage(MessageContext ctx, in float msg)
            {
                if (ctx.Port == SimulationPorts.ValueX)
                    ctx.EmitMessage(SimulationPorts.OutInternalBlendXValue, msg);
                else if (ctx.Port == SimulationPorts.ValueY)
                    ctx.EmitMessage(SimulationPorts.OutInternalBlendYValue, msg);
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
