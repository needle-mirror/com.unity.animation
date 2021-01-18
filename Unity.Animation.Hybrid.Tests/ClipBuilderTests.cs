using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEditor;

using Unity.Animation.Hybrid;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;


namespace Unity.Animation.Tests
{
    public class ClipBuilderTests : AnimationTestsFixture
    {
        const float kAnimatedDataTolerance = 1e-5f;

        class DummySyncTag : MonoBehaviour, ISynchronizationTag
        {
            public StringHash Type => new StringHash(nameof(DummySyncTag));

            public int State { get; set; }
        }

        GameObject CreateSyncTag(int state)
        {
            var tag = CreateGameObject();
            var syncTag = tag.AddComponent<DummySyncTag>() as ISynchronizationTag;

            syncTag.State = state;
            return tag;
        }

        UnityEngine.AnimationCurve CreateRandomCurve(int keyLength, float deltaTime, float min, float max)
        {
            Keyframe[] keys = new Keyframe[keyLength];
            for (var i = 0; i < keyLength; ++i)
            {
                keys[i] = new Keyframe(i * deltaTime, Random.Range(min, max),
                    Random.Range(min, max),
                    Random.Range(min, max));
            }

            return new UnityEngine.AnimationCurve(keys);
        }

        BlobAssetReference<RigDefinition> CreateTestRigDefinition()
        {
            var skeletonNodes = new[]
            {
                new SkeletonNode {ParentIndex = -1, Id = TransformChannelID(""), AxisIndex = -1},
                new SkeletonNode {ParentIndex = 0, Id = TransformChannelID("Child1"), AxisIndex = -1},
                new SkeletonNode {ParentIndex = 0, Id = TransformChannelID("Child2"), AxisIndex = -1}
            };

            var animationChannels = new IAnimationChannel[]
            {
                new IntChannel { DefaultValue = 0, Id = GenericChannelID("intVar", "", typeof(Dummy)) },
                new FloatChannel { DefaultValue = 0, Id = GenericChannelID("floatVar", "", typeof(Dummy)) }
            };

            return RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannels);
        }

