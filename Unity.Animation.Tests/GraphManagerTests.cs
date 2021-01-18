using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    internal class GraphManagerTests
    {
        private static BlobAssetReference<Graph> s_Graph;

        World                               World;
        EntityManager                       m_Manager;
        EntityManager.EntityManagerDebug    m_ManagerDebug;

        GraphManagerSystem                  m_GraphManagerSystem;

        [SetUp]
        protected virtual void SetUp()
        {
            World = new World("Test World");

            m_Manager = World.EntityManager;
            m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);

            m_GraphManagerSystem = World.GetOrCreateSystem<GraphManagerSystem>();

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var asset = ref builder.ConstructRoot<Graph>();
            s_Graph = builder.CreateBlobAssetReference<Graph>(Allocator.Persistent);
            s_Graph.Value.m_HashCode = 123456789;
            builder.Dispose();
        }

        [TearDown]
        protected virtual void TearDown()
        {
            s_Graph.Dispose();

            if (World != null)
            {
                // Clean up systems before calling CheckInternalConsistency because we might have filters etc
                // holding on SharedComponentData making checks fail
                var system = World.GetExistingSystem<ComponentSystemBase>();
                while (system != null)
                {
                    World.DestroySystem(system);
                    system = World.GetExistingSystem<ComponentSystemBase>();
                }

                m_ManagerDebug.CheckInternalConsistency();

                World.Dispose();
                World = null;
            }
        }

        [Test]
        public void GraphManagerSystemCreateSingleton()
        {
            Assert.That(m_GraphManagerSystem.HasSingleton<GraphManager>(), Is.True);
        }

        [Test]
        public void GraphManagerSystemInitializeSingleton()
        {
            Assert.That(m_GraphManagerSystem.HasSingleton<GraphManager>(), Is.True);

            var singleton = m_GraphManagerSystem.GetSingleton<GraphManager>();

            Assert.That(singleton.m_Graphs.IsCreated, Is.True);
        }

        [Test]
        public void AddAsset_InvalidID_Throws()
        {
            Assert.That(m_GraphManagerSystem.HasSingleton<GraphManager>(), Is.True);

            var manager = m_GraphManagerSystem.GetSingleton<GraphManager>();

            var register = new GraphRegister(){ID = new GraphID(), Graph = s_Graph};
            Assert.Throws<ArgumentException>(() => manager.AddGraph(register));
        }

        [Test]
        public void AddAsset_Invalid_Throws()
        {
            Assert.That(m_GraphManagerSystem.HasSingleton<GraphManager>(), Is.True);

            var manager = m_GraphManagerSystem.GetSingleton<GraphManager>();

            var register = new GraphRegister(){ID = new GraphID() {Value = new Hash128("4fb16d384de56ba44abed9ffe2fc0370")}, Graph = default};
            Assert.Throws<ArgumentException>(() => manager.AddGraph(register));
        }

        [Test]
        public void AddAsset_Valid_IsRegistered()
        {
            Assert.That(m_GraphManagerSystem.HasSingleton<GraphManager>(), Is.True);

            var manager = m_GraphManagerSystem.GetSingleton<GraphManager>();

            BlobAssetReference<Graph> asset = s_Graph;
            var id = new GraphID() {Value = new Hash128("4fb16d384de56ba44abed9ffe2fc0372")};
            var register = new GraphRegister(){ID = id , Graph = asset};
            manager.AddGraph(register);
            var outAsset = manager.GetGraph(id);
            Assert.IsTrue(outAsset.Graph.IsCreated);
            Assert.IsTrue(manager.TryGetGraph(id, out _));
        }

        [Test]
        public void GetGraph_Unregistered_Throws()
        {
            Assert.That(m_GraphManagerSystem.HasSingleton<GraphManager>(), Is.True);

            var manager = m_GraphManagerSystem.GetSingleton<GraphManager>();

            var id = new GraphID() {Value = new Hash128("4fb16d384de56ba44abed9ffe2fc0371")};
            Assert.Throws<ArgumentException>(() => manager.GetGraph(id));
            Assert.IsFalse(manager.TryGetGraph(id, out _));
        }

        [Test]
        public void GraphManagerCanReallocHashMap()
        {
            Assert.That(m_GraphManagerSystem.HasSingleton<GraphManager>(), Is.True);

            var graphManager = m_GraphManagerSystem.GetSingleton<GraphManager>();

            for (uint i = 0; i < 128; i++)
            {
                // 0,0,0,0 is not a valid Hash128
                var graphID = new GraphID { Value = new Hash128(1, 0, 0, i)};
                GraphRegister graphRegister = new GraphRegister { ID = graphID, Graph = s_Graph };

                graphManager.AddGraph(graphRegister);

                Assert.That(graphManager.m_Graphs.Count(), Is.EqualTo(i + 1), "GraphManager on stack not updated");


                var sameGraphManager = m_GraphManagerSystem.GetSingleton<GraphManager>();
                Assert.That(sameGraphManager.m_Graphs.Count(), Is.EqualTo(i + 1), "GraphManager from ecs component data store not updated");
            }


            graphManager.m_Graphs.Capacity = 256;

            var sameGraphManager2 = m_GraphManagerSystem.GetSingleton<GraphManager>();
            Assert.That(sameGraphManager2.m_Graphs.Count(), Is.EqualTo(graphManager.m_Graphs.Count()), "GraphManager from ecs component data store not updated");
            for (uint i = 0; i < 128; i++)
            {
                var graphID = new GraphID { Value = new Hash128(1, 0, 0, i)};

                Assert.DoesNotThrow(() => { graphManager.TryGetGraph(graphID, out var graph1); });
                Assert.DoesNotThrow(() => { sameGraphManager2.TryGetGraph(graphID, out var graph2); });
            }
        }
    }

    internal class GenericAssetManagerTests
    {
        private static BlobAssetReference<GraphInstanceParameters> s_Asset;

        [SetUp]
        public void SetUp()
        {
            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var asset = ref builder.ConstructRoot<GraphInstanceParameters>();
            s_Asset = builder.CreateBlobAssetReference<GraphInstanceParameters>(Allocator.Persistent);
            s_Asset.Value.m_HashCode = 123456789;
            builder.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            s_Asset.Dispose();
        }

        [Test]
        public void AddAsset_InvalidID_Throws()
        {
            var manager = GenericAssetManager<GraphInstanceParameters, GraphParameterRegister>.Instance;
            var register = new GraphParameterRegister(){ID = new Hash128(), Asset = s_Asset};
            Assert.Throws<ArgumentException>(() => manager.AddAsset(register));
        }

        [Test]
        public void AddAsset_Invalid_Throws()
        {
            var manager = GenericAssetManager<GraphInstanceParameters, GraphParameterRegister>.Instance;
            var register = new GraphParameterRegister(){ID = new Hash128("4fb16d384de56ba44abed9ffe2fc0370"), Asset = default};
            Assert.Throws<ArgumentException>(() => manager.AddAsset(register));
        }

        [Test]
        public void AddAsset_Valid_IsRegistered()
        {
            var manager = GenericAssetManager<GraphInstanceParameters, GraphParameterRegister>.Instance;
            BlobAssetReference<GraphInstanceParameters> asset = s_Asset;
            var id = new Hash128("4fb16d384de56ba44abed9ffe2fc0372");
            var register = new GraphParameterRegister(){ID = id , Asset = asset};
            manager.AddAsset(register);
            var outAsset = manager.GetAsset(id);
            Assert.IsTrue(outAsset.IsCreated);
            Assert.IsTrue(manager.TryGetAsset(id, out _));
        }

        [Test]
        public void GetAsset_Unregistered_Throws()
        {
            var manager = GenericAssetManager<GraphInstanceParameters, GraphParameterRegister>.Instance;
            var id = new Hash128("4fb16d384de56ba44abed9ffe2fc0371");
            Assert.Throws<ArgumentException>(() => manager.GetAsset(id));
            Assert.IsFalse(manager.TryGetAsset(id, out _));
        }
    }
}
