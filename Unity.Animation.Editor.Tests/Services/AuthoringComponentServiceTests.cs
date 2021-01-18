using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Editor.Tests
{
    class AuthoringComponentServiceTests : BaseGraphFixture
    {
        Type GetAuthoringType(string authoringTypeName)
        {
            return AuthoringComponentService.FindType(
                TypeCache.GetTypesDerivedFrom<MonoBehaviour>().ToList(),
                authoringTypeName);
        }

        [Test]
        public void ComponentsList_ContainsEntry_WithRuntimeType()
        {
            var component = AuthoringComponentService.GetComponentInfos().SingleOrDefault(c => c.RuntimeType == typeof(DummyAuthoringComponent));
            Assert.IsNotNull(component);
        }

        [Test]
        public void ComponentsList_ContainsEntry_WithAuthoringType()
        {
            var component = AuthoringComponentService.GetComponentInfos().SingleOrDefault(c => c.AuthoringType ==
                GetAuthoringType($"{typeof(DummyAuthoringComponent).FullName}Authoring"));
            Assert.IsNotNull(component);
            Assert.AreEqual(typeof(DummyAuthoringComponent), component.RuntimeType);
        }

        [Test]
        public void CanRetrieveComponent_WithRuntimeType()
        {
            Assert.IsTrue(AuthoringComponentService.TryGetComponentByRuntimeType(typeof(DummyAuthoringComponent), out var componentInfo));
            Assert.AreEqual(typeof(DummyAuthoringComponent), componentInfo.RuntimeType);
        }

        [Test]
        public void CanRetrieveComponent_WithAuthoringType()
        {
            var authoringType = GetAuthoringType($"{typeof(DummyAuthoringComponent).FullName}Authoring");
            Assert.IsNotNull(authoringType);
            Assert.IsTrue(AuthoringComponentService.TryGetComponentByAuthoringType(authoringType, out var componentInfo));
            Assert.AreEqual(typeof(DummyAuthoringComponent), componentInfo.RuntimeType);
        }

        [Test]
        public void CanRetrieveComponent_WithRuntimeAssemblyQualifiedName()
        {
            Assert.IsTrue(AuthoringComponentService.TryGetComponentByRuntimeAssemblyQualifiedName(typeof(DummyAuthoringComponent).AssemblyQualifiedName, out var componentInfo));
            Assert.AreEqual(typeof(DummyAuthoringComponent), componentInfo.RuntimeType);
        }

        [Test]
        public void StoredComponentRuntimeFields_ContainOnlyValidFields()
        {
            Assert.IsTrue(AuthoringComponentService.TryGetComponentByRuntimeType(typeof(DummyAuthoringComponent), out var componentInfo));
            Assert.IsTrue(componentInfo.RuntimeFields.TryGetValue("Field1", out var field1));
            Assert.AreEqual(UnsafeUtility.GetFieldOffset(typeof(DummyAuthoringComponent).GetField("Field1")), field1.Offset);
            Assert.IsFalse(componentInfo.RuntimeFields.ContainsKey("Field2"));
            Assert.IsFalse(componentInfo.RuntimeFields.ContainsKey("Field3"));
            Assert.IsFalse(componentInfo.RuntimeFields.ContainsKey("Field4"));
            Assert.IsTrue(componentInfo.RuntimeFields.TryGetValue("Field5", out var field5));
            Assert.AreEqual(UnsafeUtility.GetFieldOffset(typeof(DummyAuthoringComponent).GetField("Field5")), field5.Offset);
        }
    }
}