        AnimationClip CreateComplexAnimationClip()
        {
            var animationClip = CreateAnimationClip();
            animationClip.frameRate = 60.0f;
            float deltaTime = 1.0f / animationClip.frameRate;

            Random.InitState(0);

            void GenerateQuatCurves(out UnityEngine.AnimationCurve xQuat, out UnityEngine.AnimationCurve yQuat,
                out UnityEngine.AnimationCurve zQuat, out UnityEngine.AnimationCurve wQuat)
            {
                xQuat = new UnityEngine.AnimationCurve();
                yQuat = new UnityEngine.AnimationCurve();
                zQuat = new UnityEngine.AnimationCurve();
                wQuat = new UnityEngine.AnimationCurve();

                for (var i = 0; i < 10; ++i)
                {
                    var x = Random.Range(0, 1.0f);
                    var y = Random.Range(0, 1.0f);
                    var z = Random.Range(0, 1.0f);
                    var w = Random.Range(0, 1.0f);

                    var q = math.normalize(new quaternion(x, y, z, w));
                    xQuat.AddKey(i * deltaTime, q.value.x);
                    yQuat.AddKey(i * deltaTime, q.value.y);
                    zQuat.AddKey(i * deltaTime, q.value.z);
                    wQuat.AddKey(i * deltaTime, q.value.w);
                }
            }

            animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.x", CreateRandomCurve(10, deltaTime, -10.0f, 10.0f));
            animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.y", CreateRandomCurve(10, deltaTime, -10.0f, 10.0f));
            animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.z", CreateRandomCurve(10, deltaTime, -10.0f, 10.0f));
            GenerateQuatCurves(out var xRootRotation, out var yRootRotation, out var zRootRotation, out var wRootRotation);
            animationClip.SetCurve("", typeof(Transform), "m_LocalRotation.x", xRootRotation);
            animationClip.SetCurve("", typeof(Transform), "m_LocalRotation.y", yRootRotation);
            animationClip.SetCurve("", typeof(Transform), "m_LocalRotation.z", zRootRotation);
            animationClip.SetCurve("", typeof(Transform), "m_LocalRotation.w", wRootRotation);
            animationClip.SetCurve("", typeof(Transform), "m_LocalScale.x", CreateRandomCurve(10, deltaTime, 0.0f, 10.0f));
            animationClip.SetCurve("", typeof(Transform), "m_LocalScale.y", CreateRandomCurve(10, deltaTime, 0.0f, 10.0f));
            animationClip.SetCurve("", typeof(Transform), "m_LocalScale.z", CreateRandomCurve(10, deltaTime, 0.0f, 10.0f));

            animationClip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.x", CreateRandomCurve(10, deltaTime, -10.0f, 10.0f));
            animationClip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.y", CreateRandomCurve(10, deltaTime, -10.0f, 10.0f));
            animationClip.SetCurve("Child1", typeof(Transform), "m_LocalPosition.z", CreateRandomCurve(10, deltaTime, -10.0f, 10.0f));
            GenerateQuatCurves(out var xChildRotation, out var yChildRotation, out var zChildRotation, out var wChildRotation);
            animationClip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.x", xChildRotation);
            animationClip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.y", yChildRotation);
            animationClip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.z", zChildRotation);
            animationClip.SetCurve("Child1", typeof(Transform), "m_LocalRotation.w", wChildRotation);
            animationClip.SetCurve("Child1", typeof(Transform), "m_LocalScale.x", CreateRandomCurve(10, deltaTime, 0.0f, 10.0f));
            animationClip.SetCurve("Child1", typeof(Transform), "m_LocalScale.y", CreateRandomCurve(10, deltaTime, 0.0f, 10.0f));
            animationClip.SetCurve("Child1", typeof(Transform), "m_LocalScale.z", CreateRandomCurve(10, deltaTime, 0.0f, 10.0f));

            // Test for localEulerAngles
            animationClip.SetCurve("Child1/Child2", typeof(Transform), "m_LocalPosition.x", CreateRandomCurve(10, deltaTime, -10.0f, 10.0f));
            animationClip.SetCurve("Child1/Child2", typeof(Transform), "m_LocalPosition.y", CreateRandomCurve(10, deltaTime, -10.0f, 10.0f));
            animationClip.SetCurve("Child1/Child2", typeof(Transform), "m_LocalPosition.z", CreateRandomCurve(10, deltaTime, -10.0f, 10.0f));
            animationClip.SetCurve("Child1/Child2", typeof(Transform), "localEulerAngles.x", CreateRandomCurve(10, deltaTime, 0.0f, 360.0f));
            animationClip.SetCurve("Child1/Child2", typeof(Transform), "localEulerAngles.y", CreateRandomCurve(10, deltaTime, 0.0f, 360.0f));
            animationClip.SetCurve("Child1/Child2", typeof(Transform), "localEulerAngles.z", CreateRandomCurve(10, deltaTime, 0.0f, 360.0f));

            animationClip.SetCurve("", typeof(Dummy), "floatVar", CreateRandomCurve(10, deltaTime, -100.0f, 100.0f));
            animationClip.SetCurve("", typeof(Dummy), "intVar", CreateRandomCurve(10, deltaTime, 0.0f, 1.0f));

            var animationEvents = new AnimationEvent[]
            {
                new AnimationEvent { time = 0.0f, objectReferenceParameter = CreateSyncTag(0) },
                new AnimationEvent { time = 0.03333f, objectReferenceParameter = CreateSyncTag(1) },
                new AnimationEvent { time = 0.06666f, objectReferenceParameter = CreateSyncTag(2) },
                new AnimationEvent { time = 0.1f, objectReferenceParameter = CreateSyncTag(3) }
            };
            AnimationUtility.SetAnimationEvents(animationClip, animationEvents);

            return animationClip;
        }

