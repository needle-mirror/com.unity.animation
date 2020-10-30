using System.Linq;
using NUnit.Framework;
using Unity.Animation.Hybrid;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace Unity.Animation.Tests
{
    public class ClipTransformationsTests
    {
        private const int clipFrames = 42;
        private static readonly Float3AbsoluteEqualityComparer TranslationComparer = new Float3AbsoluteEqualityComparer(5e-5f);
        private static readonly QuaternionAbsoluteEqualityComparer RotationComparer = new QuaternionAbsoluteEqualityComparer(1e-4f);
        private static readonly Float3AbsoluteEqualityComparer ScaleComparer = new Float3AbsoluteEqualityComparer(1e-5f);

        BlobAssetReference<RigDefinition>  RigDefinition;
        BlobAssetReference<Clip>           FullAnimationClip;    // all bindings, sampling error on last frame
        BlobAssetReference<Clip>           PartialAnimationClip; // some bindings in animation clip
        BlobAssetReference<Clip>           AlignedClip;          // clip with no error on last frame

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode {ParentIndex = -1, Id = string.Empty, AxisIndex = -1},
                new SkeletonNode {ParentIndex = 0, Id = "Child1", AxisIndex = -1},
                new SkeletonNode {ParentIndex = 0, Id = "Child2", AxisIndex = -1}
            };
            RigDefinition = RigBuilder.CreateRigDefinition(skeletonNodes);

            var r = new Mathematics.Random(0x12345678);
            var range = new float3(100);
            var fullClip = new UnityEngine.AnimationClip();

            // add some error into the clip, so it is not perfectly frame aligned
            float clipDuration = clipFrames / fullClip.frameRate + 0.123f / fullClip.frameRate;


            CreateLinearTranslate(fullClip, string.Empty, float3.zero, new float3(0, 1, 0), clipDuration);
            CreateLinearTranslate(fullClip, "Child1", r.NextFloat3(-range, range), r.NextFloat3(-range, range), clipDuration);
            CreateLinearTranslate(fullClip, "Child2", r.NextFloat3(-range, range), r.NextFloat3(-range, range), clipDuration);
            CreateRotation(fullClip, string.Empty, quaternion.identity, r.NextQuaternionRotation(), clipDuration);
            CreateRotation(fullClip, "Child1", r.NextQuaternionRotation(), r.NextQuaternionRotation(), clipDuration);
            CreateRotation(fullClip, "Child2", r.NextQuaternionRotation(), r.NextQuaternionRotation(), clipDuration);
            CreateScale(fullClip, string.Empty, new float3(1), new float3(1), clipDuration);
            CreateScale(fullClip, "Child1", new float3(1), new float3(2), clipDuration);
            CreateScale(fullClip, "Child2", new float3(2), new float3(3), clipDuration);
            FullAnimationClip = fullClip.ToDenseClip();

            var partialClip = new UnityEngine.AnimationClip();
            CreateLinearTranslate(partialClip, "Child1", r.NextFloat3(-range, range), r.NextFloat3(-range, range), clipDuration);
            CreateRotation(partialClip, string.Empty, quaternion.identity, r.NextQuaternionRotation(), clipDuration);
            CreateRotation(partialClip, "Child2", quaternion.identity, r.NextQuaternionRotation(), clipDuration);
            CreateScale(partialClip, string.Empty, float3.zero, new float3(1), clipDuration);
            PartialAnimationClip = partialClip.ToDenseClip();

            var alignedClip = new UnityEngine.AnimationClip();
            CreateLinearTranslate(alignedClip, string.Empty, float3.zero, new float3(0, 1, 0), 1.0f);
            CreateLinearTranslate(alignedClip, "Child1", r.NextFloat3(-range, range), r.NextFloat3(-range, range), 1.0f);
            CreateLinearTranslate(alignedClip, "Child2", r.NextFloat3(-range, range), r.NextFloat3(-range, range), 1.0f);
            CreateRotation(alignedClip, string.Empty, quaternion.identity, r.NextQuaternionRotation(), 1.0f);
            AlignedClip = alignedClip.ToDenseClip();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            RigDefinition.Dispose();
            FullAnimationClip.Dispose();
            PartialAnimationClip.Dispose();
            AlignedClip.Dispose();

            ClipManager.Instance.Clear();
        }

        [Test]
        public void Clone_CreatesExactDuplicate()
        {
            using (var clone = ClipTransformations.Clone(FullAnimationClip))
            {
                Assert.That(clone.Value.SampleRate, Is.EqualTo(FullAnimationClip.Value.SampleRate));
                Assert.That(clone.Value.Duration, Is.EqualTo(FullAnimationClip.Value.Duration));
                Assert.That(clone.Value.Bindings.CurveCount, Is.EqualTo(FullAnimationClip.Value.Bindings.CurveCount));
                Assert.That(AreEqual(ref clone.Value.Bindings.TranslationBindings, ref FullAnimationClip.Value.Bindings.TranslationBindings));
                Assert.That(AreEqual(ref clone.Value.Bindings.RotationBindings, ref FullAnimationClip.Value.Bindings.RotationBindings));
                Assert.That(AreEqual(ref clone.Value.Bindings.ScaleBindings, ref FullAnimationClip.Value.Bindings.ScaleBindings));
                Assert.That(AreEqual(ref clone.Value.Bindings.FloatBindings, ref FullAnimationClip.Value.Bindings.FloatBindings));
                Assert.That(AreEqual(ref clone.Value.Bindings.IntBindings, ref FullAnimationClip.Value.Bindings.IntBindings));
                Assert.That(AreEqual(ref clone.Value.Samples, ref FullAnimationClip.Value.Samples));
            }
        }

        [TestCase(0, 0, Description = "Start")]
        [TestCase(clipFrames / 3, clipFrames / 3, Description = "Middle")]
        [TestCase(clipFrames, clipFrames, Description = "Frame Count")]
        [TestCase(clipFrames + 1, clipFrames + 1, Description = "End")]
        [TestCase(clipFrames + 5, clipFrames + 1, Description = "Past End")]
        [TestCase(-1, 0, Description = "Before Start")]
        public void CreatePose_ExtractsCorrectPose(int frame, int expectedFrame)
        {
            using (var pose = ClipTransformations.CreatePose(FullAnimationClip, frame))
            {
                Assert.That(pose.Value.SampleRate, Is.EqualTo(FullAnimationClip.Value.SampleRate));
                Assert.That(pose.Value.Duration, Is.EqualTo(0));
                Assert.That(pose.Value.Bindings.CurveCount, Is.EqualTo(FullAnimationClip.Value.Bindings.CurveCount));
                Assert.That(pose.Value.Samples.Length, Is.EqualTo(FullAnimationClip.Value.Bindings.CurveCount));
                Assert.That(AreEqual(ref pose.Value.Bindings.TranslationBindings, ref FullAnimationClip.Value.Bindings.TranslationBindings));
                Assert.That(AreEqual(ref pose.Value.Samples, ref FullAnimationClip.Value.Samples, expectedFrame * pose.Value.Bindings.CurveCount));
            }
        }

        [Test]
        public void Reverse_AlignedClip_EvaluatesCorrectly() => Reverse_InversesClipEvaluation(AlignedClip);

        [Test]
        public void Reverse_UnAlignedClip_EvaluatesCorrectly() => Reverse_InversesClipEvaluation(FullAnimationClip);

        private void Reverse_InversesClipEvaluation(BlobAssetReference<Clip> clip)
        {
            var evaluationTimes = Enumerable.Range(0, clip.Value.FrameCount + 1).Select(x => x / clip.Value.SampleRate).ToArray();
            var reverseTimes = evaluationTimes.Select(x => clip.Value.Duration - x).ToArray();

            using (var reverseClip = ClipTransformations.Reverse(clip))
            {
                Assert.That(reverseClip.Value.SampleRate, Is.EqualTo(clip.Value.SampleRate));
                Assert.That(reverseClip.Value.Duration, Is.EqualTo(clip.Value.Duration));
                Assert.That(reverseClip.Value.Bindings.CurveCount, Is.EqualTo(clip.Value.Bindings.CurveCount));
                Assert.That(AreEqual(ref reverseClip.Value.Bindings.TranslationBindings, ref clip.Value.Bindings.TranslationBindings));
                Assert.That(AreEqual(ref reverseClip.Value.Bindings.RotationBindings, ref clip.Value.Bindings.RotationBindings));
                Assert.That(AreEqual(ref reverseClip.Value.Bindings.ScaleBindings, ref clip.Value.Bindings.ScaleBindings));

                // Evaluate
                for (var bone = 0; bone < clip.Value.Bindings.TranslationBindings.Length; bone++)
                {
                    EvaluateClip(clip, bone, reverseTimes, out float3[] expPos, out quaternion[] expRot, out float3[] expScale);
                    EvaluateClip(reverseClip, bone, evaluationTimes, out float3[] actualPos, out quaternion[] actualRot, out float3[] actualScale);

                    for (int i = 0; i < actualPos.Length; i++)
                    {
                        Assert.That(actualPos[i], Is.EqualTo(expPos[i]).Using(TranslationComparer), "Translation mismatch at frame ${i}");
                        Assert.That(actualRot[i], Is.EqualTo(expRot[i]).Using(RotationComparer),  "Rotation mismatch at frame ${i}");
                        Assert.That(actualScale[i], Is.EqualTo(expScale[i]).Using(ScaleComparer), "Scale mismatch at frame ${i}");
                    }
                }
            }
        }

        [Test]
        public void FilterBindings_RemovesCorrectBindings()
        {
            using (var clip = ClipTransformations.FilterBindings(FullAnimationClip, ref PartialAnimationClip.Value.Bindings))
            {
                Assert.That(AreEqual(ref clip.Value.Bindings.TranslationBindings, ref PartialAnimationClip.Value.Bindings.TranslationBindings));
                Assert.That(AreEqual(ref clip.Value.Bindings.RotationBindings, ref PartialAnimationClip.Value.Bindings.RotationBindings));
                Assert.That(AreEqual(ref clip.Value.Bindings.ScaleBindings, ref PartialAnimationClip.Value.Bindings.ScaleBindings));
                Assert.That(AreEqual(ref clip.Value.Bindings.FloatBindings, ref PartialAnimationClip.Value.Bindings.FloatBindings));
                Assert.That(AreEqual(ref clip.Value.Bindings.IntBindings, ref PartialAnimationClip.Value.Bindings.IntBindings));
            }

            using (var clip = ClipTransformations.FilterBindings(PartialAnimationClip, ref RigDefinition.Value.Bindings))
            {
                Assert.That(AreEqual(ref clip.Value.Bindings.TranslationBindings, ref PartialAnimationClip.Value.Bindings.TranslationBindings));
                Assert.That(AreEqual(ref clip.Value.Bindings.RotationBindings, ref PartialAnimationClip.Value.Bindings.RotationBindings));
                Assert.That(AreEqual(ref clip.Value.Bindings.ScaleBindings, ref PartialAnimationClip.Value.Bindings.ScaleBindings));
                Assert.That(AreEqual(ref clip.Value.Bindings.FloatBindings, ref PartialAnimationClip.Value.Bindings.FloatBindings));
                Assert.That(AreEqual(ref clip.Value.Bindings.IntBindings, ref PartialAnimationClip.Value.Bindings.IntBindings));
            }
        }

        [Test]
        public void FilterBindings_FilteredClipRetainsCorrectEvaluation()
        {
            // full binding set vs partial
            Filter_SampleDataIsCorrect(FullAnimationClip, ref PartialAnimationClip.Value.Bindings);
            // partial against the rig
            Filter_SampleDataIsCorrect(PartialAnimationClip, ref RigDefinition.Value.Bindings);
            // partial against the full
            Filter_SampleDataIsCorrect(PartialAnimationClip, ref FullAnimationClip.Value.Bindings);
        }

        void Filter_SampleDataIsCorrect(BlobAssetReference<Clip> fullClip, ref BindingSet bindingFilter)
        {
            using (var clip = ClipTransformations.FilterBindings(fullClip, ref bindingFilter))
            {
                Assert_ClipEvaluateTheSameAsReference(clip, fullClip);
            }
        }

        [Test]
        public void FilterBindings_UpdatesHashCode()
        {
            using (var clip = ClipTransformations.FilterBindings(FullAnimationClip, ref PartialAnimationClip.Value.Bindings))
            {
                Assert.That(clip.Value.GetHashCode(), Is.Not.EqualTo(0));
                Assert.That(clip.Value.GetHashCode(), Is.Not.EqualTo(FullAnimationClip.Value.GetHashCode()));
            }
        }

        [Test]
        public void Reverse_UpdatesHashCode()
        {
            using (var clip = ClipTransformations.Reverse(FullAnimationClip))
            {
                Assert.That(clip.Value.GetHashCode(), Is.Not.EqualTo(0));
                Assert.That(clip.Value.GetHashCode(), Is.Not.EqualTo(FullAnimationClip.Value.GetHashCode()));
            }
        }

        [Test]
        public void CreatePose_UpdatesHashCode()
        {
            using (var clip = ClipTransformations.CreatePose(FullAnimationClip, 0))
            {
                Assert.That(clip.Value.GetHashCode(), Is.Not.EqualTo(0));
                Assert.That(clip.Value.GetHashCode(), Is.Not.EqualTo(FullAnimationClip.Value.GetHashCode()));
            }
        }

        private static unsafe bool AreEqual<T>(ref BlobArray<T> a, ref BlobArray<T> b) where T : struct
        {
            return a.Length == b.Length && UnsafeUtility.MemCmp(a.GetUnsafePtr(), b.GetUnsafePtr(), a.Length * UnsafeUtility.SizeOf<T>()) == 0;
        }

        private static unsafe bool AreEqual<T>(ref BlobArray<T> a, ref BlobArray<T> b, int offset) where T : struct
        {
            var pB = (byte*)b.GetUnsafePtr();
            pB += offset * UnsafeUtility.SizeOf<T>();
            return b.Length >= a.Length + offset && UnsafeUtility.MemCmp(a.GetUnsafePtr(), pB, a.Length * UnsafeUtility.SizeOf<T>()) == 0;
        }

        private void Assert_ClipEvaluateTheSameAsReference(BlobAssetReference<Clip> clip, BlobAssetReference<Clip> referenceClip)
        {
            var times = Enumerable.Range(0, clip.Value.FrameCount).Select(x => x / clip.Value.SampleRate).ToArray();

            for (var bone = 0; bone < clip.Value.Bindings.TranslationBindings.Length; bone++)
            {
                int boneIndex = Core.FindBindingIndex(ref RigDefinition.Value.Bindings.TranslationBindings, clip.Value.Bindings.TranslationBindings[bone]);
                EvaluateClip(referenceClip, boneIndex, times, out float3[] expPos, out quaternion[] _, out float3[] _);
                EvaluateClip(clip, boneIndex, times, out float3[] actualPos, out quaternion[] _, out float3[] _);
                for (int i = 0; i < actualPos.Length; i++)
                    Assert.That(actualPos[i], Is.EqualTo(expPos[i]).Using(TranslationComparer), $"Translation mismatch at frame {i}");
            }

            for (var bone = 0; bone < clip.Value.Bindings.RotationBindings.Length; bone++)
            {
                int boneIndex = Core.FindBindingIndex(ref RigDefinition.Value.Bindings.RotationBindings, clip.Value.Bindings.RotationBindings[bone]);
                EvaluateClip(referenceClip, boneIndex, times, out float3[] _, out quaternion[] expRot, out float3[] _);
                EvaluateClip(clip, boneIndex, times, out float3[] _, out quaternion[] actualRot, out float3[] _);
                for (int i = 0; i < actualRot.Length; i++)
                    Assert.That(actualRot[i], Is.EqualTo(expRot[i]).Using(RotationComparer), $"Rotation mismatch at frame {i}");
            }

            for (var bone = 0; bone < clip.Value.Bindings.ScaleBindings.Length; bone++)
            {
                int boneIndex = Core.FindBindingIndex(ref RigDefinition.Value.Bindings.ScaleBindings, clip.Value.Bindings.ScaleBindings[bone]);
                EvaluateClip(referenceClip, boneIndex, times, out float3[] _, out quaternion[] _, out float3[] expScale);
                EvaluateClip(clip, boneIndex, times, out float3[] _, out quaternion[] _, out float3[] actualScale);
                for (int i = 0; i < actualScale.Length; i++)
                    Assert.That(actualScale[i], Is.EqualTo(expScale[i]).Using(ScaleComparer), $"Scale mismatch at frame {i}");
            }
        }

        private void EvaluateClip(BlobAssetReference<Clip> clip, int boneIndex, float[] times, out float3[] pos, out quaternion[] rot, out float3[] scale)
        {
            pos = new float3[times.Length];
            rot = new quaternion[times.Length];
            scale = new float3[times.Length];

            using (var clipInstance = ClipInstanceBuilder.Create(RigDefinition, clip))
            using (var buffer = new NativeArray<AnimatedData>(RigDefinition.Value.Bindings.StreamSize, Allocator.Temp))
            {
                var outputStream = AnimationStream.Create(RigDefinition, buffer);
                for (int i = 0; i < times.Length; i++)
                {
                    Core.EvaluateClip(clipInstance, times[i], ref outputStream, 0);
                    outputStream.GetLocalToParentTRS(boneIndex, out pos[i], out rot[i], out scale[i]);
                }
            }
        }

        static void CreateLinearTranslate(AnimationClip animClip, string path, float3 start, float3 end, float time)
        {
            animClip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", UnityEngine.AnimationCurve.Linear(0, start.x, time, end.x));
            animClip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", UnityEngine.AnimationCurve.Linear(0, start.y, time, end.y));
            animClip.SetCurve(path, typeof(Transform), "m_LocalPosition.z", UnityEngine.AnimationCurve.Linear(0, start.z, time, end.z));
        }

        static void CreateRotation(AnimationClip animClip, string path, quaternion start, quaternion end, float time)
        {
            animClip.SetCurve(path, typeof(Transform), "m_LocalRotation.x", UnityEngine.AnimationCurve.Linear(0, start.value.x, time, end.value.x));
            animClip.SetCurve(path, typeof(Transform), "m_LocalRotation.y", UnityEngine.AnimationCurve.Linear(0, start.value.y, time, end.value.y));
            animClip.SetCurve(path, typeof(Transform), "m_LocalRotation.z", UnityEngine.AnimationCurve.Linear(0, start.value.z, time, end.value.z));
            animClip.SetCurve(path, typeof(Transform), "m_LocalRotation.w", UnityEngine.AnimationCurve.Linear(0, start.value.w, time, end.value.w));
        }

        static void CreateScale(AnimationClip animClip, string path, float3 start, float3 end, float time)
        {
            animClip.SetCurve(path, typeof(Transform), "m_LocalScale.x", UnityEngine.AnimationCurve.Linear(0, start.x, time, end.x));
            animClip.SetCurve(path, typeof(Transform), "m_LocalScale.y", UnityEngine.AnimationCurve.Linear(0, start.y, time, end.y));
            animClip.SetCurve(path, typeof(Transform), "m_LocalScale.z", UnityEngine.AnimationCurve.Linear(0, start.z, time, end.z));
        }
    }
}
