using NUnit.Framework;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    public class ComputeBlendTree1DWeightsNodeTests : AnimationTestsFixture
    {
        BlobAssetReference<BlendTree1D>     m_BlendTree;

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            var motionData = new BlendTree1DMotionData[]
            {
                new BlendTree1DMotionData { MotionThreshold = 0.2f, MotionSpeed = 1.0f},
                new BlendTree1DMotionData { MotionThreshold = 0.4f, MotionSpeed = 0.8f},
                new BlendTree1DMotionData { MotionThreshold = 0.6f, MotionSpeed = 0.6f},
                new BlendTree1DMotionData { MotionThreshold = 0.8f, MotionSpeed = 0.4f},
                new BlendTree1DMotionData { MotionThreshold = 1.0f, MotionSpeed = 0.2f},
            };

            m_BlendTree = BlendTreeBuilder.CreateBlendTree(motionData);
        }

        [Test]
        public void SettingBlendTreeAssetResizePorts()
        {
            var node = CreateNode<ComputeBlendTree1DWeightsNode>();

            Set.SendMessage(node, ComputeBlendTree1DWeightsNode.SimulationPorts.BlendTree, m_BlendTree);

            for(int i=0;i<m_BlendTree.Value.Motions.Length;i++)
            {
                Set.SetData(node, ComputeBlendTree1DWeightsNode.KernelPorts.MotionDurations, i, 1.0f);
            }
        }

        [Test]
        public void CannotAccessArrayPortOutOfBound()
        {
            var node = CreateNode<ComputeBlendTree1DWeightsNode>();

            Set.SendMessage(node, ComputeBlendTree1DWeightsNode.SimulationPorts.BlendTree, m_BlendTree);

            Assert.Throws(Is.TypeOf<System.IndexOutOfRangeException>()
                 .And.Message.EqualTo("Port array index 6 was out of bounds, array only has 5 indices"),
                 () => Set.SetData(node, ComputeBlendTree1DWeightsNode.KernelPorts.MotionDurations, m_BlendTree.Value.Motions.Length+1, 1.0f));
        }

        [TestCase(0.0f, new float[] {1.0f, 0.0f, 0.0f, 0.0f, 0.0f })]
        [TestCase(0.2f, new float[] {1.0f, 0.0f, 0.0f, 0.0f, 0.0f })]
        [TestCase(0.25f, new float[] {0.75f, 0.25f, 0.0f, 0.0f, 0.0f })]
        [TestCase(0.3f, new float[] {0.5f, 0.5f, 0.0f, 0.0f, 0.0f })]
        [TestCase(0.4f, new float[] {0.0f, 1.0f, 0.0f, 0.0f, 0.0f })]
        [TestCase(0.5f, new float[] {0.0f, 0.5f, 0.5f, 0.0f, 0.0f })]
        [TestCase(0.6f, new float[] {0.0f, 0.0f, 1.0f, 0.0f, 0.0f })]
        [TestCase(0.8f, new float[] {0.0f, 0.0f, 0.0f, 1.0f, 0.0f })]
        [TestCase(1.0f, new float[] {0.0f, 0.0f, 0.0f, 0.0f, 1.0f })]
        [TestCase(1.2f, new float[] {0.0f, 0.0f, 0.0f, 0.0f, 1.0f })]
        public void CanComputeWeights(float blendParameter, float[] expectedWeights)
        {
            var node = CreateNode<ComputeBlendTree1DWeightsNode>();

            Set.SendMessage(node, ComputeBlendTree1DWeightsNode.SimulationPorts.BlendTree, m_BlendTree);
            Set.SetData(node, ComputeBlendTree1DWeightsNode.KernelPorts.BlendParameter, blendParameter);

            var result = Set.CreateGraphValue(node, ComputeBlendTree1DWeightsNode.KernelPorts.Weights);

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

        [TestCase(0.0f, new float[] {1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, 1.0f/1.0f)]
        [TestCase(0.2f, new float[] {1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, 1.0f/1.0f)]
        [TestCase(0.25f, new float[] {1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, 1.0f/1.0f*0.75f + 2.0f/0.8f*0.25f)]
        [TestCase(0.3f, new float[] {1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, 1.0f/1.0f*0.5f + 2.0f/0.8f*0.5f)]
        [TestCase(0.4f, new float[] {1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, 2.0f/0.8f)]
        [TestCase(0.5f, new float[] {1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, 2.0f/0.8f*0.5f + 3.0f/0.6f*0.5f)]
        [TestCase(0.6f, new float[] {1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, 3.0f/0.6f)]
        [TestCase(0.8f, new float[] {1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, 4.0f/0.4f)]
        [TestCase(1.0f, new float[] {1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, 5.0f/0.2f)]
        [TestCase(1.2f, new float[] {1.0f, 2.0f, 3.0f, 4.0f, 5.0f }, 5.0f/0.2f)]
        public void CanComputeDuration(float blendParameter, float[] durations, float expectedDuration)
        {
            var node = CreateNode<ComputeBlendTree1DWeightsNode>();

            Set.SendMessage(node, ComputeBlendTree1DWeightsNode.SimulationPorts.BlendTree, m_BlendTree);
            Set.SetData(node, ComputeBlendTree1DWeightsNode.KernelPorts.BlendParameter, blendParameter);

            Assert.AreEqual(m_BlendTree.Value.Motions.Length, durations.Length);
            for(int i=0;i<durations.Length;i++)
            {
                Set.SetData(node, ComputeBlendTree1DWeightsNode.KernelPorts.MotionDurations, i, durations[i]);
            }

            var result = CreateGraphValue(node, ComputeBlendTree1DWeightsNode.KernelPorts.Duration);

            Set.Update(default);

            var duration = Set.GetValueBlocking(result);

            Assert.That(duration, Is.EqualTo(expectedDuration).Using(FloatComparer));
        }
    }
}
