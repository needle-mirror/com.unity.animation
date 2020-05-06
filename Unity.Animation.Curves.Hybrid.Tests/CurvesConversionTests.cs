using UnityEngine;
using NUnit.Framework;

using Unity.Animation.Hybrid;

namespace Unity.Animation.Tests
{
    public class CurvesConversionTests
    {
        [TestCase(-1)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.51f)]
        public void TestStepAnimationCurveToBlobAssetRef(float time)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            animCurve.AddKey(new UnityEngine.Keyframe(0, 0, Mathf.Infinity, Mathf.Infinity));
            animCurve.AddKey(new UnityEngine.Keyframe(0.5f, 1, Mathf.Infinity, Mathf.Infinity));
            animCurve.AddKey(new UnityEngine.Keyframe(1, 1, Mathf.Infinity, Mathf.Infinity));
            float expected = animCurve.Evaluate(time);

            var keyframeCurveBlob = animCurve.ToAnimationCurveBlobAssetRef();
            var eval = AnimationCurveEvaluator.Evaluate(time, keyframeCurveBlob);
            Assert.AreEqual(expected, eval);

            keyframeCurveBlob.Dispose();
        }

        [TestCase(-1)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.51f)]
        public void TestStepAnimationCurveToDotsAnimCurve(float time)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            animCurve.AddKey(new UnityEngine.Keyframe(0, 0, Mathf.Infinity, Mathf.Infinity));
            animCurve.AddKey(new UnityEngine.Keyframe(0.5f, 1, Mathf.Infinity, Mathf.Infinity));
            animCurve.AddKey(new UnityEngine.Keyframe(1, 1, Mathf.Infinity, Mathf.Infinity));
            float expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();
            var eval = AnimationCurveEvaluator.Evaluate(time, ref dotsCurve);
            Assert.AreEqual(expected, eval);

            dotsCurve.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestLinearAnimationCurveToBlobAssetRef(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(0, 0, 1, 1);
            float expected = animCurve.Evaluate(time);

            var keyframeCurveBlob = animCurve.ToAnimationCurveBlobAssetRef();
            var eval = AnimationCurveEvaluator.Evaluate(time, keyframeCurveBlob);
            Assert.AreEqual(expected, eval);

            keyframeCurveBlob.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestLinearAnimationCurveToDotsAnimCurve(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(0, 0, 1, 1);
            float expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();
            var eval = AnimationCurveEvaluator.Evaluate(time, ref dotsCurve);
            Assert.AreEqual(expected, eval);

            dotsCurve.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestEaseInOutAnimationCurveToBlobAssetRef(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.EaseInOut(0, 0, 1, 1);
            float expected = animCurve.Evaluate(time);

            var keyframeCurveBlob = animCurve.ToAnimationCurveBlobAssetRef();
            var eval = AnimationCurveEvaluator.Evaluate(time, keyframeCurveBlob);
            Assert.AreEqual(expected, eval);

            keyframeCurveBlob.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestEaseInOutAnimationCurveToDotsAnimCurve(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.EaseInOut(0, 0, 1, 1);
            float expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();
            var eval = AnimationCurveEvaluator.Evaluate(time, ref dotsCurve);
            Assert.AreEqual(expected, eval);

            dotsCurve.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestConstantAnimationCurveToBlobAssetRef(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Constant(0, 1, 1);
            float expected = animCurve.Evaluate(time);

            var keyframeCurveBlob = animCurve.ToAnimationCurveBlobAssetRef();
            var eval = AnimationCurveEvaluator.Evaluate(time, keyframeCurveBlob);
            Assert.AreEqual(expected, eval);

            keyframeCurveBlob.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestConstantAnimationCurveToDotsAnimCurve(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Constant(0, 1, 1);
            float expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();
            var eval = AnimationCurveEvaluator.Evaluate(time, ref dotsCurve);
            Assert.AreEqual(expected, eval);

            dotsCurve.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestComplexAnimationCurveToBlobAssetRef(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(10, -10, 100, -10);

            // Only supports clamp
            animCurve.preWrapMode = WrapMode.Clamp;
            animCurve.postWrapMode = WrapMode.Clamp;

            animCurve.AddKey(new UnityEngine.Keyframe() { time = 50, value = 100 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 75, value = -30 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 25, value = -30 });

            float expected = animCurve.Evaluate(time);

            var keyframeCurveBlob = animCurve.ToAnimationCurveBlobAssetRef();
            var eval = AnimationCurveEvaluator.Evaluate(time, keyframeCurveBlob);
            Assert.AreEqual(expected, eval);

            keyframeCurveBlob.Dispose();
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0.5f)]
        [TestCase(0.7f)]
        public void TestComplexAnimationCurveToDotsAnimCurve(float time)
        {
            var animCurve = UnityEngine.AnimationCurve.Linear(10, -10, 100, -10);

            // Only supports clamp
            animCurve.preWrapMode = WrapMode.Clamp;
            animCurve.postWrapMode = WrapMode.Clamp;

            animCurve.AddKey(new UnityEngine.Keyframe() { time = 50, value = 100 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 75, value = -30 });
            animCurve.AddKey(new UnityEngine.Keyframe() { time = 25, value = -30 });

            float expected = animCurve.Evaluate(time);

            var dotsCurve = animCurve.ToDotsAnimationCurve();
            var eval = AnimationCurveEvaluator.Evaluate(time, ref dotsCurve);
            Assert.AreEqual(expected, eval);

            dotsCurve.Dispose();
        }
    }
}
