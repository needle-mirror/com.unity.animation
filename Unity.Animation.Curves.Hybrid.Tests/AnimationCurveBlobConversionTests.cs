using UnityEngine;
using NUnit.Framework;

using Unity.Animation.Hybrid;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation.Tests
{
    public class AnimationCurveBlobConversionTests
    {
        [TestCase(-1)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.51f)]
        public void TestStepAnimationCurveToAnimationCurveBlob(float time)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            animCurve.AddKey(new UnityEngine.Keyframe(0, 0, Mathf.Infinity, Mathf.Infinity));
            animCurve.AddKey(new UnityEngine.Keyframe(0.5f, 1, Mathf.Infinity, Mathf.Infinity));
            animCurve.AddKey(new UnityEngine.Keyframe(1, 1, Mathf.Infinity, Mathf.Infinity));
            float expected = animCurve.Evaluate(time);

            var cache = new AnimationCurveCache();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curveBlob = ref blobBuilder.ConstructRoot<AnimationCurveBlob>();
            CurveConversion.FillAnimationCurveBlob(animCurve, ref blobBuilder, ref curveBlob);

            var blobAsset = blobBuilder.CreateBlobAssetReference<AnimationCurveBlob>(Allocator.Persistent);

            var eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value, ref cache);
            Assert.AreEqual(expected, eval);

            blobBuilder.Dispose();
            blobAsset.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestLinearAnimationCurveToAnimationCurveBlob(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(0, 2, 1, 3);
            float expected = animCurve.Evaluate(time);

            var cache = new AnimationCurveCache();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curveBlob = ref blobBuilder.ConstructRoot<AnimationCurveBlob>();
            CurveConversion.FillAnimationCurveBlob(animCurve, ref blobBuilder, ref curveBlob);

            var blobAsset = blobBuilder.CreateBlobAssetReference<AnimationCurveBlob>(Allocator.Persistent);

            var eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value, ref cache);
            Assert.AreEqual(expected, eval);

            blobBuilder.Dispose();
            blobAsset.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestEaseInOutAnimationCurveToAnimationCurveBlob(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.EaseInOut(0, 2, 1, 3);
            float expected = animCurve.Evaluate(time);

            var cache = new AnimationCurveCache();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curveBlob = ref blobBuilder.ConstructRoot<AnimationCurveBlob>();
            CurveConversion.FillAnimationCurveBlob(animCurve, ref blobBuilder, ref curveBlob);

            var blobAsset = blobBuilder.CreateBlobAssetReference<AnimationCurveBlob>(Allocator.Persistent);

            var eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value, ref cache);
            Assert.AreEqual(expected, eval);

            blobBuilder.Dispose();
            blobAsset.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestConstantAnimationCurveToAnimationCurveBlob(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Constant(0, 1, 1.5f);
            float expected = animCurve.Evaluate(time);

            var cache = new AnimationCurveCache();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curveBlob = ref blobBuilder.ConstructRoot<AnimationCurveBlob>();
            CurveConversion.FillAnimationCurveBlob(animCurve, ref blobBuilder, ref curveBlob);

            var blobAsset = blobBuilder.CreateBlobAssetReference<AnimationCurveBlob>(Allocator.Persistent);

            var eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value, ref cache);
            Assert.AreEqual(expected, eval);

            blobBuilder.Dispose();
            blobAsset.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestComplexAnimationCurveToAnimationCurveBlob(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(10, -10, 100, -10);

            // Only supports clamp
            animCurve.preWrapMode = WrapMode.Clamp;
            animCurve.postWrapMode = WrapMode.Clamp;

            animCurve.AddKey(new UnityEngine.Keyframe() { time = 50, value = 100 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 75, value = -30 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 25, value = -30 });

            float expected = animCurve.Evaluate(time);

            var cache = new AnimationCurveCache();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curveBlob = ref blobBuilder.ConstructRoot<AnimationCurveBlob>();
            CurveConversion.FillAnimationCurveBlob(animCurve, ref blobBuilder, ref curveBlob);

            var blobAsset = blobBuilder.CreateBlobAssetReference<AnimationCurveBlob>(Allocator.Persistent);

            var eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value, ref cache);
            Assert.AreEqual(expected, eval);

            blobBuilder.Dispose();
            blobAsset.Dispose();
        }

        struct MultipleCurvesBlob
        {
            public AnimationCurveBlob LinearCurve;
            public AnimationCurveBlob ConstantCurve;
            public AnimationCurveBlob EaseCurve;
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestMultipleCurveBlob(float time)
        {
            var linearCurve = UnityEngine.AnimationCurve.Linear(0, 2, 1, 3);
            var constantCurve = UnityEngine.AnimationCurve.Constant(0, 1, 1.5f);
            var easeCurve = UnityEngine.AnimationCurve.EaseInOut(0, -1, 1, 9);

            var cache = new AnimationCurveCache();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curveBlob = ref blobBuilder.ConstructRoot<MultipleCurvesBlob>();

            CurveConversion.FillAnimationCurveBlob(linearCurve, ref blobBuilder, ref curveBlob.LinearCurve);
            CurveConversion.FillAnimationCurveBlob(constantCurve, ref blobBuilder, ref curveBlob.ConstantCurve);
            CurveConversion.FillAnimationCurveBlob(easeCurve, ref blobBuilder, ref curveBlob.EaseCurve);
            var blobAsset = blobBuilder.CreateBlobAssetReference<MultipleCurvesBlob>(Allocator.Persistent);

            float expected = linearCurve.Evaluate(time);
            var eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value.LinearCurve, ref cache);
            Assert.AreEqual(expected, eval);

            expected = constantCurve.Evaluate(time);
            eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value.ConstantCurve, ref cache);
            Assert.AreEqual(expected, eval);

            expected = easeCurve.Evaluate(time);
            eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value.EaseCurve, ref cache);
            Assert.AreEqual(expected, eval);

            blobBuilder.Dispose();
            blobAsset.Dispose();
        }

        struct ArrayOfCurveBlobs
        {
            public BlobArray<AnimationCurveBlob> CurveBlobs;
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestArrayOfCurveBlobs(float time)
        {
            var linearCurve = UnityEngine.AnimationCurve.Linear(0, 2, 1, 3);
            var constantCurve = UnityEngine.AnimationCurve.Constant(0, 1, 1.5f);
            var easeCurve = UnityEngine.AnimationCurve.EaseInOut(0, 2, 1, 3);

            var cache = new AnimationCurveCache();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curveBlob = ref blobBuilder.ConstructRoot<ArrayOfCurveBlobs>();
            var curvesArray = blobBuilder.Allocate(ref curveBlob.CurveBlobs, 3);

            CurveConversion.FillAnimationCurveBlob(linearCurve, ref blobBuilder, ref curvesArray[0]);
            CurveConversion.FillAnimationCurveBlob(constantCurve, ref blobBuilder, ref curvesArray[1]);
            CurveConversion.FillAnimationCurveBlob(easeCurve, ref blobBuilder, ref curvesArray[2]);
            var blobAsset = blobBuilder.CreateBlobAssetReference<ArrayOfCurveBlobs>(Allocator.Persistent);

            float expected = linearCurve.Evaluate(time);
            var eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value.CurveBlobs[0], ref cache);
            Assert.AreEqual(expected, eval);

            expected = constantCurve.Evaluate(time);
            eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value.CurveBlobs[1], ref cache);
            Assert.AreEqual(expected, eval);

            expected = easeCurve.Evaluate(time);
            eval = AnimationCurveEvaluator.Evaluate(time, ref blobAsset.Value.CurveBlobs[2], ref cache);
            Assert.AreEqual(expected, eval);

            blobBuilder.Dispose();
            blobAsset.Dispose();
        }
    }
}
