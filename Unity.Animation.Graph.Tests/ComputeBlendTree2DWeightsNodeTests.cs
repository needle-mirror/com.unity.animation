using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    public class ComputeBlendTree2DWeightsNodeTests : AnimationTestsFixture
    {
        BlobAssetReference<BlendTree2DSimpleDirectional>     m_BlendTree;

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            var motionData = new BlendTree2DMotionData[]
            {
                new BlendTree2DMotionData { MotionPosition = new float2(-2, 0), MotionSpeed = 0.8f, },
                new BlendTree2DMotionData { MotionPosition = new float2(2, 0), MotionSpeed = 0.6f,  },
                new BlendTree2DMotionData { MotionPosition = new float2(0, 2), MotionSpeed = 0.4f,  },
                new BlendTree2DMotionData { MotionPosition = new float2(0, -2), MotionSpeed = 0.2f, },
            };

            m_BlendTree = BlendTreeBuilder.CreateBlendTree2DSimpleDirectional(motionData);
        }

        [Test]
        public void SettingBlendTreeAssetResizePorts()
        {
            var node = CreateNode<ComputeBlendTree2DWeightsNode>();

            Set.SendMessage(node, ComputeBlendTree2DWeightsNode.SimulationPorts.BlendTree, m_BlendTree);

            for (int i = 0; i < m_BlendTree.Value.Motions.Length; i++)
            {
                Set.SetData(node, ComputeBlendTree2DWeightsNode.KernelPorts.MotionDurations, i, 1.0f);
            }
        }

        [Test]
        public void CannotAccessArrayPortOutOfBound()
        {
            var node = CreateNode<ComputeBlendTree2DWeightsNode>();

            Set.SendMessage(node, ComputeBlendTree2DWeightsNode.SimulationPorts.BlendTree, m_BlendTree);

            Assert.Throws(Is.TypeOf<System.IndexOutOfRangeException>()
                .And.Message.EqualTo("Port array index 5 was out of bounds, array only has 4 indices"),
                () => Set.SetData(node, ComputeBlendTree2DWeightsNode.KernelPorts.MotionDurations, m_BlendTree.Value.Motions.Length + 1, 1.0f));
        }

#if !ENABLE_IL2CPP
        [TestCase(0.0f, 0.0f, new float[] {0.25f, 0.25f, 0.25f, 0.25f })]
        [TestCase(-2.0f, 0.0f, new float[] {1.0f, 0.0f, 0.0f, 0.0f})]
        [TestCase(2.0f, 0.0f, new float[] {0.0f, 1.0f, 0.0f, 0.0f})]
        [TestCase(0.0f, 2.0f, new float[] {0.0f, 0.0f, 1.0f, 0.0f})]
        [TestCase(0.0f, -2.0f, new float[] {0.0f, 0.0f, 0.0f, 1.0f})]
        public void CanComputeWeights(float blendParameterX, float blendParameterY, float[] expectedWeights)
        {
            var node = CreateNode<ComputeBlendTree2DWeightsNode>();

            Set.SendMessage(node, ComputeBlendTree2DWeightsNode.SimulationPorts.BlendTree, m_BlendTree);
            Set.SetData(node, ComputeBlendTree2DWeightsNode.KernelPorts.BlendParameterX, blendParameterX);
            Set.SetData(node, ComputeBlendTree2DWeightsNode.KernelPorts.BlendParameterY, blendParameterY);

            var result = Set.CreateGraphValue(node, ComputeBlendTree2DWeightsNode.KernelPorts.Weights);

            Set.Update(default);

            var resolver = Set.GetGraphValueResolver(out var valueResolverDeps);
            valueResolverDeps.Complete();
            var weights = resolver.Resolve(result);

            Assert.AreEqual(m_BlendTree.Value.Motions.Length, weights.Length);
            Assert.AreEqual(m_BlendTree.Value.Motions.Length, expectedWeights.Length);
            for (int i = 0; i < weights.Length; ++i)
                Assert.That(weights[i], Is.EqualTo(expectedWeights[i]).Using(FloatComparer));

            Set.ReleaseGraphValue(result);
        }

        [TestCase(0.0f, 0.0f, new float[] {1.0f, 2.0f, 3.0f, 4.0f }, 1.0f / 0.8f * 0.25f + 2.0f / 0.6f * 0.25f + 3.0f / 0.4f * 0.25f + 4.0f / 0.2f * 0.25f)]
        [TestCase(-2.0f, 0.0f, new float[] {1.0f, 2.0f, 3.0f, 4.0f}, 1.0f / 0.8f)]
        [TestCase(2.0f, 0.0f, new float[] {1.0f, 2.0f, 3.0f, 4.0f}, 2.0f / 0.6f)]
        [TestCase(0.0f, 2.0f, new float[] {1.0f, 2.0f, 3.0f, 4.0f}, 3.0f / 0.4f)]
        [TestCase(0.0f, -2.0f, new float[] {1.0f, 2.0f, 3.0f, 4.0f}, 4.0f / 0.2f)]
        public void CanComputeDuration(float blendParameterX, float blendParameterY, float[] durations, float expectedDuration)
        {
            var node = CreateNode<ComputeBlendTree2DWeightsNode>();

            Set.SendMessage(node, ComputeBlendTree2DWeightsNode.SimulationPorts.BlendTree, m_BlendTree);
            Set.SetData(node, ComputeBlendTree2DWeightsNode.KernelPorts.BlendParameterX, blendParameterX);
            Set.SetData(node, ComputeBlendTree2DWeightsNode.KernelPorts.BlendParameterY, blendParameterY);

            Assert.AreEqual(m_BlendTree.Value.Motions.Length, durations.Length);
            for (int i = 0; i < durations.Length; i++)
            {
                Set.SetData(node, ComputeBlendTree2DWeightsNode.KernelPorts.MotionDurations, i, durations[i]);
            }

            var result = CreateGraphValue(node, ComputeBlendTree2DWeightsNode.KernelPorts.Duration);

            Set.Update(default);

            var duration = Set.GetValueBlocking(result);

            Assert.That(duration, Is.EqualTo(expectedDuration).Using(FloatComparer));
        }

#endif
    }
}
