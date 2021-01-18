using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Tests
{
    public class ClipManagerTests : AnimationTestsFixture
    {
        private BlobAssetReference<RigDefinition> m_Rig;

        private static BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode {ParentIndex = -1, Id = "Root", AxisIndex = -1},
                new SkeletonNode {ParentIndex = 0, Id = "Child1", AxisIndex = -1},
                new SkeletonNode {ParentIndex = 0, Id = "Child2", AxisIndex = -1}
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }

        private static void CheckClipInstanceTranslationBindings(ref ClipInstance clipInstance, ref RigDefinition rig)
        {
            ref var clipBindings = ref clipInstance.Clip.Bindings.TranslationBindings;
            ref var rigBindings = ref rig.Bindings.TranslationBindings;

            var rigIndex = clipInstance.TranslationBindingMap[0];
            Assert.That(clipBindings[0], Is.EqualTo(rigBindings[rigIndex]));

            for (var i = 1; i < clipBindings.Length; ++i)
            {
                var prevRigIndex = rigIndex;
                rigIndex = clipInstance.TranslationBindingMap[i];
                Assert.That(rigIndex, Is.GreaterThan(prevRigIndex));
                Assert.That(clipBindings[i], Is.EqualTo(rigBindings[rigIndex]));
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_Rig = CreateTestRigDefinition();
        }

        [Test]
        [Description("Test that the bindings set in the clip instance is the intersection of the rig and the clip bindings sets. Case: same bindings set in both.")]
        public void BindingIntersection_ClipAndRigHaveSameBindings()
        {
            var clip = new AnimationClip();
            var constantCurve = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.z", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.z", constantCurve);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalPosition.z", constantCurve);
            var denseClip = clip.ToDenseClip();

            var clipInstance = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            ref var bindings = ref clipInstance.Value.Clip.Bindings;

            // Verify instance bindings.
            var expectedBindingCount = m_Rig.Value.Bindings.TranslationBindings.Length;
            Assert.That(bindings.TranslationBindings.Length, Is.EqualTo(expectedBindingCount));
            CheckClipInstanceTranslationBindings(ref clipInstance.Value, ref m_Rig.Value);
        }

        [Test]
        [Description("Test that the bindings set in the clip instance is the intersection of the rig and the clip bindings sets. Case: clip has more bindings than rig.")]
        public void BindingIntersection_ClipHasMoreBindingsThanRig()
        {
            var clip = new AnimationClip();
            var constantCurve = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.z", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.z", constantCurve);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalPosition.z", constantCurve);
            clip.SetCurve("Child3", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Child3", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Child3", typeof(Transform), "m_LocalPosition.z", constantCurve);
            var denseClip = clip.ToDenseClip();

            var clipInstance = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            ref var bindings = ref clipInstance.Value.Clip.Bindings;

            // Verify instance bindings.
            var expectedBindingCount = m_Rig.Value.Bindings.TranslationBindings.Length;
            Assert.That(bindings.TranslationBindings.Length, Is.EqualTo(expectedBindingCount));
            CheckClipInstanceTranslationBindings(ref clipInstance.Value, ref m_Rig.Value);
        }

        [Test]
        [Description("Test that the bindings set in the clip instance is the intersection of the rig and the clip bindings sets. Case: clip has less bindings.")]
        public void BindingIntersection_ClipHasLessBindingsThanRig()
        {
            var clip = new AnimationClip();
            var constantCurve = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.z", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.z", constantCurve);
            var denseClip = clip.ToDenseClip();

            var clipInstance = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            ref var bindings = ref clipInstance.Value.Clip.Bindings;

            // Verify instance bindings.
            var expectedBindingCount = denseClip.Value.Bindings.TranslationBindings.Length;
            Assert.That(bindings.TranslationBindings.Length, Is.EqualTo(expectedBindingCount));
            Assert.That(clipInstance.Value.TranslationBindingMap.Length, Is.EqualTo(expectedBindingCount));
            CheckClipInstanceTranslationBindings(ref clipInstance.Value, ref m_Rig.Value);
        }

        [Test]
        [Description("Test that the bindings set in the clip instance is the intersection of the rig and the clip bindings sets. Case: clip has less bindings.")]
        public void BindingIntersection_ClipHasDifferentBindingsThanRig()
        {
            var clip = new AnimationClip();
            var constantCurve = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.z", constantCurve);
            clip.SetCurve("NotInRig", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("NotInRig", typeof(Transform), "m_LocalPosition.y", constantCurve);
            clip.SetCurve("NotInRig", typeof(Transform), "m_LocalPosition.z", constantCurve);
            var denseClip = clip.ToDenseClip();

            var clipInstance = ClipManager.Instance.GetClipFor(m_Rig, denseClip);
            ref var bindings = ref clipInstance.Value.Clip.Bindings;

            // Verify instance bindings.
            const int expectedBindingCount = 1;
            Assert.That(bindings.TranslationBindings.Length, Is.EqualTo(expectedBindingCount));
            Assert.That(clipInstance.Value.TranslationBindingMap.Length, Is.EqualTo(expectedBindingCount));
            CheckClipInstanceTranslationBindings(ref clipInstance.Value, ref m_Rig.Value);
        }

        private static BlobAssetReference<RigDefinition> CreateTestFullRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode {ParentIndex = -1, Id = "Root", AxisIndex = -1},
                new SkeletonNode {ParentIndex = 0, Id = "Child1", AxisIndex = -1},
                new SkeletonNode {ParentIndex = 0, Id = "Child2", AxisIndex = -1}
            };

            var animationChannels = new IAnimationChannel[]
            {
                new FloatChannel {DefaultValue = 1.0f, Id = GenericChannelID("floatVar", "Float", typeof(Dummy)) },
                new IntChannel {DefaultValue = 1, Id = GenericChannelID("intVar", "Int", typeof(Dummy)) }
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannels);
        }

        [Test]
        public void ClipInstanceHasAllBindings()
        {
            var clip = new AnimationClip();
            var constantCurve = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.x", constantCurve);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.x", constantCurve);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalScale.x", constantCurve);
            clip.SetCurve("Float", typeof(Dummy), "floatVar", constantCurve);
            clip.SetCurve("Int", typeof(Dummy), "intVar", constantCurve);
            var denseClip = clip.ToDenseClip();

            var rig = CreateTestFullRigDefinition();
            var clipInstance = ClipManager.Instance.GetClipFor(rig, denseClip);
            ref var bindings = ref clipInstance.Value.Clip.Bindings;
            ref var denseClipBindings = ref denseClip.Value.Bindings;
            ref var rigBindings = ref rig.Value.Bindings;

            // Verify instance translation bindings.
            Assert.That(bindings.TranslationBindings.Length, Is.EqualTo(denseClipBindings.TranslationBindings.Length));
            for (var i = 0; i < bindings.TranslationBindings.Length; ++i)
            {
                var rigBindingIndex = clipInstance.Value.TranslationBindingMap[i];
                Assert.That(bindings.TranslationBindings[i], Is.EqualTo(rigBindings.TranslationBindings[rigBindingIndex]));
            }

            // Verify instance rotation bindings.
            Assert.That(bindings.RotationBindings.Length, Is.EqualTo(denseClipBindings.RotationBindings.Length));
            for (var i = 0; i < bindings.RotationBindings.Length; ++i)
            {
                var rigBindingIndex = clipInstance.Value.RotationBindingMap[i];
                Assert.That(bindings.RotationBindings[i], Is.EqualTo(rigBindings.RotationBindings[rigBindingIndex]));
            }

            // Verify instance scale bindings.
            Assert.That(bindings.ScaleBindings.Length, Is.EqualTo(denseClipBindings.ScaleBindings.Length));
            for (var i = 0; i < bindings.ScaleBindings.Length; ++i)
            {
                var rigBindingIndex = clipInstance.Value.ScaleBindingMap[i];
                Assert.That(bindings.ScaleBindings[i], Is.EqualTo(rigBindings.ScaleBindings[rigBindingIndex]));
            }

            // Verify instance float bindings.
            Assert.That(bindings.FloatBindings.Length, Is.EqualTo(denseClipBindings.FloatBindings.Length));
            for (var i = 0; i < bindings.FloatBindings.Length; ++i)
            {
                var rigBindingIndex = clipInstance.Value.FloatBindingMap[i];
                Assert.That(bindings.FloatBindings[i], Is.EqualTo(rigBindings.FloatBindings[rigBindingIndex]));
            }

            // Verify instance int bindings.
            Assert.That(bindings.IntBindings.Length, Is.EqualTo(denseClipBindings.IntBindings.Length));
            for (var i = 0; i < bindings.IntBindings.Length; ++i)
            {
                var rigBindingIndex = clipInstance.Value.IntBindingMap[i];
                Assert.That(bindings.IntBindings[i], Is.EqualTo(rigBindings.IntBindings[rigBindingIndex]));
            }
        }

        [Test]
        public void ClipInstanceHasSameFrameCountAndSampleRate()
        {
            var clip = new AnimationClip();
            clip.frameRate = 30.0f;
            var constantCurve = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.x", constantCurve);
            var denseClip = clip.ToDenseClip();

            var clipInstance = ClipManager.Instance.GetClipFor(m_Rig, denseClip);

            Assert.That(clipInstance.Value.Clip.FrameCount, Is.EqualTo(30));
            Assert.That(clipInstance.Value.Clip.SampleRate, Is.EqualTo(30.0f));
        }

        [Test]
        public void DenseClipsWithSameDataHaveSameHashCode()
        {
            var clip = new AnimationClip();
            var constantCurve0 = UnityEngine.AnimationCurve.Constant(1.0f, 0.0f, 0.0f);
            var constantCurve1 = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
            var constantCurve2 = UnityEngine.AnimationCurve.Constant(0.0f, 0.0f, 1.0f);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.x", constantCurve0);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.x", constantCurve1);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalScale.x", constantCurve2);
            clip.SetCurve("Float", typeof(Dummy), "floatVar", constantCurve0);
            clip.SetCurve("Int", typeof(Dummy), "intVar", constantCurve1);
            clip.frameRate = 30.0f;

            var denseClip0 = clip.ToDenseClip();
            var denseClip1 = clip.ToDenseClip();

            Assert.That(denseClip0.Value.GetHashCode(), Is.EqualTo(denseClip1.Value.GetHashCode()));
        }

        [Test]
        public void DenseClipsWithDifferentDataHaveDifferentHashCode()
        {
            var clip = new AnimationClip();
            var constantCurve0 = UnityEngine.AnimationCurve.Constant(1.0f, 0.0f, 0.0f);
            var constantCurve1 = UnityEngine.AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
            var constantCurve2 = UnityEngine.AnimationCurve.Constant(0.0f, 0.0f, 1.0f);
            clip.SetCurve("Root", typeof(Transform), "m_LocalPosition.x", constantCurve0);
            clip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.x", constantCurve1);
            clip.SetCurve("Child2", typeof(Transform), "m_LocalScale.x", constantCurve2);
            clip.SetCurve("Float", typeof(Dummy), "floatVar", constantCurve0);
            clip.SetCurve("Int", typeof(Dummy), "intVar", constantCurve1);
            clip.frameRate = 30.0f;

            var denseClip0 = clip.ToDenseClip();

            clip.frameRate = 60f;
            var denseClip1 = clip.ToDenseClip();

            Assert.That(denseClip0.Value.GetHashCode(), Is.Not.EqualTo(denseClip1.Value.GetHashCode()));
        }
    }
}
