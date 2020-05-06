using NUnit.Framework;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    public class BlendTreeBuilderTests : AnimationTestsFixture
    {
        [Test]
        public void BlendTree1DCreatedWithoutMotionDataReturnNull()
        {
            var blendTree = BlendTreeBuilder.CreateBlendTree(null);

            Assert.That(blendTree, Is.EqualTo(BlobAssetReference<BlendTree1D>.Null));
        }

        [Test]
        public void CreateBlendTree1DCanSortMotionData()
        {
            var motionData = new[]
            {
                new BlendTree1DMotionData { MotionThreshold = 1.0f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.8f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.1f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.9f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.5f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.6f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.7f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.3f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.2f, MotionSpeed = 1.0f },
                new BlendTree1DMotionData { MotionThreshold = 0.0f, MotionSpeed = 1.0f },
            };

            var blendTree = BlendTreeBuilder.CreateBlendTree(motionData);

            for (int i = 1; i < blendTree.Value.MotionThresholds.Length; i++)
            {
                Assert.That(blendTree.Value.MotionThresholds[i], Is.GreaterThanOrEqualTo(blendTree.Value.MotionThresholds[i - 1]));
            }
        }
    }
}