        AnimationClip CreateSimpleTestAnimationClip()
        {
            var animationClip = CreateAnimationClip();
            animationClip.frameRate = 60.0f;

            animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.x", UnityEngine.AnimationCurve.Linear(0, 1.0f, 1, 2.0f));
            animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.y", UnityEngine.AnimationCurve.Linear(0, 1.0f, 1, 2.0f));
            animationClip.SetCurve("", typeof(Transform), "m_LocalPosition.z", UnityEngine.AnimationCurve.Linear(0, 1.0f, 1, 2.0f));

            animationClip.SetCurve("", typeof(Dummy), "floatVar", UnityEngine.AnimationCurve.Constant(0, 1, 1.5f));

            return animationClip;
        }

        void AssertClipAreEqual(ref Clip clip1, ref Clip clip2)
        {
            Assert.That(clip1.Duration, Is.EqualTo(clip2.Duration).Using(FloatComparer));
            Assert.That(clip1.SampleRate, Is.EqualTo(clip2.SampleRate).Using(FloatComparer));
            Assert.That(clip1.FrameCount, Is.EqualTo(clip2.FrameCount));
            Assert.That(clip1.LastFrameError, Is.EqualTo(clip2.LastFrameError).Using(FloatComparer));
            Assert.IsTrue(clip1.Samples.Length == clip2.Samples.Length);

            int FindPropertyHash(StringHash propertyHash, ref BlobArray<StringHash> bindings)
            {
                for (int i = 0; i < bindings.Length; ++i)
                {
                    if (bindings[i] == propertyHash)
                    {
                        return i;
                    }
                }

                return -1;
            }

            float GetFloatSample(ref BlobArray<float> samples, int samplesOffset, int bindingIndex, int componentIndex = 0, int componentCount = 1)
            {
                return samples[samplesOffset + bindingIndex * componentCount + componentIndex];
            }

            float3 GetFloat3Sample(ref BlobArray<float> samples, int samplesOffset, int bindingIndex)
            {
                return new float3(GetFloatSample(ref samples, samplesOffset, bindingIndex, 0, 3),
                    GetFloatSample(ref samples, samplesOffset, bindingIndex, 1, 3),
                    GetFloatSample(ref samples, samplesOffset, bindingIndex, 2, 3));
            }

            quaternion GetQuaternionSample(ref BlobArray<float> samples, int samplesOffset, int bindingIndex)
            {
                return new quaternion(GetFloatSample(ref samples, samplesOffset, bindingIndex, 0, 4),
                    GetFloatSample(ref samples, samplesOffset, bindingIndex, 1, 4),
                    GetFloatSample(ref samples, samplesOffset, bindingIndex, 2, 4),
                    GetFloatSample(ref samples, samplesOffset, bindingIndex, 3, 4));
            }

            // Translations
            Assert.That(clip1.Bindings.TranslationBindings.Length, Is.EqualTo(clip2.Bindings.TranslationBindings.Length));
            for (int bindingIndex1 = 0; bindingIndex1 < clip1.Bindings.TranslationBindings.Length; ++bindingIndex1)
            {
                int bindingIndex2 = FindPropertyHash(clip1.Bindings.TranslationBindings[bindingIndex1], ref clip2.Bindings.TranslationBindings);
                Assert.IsTrue(bindingIndex2 >= 0);

                float3 translation1 = GetFloat3Sample(ref clip1.Samples, clip1.Bindings.TranslationSamplesOffset, bindingIndex1);
                float3 translation2 = GetFloat3Sample(ref clip2.Samples, clip2.Bindings.TranslationSamplesOffset, bindingIndex2);

                Assert.That(translation1, Is.EqualTo(translation2).Using(TranslationComparer));
            }

            // Rotations
            Assert.That(clip1.Bindings.RotationBindings.Length, Is.EqualTo(clip2.Bindings.RotationBindings.Length));
            for (int bindingIndex1 = 0; bindingIndex1 < clip1.Bindings.RotationBindings.Length; ++bindingIndex1)
            {
                int bindingIndex2 = FindPropertyHash(clip1.Bindings.RotationBindings[bindingIndex1], ref clip2.Bindings.RotationBindings);
                Assert.IsTrue(bindingIndex2 >= 0);

                quaternion rotation1 = GetQuaternionSample(ref clip1.Samples, clip1.Bindings.RotationSamplesOffset, bindingIndex1);
                quaternion rotation2 = GetQuaternionSample(ref clip2.Samples, clip2.Bindings.RotationSamplesOffset, bindingIndex2);

                Assert.That(rotation1, Is.EqualTo(rotation2).Using(RotationComparer));
            }

            // Scales
            Assert.That(clip1.Bindings.ScaleBindings.Length, Is.EqualTo(clip2.Bindings.ScaleBindings.Length));
            for (int bindingIndex1 = 0; bindingIndex1 < clip1.Bindings.ScaleBindings.Length; ++bindingIndex1)
            {
                int bindingIndex2 = FindPropertyHash(clip1.Bindings.ScaleBindings[bindingIndex1], ref clip2.Bindings.ScaleBindings);
                Assert.IsTrue(bindingIndex2 >= 0);

                float3 scale1 = GetFloat3Sample(ref clip1.Samples, clip1.Bindings.ScaleSamplesOffset, bindingIndex1);
                float3 scale2 = GetFloat3Sample(ref clip2.Samples, clip2.Bindings.ScaleSamplesOffset, bindingIndex2);

                Assert.That(scale1, Is.EqualTo(scale2).Using(TranslationComparer));
            }

            // Float
            Assert.That(clip1.Bindings.FloatBindings.Length, Is.EqualTo(clip2.Bindings.FloatBindings.Length));
            for (int bindingIndex1 = 0; bindingIndex1 < clip1.Bindings.FloatBindings.Length; ++bindingIndex1)
            {
                int bindingIndex2 = FindPropertyHash(clip1.Bindings.FloatBindings[bindingIndex1], ref clip2.Bindings.FloatBindings);
                Assert.IsTrue(bindingIndex2 >= 0);

                float float1 = GetFloatSample(ref clip1.Samples, clip1.Bindings.FloatSamplesOffset, bindingIndex1);
                float float2 = GetFloatSample(ref clip2.Samples, clip2.Bindings.FloatSamplesOffset, bindingIndex2);

                Assert.That(float1, Is.EqualTo(float2).Using(FloatComparer));
            }

            // Int
            Assert.That(clip1.Bindings.IntBindings.Length, Is.EqualTo(clip2.Bindings.IntBindings.Length));
            for (int bindingIndex1 = 0; bindingIndex1 < clip1.Bindings.IntBindings.Length; ++bindingIndex1)
            {
                int bindingIndex2 = FindPropertyHash(clip1.Bindings.IntBindings[bindingIndex1], ref clip2.Bindings.IntBindings);
                Assert.IsTrue(bindingIndex2 >= 0);

                int int1 = (int)GetFloatSample(ref clip1.Samples, clip1.Bindings.FloatSamplesOffset, bindingIndex1);
                int int2 = (int)GetFloatSample(ref clip2.Samples, clip2.Bindings.FloatSamplesOffset, bindingIndex2);

                Assert.That(int1, Is.EqualTo(int2));
            }

            // Sync tags
            Assert.That(clip1.SynchronizationTags.Length, Is.EqualTo(clip2.SynchronizationTags.Length));
            for (var i = 0; i < clip1.SynchronizationTags.Length; ++i)
            {
                Assert.That(clip1.SynchronizationTags[i], Is.EqualTo(clip2.SynchronizationTags[i]).Using(SynchronizationTagComparer));
            }
        }

