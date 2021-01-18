using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation.Editor.Tests
{
    [NodeDefinition(guid: "96e673777f6e4205ae5f6644b6636c87", version: 1, isHidden: true)]
    public class HiddenNode : SimulationNodeDefinition<HiddenNode.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }

    [NodeDefinition(guid: "3c456a0a6b854f03add1d49df7d6894c", version: 1, "Test/Category", "Test description")]
    public class CategorizedNode : SimulationNodeDefinition<CategorizedNode.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }

    [NodeDefinition(guid: "219e1e3401b7499986e45e24f53d3e5d", version: 1, "Test / Category", "Test description")]
    public class DuplicateCategorizedNode : SimulationNodeDefinition<DuplicateCategorizedNode.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }

    [PortGroupDefinition("Number of Inputs", 1, 2, 10)]
    [NodeDefinition(guid: "041bf00c575b46c49e5e29673bfbc801", version: 1)]
    public class ResizablePortNode : SimulationKernelNodeDefinition<ResizablePortNode.MySimPorts, ResizablePortNode.MyKernelPorts>
    {
        private struct MyData : INodeData {}
        public struct MyKernelData : IKernelData {}
        public struct MySimPorts : ISimulationPortDefinition {}
        public struct MyKernelPorts : IKernelPortDefinition
        {
            [PortDefinition(guid: "0e0027b273a841f193e7a4f8e920ff1b", portGroupIndex: 1)] public PortArray<DataInput<ResizablePortNode, Buffer<float>>> Inputs;
        }
        public struct MyKernel : IGraphKernel<MyKernelData, MyKernelPorts>
        {
            public void Execute(RenderContext ctx, in MyKernelData data, ref MyKernelPorts ports) {}
        }
    }

    [NodeDefinition(guid: "1e29f23c471346c084c9f63ba169b0e7", version: 1)]
    public class HiddenPortNode : SimulationNodeDefinition<HiddenPortNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "a0ce1ae20e434648bda0892bb3dd86ec", isHidden: true)] public MessageInput<HiddenPortNode, float> Input;
            public MessageInput<HiddenPortNode, float> VisiblePort;
        }
    }

    [NodeDefinition(guid: "17c5dc12c0114a09985c3bb0a3bcdf5d", version: 1)]
    public class TooltipPortNode : SimulationNodeDefinition<TooltipPortNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "8c8d5c2cc3e24034881222f7cc746655", description: "Tooltip test")] public MessageInput<TooltipPortNode, float> Input;
        }
    }

    [NodeDefinition(guid: "c76f4858b7cb4fe7a9c57ad904d803e7", version: 1)]
    public class DefaultValuePortNode : SimulationNodeDefinition<DefaultValuePortNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "b4f8848bccd64c19bd4a803fac207153", defaultValue: 10.0F)] public MessageInput<DefaultValuePortNode, float> Input;
        }
    }

    [NodeDefinition(guid: "e050906ddd8f492092505b2370b38a63", version: 1)]
    public class StaticPortNode : SimulationNodeDefinition<StaticPortNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "c45d42c450fc4327ae63ff6785e4c95b", isStatic: true)] public MessageInput<StaticPortNode, float> Input;
        }
    }

    [NodeDefinition(guid: "0ce61a20408f4431b40d26db4ffa73dc", version: 1)]
    public class NamedPortNode : SimulationNodeDefinition<NamedPortNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "400e4f4b967f4abfb42d816102f940cf", displayName: "Test Port")] public MessageInput<NamedPortNode, float> Input;
        }
    }

    [NodeDefinition(guid: "42e4288f8a644ecf89583502e8b62356", version: 1)]
    public class MotionIDNode : SimulationNodeDefinition<MotionIDNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<MotionID>
        {
            public void HandleMessage(MessageContext ctx, in MotionID msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "e0c67adf3c974279aa2cb209095e779d", displayName: "Test Port")] public MessageInput<MotionIDNode, MotionID> Input;
        }
    }
}
