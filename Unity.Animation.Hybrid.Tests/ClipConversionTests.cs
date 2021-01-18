using NUnit.Framework;
using UnityEngine;
using Unity.Animation.Hybrid;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation.Tests
{
    public class ClipConversionTests : AnimationTestsFixture
    {
        bool HasPropertyHash(StringHash propertyHash, ref BlobArray<StringHash> bindingsArray)
        {
            return Core.FindBindingIndex(ref bindingsArray, propertyHash) >= 0;
        }

        [Test]
        [Description("Verify AnimationClip and DenseClip have the same frame rate, frame count, and bindings.")]
        public void CheckAnimationClipAndDenseClipHaveSameData()
        {
            var clip = CreateAnimationClip();
            var constantCurve = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);

            clip.SetCurve("", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalPosition.z", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalRotation.x", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalRotation.y", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalRotation.z", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalRotation.w", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.x", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.y", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.z", constantCurve);

            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.z", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.x", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.y", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.z", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.w", constantCurve);

            clip.SetCurve("Child2", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalPosition.z", constantCurve);

            // Test for localEulerAnglesRaw
            clip.SetCurve("Child3", typeof(Transform), "localEulerAnglesRaw.x", constantCurve);
            clip.SetCurve("Child3", typeof(Transform), "localEulerAnglesRaw.y", constantCurve);
            clip.SetCurve("Child3", typeof(Transform), "localEulerAnglesRaw.z", constantCurve);

            // Test for localEulerAngles
            clip.SetCurve("Child4", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Child4", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Child4", typeof(Transform), "m_LocalPosition.z", constantCurve);
            clip.SetCurve("Child4", typeof(Transform), "localEulerAngles.x", constantCurve);
            clip.SetCurve("Child4", typeof(Transform), "localEulerAngles.y", constantCurve);
            clip.SetCurve("Child4", typeof(Transform), "localEulerAngles.z", constantCurve);

            var denseClip = clip.ToDenseClip();
            var frameCount = (int)(clip.length * clip.frameRate);

            Assert.That(denseClip.Value.SampleRate, Is.EqualTo(clip.frameRate));
            Assert.That(denseClip.Value.FrameCount, Is.EqualTo(frameCount));

            ref var bindings = ref denseClip.Value.Bindings;

            Assert.That(bindings.TranslationBindings.Length, Is.EqualTo(4));
            Assert.That(HasPropertyHash(new StringHash(""), ref bindings.TranslationBindings));
            Assert.That(HasPropertyHash(new StringHash("Child1"), ref bindings.TranslationBindings));
            Assert.That(HasPropertyHash(new StringHash("Child2"), ref bindings.TranslationBindings));
            Assert.That(HasPropertyHash(new StringHash("Child4"), ref bindings.TranslationBindings));


            Assert.That(bindings.RotationBindings.Length, Is.EqualTo(4));
            Assert.That(HasPropertyHash(new StringHash(""), ref bindings.RotationBindings));
            Assert.That(HasPropertyHash(new StringHash("Child1"), ref bindings.RotationBindings));
            Assert.That(HasPropertyHash(new StringHash("Child3"), ref bindings.RotationBindings));
            Assert.That(HasPropertyHash(new StringHash("Child4"), ref bindings.RotationBindings));

            Assert.That(bindings.ScaleBindings.Length, Is.EqualTo(1));
            Assert.That(bindings.ScaleBindings[0], Is.EqualTo(new StringHash("")));

            Assert.That(bindings.FloatBindings.Length, Is.EqualTo(0));
            Assert.That(bindings.IntBindings.Length, Is.EqualTo(0));

            denseClip.Dispose();
        }

        [Test]
        [Description("Verify DenseClip always have 3 curves per translation, 4 per rotation, and 3 per scale.")]
        public void CheckDenseClipHasAllComponentsPerBindingType()
        {
            var clip = CreateAnimationClip();
            var constantCurve = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);

            clip.SetCurve("", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalRotation.x", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.x", constantCurve);

            var denseClip = clip.ToDenseClip();
            ref var bindings = ref denseClip.Value.Bindings;

            Assert.That(bindings.TranslationBindings.Length, Is.EqualTo(1));
            Assert.That(bindings.RotationBindings.Length, Is.EqualTo(1));
            Assert.That(bindings.ScaleBindings.Length, Is.EqualTo(1));

            Assert.That(bindings.TranslationCurveCount, Is.EqualTo(3));
            Assert.That(bindings.RotationCurveCount, Is.EqualTo(4));
            Assert.That(bindings.ScaleCurveCount, Is.EqualTo(3));

            denseClip.Dispose();
        }

        [Test]
        [Description("Verify DenseClip has 4 curves for each Euler rotation binding.")]
        public void CheckDenseClipHasEulerRotationBinding()
        {
            var clip = CreateAnimationClip();
            var constantCurve = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);

            clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.x", constantCurve);
            clip.SetCurve("Child", typeof(Transform), "localEulerAngles.x", constantCurve);

            var denseClip = clip.ToDenseClip();
            ref var bindings = ref denseClip.Value.Bindings;

            Assert.That(bindings.RotationBindings.Length, Is.EqualTo(2));

            Assert.That(bindings.RotationCurveCount, Is.EqualTo(8));

            denseClip.Dispose();
        }

        [Test]
        [Description("Verify that the Euler conversion to quaternion gives the right values.")]
        public void CheckEulerValuesAreCorrectlyConverted()
        {
            var clip = CreateAnimationClip();
            var xEulerCurve = UnityEngine.AnimationCurve.Linear(0.0f, 45.0f, 1.0f, 60.0f);
            var yEulerCurve = UnityEngine.AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 90.0f);
            var zEulerCurve = UnityEngine.AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 180.0f);
            clip.SetCurve("", typeof(Transform), "localEulerAngles.x", xEulerCurve);
            clip.SetCurve("", typeof(Transform), "localEulerAngles.y", yEulerCurve);
            clip.SetCurve("", typeof(Transform), "localEulerAngles.z", zEulerCurve);

            var denseClip = clip.ToDenseClip();
            ref var bindings = ref denseClip.Value.Bindings;
            ref var samples = ref denseClip.Value.Samples;

            for (int frameIter = 0, sampleIndex = bindings.RotationBindingIndex; frameIter < denseClip.Value.FrameCount; frameIter++, sampleIndex += bindings.RotationCurveCount)
            {
                var frameTime = frameIter / denseClip.Value.SampleRate;
                var x = xEulerCurve.Evaluate(frameTime);
                var y = yEulerCurve.Evaluate(frameTime);
                var z = zEulerCurve.Evaluate(frameTime);
                var q = quaternion.Euler(math.radians(x), math.radians(y), math.radians(z));

                var convertedQ = new quaternion(samples[sampleIndex + 0], samples[sampleIndex + 1], samples[sampleIndex + 2], samples[sampleIndex + 3]);

                Assert.That(q, Is.EqualTo(convertedQ).Using(RotationComparer), $"Rotation mismatch at frame {frameIter}");
            }

            denseClip.Dispose();
        }

        [Test]
        [Description("Verify that the Raw Euler conversion to quaternion gives the right values.")]
        public void CheckRawEulerValuesAreCorrectlyConverted()
        {
            var clip = CreateAnimationClip();
            var xEulerCurve = UnityEngine.AnimationCurve.Linear(0.0f, 45.0f, 1.0f, 60.0f);
            var yEulerCurve = UnityEngine.AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 90.0f);
            var zEulerCurve = UnityEngine.AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 180.0f);
            clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.x", xEulerCurve);
            clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", yEulerCurve);
            clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", zEulerCurve);

            var denseClip = clip.ToDenseClip();
            ref var bindings = ref denseClip.Value.Bindings;
            ref var samples = ref denseClip.Value.Samples;

            for (int frameIter = 0, sampleIndex = bindings.RotationBindingIndex;
                 frameIter < denseClip.Value.FrameCount;
                 frameIter++, sampleIndex += bindings.RotationCurveCount)
            {
                var frameTime = frameIter / denseClip.Value.SampleRate;
                var x = xEulerCurve.Evaluate(frameTime);
                var y = yEulerCurve.Evaluate(frameTime);
                var z = zEulerCurve.Evaluate(frameTime);
                var q = quaternion.Euler(math.radians(x), math.radians(y), math.radians(z));

                var convertedQ = new quaternion(samples[sampleIndex + 0], samples[sampleIndex + 1],
                    samples[sampleIndex + 2], samples[sampleIndex + 3]);

                Assert.That(q, Is.EqualTo(convertedQ).Using(RotationComparer), $"Rotation mismatch at frame {frameIter}");
            }

            denseClip.Dispose();
        }
    }
}