        void AssertBufferIsntFilledWithZero(NativeArray<AnimatedData> buffer)
        {
            for (int i = 0; i < buffer.Length; ++i)
            {
                if (math.abs(buffer[i].Value) > kAnimatedDataTolerance)
                {
                    return;
                }
            }

            Assert.Fail();
        }

        [Test]
        public void CheckManualSimpleClipBuilderToDenseClipIsValid()
        {
            var clip = CreateSimpleTestAnimationClip();
            var denseClip = clip.ToDenseClip();

            var clipBuilder = new ClipBuilder(clip.length, clip.frameRate, Allocator.Temp);
            var translations = new NativeArray<float3>(clipBuilder.SampleCount, Allocator.Temp);
            var floatData = new NativeArray<float>(clipBuilder.SampleCount, Allocator.Temp);
            for (var i = 0; i < clipBuilder.SampleCount; ++i)
            {
                translations[i] = new float3(1.0f + (float)i / clipBuilder.FrameCount);
                floatData[i] = 1.5f;
            }

            var translationsBindingHash = TransformChannelID("");
            clipBuilder.AddTranslationCurve(translations, translationsBindingHash);
            var floatDataBindingHash = GenericChannelID("floatVar", "", typeof(Dummy));
            clipBuilder.AddFloatCurve(floatData, floatDataBindingHash);
            var builderDenseClip = clipBuilder.ToDenseClip();

            AssertClipAreEqual(ref denseClip.Value, ref builderDenseClip.Value);

            floatData.Dispose();
            translations.Dispose();
            denseClip.Dispose();
            clipBuilder.Dispose();
            builderDenseClip.Dispose();
        }

