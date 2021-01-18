using NUnit.Framework;
using Unity.Entities;
using Unity.DataFlowGraph;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;

namespace Unity.Animation.Hybrid.Tests
{
    public class ConversionServiceTests
    {
        public class DummyGraphNodeDefinition : SimulationNodeDefinition<DummyGraphNodeDefinition.MessagePorts>,
            IComponentNodeHandler<DummyGraphNodeDefinition.NodeData>,
            IGraphHandler<DummyGraphNodeDefinition.NodeData>,
            IEntityManagerHandler<DummyGraphNodeDefinition.NodeData>,
            IInputReferenceHandler<DummyGraphNodeDefinition.NodeData>,
            IGraphInstanceHandler<DummyGraphNodeDefinition.NodeData>
        {
            public struct MessagePorts : ISimulationPortDefinition
            {
            }

            private struct NodeData : INodeData,
                                         IRootGraphSimulationData,
                                         IMsgHandler<NodeHandle<ComponentNode>>,
                                         IMsgHandler<BlobAssetReference<GraphInstanceParameters>>,
                                         IMsgHandler<BlobAssetReference<Graph>>
            {
                public NodeHandle<ComponentNode> ComponentNode { get; set; }
                public BlobAssetReference<Graph> CachedGraphReference { get; set; }
                public Dictionary<NodeID, NodeHandle> Nodes { get; set; }
                public void AddNode(NodeSetAPI set, CreateNodeCommand create)
                {
                    throw new System.NotImplementedException();
                }

                public NodeHandle GetNodeByID(NodeID id)
                {
                    throw new System.NotImplementedException();
                }

                public PortDescription.InputPort GetInputPort(NodeSetAPI set, NodeID Node, PortID Port)
                {
                    throw new System.NotImplementedException();
                }

                public PortDescription.OutputPort GetOutputPort(NodeSetAPI set, NodeID Node, PortID Port)
                {
                    throw new System.NotImplementedException();
                }

                public void HandleMessage(MessageContext ctx, in BlobAssetReference<Graph> msg)
                {
                    throw new System.NotImplementedException();
                }

                public void HandleMessage(MessageContext ctx, in EntityManager msg)
                {
                    throw new System.NotImplementedException();
                }

                public void HandleMessage(MessageContext ctx, in BlobAssetReference<GraphInstanceParameters> msg)
                {
                    throw new System.NotImplementedException();
                }

                public void HandleMessage(MessageContext ctx, in NodeHandle<ComponentNode> msg)
                {
                    throw new System.NotImplementedException();
                }

                public void HandleMessage(MessageContext ctx, in NativeArray<InputReference> msg)
                {
                    throw new System.NotImplementedException();
                }
            }

            public InputPortID GetEntityManagerPort(NodeHandle handle) => throw new System.NotImplementedException();
            protected InputPortID GetGraphPort(NodeHandle handle) => throw new System.NotImplementedException();
            protected InputPortID GetComponentNodePort(NodeHandle handle) => throw new System.NotImplementedException();
            protected InputPortID GetGraphInstancePort(NodeHandle handle) => throw new System.NotImplementedException();

            public InputPortID GetPort(NodeHandle handle)
            {
                throw new System.NotImplementedException();
            }
        }

        public class DummyGraphLoaderSystem1 :
            BaseGraphLoaderSystem<DummyGraphNodeDefinition, DummyGraphLoaderSystem1.Tag, DummyGraphLoaderSystem1.AllocatedTag>
        {
            public override NodeSet Set => throw new System.NotImplementedException();

            [Phase(description: "B")]
            public struct Tag : IComponentData {}
            public struct AllocatedTag : ISystemStateComponentData {}
        }

        public class DummyGraphLoaderSystem2 :
            BaseGraphLoaderSystem<DummyGraphNodeDefinition, DummyGraphLoaderSystem2.Tag, DummyGraphLoaderSystem2.AllocatedTag>
        {
            public override NodeSet Set => throw new System.NotImplementedException();

            [Phase(description: "A")]
            public struct Tag : IComponentData {}
            public struct AllocatedTag : ISystemStateComponentData {}
        }

        [Phase(description: "Invalid Phase")]
        public struct InvalidComponentDataTag : IComponentData {}

        [Phase(description: "Invalid Phase")]
        public struct InvalidTag {}

        [Test]
        public void DetectAvailablePhases()
        {
            ConversionService.CachedAssemblies = new[] { Assembly.Load("Unity.Animation.Hybrid.Tests") };
            var unsortedPhases = ConversionService.GetPhases(false);
            Assert.AreEqual(2, unsortedPhases.Count);
            Assert.AreEqual("B", unsortedPhases[0].Description);
            var sortedPhases = ConversionService.GetPhases(true);
            Assert.AreEqual("A", sortedPhases[0].Description);
            ConversionService.CachedAssemblies = null;
        }
    }
}
