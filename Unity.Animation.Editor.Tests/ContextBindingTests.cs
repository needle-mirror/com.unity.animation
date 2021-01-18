using NUnit.Framework;
using UnityEngine;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Editor.Tests
{
    class ContextBindingTests : BaseGraphFixture
    {
        protected override string[] TestAssemblies => new[] { "Unity.Animation.Editor.Nodes.Tests", "Unity.Animation.Editor.Tests" };
        protected override bool CreateBoundObjectOnStartup => true;

        [Test]
        public void ValidateBoundObjectBinding()
        {
            Assert.That(CreatedBoundObject != null);

            var boundObject = m_Store.State.WindowState.CurrentGraph.BoundObject;
            Assert.That(boundObject != null);

            var animGraph = (boundObject as GameObject).GetComponent<AnimationGraph>();
            Assert.That(animGraph.Graph == null);

            CompileGraphAndAddToBoundObject();

            Assert.That(animGraph.Graph != null);
        }
    }
}