        [Test]
        [Description("Verify ClipBuilder and ClipInstance sample the same AnimationStream")]
        public void CheckComplexClipBuilderEvaluateClipIsValid()
        {
            var rigDefinition = CreateTestRigDefinition();
            var clip = CreateComplexAnimationClip();
            var denseClip = clip.ToDenseClip();
            var clipInstance = ClipInstanceBuilder.Create(rigDefinition, denseClip);

            var clipBuilder = clip.ToClipBuilder(Allocator.Temp);

            var buffer1 = new NativeArray<AnimatedData>(rigDefinition.Value.Bindings.StreamSize, Allocator.Temp);
            var buffer2 = new NativeArray<AnimatedData>(rigDefinition.Value.Bindings.StreamSize, Allocator.Temp);

            var stream1 = AnimationStream.Create(rigDefinition, buffer1);
            var stream2 = AnimationStream.Create(rigDefinition, buffer2);

            int sampleCount = 20;
            float deltaTime = denseClip.Value.Duration / (sampleCount - 1);

            for (var i = 0; i < sampleCount; ++i)
            {
                float time = i * deltaTime;
                Core.EvaluateClip(clipInstance, time, ref stream1, 0);
                ClipBuilderUtils.EvaluateClip(clipBuilder, time, ref stream2, 0);

                AssertBufferIsntFilledWithZero(buffer1);
                AssertBufferIsntFilledWithZero(buffer2);

                Assert.That(stream1, Is.EqualTo(stream2).Using(AnimationStreamComparer));
            }

            rigDefinition.Dispose();
            denseClip.Dispose();
            clipInstance.Dispose();
            clipBuilder.Dispose();

            buffer1.Dispose();
            buffer2.Dispose();
        }

        [Test]
        [Description("Verify ClipBuilder and ClipInstance sample the same AnimationStream")]
        public void CheckSimpleClipBuilderEvaluateClipIsValid()
        {
            var rigDefinition = CreateTestRigDefinition();
            var clip = CreateSimpleTestAnimationClip();
            var denseClip = clip.ToDenseClip();
            var clipInstance = ClipInstanceBuilder.Create(rigDefinition, denseClip);

            var clipBuilder = clip.ToClipBuilder(Allocator.Temp);

            var buffer1 = new NativeArray<AnimatedData>(rigDefinition.Value.Bindings.StreamSize, Allocator.Temp);
            var buffer2 = new NativeArray<AnimatedData>(rigDefinition.Value.Bindings.StreamSize, Allocator.Temp);

            var stream1 = AnimationStream.Create(rigDefinition, buffer1);
            var stream2 = AnimationStream.Create(rigDefinition, buffer2);

            int sampleCount = 20;
            float deltaTime = denseClip.Value.Duration / (sampleCount - 1);

            for (var i = 0; i < sampleCount; ++i)
            {
                float time = i * deltaTime;
                Core.EvaluateClip(clipInstance, time, ref stream1, 0);
                ClipBuilderUtils.EvaluateClip(clipBuilder, time, ref stream2, 0);

                AssertBufferIsntFilledWithZero(buffer1);
                AssertBufferIsntFilledWithZero(buffer2);

                Assert.That(stream1, Is.EqualTo(stream2).Using(AnimationStreamComparer));
            }

            rigDefinition.Dispose();
            denseClip.Dispose();
            clipInstance.Dispose();
            clipBuilder.Dispose();

            buffer1.Dispose();
            buffer2.Dispose();
        }

