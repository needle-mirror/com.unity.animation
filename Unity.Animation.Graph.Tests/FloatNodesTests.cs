using NUnit.Framework;

namespace Unity.Animation.Tests
{
    class FloatNodesTests : AnimationTestsFixture
    {
        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(3, 0.14159f)]
        public void CanAddTwoFloats(float a, float b)
        {
            var addNode = CreateNode<FloatAddNode>();
            Set.SetData(addNode, FloatAddNode.KernelPorts.InputA, a);
            Set.SetData(addNode, FloatAddNode.KernelPorts.InputB, b);

            var output = CreateGraphValue(addNode, FloatAddNode.KernelPorts.Output);

            Set.Update(default);

            float value = Set.GetValueBlocking(output);

            Assert.That(value, Is.EqualTo(a + b));
        }

        [TestCase(1, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 10)]
        public void CanMulTwoFloats(float a, float b)
        {
            var mulNode = CreateNode<FloatMulNode>();
            Set.SetData(mulNode, FloatMulNode.KernelPorts.InputA, a);
            Set.SetData(mulNode, FloatMulNode.KernelPorts.InputB, b);

            var output = CreateGraphValue(mulNode, FloatMulNode.KernelPorts.Output);

            Set.Update(default);

            float value = Set.GetValueBlocking(output);

            Assert.That(value, Is.EqualTo(a * b));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(100)]
        public void CanForwardFloat(float a)
        {
            var passthroughNode = CreateNode<KernelPassThroughNodeFloat>();
            Set.SetData(passthroughNode, KernelPassThroughNodeFloat.KernelPorts.Input, a);

            var output = CreateGraphValue(passthroughNode, KernelPassThroughNodeFloat.KernelPorts.Output);

            Set.Update(default);

            float value = Set.GetValueBlocking(output);

            Assert.That(value, Is.EqualTo(a));
        }
    }
}
