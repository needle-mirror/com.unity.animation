using NUnit.Framework;

using Unity.Animation.Hybrid;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    public class CurvesPrecisionTests
    {
        private const float kTolerance = 1e-7f;

        [TestCase(0f)]
        [TestCase(5f)]
        [TestCase(100_001.95f)]
        [TestCase(250_000f)]
        [TestCase(300_003.05f)]
        [TestCase(499_995f)]
        [TestCase(500_000f)]
        public void TestLinearCurve1Precision(float time)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            animCurve.AddKey(0f, 0f);
            animCurve.AddKey(500_000f, 1f);

            var keyframeCurve = animCurve.ToAnimationCurveBlobAssetRef();
            var eval = AnimationCurveEvaluator.Evaluate(time, keyframeCurve);
            var expected = time / 500_000f;
            Assert.IsTrue(NearlyEqual(expected, eval, kTolerance), $"Expected {expected:n6} but was {eval:n6}.");

            keyframeCurve.Dispose();
        }

        [TestCase(0f)]
        [TestCase(5f)]
        [TestCase(100_001.95f)]
        [TestCase(250_000f)]
        [TestCase(300_003.05f)]
        [TestCase(499_995f)]
        [TestCase(500_000f)]
        public void TestLinearCurve1000Precision(float time)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            animCurve.AddKey(0f, 0f);
            animCurve.AddKey(500_000f, 1_000f);

            var keyframeCurve = animCurve.ToAnimationCurveBlobAssetRef();
            var eval = AnimationCurveEvaluator.Evaluate(time, keyframeCurve);
            var expected = time / 500f;
            Assert.IsTrue(NearlyEqual(expected, eval, kTolerance), $"Expected {expected:n6} but was {eval:n6}.");

            keyframeCurve.Dispose();
        }

        [TestCase(0f)]
        [TestCase(1f)]
        [TestCase(101.95f)]
        [TestCase(250f)]
        [TestCase(303.05f)]
        [TestCase(499f)]
        [TestCase(500f)]
        public void TestLinearCurve500Precision(float time)
        {
            var animCurve = new UnityEngine.AnimationCurve();
            animCurve.AddKey(0f, 0f);
            animCurve.AddKey(500f, 1f);

            var keyframeCurve = animCurve.ToAnimationCurveBlobAssetRef();
            var eval = AnimationCurveEvaluator.Evaluate(time, keyframeCurve);
            var expected = time / 500f;
            Assert.IsTrue(NearlyEqual(expected, eval, kTolerance), $"Expected {expected:n6} but was {eval:n6}.");

            keyframeCurve.Dispose();
        }

        private bool NearlyEqual(float a, float b, float epsilon)
        {
            float absA = math.abs(a);
            float absB = math.abs(b);
            float diff = math.abs(a - b);

            if (a == b)
            {
                return true;
            }
            else if (a == 0 || b == 0 || absA + absB < math.FLT_MIN_NORMAL)
            {
                return diff < (epsilon * math.FLT_MIN_NORMAL);
            }
            else
            {
                return diff / (absA + absB) < epsilon;
            }
        }
    }
}