        [Test, Description("Removing a curve when the clip builder is empty does not throw.")]
        public void RemoveCurveOnEmptyClipBuilder()
        {
            var clipBuilder = new ClipBuilder(1.0f, 30.0f, Allocator.Temp);
            const string hash = "DummyName";

            Assert.IsTrue(clipBuilder.m_TranslationCurves.IsEmpty);
            Assert.IsTrue(clipBuilder.m_QuaternionCurves.IsEmpty);
            Assert.IsTrue(clipBuilder.m_ScaleCurves.IsEmpty);
            Assert.IsTrue(clipBuilder.m_FloatCurves.IsEmpty);
            Assert.IsTrue(clipBuilder.m_IntCurves.IsEmpty);

            Assert.DoesNotThrow(() => clipBuilder.RemoveTranslationCurve(hash));
            Assert.DoesNotThrow(() => clipBuilder.RemoveQuaternionCurve(hash));
            Assert.DoesNotThrow(() => clipBuilder.RemoveScaleCurve(hash));
            Assert.DoesNotThrow(() => clipBuilder.RemoveFloatCurve(hash));
            Assert.DoesNotThrow(() => clipBuilder.RemoveIntCurve(hash));

            clipBuilder.Dispose();
        }

        [Test]
        public void CheckAddAndRemoveCurveToClipBuilder()
        {
            var clip = CreateComplexAnimationClip();
            var clipBuilder = clip.ToClipBuilder(Allocator.Temp);

            var nbKeyframes = clipBuilder.SampleCount;

            var samplesFloat = new NativeArray<float>(nbKeyframes, Allocator.Temp);
            var samplesFloat3 = new NativeArray<float3>(nbKeyframes, Allocator.Temp);
            var samplesQuaternion = new NativeArray<quaternion>(nbKeyframes, Allocator.Temp);
            var samplesInt = new NativeArray<float>(nbKeyframes, Allocator.Temp);

            for (var i = 0; i < nbKeyframes; i++)
            {
                samplesFloat[i] = i;
                samplesFloat3[i] = new float3(i, i, i);
                samplesQuaternion[i] = quaternion.Euler(i, i, i);
                samplesInt[i] = i;
            }

            var strHash = "bindingKey";

            var skeletonNodes = new[]
            {
                new SkeletonNode {ParentIndex = -1, Id = strHash, AxisIndex = -1},
            };

            var animationChannels = new IAnimationChannel[]
            {
                new IntChannel { DefaultValue = 0, Id = strHash },
                new FloatChannel { DefaultValue = 0, Id = strHash }
            };

            var rigDefinition = RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannels);

            var streamBuffer = new NativeArray<AnimatedData>(rigDefinition.Value.Bindings.StreamSize, Allocator.Temp);
            var animationStream = AnimationStream.Create(rigDefinition, streamBuffer);

            clipBuilder.AddTranslationCurve(samplesFloat3, strHash);
            // Only translations should have this hash
            Assert.IsTrue(clipBuilder.m_TranslationCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_QuaternionCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_ScaleCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_FloatCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_IntCurves.ContainsKey(strHash));

            // make sure sampled values of added curve are valid
            for (var i = 0; i < nbKeyframes; i++)
            {
                float time = i / clipBuilder.SampleRate;
                ClipBuilderUtils.EvaluateClip(clipBuilder, time, ref animationStream, 0);
                Assert.IsTrue(TranslationComparer.Equals(animationStream.GetLocalToParentTranslation(0), samplesFloat3[i]));
            }

