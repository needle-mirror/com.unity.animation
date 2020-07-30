using NUnit.Framework;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    public class ManagedGraphTests : AnimationTestsFixture
    {
        [DisableAutoCreation]
        internal class TestAnimationSystem : AnimationSystemBase<
            TestAnimationSystem.Tag,
            NotSupportedTransformHandle,
            NotSupportedTransformHandle
        >
        {
            internal struct Tag : IAnimationSystemTag {}
        }

        [Test]
        public void InvalidGraphHandlesThrowExceptions()
        {
            var defaultGraphHandle = default(GraphHandle);
            Assert.Throws<System.ArgumentException>(() => m_PreAnimationGraph.CreateNode<TimeCounterNode>(defaultGraphHandle));
            Assert.Throws<System.ArgumentException>(() => m_PreAnimationGraph.CreateNode(defaultGraphHandle, Entity.Null));
            Assert.Throws<System.ArgumentException>(() => m_PreAnimationGraph.Dispose(defaultGraphHandle));

            // Using a graph handle created from another system should also throw an exception
            var wrongGraphHandle = m_PostAnimationGraph.CreateGraph();
            Assert.Throws<System.ArgumentException>(() => m_PreAnimationGraph.CreateNode<TimeCounterNode>(wrongGraphHandle));
            Assert.Throws<System.ArgumentException>(() => m_PreAnimationGraph.CreateNode(wrongGraphHandle, Entity.Null));
            Assert.Throws<System.ArgumentException>(() => m_PreAnimationGraph.Dispose(wrongGraphHandle));
        }

        [Test]
        public void ManagedNodesAreDisposedAfterExplicitGraphDispose()
        {
            var tmpSystem = World.GetOrCreateSystem<TestAnimationSystem>();
            tmpSystem.AddRef();

            var entity0 = m_Manager.CreateEntity();
            var entity1 = m_Manager.CreateEntity();
            var graph0 = tmpSystem.CreateGraph();
            var graph1 = tmpSystem.CreateGraph();

            var node0 = tmpSystem.CreateNode<TimeCounterNode>(graph0);
            var node1 = tmpSystem.CreateNode(graph0, entity0);
            var node2 = tmpSystem.CreateNode<TimeCounterNode>(graph1);
            var node3 = tmpSystem.CreateNode(graph1, entity1);

            Assert.DoesNotThrow(() => tmpSystem.Dispose(graph0));
            Assert.IsFalse(tmpSystem.Set.Exists(node0));
            Assert.IsFalse(tmpSystem.Set.Exists(node1));
            Assert.IsTrue(tmpSystem.Set.Exists(node2));
            Assert.IsTrue(tmpSystem.Set.Exists(node3));

            Assert.DoesNotThrow(() => tmpSystem.Dispose(graph1));
            Assert.IsFalse(tmpSystem.Set.Exists(node2));
            Assert.IsFalse(tmpSystem.Set.Exists(node3));

            tmpSystem.RemoveRef();
        }

        [Test]
        public void ManagedNodesAreDisposedOnSystemCleanUp()
        {
            var tmpSystem = World.GetOrCreateSystem<TestAnimationSystem>();
            tmpSystem.AddRef();

            var entity0 = m_Manager.CreateEntity();
            var entity1 = m_Manager.CreateEntity();
            var graph0 = tmpSystem.CreateGraph();
            var graph1 = tmpSystem.CreateGraph();

            // Intentionally not keeping a local handle to these nodes
            tmpSystem.CreateNode<TimeCounterNode>(graph0);
            tmpSystem.CreateNode(graph0, entity0);
            tmpSystem.CreateNode<TimeCounterNode>(graph1);
            tmpSystem.CreateNode(graph1, entity1);

            Assert.DoesNotThrow(() => tmpSystem.RemoveRef());
            Assert.DoesNotThrow(() => World.DestroySystem(tmpSystem));
        }
    }
}
