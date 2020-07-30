using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.TestTools;

namespace Unity.Animation.Tests
{
    public class AnimationStreamChannelMaskTests : AnimationTestsFixture, IPrebuildSetup
    {
        private Rig m_Rig;
        private BlobAssetReference<Clip> m_Clip1;
        private BlobAssetReference<Clip> m_Clip2;

        BlobAssetReference<RigDefinition> CreateRigDefinition(int boneCount)
        {
            var animationChannel = new IAnimationChannel[]
            {
                new FloatChannel {Id = "PropertyA"},
                new FloatChannel {Id = "PropertyB"},
                new FloatChannel {Id = "PropertyC"},
                new IntChannel {Id = "PropertyD"},
                new IntChannel {Id = "PropertyE"},
                new IntChannel {Id = "PropertyF"},
                new LocalScaleChannel {Id = "CustomScale"},
                new LocalRotationChannel {Id = "CustomRotation"},
                new LocalTranslationChannel {Id = "CustomTranslation"}
            };

            var skeleton = new SkeletonNode[boneCount];

            skeleton[0] = new SkeletonNode
            {
                Id = "Root", ParentIndex = -1,
                AxisIndex = -1,
                LocalTranslationDefaultValue = float3.zero,
                LocalRotationDefaultValue = quaternion.identity,
                LocalScaleDefaultValue = new float3(1)
            };

            // Create a chain of transforms.
            for (int i = 1; i < boneCount; i++)
            {
                skeleton[i] = new SkeletonNode
                {
                    Id = $"{i}",
                    ParentIndex = i - 1,
                    AxisIndex = -1,
                    LocalTranslationDefaultValue = float3.zero,
                    LocalRotationDefaultValue = quaternion.identity,
                    LocalScaleDefaultValue = new float3(1)
                };
            }

            return RigBuilder.CreateRigDefinition(skeleton, null, animationChannel);
        }

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            {
                var denseClip = CreateConstantDenseClip(
                    new[] { ("Root", float3.zero), ("4", float3.zero) },
                    new[] { ("Root", quaternion.identity), ("6", quaternion.identity) },
                    new[] { ("Root", new float3(1.0f)) },
                    new[] { ("PropertyA", 1.0f), ("PropertyB", 2.0f) },
                    new[] { ("PropertyD", 10) });

                var blobPath = "AnimationStreamMaskTestsDenseClip1.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
            {
                var denseClip = CreateConstantDenseClip(
                    new[] { ("Root", float3.zero), ("1", float3.zero), ("CustomTranslation", float3.zero) },
                    new[] { ("Root", quaternion.identity), ("1", quaternion.identity), ("CustomRotation", quaternion.identity) },
                    new[] { ("Root", new float3(1.0f)), ("1", new float3(1.0f)), ("CustomScale", new float3(1.0f)) },
                    new[] { ("PropertyA", 1.0f), ("PropertyC", 2.0f) },
                    new[] { ("PropertyE", 10) });

                var blobPath = "AnimationStreamMaskTestsDenseClip2.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
#endif
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            // Create rig
            m_Rig = new Rig { Value = CreateRigDefinition(30) };

            {
                var path = "AnimationStreamMaskTestsDenseClip1.blob";
                m_Clip1 = BlobFile.ReadBlobAsset<Clip>(path);
                ClipManager.Instance.GetClipFor(m_Rig, m_Clip1);
            }

            {
                var path = "AnimationStreamMaskTestsDenseClip2.blob";
                m_Clip2 = BlobFile.ReadBlobAsset<Clip>(path);

                ClipManager.Instance.GetClipFor(m_Rig, m_Clip2);
            }
        }

        [Test]
        [TestCase(0, 8, 0)]
        [TestCase(1, 8, 8)]
        [TestCase(2, 8, 8)]
        [TestCase(3, 8, 8)]
        [TestCase(4, 8, 8)]
        [TestCase(5, 8, 8)]
        [TestCase(6, 8, 8)]
        [TestCase(7, 8, 8)]
        [TestCase(8, 8, 8)]
        [TestCase(9, 8, 16)]
        [TestCase(32, 8, 32)]
        [TestCase(63, 8, 64)]
        [TestCase(64, 8, 64)]
        [TestCase(65, 8, 72)]
        public void CanAlignUp(int value, int align, int expectedResult)
        {
            Assert.That(Core.AlignUp(value, align), Is.EqualTo(expectedResult));
        }

        [Test]
        public void CanSetMask()
        {
            var bitArray = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            bitArray.Clear();

            Assert.That(bitArray.CountBits(0, 64), Is.EqualTo(0));

            bitArray.SetBits(true);
            Assert.That(bitArray.CountBits(0, 64), Is.EqualTo(64));

            bitArray.SetBits(false);
            Assert.That(bitArray.CountBits(0, 64), Is.EqualTo(0));
        }

        [Test]
        public void CanCopyMask()
        {
            var bitArrayA = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bitArrayB = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            bitArrayA.Clear();
            bitArrayB.Clear();

            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(0));
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(0));

