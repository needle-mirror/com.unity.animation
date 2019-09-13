using UnityEngine;
using NUnit.Framework;

using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    public class RuntimeCurvesTest
    {
        public class CurveConversion
        {
            [TestCase(-1, 0)]
            [TestCase(1, 1)]
            [TestCase(2, 1)]
            [TestCase(0.49f, 0)]
            [TestCase(0.5f, 1)]
            public void TestBuiltStepKeyframeCurve(float time, float expected)
            {
                var keyframeCurve = new KeyframeCurve(3, Allocator.Temp);
                keyframeCurve[0] = new Keyframe { InTangent = Mathf.Infinity, OutTangent = Mathf.Infinity, Time = 0, Value = 0 };
                keyframeCurve[1] = new Keyframe { InTangent = Mathf.Infinity, OutTangent = Mathf.Infinity, Time = 0.5f, Value = 1 };
                keyframeCurve[2] = new Keyframe { InTangent = Mathf.Infinity, OutTangent = Mathf.Infinity, Time = 1, Value = 1 };

                var eval = KeyframeCurveEvaluator.Evaluate(time, ref keyframeCurve);
                Assert.AreEqual(expected, eval);

                var keyframeCurveBlob = keyframeCurve.ToBlobAssetRef();
                eval = KeyframeCurveEvaluator.Evaluate(time, keyframeCurveBlob);
                Assert.AreEqual(expected, eval);

                keyframeCurve.Dispose();
                keyframeCurveBlob.Release();
            }

            [TestCase(-1)]
            [TestCase(1)]
            [TestCase(2)]
            [TestCase(0.5f)]
            [TestCase(0.51f)]
            public void TestStepAnimationCurveToKeyframeCurve(float time)
            {
                var animCurve = new AnimationCurve();
                animCurve.AddKey(new UnityEngine.Keyframe(0, 0, Mathf.Infinity, Mathf.Infinity));
                animCurve.AddKey(new UnityEngine.Keyframe(0.5f, 1, Mathf.Infinity, Mathf.Infinity));
                animCurve.AddKey(new UnityEngine.Keyframe(1, 1, Mathf.Infinity, Mathf.Infinity));
                float expected = animCurve.Evaluate(time);

                var keyframeCurve = animCurve.ToKeyframeCurve();
                var eval = KeyframeCurveEvaluator.Evaluate(time, ref keyframeCurve);
                Assert.AreEqual(expected, eval);

                var keyframeCurveBlob = animCurve.ToKeyframeCurveBlob();
                eval = KeyframeCurveEvaluator.Evaluate(time, keyframeCurveBlob);
                Assert.AreEqual(expected, eval);

                keyframeCurve.Dispose();
                keyframeCurveBlob.Release();
            }

            [TestCase(-1)]
            [TestCase(0)]
            [TestCase(1)]
            [TestCase(2)]
            [TestCase(0.5f)]
            [TestCase(0.7f)]
            public void TestLinearAnimationCurveToKeyframeCurve(float time)
            {
                var animCurve = AnimationCurve.Linear(0, 0, 1, 1);
                float expected = animCurve.Evaluate(time);

                var keyframeCurve = animCurve.ToKeyframeCurve();
                var eval = KeyframeCurveEvaluator.Evaluate(time, ref keyframeCurve);
                Assert.AreEqual(expected, eval);

                var keyframeCurveBlob = animCurve.ToKeyframeCurveBlob();
                eval = KeyframeCurveEvaluator.Evaluate(time, keyframeCurveBlob);
                Assert.AreEqual(expected, eval);

                keyframeCurve.Dispose();
                keyframeCurveBlob.Release();
            }

            [TestCase(-1)]
            [TestCase(0)]
            [TestCase(1)]
            [TestCase(2)]
            [TestCase(0.5f)]
            [TestCase(0.7f)]
            public void TestEaseInOutAnimationCurveToKeyframeCurve(float time)
            {
                AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
                float expected = animCurve.Evaluate(time);

                var keyframeCurve = animCurve.ToKeyframeCurve();
                var eval = KeyframeCurveEvaluator.Evaluate(time, ref keyframeCurve);
                Assert.AreEqual(expected, eval);

                var keyframeCurveBlob = animCurve.ToKeyframeCurveBlob();
                eval = KeyframeCurveEvaluator.Evaluate(time, keyframeCurveBlob);
                Assert.AreEqual(expected, eval);

                keyframeCurve.Dispose();
                keyframeCurveBlob.Release();
            }

            [TestCase(-1)]
            [TestCase(0)]
            [TestCase(1)]
            [TestCase(2)]
            [TestCase(0.5f)]
            [TestCase(0.7f)]
            public void TestConstantAnimationCurveToKeyframeCurve(float time)
            {
                AnimationCurve animCurve = AnimationCurve.Constant(0, 1, 1);
                float expected = animCurve.Evaluate(time);

                var keyframeCurve = animCurve.ToKeyframeCurve();
                var eval = KeyframeCurveEvaluator.Evaluate(time, ref keyframeCurve);
                Assert.AreEqual(expected, eval);

                var keyframeCurveBlob = animCurve.ToKeyframeCurveBlob();
                eval = KeyframeCurveEvaluator.Evaluate(time, keyframeCurveBlob);
                Assert.AreEqual(expected, eval);

                keyframeCurve.Dispose();
                keyframeCurveBlob.Release();
            }

            [TestCase(-1)]
            [TestCase(0)]
            [TestCase(1)]
            [TestCase(2)]
            [TestCase(0.5f)]
            [TestCase(0.7f)]
            public void TestComplexAnimationCurveToKeyframeCurve(float time)
            {
                var animCurve = AnimationCurve.Linear(10, -10, 100, -10);

                // Only supports clamp
                animCurve.preWrapMode = WrapMode.Clamp;
                animCurve.postWrapMode = WrapMode.Clamp;

                animCurve.AddKey(new UnityEngine.Keyframe() { time = 50, value = 100 });
                animCurve.AddKey(new UnityEngine.Keyframe() { time = 75, value = -30 });
                animCurve.AddKey(new UnityEngine.Keyframe() { time = 25, value = -30 });

                float expected = animCurve.Evaluate(time);

                var keyframeCurve = animCurve.ToKeyframeCurve();
                var eval = KeyframeCurveEvaluator.Evaluate(time, ref keyframeCurve);
                Assert.AreEqual(expected, eval);

                var keyframeCurveBlob = animCurve.ToKeyframeCurveBlob();
                eval = KeyframeCurveEvaluator.Evaluate(time, keyframeCurveBlob);
                Assert.AreEqual(expected, eval);

                keyframeCurve.Dispose();
                keyframeCurveBlob.Release();
            }
        }

        public class CurvePrecision
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
                var animCurve = new AnimationCurve();
                animCurve.AddKey(0f, 0f);
                animCurve.AddKey(500_000f, 1f);

                var keyframeCurve = animCurve.ToKeyframeCurve();
                var eval = KeyframeCurveEvaluator.Evaluate(time, ref keyframeCurve);
                var expected = time / 500_000f;
                Assert.IsTrue(NearlyEqual(expected , eval, kTolerance), $"Expected {expected:n6} but was {eval:n6}.");

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
                var animCurve = new AnimationCurve();
                animCurve.AddKey(0f, 0f);
                animCurve.AddKey(500_000f, 1_000f);

                var keyframeCurve = animCurve.ToKeyframeCurve();
                var eval = KeyframeCurveEvaluator.Evaluate(time, ref keyframeCurve);
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
                var animCurve = new AnimationCurve();
                animCurve.AddKey(0f, 0f);
                animCurve.AddKey(500f, 1f);

                var keyframeCurve = animCurve.ToKeyframeCurve();
                var eval = KeyframeCurveEvaluator.Evaluate(time, ref keyframeCurve);
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
}
