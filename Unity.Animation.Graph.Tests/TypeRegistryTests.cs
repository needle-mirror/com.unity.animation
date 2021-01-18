using System;
using NUnit.Framework;
using Unity.DataFlowGraph;

namespace Unity.Animation.Tests
{
    [TestFixture]
    class TypeRegistryTests
    {
        [Test]
        public void RegisterTypes_NonNodeDefinitionType_Throws()
        {
            var registry = new TypeRegistry();
            System.Type t = typeof(int);
            Assert.Throws<ArgumentException>(() => registry.RegisterType(t.AssemblyQualifiedName));
        }

        [Test]
        public void RegisterTypes_NonExistingType_Throws()
        {
            var registry = new TypeRegistry();
            string myNonExistingType = "Unity.Animation.NotATypeThatExists";
            Assert.Throws<ArgumentException>(() => registry.RegisterType(myNonExistingType));
        }

        [TestCase(typeof(UtilityNodeTests.SimpleMessageNode))]
        [TestCase(typeof(UtilityNodeTests.SimpleTemplateNode<float>))]
        public void RegisterTypes_WithUnregisteredValidTypeHash_Throws(Type t)
        {
            var registry = new TypeRegistry();
            string qualifiedName = t.AssemblyQualifiedName;
            NodeSet nodeSet = new NodeSet();
            Assert.Throws<ArgumentException>(() => registry.CreateNodeFromHash(nodeSet,  qualifiedName.GetHashCode()));
            registry.RegisterType(qualifiedName);
            NodeHandle nodeHandle = registry.CreateNodeFromHash(nodeSet, qualifiedName.GetHashCode());
            Assert.That(nodeHandle != default);
            Assert.True(nodeSet.Exists(nodeHandle));
            nodeSet.Destroy(nodeHandle);
            nodeSet.Dispose();
        }

        [TestCase(typeof(UtilityNodeTests.SimpleMessageNode))]
        [TestCase(typeof(UtilityNodeTests.SimpleTemplateNode<float>))]
        public void RegisterTypes_ValidTypes_IsRegistered(Type t)
        {
            var registry = new TypeRegistry();
            string qualifiedName = t.AssemblyQualifiedName;
            registry.RegisterType(qualifiedName);
            NodeSet nodeSet = new NodeSet();
            NodeHandle nodeHandle = registry.CreateNodeFromHash(nodeSet, qualifiedName.GetHashCode());
            Assert.That(nodeHandle != default);
            Assert.True(nodeSet.Exists(nodeHandle));
            nodeSet.Destroy(nodeHandle);
            nodeSet.Dispose();
        }

        public void RegisterTypes_Twice_DoesNotThrow()
        {
            var registry = new TypeRegistry();
            string qualifiedName = typeof(UtilityNodeTests.SimpleMessageNode).AssemblyQualifiedName;
            registry.RegisterType(qualifiedName);
            registry.RegisterType(qualifiedName);
            NodeSet nodeSet = new NodeSet();
            NodeHandle nodeHandle = registry.CreateNodeFromHash(nodeSet, qualifiedName.GetHashCode());
            Assert.That(nodeHandle != default);
            Assert.True(nodeSet.Exists(nodeHandle));
            nodeSet.Destroy(nodeHandle);
            nodeSet.Dispose();
        }

        [Test]
        public void CreateNodeFromHash_WithInvalidHash_Throws()
        {
            var registry = new TypeRegistry();
            System.Type existingType = typeof(UtilityNodeTests.SimpleMessageNode);
            registry.RegisterType(existingType.AssemblyQualifiedName);
            NodeSet nodeSet = new NodeSet();
            Assert.Throws<ArgumentException>(() => registry.CreateNodeFromHash(nodeSet, 12345));
            nodeSet.Dispose();
        }
    }
}
