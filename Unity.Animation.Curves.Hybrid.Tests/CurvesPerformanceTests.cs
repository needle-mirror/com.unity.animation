using UnityEngine;
using NUnit.Framework;

using Unity.Animation.Hybrid;
using Unity.PerformanceTesting;

namespace Unity.Animation.PerformanceTests
{
    public class CurvesPerformancesTest
    {
        [Test, Performance]
        [TestCase(1, 2)]
        [TestCase(100, 2)]
        [TestCase(0.1f, 1000)]
        [TestCase(1, 1000)]
        [TestCase(50, 100)]
        public void TestForwardCurveWithCache(float timeStep, int keyframeCount)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            for (var i = 0; i < keyframeCount; ++i)
            {
                animCurve.AddKey(new UnityEngine.Keyframe(i, i * timeStep, Mathf.Infinity, Mathf.Infinity));
            }

            var curveBlob = animCurve.ToAnimationCurveBlobAssetRef();
            var curve = new Unity.Animation.AnimationCurve();
            curve.SetAnimationCurveBlobAssetRef(curveBlob);

            Measure.Method(() =>
            {
                // The longer the time step, the more the cache gets reused.
                for (var t = 0.0f; t <= keyframeCount * timeStep; t += 0.1f)
                {
                    AnimationCurveEvaluator.Evaluate(t, ref curve);
                }
            })
                .WarmupCount(1)
                .MeasurementCount(100)
                .Run();

            curveBlob.Dispose();
        }

        [Test, Performance]
        [TestCase(1, 2)]
        [TestCase(100, 2)]
        [TestCase(0.1f, 1000)]
        [TestCase(1, 1000)]
        [TestCase(50, 100)]
        public void TestForwardCurveWithoutCache(float timeStep, int keyframeCount)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            for (var i = 0; i < keyframeCount; ++i)
            {
                animCurve.AddKey(new UnityEngine.Keyframe(i, i * timeStep, Mathf.Infinity, Mathf.Infinity));
            }

            var curveBlob = animCurve.ToAnimationCurveBlobAssetRef();

            Measure.Method(() =>
            {
                // The longer the time step, the more the cache gets reused.
                for (var t = 0.0f; t <= keyframeCount * timeStep; t += 0.1f)
                {
                    AnimationCurveEvaluator.Evaluate(t, curveBlob);
                }
            })
                .WarmupCount(1)
                .MeasurementCount(100)
                .Run();

            curveBlob.Dispose();
        }

        [Test, Performance]
        [TestCase(1, 2)]
        [TestCase(100, 2)]
        [TestCase(0.1f, 1000)]
        [TestCase(1, 1000)]
        [TestCase(50, 100)]
        public void TestBackwardCurveWithCache(float timeStep, int keyframeCount)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            for (var i = 0; i < keyframeCount; ++i)
            {
                animCurve.AddKey(new UnityEngine.Keyframe(i, i * timeStep, Mathf.Infinity, Mathf.Infinity));
            }

            var curveBlob = animCurve.ToAnimationCurveBlobAssetRef();
            var curve = new Unity.Animation.AnimationCurve();
            curve.SetAnimationCurveBlobAssetRef(curveBlob);

            Measure.Method(() =>
            {
                // The longer the time step, the more the cache gets reused.
                for (var t = keyframeCount * timeStep; t >= 0.0f; t -= 0.1f)
                {
                    AnimationCurveEvaluator.Evaluate(t, ref curve);
                }
            })
                .WarmupCount(1)
                .MeasurementCount(100)
                .Run();

            curveBlob.Dispose();
        }

        [Test, Performance]
        [TestCase(1, 2)]
        [TestCase(100, 2)]
        [TestCase(0.1f, 1000)]
        [TestCase(1, 1000)]
        [TestCase(50, 100)]
        public void TestBackwardCurveWithoutCache(float timeStep, int keyframeCount)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            for (var i = 0; i < keyframeCount; ++i)
            {
                animCurve.AddKey(new UnityEngine.Keyframe(i, i * timeStep, Mathf.Infinity, Mathf.Infinity));
            }

            var curveBlob = animCurve.ToAnimationCurveBlobAssetRef();

            Measure.Method(() =>
            {
                // The longer the time step, the more the cache gets reused.
                for (var t = keyframeCount * timeStep; t >= 0.0f; t -= 0.1f)
                {
                    AnimationCurveEvaluator.Evaluate(t, curveBlob);
                }
            })
                .WarmupCount(1)
                .MeasurementCount(100)
                .Run();

            curveBlob.Dispose();
        }
    }
}
