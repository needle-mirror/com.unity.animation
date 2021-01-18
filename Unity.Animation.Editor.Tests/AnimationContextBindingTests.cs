using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Editor.Tests
{
    class AnimationContextBindingTests : BaseGraphFixture
    {
        protected override string[] TestAssemblies => new[] { "Unity.Animation.Editor.Tests" };
        protected override bool CreateBoundObjectOnStartup => true;

        //TODO: Create generic test for this
        [Test]
        public void UpdateExposedObjectBinding_MotionID()
        {
            var boundObject = m_Store.State.WindowState.CurrentGraph.BoundObject;
            var animGraph = boundObject.GetComponent<AnimationGraph>();
            var newObject = new GameObject("Test Object");
            var node = CreateNode(typeof(MotionIDNode));

            Assert.AreEqual(0, animGraph.ExposedObjects.Count);

            var port = node.GetInputMessagePorts().ToArray()[0];
            port.EmbeddedValue.ObjectValue = newObject.transform;
            var changeEvent = new ChangeEvent<Object>();
            (port.EmbeddedValue as MotionIDConstant).UpdateExposedObjects(changeEvent, m_Store, port);

            Assert.AreEqual(1, animGraph.ExposedObjects.Count);
            Assert.AreEqual(newObject.transform, animGraph.ExposedObjects[0].Value);
            Assert.AreEqual(port.UniqueName, animGraph.ExposedObjects[0].TargetGUID.PortUniqueName);
            Assert.IsTrue(port.NodeModel.Guid.CompareTo(SerializableGUID.FromParts(animGraph.ExposedObjects[0].TargetGUID.NodeIDPart1, animGraph.ExposedObjects[0].TargetGUID.NodeIDPart2)) == 0);

            Object.DestroyImmediate(newObject);
        }
    }
}
