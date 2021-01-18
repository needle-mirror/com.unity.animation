using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    class UtilityNodeTests
    {
        private struct TestComponentData : IComponentData
        {
            public int Value;
        }

        internal class SimpleMessageNode : SimulationNodeDefinition<SimpleMessageNode.SimPorts>
        {
            public struct SimPorts : ISimulationPortDefinition
            {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
                public MessageInput<SimpleMessageNode, int> Input;
#pragma warning restore 649
            }

            internal struct Data : INodeData, IMsgHandler<int>
            {
                public int value;

                public void HandleMessage(MessageContext ctx, in int msg)
                {
                    Assert.That(msg == 20);
                    value = msg;
                }
            }
        }

        internal class SimpleTemplateNode<T> : SimulationNodeDefinition<SimpleTemplateNode<T>.SimPorts>
        {
            public struct SimPorts : ISimulationPortDefinition
            {
            }

            private struct Data : INodeData
            {
            }
        }

        [Test]
        public void ValidateFunctionality_ComponentDataFieldReaderNode()
        {
            using (World world = new World("Test"))
            {
                EntityManager entityManager = world.EntityManager;
                Entity entity = entityManager.CreateEntity();
                var entityContext = new EntityContext
                {
                    Manager = entityManager,
                    e = entity
                };

                entityManager.AddComponentData(entity, new TestComponentData());

                using (var set = new NodeSet())
                {
                    var readerNode = set.Create<ComponentDataFieldReaderNode<TestComponentData, int>>();
                    set.SendMessage(
                        readerNode,
                        ComponentDataFieldReaderNode<TestComponentData, int>.SimulationPorts.EntityContext,
                        entityContext);
                    set.SendMessage(
                        readerNode,
                        ComponentDataFieldReaderNode<TestComponentData, int>.SimulationPorts.FieldOffset,
                        UnsafeUtility.GetFieldOffset(typeof(TestComponentData).GetField("Value")));

                    set.SendTest<ComponentDataFieldReaderNode<TestComponentData, int>.Data>(readerNode, ctx =>
                    {
                        Assert.AreEqual(0, ctx.CachedValue);
                    });

                    entityManager.SetComponentData(entity, new TestComponentData { Value = 20 });
                    set.Update();
                    //Need to call Update() twice to trigger Update() in the node
                    set.Update();

                    set.SendTest<ComponentDataFieldReaderNode<TestComponentData, int>.Data>(readerNode, ctx =>
                    {
                        Assert.AreEqual(20, ctx.CachedValue);
                    });

                    set.Destroy(readerNode);
                }

                entityManager.DestroyEntity(entity);
            }
        }

        public class TestValuesNode :
            SimulationNodeDefinition<TestValuesNode.MyPorts>
        {
            public enum Values
            {
                Invalid,
                Value1,
                Value2,
            }


            internal struct MyData : INodeData,
                                     IMsgHandler<TestValuesNode.Values>
            {
                public Values Value;
                public int Count;

                public void HandleMessage(MessageContext ctx, in Values value)
                {
                    Value = value;
                    Count++;
                }
            }

            public struct MyPorts : ISimulationPortDefinition
            {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
                public MessageInput<TestValuesNode, Values> Input;
#pragma warning restore 649
            }
        }

        [Test]
        public void EnumConverter_ConvertsIntToEnumValue()
        {
            using (var set = new NodeSet())
            {
                var converterNode = set.Create<EnumConverter<TestValuesNode.Values>>();
                var testValuesNode = set.Create<TestValuesNode>();

                set.Connect(
                    converterNode,
                    EnumConverter<TestValuesNode.Values>.SimulationPorts.EnumValue,
                    testValuesNode,
                    TestValuesNode.SimulationPorts.Input);

                set.SendMessage(converterNode, EnumConverter<TestValuesNode.Values>.SimulationPorts.IntValue, 2);

                var def = set.GetDefinition(testValuesNode);

                set.SendTest<TestValuesNode.MyData>(testValuesNode, ctx =>
                {
                    Assert.That(ctx.NodeData.Count, Is.EqualTo(1));
                });

                set.SendTest<TestValuesNode.MyData>(testValuesNode, ctx =>
                {
                    Assert.That(ctx.NodeData.Value, Is.EqualTo(TestValuesNode.Values.Value2));
                });

                set.SendMessage(converterNode, EnumConverter<TestValuesNode.Values>.SimulationPorts.IntValue, (int)TestValuesNode.Values.Value1);

                set.SendTest<TestValuesNode.MyData>(testValuesNode, ctx =>
                {
                    Assert.That(ctx.NodeData.Count, Is.EqualTo(2));
                });

                set.SendTest<TestValuesNode.MyData>(testValuesNode, ctx =>
                {
                    Assert.That(ctx.NodeData.Value, Is.EqualTo(TestValuesNode.Values.Value1));
                });

                set.Destroy(converterNode);
                set.Destroy(testValuesNode);
            }
        }
    }
}
