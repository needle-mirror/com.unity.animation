using UnityEngine;
using NUnit.Framework;

using Unity.Animation.Hybrid;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    public class AnimationCurveCacheTests
    {
        [TestCase(-1, 0, 0, 0)]
        [TestCase(1.1f, 0, 6.3f, 2)]
        [TestCase(4.1f, 1, 5.3f, 2)]
        [TestCase(5.3f, 2, 3.2f, 1)]
        [TestCase(8, 2, 8.2f, 3)]
        [TestCase(10, 4, 11, 4)]
        public void TestCurveCacheIndexChange(float time, int expectedCacheIndex, float time2, int expectedCacheIndex2)
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(0, -10, 10, -10);

            // Only supports clamp
            animCurve.preWrapMode = WrapMode.Clamp;
            animCurve.postWrapMode = WrapMode.Clamp;

            animCurve.AddKey(new UnityEngine.Keyframe() { time = 3, value = 2.5f });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 4.5f, value = -30 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 8.1f, value = 8 });

            var cache = new AnimationCurveCache();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curveBlob = ref blobBuilder.ConstructRoot<AnimationCurveBlob>();
            CurveConversion.FillAnimationCurveBlob(animCurve, ref blobBuilder, ref curveBlob);

            var blobAsset = blobBuilder.CreateBlobAssetReference<AnimationCurveBlob>(Allocator.Persistent);

            AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value, ref cache);
            Assert.AreEqual(expectedCacheIndex, cache.LhsIndex);

            AnimationCurveEvaluator.Evaluate(time2, ref blobAsset.Value, ref cache);
            Assert.AreEqual(expectedCacheIndex2, cache.LhsIndex);
        }

        [TestCase(-1, 0, 1)]
        [TestCase(1.1f, 0, 1)]
        [TestCase(4.1f, 1, 2)]
        [TestCase(5.3f, 2, 3)]
        [TestCase(8, 2, 3)]
        [TestCase(11, 4, 4)]
        public void TestCurveCacheIndexValue(float time, int lhsIndex, int rhsIndex)
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(0, -10, 10, -10);

            // Only supports clamp
            animCurve.preWrapMode = WrapMode.Clamp;
            animCurve.postWrapMode = WrapMode.Clamp;

            animCurve.AddKey(new UnityEngine.Keyframe() { time = 3, value = 2.5f });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 4.5f, value = -30 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 8.1f, value = 8 });

            var cache = new AnimationCurveCache();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curveBlob = ref blobBuilder.ConstructRoot<AnimationCurveBlob>();
            CurveConversion.FillAnimationCurveBlob(animCurve, ref blobBuilder, ref curveBlob);

            var blobAsset = blobBuilder.CreateBlobAssetReference<AnimationCurveBlob>(Allocator.Persistent);

            AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value, ref cache);
            Assert.AreEqual(lhsIndex, cache.LhsIndex);
            Assert.AreEqual(rhsIndex, cache.RhsIndex);
        }
    }
}
