using NUnit.Framework;
using UnityEngine;

namespace Unity.Animation.Tests
{
    public class ClipBuilderTests : AnimationTestsFixture
    {
        [Test]
        [Description("Verify AnimationClip and DenseClip have the same frame rate, frame count, and bindings.")]
        public void CheckAnimationClipAndDenseClipHaveSameData()
        {
            var clip = new AnimationClip();
            var constantCurve = AnimationCurve.Constant(0.0f, 1.0f, 0.0f);

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

            var denseClip = ClipBuilder.AnimationClipToDenseClip(clip);
            var frameCount = (int) (clip.length * clip.frameRate);

            Assert.That(denseClip.Value.SampleRate, Is.EqualTo(clip.frameRate));
            Assert.That(denseClip.Value.FrameCount, Is.EqualTo(frameCount));

            ref var bindings = ref denseClip.Value.Bindings;

            Assert.That(bindings.TranslationBindings.Length, Is.EqualTo(3));
            Assert.That(bindings.TranslationBindings[0], Is.EqualTo(new StringHash("")));
            Assert.That(bindings.TranslationBindings[1], Is.EqualTo(new StringHash("Child1")));
            Assert.That(bindings.TranslationBindings[2], Is.EqualTo(new StringHash("Child2")));

            Assert.That(bindings.RotationBindings.Length, Is.EqualTo(2));
            Assert.That(bindings.RotationBindings[0], Is.EqualTo(new StringHash("")));
            Assert.That(bindings.RotationBindings[1], Is.EqualTo(new StringHash("Child1")));

            Assert.That(bindings.ScaleBindings.Length, Is.EqualTo(1));
            Assert.That(bindings.ScaleBindings[0], Is.EqualTo(new StringHash("")));

            Assert.That(bindings.FloatBindings.Length, Is.EqualTo(0));
            Assert.That(bindings.IntBindings.Length, Is.EqualTo(0));
        }

        [Test]
        [Description("Verify DenseClip always have 3 curves per translation, 4 per rotation, and 3 per scale.")]
        public void CheckDenseClipHasAllComponentsPerBindingType()
        {
            var clip = new AnimationClip();
            var constantCurve = AnimationCurve.Constant(0.0f, 1.0f, 0.0f);

            clip.SetCurve("", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalRotation.x", constantCurve);
            clip.SetCurve("", typeof(Transform), "m_LocalScale.x", constantCurve);

            var denseClip = ClipBuilder.AnimationClipToDenseClip(clip);
            ref var bindings = ref denseClip.Value.Bindings;

            Assert.That(bindings.TranslationBindings.Length, Is.EqualTo(1));
            Assert.That(bindings.RotationBindings.Length, Is.EqualTo(1));
            Assert.That(bindings.ScaleBindings.Length, Is.EqualTo(1));

            Assert.That(bindings.TranslationCurveCount, Is.EqualTo(3));
            Assert.That(bindings.RotationCurveCount, Is.EqualTo(4));
            Assert.That(bindings.ScaleCurveCount, Is.EqualTo(3));
        }
    }
}
