using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation.Editor.Tests
{
    // Not picked up by DFGService
    [NodeDefinition(guid: "0b143c9c21824e1da02676dcd11b99f1", version: 1)]
    public class GenericNode<T> : SimulationNodeDefinition<GenericNode<T>.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }

    // Not picked up by DFGService
    [NodeDefinition(guid: "bb6f18965b224aa2a262c51b2ffebc39", version: 1)]
    class PrivateNode : SimulationNodeDefinition<PrivateNode.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }

    // Not picked up by DFGService
    [NodeDefinition(guid: "b2df401d7509467eaf8a720b04d6b442", version: 1)]
    internal class InternalNode : SimulationNodeDefinition<InternalNode.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }

    // Not picked up by DFGService
    [NodeDefinition(guid: "57c530b922444a29939ccbd9a9d6ab48", version: 1)]
    public abstract class AbstractNode : SimulationNodeDefinition<AbstractNode.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }

    // Not picked up by DFGService
    [NodeDefinition(guid: "jsadfoiuhslnbajsdf", version: 1)]
    public class InvalidGUIDNode : SimulationNodeDefinition<InvalidGUIDNode.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }

    // Not picked up by DFGService
    [NodeDefinition(guid: "24832f04d5d94fe0ab63221fc6a51993", version: 1)]
    public class DuplicateGUIDDummyNode : SimulationNodeDefinition<DuplicateGUIDDummyNode.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }
}
