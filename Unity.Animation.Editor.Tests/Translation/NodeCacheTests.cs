using NUnit.Framework;
using System;
using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation.Editor.Tests
{
    public class NodeCacheTests
    {
        [Test]
        public void RegisterNode_SimpleNode_NodeValid()
        {
            NodeCache nodeCache = new NodeCache();
            var path = new GraphPath();
            var nodeDef = new IRNodeDefinition("TestNodeDef", "bool");
            var id = nodeCache.RegisterNode(nodeDef, path);
            Assert.That(id.IsValid());
            Assert.AreEqual(nodeDef, nodeCache.GetNode(id));
        }

        [Test]
        public void GetNodePath_MultipleNodes_ReturnValidPaths()
        {
            NodeCache nodeCache = new NodeCache();
            var path = new GraphPath();
            var testNodeDef = new IRNodeDefinition("TestNodeDef", "bool");
            var testNodeDef2 = new IRNodeDefinition("TestNodeDef2", "bool");
            var testNodeDef3 = new IRNodeDefinition("TestNodeDef3", "bool");
            var id0 = nodeCache.RegisterNode(testNodeDef, path);
            var id1 = nodeCache.RegisterNode(testNodeDef2, path);
            path.Push("SubNode");
            var id2 = nodeCache.RegisterNode(testNodeDef, path);
            var id3 = nodeCache.RegisterNode(testNodeDef2, path);
            path.Pop();
            var id4 = nodeCache.RegisterNode(testNodeDef3, path);
            Assert.AreEqual("/TestNodeDef", nodeCache.GetNodePath(id0).ToString());
            Assert.AreEqual("/TestNodeDef2", nodeCache.GetNodePath(id1).ToString());
            Assert.AreEqual("SubNode/TestNodeDef", nodeCache.GetNodePath(id2).ToString());
            Assert.AreEqual("SubNode/TestNodeDef2", nodeCache.GetNodePath(id3).ToString());
            Assert.AreEqual("/TestNodeDef3", nodeCache.GetNodePath(id4).ToString());
        }

        [Test]
        public void RegisterNode_DoubleRegister_ThrowException()
        {
            NodeCache nodeCache = new NodeCache();
            var path = new GraphPath();
            var nodeDef = new IRNodeDefinition("TestNodeDef", "bool");
            var id = nodeCache.RegisterNode(nodeDef, path);
            Assert.Throws<InvalidOperationException>(() => { nodeCache.RegisterNode(nodeDef, path); });
        }

        [Test]
        public void GetNodeID_UnregisteredNode_ReturnInvalidID()
        {
            NodeCache nodeCache = new NodeCache();
            var path = new GraphPath();
            path.Push("Bla");
            var id = nodeCache.GetNodeID(path, "TestNode");
            Assert.False(id.IsValid());
        }
    }
    public class NodeIDGeneratorTests
    {
        [Test]
        public void CreateUniqueID_MultipleCalls_NotEqualIDs()
        {
            NodeIDGenerator generator = new NodeIDGenerator();
            var id1 = generator.CreateUniqueID();
            var id2 = generator.CreateUniqueID();
            var id3 = generator.CreateUniqueID();
            Assert.AreNotEqual(id1, id2);
            Assert.AreNotEqual(id1, id3);
            Assert.AreNotEqual(id2, id3);
        }

        [Test]
        public void CreateUniqueID_MultipleCalls_ValidIDs()
        {
            NodeIDGenerator generator = new NodeIDGenerator();
            var id1 = generator.CreateUniqueID();
            var id2 = generator.CreateUniqueID();
            var id3 = generator.CreateUniqueID();
            Assert.AreNotEqual(id1, NodeID.Invalid);
            Assert.AreNotEqual(id2, NodeID.Invalid);
            Assert.AreNotEqual(id3, NodeID.Invalid);
        }
    }

    public class PortIDTests
    {
        [TestCase((ushort)0,       UInt16.MaxValue, ExpectedResult = true)]
        [TestCase((ushort)1,       UInt16.MaxValue, ExpectedResult = true)]
        [TestCase(UInt16.MaxValue, UInt16.MaxValue, ExpectedResult = false)]
        [TestCase((ushort)0,       (ushort)0,       ExpectedResult = true)]
        [TestCase((ushort)1,       (ushort)0,       ExpectedResult = true)]
        [TestCase(UInt16.MaxValue, (ushort)0,       ExpectedResult = false)]
        [TestCase((ushort)0,       (ushort)1,       ExpectedResult = true)]
        [TestCase((ushort)1,       (ushort)1,       ExpectedResult = true)]
        [TestCase(UInt16.MaxValue, (ushort)1,       ExpectedResult = false)]
        public bool IsValid_MultipleValues(ushort idAsUshort, ushort index)
        {
            var id = new PortID(idAsUshort, index);
            return id.IsValid();
        }

        [TestCase((ushort)0,       UInt16.MaxValue,       (ushort)0,       UInt16.MaxValue, ExpectedResult = true)]
        [TestCase((ushort)0,       UInt16.MaxValue,       (ushort)0,             (ushort)0, ExpectedResult = false)]
        [TestCase((ushort)0,       UInt16.MaxValue, UInt16.MaxValue,       UInt16.MaxValue, ExpectedResult = false)]
        [TestCase((ushort)0,       UInt16.MaxValue, UInt16.MaxValue,             (ushort)0, ExpectedResult = false)]

        [TestCase((ushort)0,             (ushort)0,       (ushort)0,       UInt16.MaxValue, ExpectedResult = false)]
        [TestCase((ushort)0,             (ushort)0,       (ushort)0,             (ushort)0, ExpectedResult = true)]
        [TestCase((ushort)0,             (ushort)0, UInt16.MaxValue,       UInt16.MaxValue, ExpectedResult = false)]
        [TestCase((ushort)0,             (ushort)0, UInt16.MaxValue,             (ushort)0, ExpectedResult = false)]

        [TestCase(UInt16.MaxValue, UInt16.MaxValue,       (ushort)0,       UInt16.MaxValue, ExpectedResult = false)]
        [TestCase(UInt16.MaxValue, UInt16.MaxValue,       (ushort)0,             (ushort)0, ExpectedResult = false)]
        [TestCase(UInt16.MaxValue, UInt16.MaxValue, UInt16.MaxValue,       UInt16.MaxValue, ExpectedResult = true)]
        [TestCase(UInt16.MaxValue, UInt16.MaxValue, UInt16.MaxValue,             (ushort)0, ExpectedResult = true)]

        [TestCase(UInt16.MaxValue,       (ushort)0,       (ushort)0,       UInt16.MaxValue, ExpectedResult = false)]
        [TestCase(UInt16.MaxValue,       (ushort)0,       (ushort)0,             (ushort)0, ExpectedResult = false)]
        [TestCase(UInt16.MaxValue,       (ushort)0, UInt16.MaxValue,       UInt16.MaxValue, ExpectedResult = true)]
        [TestCase(UInt16.MaxValue,       (ushort)0, UInt16.MaxValue,             (ushort)0, ExpectedResult = true)]
        public bool EqualOperator_MultipleValues(ushort id1AsUshort, ushort index1, ushort id2AsUshort, ushort index2)
        {
            var id1 = new PortID(id1AsUshort, index1);
            var id2 = new PortID(id2AsUshort, index2);
            return id1 == id2;
        }

        [Test]
        public void Copy_SimpleValues_AreEqual()
        {
            PortID id0 = new PortID(1);
            PortID id1 = id0;
            Assert.AreEqual(id0, id1);
            id1 = new PortID(2);
            Assert.AreNotEqual(id0, id1);
        }
    }

    public class NodeIDTests
    {
        [TestCase(0, ExpectedResult = true)]
        [TestCase(1, ExpectedResult = true)]
        [TestCase(Int32.MaxValue, ExpectedResult = true)]
        [TestCase(Int32.MinValue, ExpectedResult = true)]
        [TestCase(-1, ExpectedResult = false)]
        public bool IsValid_MultipleValues(int idAsInt)
        {
            var id = new NodeID(idAsInt);
            return id.IsValid();
        }

        [Test]
        public void EqualOperator_MultipleValues()
        {
            NodeID id0 = (NodeID)1;
            NodeID id1 = (NodeID)0;
            NodeID id2 = NodeID.Invalid;
            Assert.AreNotEqual(id0, id1);
            Assert.AreNotEqual(id0, id2);
            Assert.AreNotEqual(id1, id2);
            Assert.AreEqual(id0, id0);
            Assert.AreEqual(id1, id1);
            Assert.AreEqual(id1, id1);
        }

        [Test]
        public void Copy_SimpleValues_AreEqual()
        {
            NodeID id0 = new NodeID(1);
            NodeID id1 = id0;
            Assert.AreEqual(id0, id1);
            id1 = new NodeID(2);
            Assert.AreNotEqual(id0, id1);
        }
    }

    public class GraphPathTests
    {
        [Test]
        public void PushPop_Multiple_ValidPaths()
        {
            var testPath = new GraphPath();
            Assert.AreEqual("/", testPath.ToString());
            testPath.Push("Root");
            Assert.AreEqual("Root/", testPath.ToString());
            testPath.Push("SubRoot");
            Assert.AreEqual("Root/SubRoot/", testPath.ToString());
            testPath.Pop();
            testPath.Push("NewSubRoot");
            Assert.AreEqual("Root/NewSubRoot/", testPath.ToString());
            testPath.Pop();
            Assert.AreEqual("Root/", testPath.ToString());
        }

        [Test]
        public void Pop_Empty_ThrowException()
        {
            var testPath = new GraphPath();
            Assert.Throws<InvalidOperationException>(testPath.Pop);
            testPath.Push("TestRoot");
            testPath.Pop();
            Assert.Throws<InvalidOperationException>(testPath.Pop);
        }

        [Test]
        public void Combine_TwoInputs_ValidPaths()
        {
            var testPath = new GraphPath();
            testPath.Push("Root");
            testPath.Push("SubNode");
            var otherPath = new GraphPath();
            otherPath.Push("Path2");
            Assert.AreEqual("Root/SubNode/Path2/", testPath.Combine(otherPath).ToString());
            Assert.AreEqual("Path2/Root/SubNode/", otherPath.Combine(testPath).ToString());
        }

        internal struct MyIComponentData : IComponentData {}

        internal interface IComponentDataInterface : IComponentData {};
        internal struct MySpecialIComponentData : IComponentDataInterface {}

        internal struct MyBufferElementData : IBufferElementData {}

        internal interface IBufferDataInterface : IBufferElementData {};
        internal struct MySpecialBufferElementData : IBufferDataInterface {}

        internal struct MySystemStateComponentData : ISystemStateComponentData {};
        internal interface ISystemStateComponentDataInterface : ISystemStateComponentData {};
        internal struct MySpecialSystemStateComponentData : ISystemStateComponentDataInterface {};

        internal struct MySharedComponentData : ISharedComponentData {}
        internal struct MyRandomStruct {}

        [TestCase(typeof(MyIComponentData), ExpectedResult = null)]
        [TestCase(typeof(MySpecialIComponentData), ExpectedResult = null)]
        [TestCase(typeof(Buffer<MyBufferElementData>), ExpectedResult = typeof(MyBufferElementData))]
        [TestCase(typeof(Buffer<MySpecialBufferElementData>), ExpectedResult = typeof(MySpecialBufferElementData))]
        [TestCase(typeof(MyBufferElementData), ExpectedResult = null)]
        [TestCase(typeof(MySpecialBufferElementData), ExpectedResult = null)]
        [TestCase(typeof(MySystemStateComponentData), ExpectedResult = null)]
        [TestCase(typeof(MySpecialSystemStateComponentData), ExpectedResult = null)]
        [TestCase(typeof(float), ExpectedResult = null)]
        [TestCase(typeof(Buffer<float>), ExpectedResult = null)]
        [TestCase(typeof(MyRandomStruct), ExpectedResult = null)]
        [TestCase(typeof(Buffer<MyRandomStruct>), ExpectedResult = null)]
        [TestCase(typeof(MySharedComponentData), ExpectedResult = null)]
        [TestCase(typeof(Buffer<MySharedComponentData>), ExpectedResult = null)]
        public Type GetUnderlyingBufferElementType_MultipleValues(Type t)
        {
            return Helpers.GetUnderlyingBufferElementType(t);
        }

        [TestCase(typeof(MyIComponentData), ExpectedResult = true)]
        [TestCase(typeof(MySpecialIComponentData), ExpectedResult = true)]
        [TestCase(typeof(Buffer<MyBufferElementData>), ExpectedResult = false)]
        [TestCase(typeof(Buffer<MySpecialBufferElementData>), ExpectedResult = false)]
        [TestCase(typeof(MyBufferElementData), ExpectedResult = false)]
        [TestCase(typeof(MySpecialBufferElementData), ExpectedResult = false)]
        [TestCase(typeof(MySystemStateComponentData), ExpectedResult = true)]
        [TestCase(typeof(MySpecialSystemStateComponentData), ExpectedResult = true)]
        [TestCase(typeof(float), ExpectedResult = false)]
        [TestCase(typeof(Buffer<float>), ExpectedResult = false)]
        [TestCase(typeof(MyRandomStruct), ExpectedResult = false)]
        [TestCase(typeof(Buffer<MyRandomStruct>), ExpectedResult = false)]
        [TestCase(typeof(MySharedComponentData), ExpectedResult = false)]
        [TestCase(typeof(Buffer<MySharedComponentData>), ExpectedResult = false)]
        public bool IsComponentDataType_MultipleValues(Type t)
        {
            return Helpers.IsComponentDataType(t);
        }

        [TestCase(typeof(MyIComponentData), ExpectedResult = false)]
        [TestCase(typeof(MySpecialIComponentData), ExpectedResult = false)]
        [TestCase(typeof(Buffer<MyBufferElementData>), ExpectedResult = true)]
        [TestCase(typeof(Buffer<MySpecialBufferElementData>), ExpectedResult = true)]
        [TestCase(typeof(MyBufferElementData), ExpectedResult = false)]
        [TestCase(typeof(MySpecialBufferElementData), ExpectedResult = false)]
        [TestCase(typeof(MySystemStateComponentData), ExpectedResult = false)]
        [TestCase(typeof(MySpecialSystemStateComponentData), ExpectedResult = false)]
        [TestCase(typeof(float), ExpectedResult = false)]
        [TestCase(typeof(Buffer<float>), ExpectedResult = false)]
        [TestCase(typeof(MyRandomStruct), ExpectedResult = false)]
        [TestCase(typeof(Buffer<MyRandomStruct>), ExpectedResult = false)]
        [TestCase(typeof(MySharedComponentData), ExpectedResult = false)]
        [TestCase(typeof(Buffer<MySharedComponentData>), ExpectedResult = false)]
        public bool IsBufferElementDataType_MultipleValues(Type t)
        {
            return Helpers.IsBufferElementDataType(t);
        }

        [TestCase(typeof(MyIComponentData), ExpectedResult = true)]
        [TestCase(typeof(MySpecialIComponentData), ExpectedResult = true)]
        [TestCase(typeof(Buffer<MyBufferElementData>), ExpectedResult = true)]
        [TestCase(typeof(Buffer<MySpecialBufferElementData>), ExpectedResult = true)]
        [TestCase(typeof(MyBufferElementData), ExpectedResult = false)]
        [TestCase(typeof(MySpecialBufferElementData), ExpectedResult = false)]
        [TestCase(typeof(MySystemStateComponentData), ExpectedResult = true)]
        [TestCase(typeof(MySpecialSystemStateComponentData), ExpectedResult = true)]
        [TestCase(typeof(float), ExpectedResult = false)]
        [TestCase(typeof(Buffer<float>), ExpectedResult = false)]
        [TestCase(typeof(MyRandomStruct), ExpectedResult = false)]
        [TestCase(typeof(Buffer<MyRandomStruct>), ExpectedResult = false)]
        [TestCase(typeof(MySharedComponentData), ExpectedResult = false)]
        [TestCase(typeof(Buffer<MySharedComponentData>), ExpectedResult = false)]
        public bool IsComponentNodeCompatible_MultipleValues(Type t)
        {
            return Helpers.IsComponentNodeCompatible(t);
        }
    }
}