            clipBuilder.RemoveTranslationCurve(strHash);
            Assert.IsFalse(clipBuilder.m_TranslationCurves.ContainsKey(strHash));

            clipBuilder.AddQuaternionCurve(samplesQuaternion, strHash);
            // Only quaternions should have this hash
            Assert.IsFalse(clipBuilder.m_TranslationCurves.ContainsKey(strHash));
            Assert.IsTrue(clipBuilder.m_QuaternionCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_ScaleCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_FloatCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_IntCurves.ContainsKey(strHash));

            // make sure sampled values of added curve are valid
            for (var i = 0; i < nbKeyframes; i++)
            {
                float time = i / clipBuilder.SampleRate;
                ClipBuilderUtils.EvaluateClip(clipBuilder, time, ref animationStream, 0);
                Assert.IsTrue(RotationComparer.Equals(animationStream.GetLocalToParentRotation(0), samplesQuaternion[i]));
            }

            clipBuilder.RemoveQuaternionCurve(strHash);
            Assert.IsFalse(clipBuilder.m_QuaternionCurves.ContainsKey(strHash));

            clipBuilder.AddScaleCurve(samplesFloat3, strHash);
            // Only scales should have this hash
            Assert.IsFalse(clipBuilder.m_TranslationCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_QuaternionCurves.ContainsKey(strHash));
            Assert.IsTrue(clipBuilder.m_ScaleCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_FloatCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_IntCurves.ContainsKey(strHash));

            // make sure sampled values of added curve are valid
            for (var i = 0; i < nbKeyframes; i++)
            {
                float time = i / clipBuilder.SampleRate;
                ClipBuilderUtils.EvaluateClip(clipBuilder, time, ref animationStream, 0);
                Assert.IsTrue(ScaleComparer.Equals(animationStream.GetLocalToParentScale(0), samplesFloat3[i]));
            }

            clipBuilder.RemoveScaleCurve(strHash);
            Assert.IsFalse(clipBuilder.m_ScaleCurves.ContainsKey(strHash));

            clipBuilder.AddFloatCurve(samplesFloat, strHash);
            // Only floats should have this hash
            Assert.IsFalse(clipBuilder.m_TranslationCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_QuaternionCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_ScaleCurves.ContainsKey(strHash));
            Assert.IsTrue(clipBuilder.m_FloatCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_IntCurves.ContainsKey(strHash));

            // make sure sampled values of added curve are valid
            for (var i = 0; i < nbKeyframes; i++)
            {
                float time = i / clipBuilder.SampleRate;
                ClipBuilderUtils.EvaluateClip(clipBuilder, time, ref animationStream, 0);
                Assert.IsTrue(FloatComparer.Equals(animationStream.GetFloat(0), samplesFloat[i]));
            }

            clipBuilder.RemoveFloatCurve(strHash);
            Assert.IsFalse(clipBuilder.m_FloatCurves.ContainsKey(strHash));

            clipBuilder.AddIntCurve(samplesInt, strHash);
            // Only ints should have this hash
            Assert.IsFalse(clipBuilder.m_TranslationCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_QuaternionCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_ScaleCurves.ContainsKey(strHash));
            Assert.IsFalse(clipBuilder.m_FloatCurves.ContainsKey(strHash));
            Assert.IsTrue(clipBuilder.m_IntCurves.ContainsKey(strHash));

            // make sure sampled values of added curve are valid
            for (var i = 0; i < nbKeyframes; i++)
            {
                float time = i / clipBuilder.SampleRate;
                ClipBuilderUtils.EvaluateClip(clipBuilder, time, ref animationStream, 0);
                Assert.IsTrue(animationStream.GetInt(0) == (int)samplesInt[i]);
            }

            clipBuilder.RemoveIntCurve(strHash);
            Assert.IsFalse(clipBuilder.m_IntCurves.ContainsKey(strHash));

            clipBuilder.Dispose();
            rigDefinition.Dispose();
            streamBuffer.Dispose();
        }
    }
}
