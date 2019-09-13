using NUnit.Framework;

namespace Unity.Animation.Tests
{
    class FloatNodesTests : AnimationTestsFixture
    {
        [Test]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(3, 0.14159f)]
        public void CanAddTwoFloats(float a, float b)
        {
            var set = Set;
            var addNode = CreateNode<FloatAddNode>();
            set.SetData(addNode, FloatAddNode.KernelPorts.InputA, a);
            set.SetData(addNode, FloatAddNode.KernelPorts.InputB, b);

            var output = CreateGraphValue(addNode, FloatAddNode.KernelPorts.Output);

            set.Update();

            float value = set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(a+b));
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 10)]
        public void CanMulTwoFloats(float a, float b)
        {
            var set = Set;
            var mulNode = CreateNode<FloatMulNode>();
            set.SetData(mulNode, FloatMulNode.KernelPorts.InputA, a);
            set.SetData(mulNode, FloatMulNode.KernelPorts.InputB, b);

            var output = CreateGraphValue(mulNode, FloatMulNode.KernelPorts.Output);

            set.Update();

            float value = set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(a*b));
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(100)]
        public void CanForwardFloat(float a)
        {
            var set = Set;
            var passthroughNode = CreateNode<KernelPassThroughNodeFloat>();
            set.SetData(passthroughNode, KernelPassThroughNodeFloat.KernelPorts.Input, a);

            var output = CreateGraphValue(passthroughNode, KernelPassThroughNodeFloat.KernelPorts.Output);

            set.Update();

            float value = set.GetValueBlocking(output);

            Assert.That(value,Is.EqualTo(a));
        }
    }
}