            bitArrayA.SetBits(true);
            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(64));

            bitArrayB.CopyFrom(ref bitArrayA);
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(64));
        }

        [Test]
        public void CanOrMaskInPlace()
        {
            var bitArrayA = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bitArrayB = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            bitArrayA.Clear();
            bitArrayB.Clear();

            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(0));
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(0));

            bitArrayB.OrBits64(ref bitArrayA);
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(0));

            bitArrayA.SetBits(true);
            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(64));

            bitArrayB.OrBits64(ref bitArrayA);
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(64));

            bitArrayA.Clear();
            bitArrayB.Clear();

            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(0));
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(0));

            bitArrayA.SetBits(10, true, 25);
            bitArrayB.OrBits64(ref bitArrayA);

            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(25));
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(25));
        }

        [Test]
        public void CanOrMask()
        {
            var bitArrayA = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bitArrayB = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bitArrayResult = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            bitArrayA.Clear();
            bitArrayB.Clear();
            bitArrayResult.Clear();

            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(0));
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(0));
            Assert.That(bitArrayResult.CountBits(0, 64), Is.EqualTo(0));

            bitArrayA.SetBits(10, true, 30);
            bitArrayB.SetBits(45, true, 15);

            bitArrayResult.OrBits64(ref bitArrayA, ref bitArrayB);
            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(30), "bitArrayA shouldn't be modify by the operation.");
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(15), "bitArrayB shouldn't be modify by the operation.");
            Assert.That(bitArrayResult.CountBits(0, 64), Is.EqualTo(30 + 15));
        }

        [Test]
        public void CanAndMaskInPlace()
        {
            var bitArrayA = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bitArrayB = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            bitArrayA.Clear();
            bitArrayB.Clear();

            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(0));
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(0));

            bitArrayB.AndBits64(ref bitArrayA);
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(0));

            bitArrayA.SetBits(true);
            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(64));

            bitArrayB.AndBits64(ref bitArrayA);
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(0));

            bitArrayA.Clear();
            bitArrayA.SetBits(10, true, 25);
            bitArrayB.SetBits(25, true, 25);

            bitArrayB.AndBits64(ref bitArrayA);
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(10));
        }

        [Test]
        public void CanAndMask()
        {
            var bitArrayA = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bitArrayB = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var bitArrayResult = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            bitArrayA.Clear();
            bitArrayB.Clear();
            bitArrayResult.Clear();

            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(0));
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(0));
            Assert.That(bitArrayResult.CountBits(0, 64), Is.EqualTo(0));

            bitArrayA.SetBits(0, true, 30);
            bitArrayB.SetBits(45, true, 15);

            bitArrayResult.AndBits64(ref bitArrayA, ref bitArrayB);
            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(30), "bitArrayA shouldn't be modify by the operation.");
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(15), "bitArrayB shouldn't be modify by the operation.");
            Assert.That(bitArrayResult.CountBits(0, 64), Is.EqualTo(0));


            bitArrayA.SetBits(35, true, 20);

            bitArrayResult.AndBits64(ref bitArrayA, ref bitArrayB);
            Assert.That(bitArrayA.CountBits(0, 64), Is.EqualTo(30 + 20), "bitArrayA shouldn't be modify by the operation.");
            Assert.That(bitArrayB.CountBits(0, 64), Is.EqualTo(15), "bitArrayB shouldn't be modify by the operation.");
            Assert.That(bitArrayResult.CountBits(0, 64), Is.EqualTo(10));
        }

        [Test]
        public void CanInvertMask()
        {
            var bitArray = new UnsafeBitArray(64, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            bitArray.Clear();
            Assert.That(bitArray.CountBits(0, 64), Is.EqualTo(0));

            bitArray.InvertBits64();
            Assert.That(bitArray.CountBits(0, 64), Is.EqualTo(64));

            bitArray.Clear();
            bitArray.SetBits(10, true, 30);
            Assert.That(bitArray.CountBits(0, 64), Is.EqualTo(30));

            bitArray.InvertBits64();
            Assert.That(bitArray.CountBits(0, 64), Is.EqualTo(64 - 30));
        }

        [Test]
        public void SettingAnimationStreamValueUpdatesMask()
        {
            var rig = m_Rig.Value;
            var buffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var channelCount = rig.Value.Bindings.BindingCount;

            Assert.That(channelCount, Is.EqualTo(99));

            var stream = AnimationStream.Create(rig, buffer);

            Assert.That(stream.GetChannelMaskBitCount(), Is.EqualTo(0));
            Assert.That(stream.HasNoChannelMasks(), Is.True);
            Assert.That(stream.HasAnyChannelMasks(), Is.False);
            Assert.That(stream.HasAllChannelMasks(), Is.False);

            stream.SetFloat(0, 1.0f);
            Assert.That(stream.GetChannelMaskBitCount(), Is.EqualTo(1));
            Assert.That(stream.HasNoChannelMasks(), Is.False);
            Assert.That(stream.HasAnyChannelMasks(), Is.True);
            Assert.That(stream.HasAllChannelMasks(), Is.False);
            Assert.IsTrue(stream.GetFloatChannelMask(0));

            stream.SetInt(2, 1);
            Assert.That(stream.GetChannelMaskBitCount(), Is.EqualTo(2));
            Assert.That(stream.HasNoChannelMasks(), Is.False);
            Assert.That(stream.HasAnyChannelMasks(), Is.True);
            Assert.That(stream.HasAllChannelMasks(), Is.False);
            Assert.IsTrue(stream.GetIntChannelMask(2));

            stream.SetLocalToParentTR(30, float3.zero, quaternion.identity);
            Assert.That(stream.GetChannelMaskBitCount(), Is.EqualTo(4));
            Assert.That(stream.HasNoChannelMasks(), Is.False);
            Assert.That(stream.HasAnyChannelMasks(), Is.True);
            Assert.That(stream.HasAllChannelMasks(), Is.False);
            Assert.IsTrue(stream.GetTranslationChannelMask(30));
            Assert.IsTrue(stream.GetRotationChannelMask(30));

            stream.SetLocalToRootTRS(2, float3.zero, quaternion.identity, new float3(1));
            Assert.That(stream.GetChannelMaskBitCount(), Is.EqualTo(7));
            Assert.That(stream.HasNoChannelMasks(), Is.False);
            Assert.That(stream.HasAnyChannelMasks(), Is.True);
            Assert.That(stream.HasAllChannelMasks(), Is.False);
            Assert.IsTrue(stream.GetTranslationChannelMask(2));
            Assert.IsTrue(stream.GetRotationChannelMask(2));
            Assert.IsTrue(stream.GetScaleChannelMask(2));
        }

        [Test]
        public void GettingAnimationStreamValueDoesNotUpdateMask()
        {
            var rig = m_Rig.Value;
            var buffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var channelCount = rig.Value.Bindings.BindingCount;

            Assert.That(channelCount, Is.EqualTo(99));

            var stream = AnimationStream.Create(rig, buffer);

            Assert.That(stream.GetChannelMaskBitCount(), Is.EqualTo(0));

            stream.GetFloat(0);
            Assert.That(stream.GetChannelMaskBitCount(), Is.EqualTo(0));

            stream.GetInt(2);
            Assert.That(stream.GetChannelMaskBitCount(), Is.EqualTo(0));

            stream.GetLocalToParentTR(30, out float3 _, out quaternion _);
            Assert.That(stream.GetChannelMaskBitCount(), Is.EqualTo(0));

            stream.GetLocalToRootTRS(2, out float3 _, out quaternion _, out float3 _);
            Assert.That(stream.GetChannelMaskBitCount(), Is.EqualTo(0));
        }

        [Test]
        public void ClipSamplingSetsChannelMask()
        {
            var rig = m_Rig.Value;
            var buffer1 = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var stream1 = AnimationStream.Create(rig, buffer1);
            var buffer2 = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var stream2 = AnimationStream.Create(rig, buffer2);
            var channelCount = rig.Value.Bindings.BindingCount;

            var clip1 = ClipManager.Instance.GetClipFor(m_Rig, m_Clip1);
            Core.EvaluateClip(clip1, 0, ref stream1, 0);
            Assert.That(stream1.GetChannelMaskBitCount(), Is.EqualTo(8));
            Assert.That(stream1.HasNoChannelMasks(), Is.False);
            Assert.That(stream1.HasAnyChannelMasks(), Is.True);
            Assert.That(stream1.HasAllChannelMasks(), Is.False);

            Assert.IsTrue(stream1.GetTranslationChannelMask(0));
            Assert.IsTrue(stream1.GetTranslationChannelMask(4));
            Assert.IsTrue(stream1.GetRotationChannelMask(0));
            Assert.IsTrue(stream1.GetRotationChannelMask(6));
            Assert.IsTrue(stream1.GetScaleChannelMask(0));
            Assert.IsTrue(stream1.GetFloatChannelMask(0));
            Assert.IsTrue(stream1.GetFloatChannelMask(1));
            Assert.IsTrue(stream1.GetIntChannelMask(0));


            var clip2 = ClipManager.Instance.GetClipFor(m_Rig, m_Clip2);
            Core.EvaluateClip(clip2, 0, ref stream2, 0);
            Assert.That(stream2.GetChannelMaskBitCount(), Is.EqualTo(12));

            Assert.IsTrue(stream2.GetTranslationChannelMask(0));
            Assert.IsTrue(stream2.GetTranslationChannelMask(1));
            Assert.IsTrue(stream2.GetTranslationChannelMask(30));
            Assert.IsTrue(stream2.GetRotationChannelMask(0));
            Assert.IsTrue(stream2.GetRotationChannelMask(1));
            Assert.IsTrue(stream2.GetRotationChannelMask(30));
            Assert.IsTrue(stream2.GetScaleChannelMask(0));
            Assert.IsTrue(stream2.GetScaleChannelMask(1));
            Assert.IsTrue(stream2.GetScaleChannelMask(30));
            Assert.IsTrue(stream2.GetFloatChannelMask(0));
            Assert.IsTrue(stream2.GetFloatChannelMask(2));
            Assert.IsTrue(stream2.GetIntChannelMask(1));
        }

        void ValidateMixingStreamChannel(ref AnimationStream outputStream)
        {
            Assert.That(outputStream.GetChannelMaskBitCount(), Is.EqualTo(16));

            Assert.IsTrue(outputStream.GetTranslationChannelMask(0));
            Assert.IsTrue(outputStream.GetTranslationChannelMask(1));
            Assert.IsTrue(outputStream.GetTranslationChannelMask(4));
            Assert.IsTrue(outputStream.GetTranslationChannelMask(30));
            Assert.IsTrue(outputStream.GetRotationChannelMask(0));
            Assert.IsTrue(outputStream.GetRotationChannelMask(1));
            Assert.IsTrue(outputStream.GetRotationChannelMask(6));
            Assert.IsTrue(outputStream.GetRotationChannelMask(30));
            Assert.IsTrue(outputStream.GetScaleChannelMask(0));
            Assert.IsTrue(outputStream.GetScaleChannelMask(1));
            Assert.IsTrue(outputStream.GetScaleChannelMask(30));
            Assert.IsTrue(outputStream.GetFloatChannelMask(0));
            Assert.IsTrue(outputStream.GetFloatChannelMask(1));
            Assert.IsTrue(outputStream.GetFloatChannelMask(2));
            Assert.IsTrue(outputStream.GetIntChannelMask(0));
            Assert.IsTrue(outputStream.GetIntChannelMask(1));
        }

        [Test]
        public void MixingClipSetChannelMask()
        {
            var rig = m_Rig.Value;
            var buffer1 = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var stream1 = AnimationStream.Create(rig, buffer1);
            var buffer2 = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var stream2 = AnimationStream.Create(rig, buffer2);
            var outputBuffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var outputStream = AnimationStream.Create(rig, outputBuffer);
            var channelCount = rig.Value.Bindings.BindingCount;

            var clip1 = ClipManager.Instance.GetClipFor(m_Rig, m_Clip1);
            Core.EvaluateClip(clip1, 0, ref stream1, 0);
            Assert.That(stream1.GetChannelMaskBitCount(), Is.EqualTo(8));

            var clip2 = ClipManager.Instance.GetClipFor(m_Rig, m_Clip2);
            Core.EvaluateClip(clip2, 0, ref stream2, 0);
            Assert.That(stream2.GetChannelMaskBitCount(), Is.EqualTo(12));

            Core.Blend(ref outputStream, ref stream1, ref stream2, 0.5f);
            ValidateMixingStreamChannel(ref outputStream);
            outputStream.ClearChannelMasks();

            Core.BlendOverrideLayer(ref outputStream, ref stream1, 0.5f);
            Core.BlendOverrideLayer(ref outputStream, ref stream2, 0.5f);
            ValidateMixingStreamChannel(ref outputStream);
            outputStream.ClearChannelMasks();

            Core.BlendAdditiveLayer(ref outputStream, ref stream1, 0.5f);
            Core.BlendAdditiveLayer(ref outputStream, ref stream2, 0.5f);
            ValidateMixingStreamChannel(ref outputStream);
        }
    }
}
