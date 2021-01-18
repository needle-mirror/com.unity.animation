using System;
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

        private Rig m_Rig64Channels;
        private Rig m_Rig128Channels;

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();

            {
                var denseClip = CreateConstantDenseClip(
                    new[] { ("Root", float3.zero), ("Child4", float3.zero) },
                    new[] { ("Root", quaternion.identity), ("Child6", quaternion.identity) },
                    new[] { ("Root", new float3(1.0f)) },
                    new[] { ("PropertyA", 1.0f), ("PropertyB", 2.0f) },
                    new[] { ("PropertyD", 10) });

                var blobPath = "AnimationStreamMaskTestsDenseClip1.blob";
                BlobFile.WriteBlobAsset(ref denseClip, blobPath);
            }
            {
                var denseClip = CreateConstantDenseClip(
                    new[] { ("Root", float3.zero), ("Child1", float3.zero), ("CustomTranslation", float3.zero) },
                    new[] { ("Root", quaternion.identity), ("Child1", quaternion.identity), ("CustomRotation", quaternion.identity) },
                    new[] { ("Root", new float3(1.0f)), ("Child1", new float3(1.0f)), ("CustomScale", new float3(1.0f)) },
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

            var animationChannel = new IAnimationChannel[]
            {
                new FloatChannel {Id = FloatChannelID("PropertyA") },
                new FloatChannel {Id = FloatChannelID("PropertyB") },
                new FloatChannel {Id = FloatChannelID("PropertyC") },
                new IntChannel {Id = IntegerChannelID("PropertyD") },
                new IntChannel {Id = IntegerChannelID("PropertyE") },
                new IntChannel {Id = IntegerChannelID("PropertyF") },
                new LocalScaleChannel {Id = "CustomScale"},
                new LocalRotationChannel {Id = "CustomRotation"},
                new LocalTranslationChannel {Id = "CustomTranslation"}
            };

            // Create rig
            m_Rig = new Rig { Value = CreateTestRigDefinition(30, animationChannel) };
            m_Rig64Channels = new Rig { Value = CreateTestRigDefinition(64, null) };
            m_Rig128Channels = new Rig { Value = CreateTestRigDefinition(128, null) };

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
        public void CanSetUnsafeBitArray()
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
        public void CanCopyUnsafeBitArray()
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
        public void CanOrUnsafeBitArrayInPlace()
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
        public void CanOrUnsafeBitArray()
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
        public void CanAndUnsafeBitArrayInPlace()
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
        public void CanAndUnsafeBitArray()
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
        public void CanInvertUnsafeBitArray()
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
        public unsafe void CanSetChannelMask()
        {
            var maskSizeInBytes = Core.AlignUp(m_Rig64Channels.Value.Value.Bindings.BindingCount, 64) / 8;

            var channelMask = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);

            channelMask.Clear();

            Assert.That(channelMask.CountChannels(), Is.EqualTo(0));

            channelMask.Set(true);
            Assert.That(channelMask.CountChannels(), Is.EqualTo(m_Rig64Channels.Value.Value.Bindings.BindingCount));

            channelMask.Set(false);
            Assert.That(channelMask.CountChannels(), Is.EqualTo(0));
        }

        [Test]
        public unsafe void CanCopyChannelMask()
        {
            var maskSizeInBytes = Core.AlignUp(m_Rig64Channels.Value.Value.Bindings.BindingCount, 64) / 8;

            var channelMaskA = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);
            var channelMaskB = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);

            channelMaskA.Clear();
            channelMaskB.Clear();

            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(0));
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(0));

            channelMaskA.Set(true);
            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(m_Rig64Channels.Value.Value.Bindings.BindingCount));

            channelMaskB.CopyFrom(ref channelMaskA);
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(m_Rig64Channels.Value.Value.Bindings.BindingCount));
        }

        [Test]
        public unsafe void CanOrChannelMaskInPlace()
        {
            var maskSizeInBytes = Core.AlignUp(m_Rig64Channels.Value.Value.Bindings.BindingCount, 64) / 8;

            var channelMaskA = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);
            var channelMaskB = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);

            channelMaskA.Clear();
            channelMaskB.Clear();

            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(0));
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(0));

            channelMaskB.Or(ref channelMaskA);
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(0));

            channelMaskA.Set(true);
            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(m_Rig64Channels.Value.Value.Bindings.BindingCount));

            channelMaskB.Or(ref channelMaskA);
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(m_Rig64Channels.Value.Value.Bindings.BindingCount));

            channelMaskA.Clear();
            channelMaskB.Clear();

            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(0));
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(0));

            channelMaskA.Set(10, true, 25);
            channelMaskB.Or(ref channelMaskA);

            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(25));
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(25));
        }

        [Test]
        public unsafe void CanOrChannekMask()
        {
            var maskSizeInBytes = Core.AlignUp(m_Rig64Channels.Value.Value.Bindings.BindingCount, 64) / 8;

            var channelMaskA = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);
            var channelMaskB = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);
            var channelMaskResult = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);

            channelMaskA.Clear();
            channelMaskB.Clear();
            channelMaskResult.Clear();

            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(0));
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(0));
            Assert.That(channelMaskResult.CountChannels(), Is.EqualTo(0));

            channelMaskA.Set(10, true, 30);
            channelMaskB.Set(45, true, 15);

            channelMaskResult.Or(ref channelMaskA, ref channelMaskB);
            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(30), "channelMaskA shouldn't be modify by the operation.");
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(15), "channelMaskB shouldn't be modify by the operation.");
            Assert.That(channelMaskResult.CountChannels(), Is.EqualTo(30 + 15));
        }

        [Test]
        public unsafe void CanAndChannekMaskInPlace()
        {
            var maskSizeInBytes = Core.AlignUp(m_Rig64Channels.Value.Value.Bindings.BindingCount, 64) / 8;

            var channelMaskA = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);
            var channelMaskB = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);

            channelMaskA.Clear();
            channelMaskB.Clear();

            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(0));
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(0));

            channelMaskB.And(ref channelMaskA);
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(0));

            channelMaskA.Set(true);
            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(m_Rig64Channels.Value.Value.Bindings.BindingCount));

            channelMaskB.And(ref channelMaskA);
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(0));

            channelMaskA.Clear();
            channelMaskA.Set(10, true, 25);
            channelMaskB.Set(25, true, 25);

            channelMaskB.And(ref channelMaskA);
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(10));
        }

        [Test]
        public unsafe void CanAndChannekMask()
        {
            var maskSizeInBytes = Core.AlignUp(m_Rig64Channels.Value.Value.Bindings.BindingCount, 64) / 8;

            var channelMaskA = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);
            var channelMaskB = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);
            var channelMaskResult = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);

            channelMaskA.Clear();
            channelMaskB.Clear();
            channelMaskResult.Clear();

            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(0));
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(0));
            Assert.That(channelMaskResult.CountChannels(), Is.EqualTo(0));

            channelMaskA.Set(0, true, 30);
            channelMaskB.Set(45, true, 15);

            channelMaskResult.And(ref channelMaskA, ref channelMaskB);
            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(30), "channelMaskA shouldn't be modify by the operation.");
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(15), "channelMaskB shouldn't be modify by the operation.");
            Assert.That(channelMaskResult.CountChannels(), Is.EqualTo(0));

            channelMaskA.Set(35, true, 20);

            channelMaskResult.And(ref channelMaskA, ref channelMaskB);
            Assert.That(channelMaskA.CountChannels(), Is.EqualTo(30 + 20), "channelMaskA shouldn't be modify by the operation.");
            Assert.That(channelMaskB.CountChannels(), Is.EqualTo(15), "channelMaskB shouldn't be modify by the operation.");
            Assert.That(channelMaskResult.CountChannels(), Is.EqualTo(10));
        }

        [Test]
        public unsafe void CanInvertChannelMask()
        {
            var maskSizeInBytes = Core.AlignUp(m_Rig64Channels.Value.Value.Bindings.BindingCount, 64) / 8;
            var channelMask = new ChannelMask(UnsafeUtility.Malloc(maskSizeInBytes, 16, Allocator.Temp), m_Rig64Channels, false, Allocator.Temp);

            channelMask.Clear();
            Assert.That(channelMask.CountChannels(), Is.EqualTo(0));

            channelMask.Invert();
            Assert.That(channelMask.CountChannels(), Is.EqualTo(m_Rig64Channels.Value.Value.Bindings.BindingCount));

            channelMask.Clear();
            channelMask.Set(10, true, 30);
            Assert.That(channelMask.CountChannels(), Is.EqualTo(30));

            channelMask.Invert();
            Assert.That(channelMask.CountChannels(), Is.EqualTo(m_Rig64Channels.Value.Value.Bindings.BindingCount - 30));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(30)]
        [TestCase(99)]
        [TestCase(130)]
        public void AnimationStreamMaskLenghtMatchBindingCount(int boneCount)
        {
            const int k_ChannelPerBone = 3;

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

            var rig = CreateTestRigDefinition(boneCount, animationChannel);
            var buffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var stream = AnimationStream.Create(rig, buffer);

            Assert.That(stream.m_ChannelPassMasks.Length, Is.EqualTo(Core.AlignUp(boneCount * k_ChannelPerBone + 9, 64)), "Mask lenght doesn't match expected value.");
            Assert.That(stream.m_ChannelPassMasks.Length, Is.EqualTo(Core.AlignUp(rig.Value.Bindings.BindingCount, 64)), "Mask lenght doesn't match expected value.");
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
        public void SettingChannelMaskFromUnmatchingRigThrow()
        {
            var buffer1 = new NativeArray<AnimatedData>(m_Rig64Channels.Value.Value.Bindings.StreamSize, Allocator.Temp);
            var stream1 = AnimationStream.Create(m_Rig64Channels.Value, buffer1);

            var buffer2 = new NativeArray<AnimatedData>(m_Rig128Channels.Value.Value.Bindings.StreamSize, Allocator.Temp);
            var stream2 = AnimationStream.Create(m_Rig128Channels.Value, buffer2);

            Assert.Throws<ArgumentException>(() => stream1.FrameMask = stream2.PassMask);
            Assert.Throws<ArgumentException>(() => stream1.PassMask = stream2.FrameMask);
            Assert.Throws<ArgumentException>(() => stream2.PassMask = stream1.FrameMask);
            Assert.Throws<ArgumentException>(() => stream2.FrameMask = stream1.PassMask);

            Assert.DoesNotThrow(() => stream1.PassMask = stream1.FrameMask);
            Assert.DoesNotThrow(() => stream2.PassMask = stream2.FrameMask);
            Assert.DoesNotThrow(() => stream1.FrameMask = stream1.PassMask);
            Assert.DoesNotThrow(() => stream2.FrameMask = stream2.PassMask);
        }

        [Test]
        public void SettingChannelMaskOnReadOnlyStreamThrow()
        {
            var buffer1 = new NativeArray<AnimatedData>(m_Rig64Channels.Value.Value.Bindings.StreamSize, Allocator.Temp);
            var stream1 = AnimationStream.CreateReadOnly(m_Rig64Channels.Value, buffer1);

            var buffer2 = new NativeArray<AnimatedData>(m_Rig64Channels.Value.Value.Bindings.StreamSize, Allocator.Temp);
            var stream2 = AnimationStream.Create(m_Rig64Channels.Value, buffer2);

            Assert.Throws<InvalidOperationException>(() => stream1.FrameMask = stream2.PassMask);
            Assert.Throws<InvalidOperationException>(() => stream1.PassMask = stream2.FrameMask);
        }

        [Test]
        public void ChangingChannelMaskOnReadOnlyStreamThrow()
        {
            var buffer1 = new NativeArray<AnimatedData>(m_Rig64Channels.Value.Value.Bindings.StreamSize, Allocator.Temp);
            var stream1 = AnimationStream.CreateReadOnly(m_Rig64Channels.Value, buffer1);

            Assert.Throws<InvalidOperationException>(() => stream1.FrameMask.Clear());
            Assert.Throws<InvalidOperationException>(() => stream1.FrameMask.Set(true));
            Assert.Throws<InvalidOperationException>(() => stream1.FrameMask.Invert());
        }

#endif
        [Test]
        public void SettingAnimationStreamValueUpdatesMask()
        {
            var rig = m_Rig.Value;
            var buffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var channelCount = rig.Value.Bindings.BindingCount;

            Assert.That(channelCount, Is.EqualTo(99));

            var stream = AnimationStream.Create(rig, buffer);

            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(0));
            Assert.That(stream.PassMask.HasNone(), Is.True);
            Assert.That(stream.PassMask.HasAny(), Is.False);
            Assert.That(stream.PassMask.HasAll(), Is.False);

            Assert.That(stream.FrameMask.CountChannels(), Is.EqualTo(0));
            Assert.That(stream.FrameMask.HasNone(), Is.True);
            Assert.That(stream.FrameMask.HasAny(), Is.False);
            Assert.That(stream.FrameMask.HasAll(), Is.False);

            stream.SetFloat(0, 1.0f);

            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(1));
            Assert.That(stream.PassMask.HasNone(), Is.False);
            Assert.That(stream.PassMask.HasAny(), Is.True);
            Assert.That(stream.PassMask.HasAll(), Is.False);
            Assert.IsTrue(stream.PassMask.IsFloatSet(0));

            Assert.That(stream.FrameMask.CountChannels(), Is.EqualTo(1));
            Assert.That(stream.FrameMask.HasNone(), Is.False);
            Assert.That(stream.FrameMask.HasAny(), Is.True);
            Assert.That(stream.FrameMask.HasAll(), Is.False);
            Assert.IsTrue(stream.PassMask.IsFloatSet(0));

            stream.SetInt(2, 1);

            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(2));
            Assert.That(stream.PassMask.HasNone(), Is.False);
            Assert.That(stream.PassMask.HasAny(), Is.True);
            Assert.That(stream.PassMask.HasAll(), Is.False);
            Assert.IsTrue(stream.PassMask.IsIntSet(2));

            Assert.That(stream.FrameMask.CountChannels(), Is.EqualTo(2));
            Assert.That(stream.FrameMask.HasNone(), Is.False);
            Assert.That(stream.FrameMask.HasAny(), Is.True);
            Assert.That(stream.FrameMask.HasAll(), Is.False);
            Assert.IsTrue(stream.PassMask.IsIntSet(2));

            stream.SetLocalToParentTR(30, float3.zero, quaternion.identity);
            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(4));
            Assert.That(stream.PassMask.HasNone(), Is.False);
            Assert.That(stream.PassMask.HasAny(), Is.True);
            Assert.That(stream.PassMask.HasAll(), Is.False);
            Assert.IsTrue(stream.PassMask.IsTranslationSet(30));
            Assert.IsTrue(stream.PassMask.IsRotationSet(30));

            Assert.That(stream.FrameMask.CountChannels(), Is.EqualTo(4));
            Assert.That(stream.FrameMask.HasNone(), Is.False);
            Assert.That(stream.FrameMask.HasAny(), Is.True);
            Assert.That(stream.FrameMask.HasAll(), Is.False);
            Assert.IsTrue(stream.FrameMask.IsTranslationSet(30));
            Assert.IsTrue(stream.FrameMask.IsRotationSet(30));

            stream.SetLocalToRootTRS(2, float3.zero, quaternion.identity, new float3(1));
            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(7));
            Assert.That(stream.PassMask.HasNone(), Is.False);
            Assert.That(stream.PassMask.HasAny(), Is.True);
            Assert.That(stream.PassMask.HasAll(), Is.False);
            Assert.IsTrue(stream.PassMask.IsTranslationSet(2));
            Assert.IsTrue(stream.PassMask.IsRotationSet(2));
            Assert.IsTrue(stream.PassMask.IsScaleSet(2));

            Assert.That(stream.FrameMask.CountChannels(), Is.EqualTo(7));
            Assert.That(stream.FrameMask.HasNone(), Is.False);
            Assert.That(stream.FrameMask.HasAny(), Is.True);
            Assert.That(stream.FrameMask.HasAll(), Is.False);
            Assert.IsTrue(stream.FrameMask.IsTranslationSet(2));
            Assert.IsTrue(stream.FrameMask.IsRotationSet(2));
            Assert.IsTrue(stream.FrameMask.IsScaleSet(2));
        }

        [Test]
        public void GettingAnimationStreamValueDoesNotUpdateMask()
        {
            var rig = m_Rig.Value;
            var buffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var channelCount = rig.Value.Bindings.BindingCount;

            Assert.That(channelCount, Is.EqualTo(99));

            var stream = AnimationStream.Create(rig, buffer);

            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(0));
            Assert.That(stream.FrameMask.CountChannels(), Is.EqualTo(0));

            stream.GetFloat(0);
            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(0));
            Assert.That(stream.FrameMask.CountChannels(), Is.EqualTo(0));

            stream.GetInt(2);
            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(0));
            Assert.That(stream.FrameMask.CountChannels(), Is.EqualTo(0));

            stream.GetLocalToParentTR(30, out float3 _, out quaternion _);
            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(0));
            Assert.That(stream.FrameMask.CountChannels(), Is.EqualTo(0));

            stream.GetLocalToRootTRS(2, out float3 _, out quaternion _, out float3 _);
            Assert.That(stream.PassMask.CountChannels(), Is.EqualTo(0));
            Assert.That(stream.FrameMask.CountChannels(), Is.EqualTo(0));
        }

        [Test]
        public void ClipSamplingSetsChannelMask()
        {
            var rig = m_Rig.Value;
            var buffer1 = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var stream1 = AnimationStream.Create(rig, buffer1);
            var buffer2 = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp);
            var stream2 = AnimationStream.Create(rig, buffer2);

            var clip1 = ClipManager.Instance.GetClipFor(m_Rig, m_Clip1);
            Core.EvaluateClip(clip1, 0, ref stream1, 0);
            Assert.That(stream1.PassMask.CountChannels(), Is.EqualTo(8));
            Assert.That(stream1.PassMask.HasNone(), Is.False);
            Assert.That(stream1.PassMask.HasAny(), Is.True);
            Assert.That(stream1.PassMask.HasAll(), Is.False);

            Assert.That(stream1.FrameMask.CountChannels(), Is.EqualTo(8));
            Assert.That(stream1.FrameMask.HasNone(), Is.False);
            Assert.That(stream1.FrameMask.HasAny(), Is.True);
            Assert.That(stream1.FrameMask.HasAll(), Is.False);

            Assert.IsTrue(stream1.PassMask.IsTranslationSet(0));
            Assert.IsTrue(stream1.PassMask.IsTranslationSet(4));
            Assert.IsTrue(stream1.PassMask.IsRotationSet(0));
            Assert.IsTrue(stream1.PassMask.IsRotationSet(6));
            Assert.IsTrue(stream1.PassMask.IsScaleSet(0));
            Assert.IsTrue(stream1.PassMask.IsFloatSet(0));
            Assert.IsTrue(stream1.PassMask.IsFloatSet(1));
            Assert.IsTrue(stream1.PassMask.IsIntSet(0));

            Assert.IsTrue(stream1.FrameMask.IsTranslationSet(0));
            Assert.IsTrue(stream1.FrameMask.IsTranslationSet(4));
            Assert.IsTrue(stream1.FrameMask.IsRotationSet(0));
            Assert.IsTrue(stream1.FrameMask.IsRotationSet(6));
            Assert.IsTrue(stream1.FrameMask.IsScaleSet(0));
            Assert.IsTrue(stream1.FrameMask.IsFloatSet(0));
            Assert.IsTrue(stream1.FrameMask.IsFloatSet(1));
            Assert.IsTrue(stream1.FrameMask.IsIntSet(0));

            var clip2 = ClipManager.Instance.GetClipFor(m_Rig, m_Clip2);
            Core.EvaluateClip(clip2, 0, ref stream2, 0);
            Assert.That(stream2.PassMask.CountChannels(), Is.EqualTo(12));
            Assert.That(stream2.FrameMask.CountChannels(), Is.EqualTo(12));

            Assert.IsTrue(stream2.PassMask.IsTranslationSet(0));
            Assert.IsTrue(stream2.PassMask.IsTranslationSet(1));
            Assert.IsTrue(stream2.PassMask.IsTranslationSet(30));
            Assert.IsTrue(stream2.PassMask.IsRotationSet(0));
            Assert.IsTrue(stream2.PassMask.IsRotationSet(1));
            Assert.IsTrue(stream2.PassMask.IsRotationSet(30));
            Assert.IsTrue(stream2.PassMask.IsScaleSet(0));
            Assert.IsTrue(stream2.PassMask.IsScaleSet(1));
            Assert.IsTrue(stream2.PassMask.IsScaleSet(30));
            Assert.IsTrue(stream2.PassMask.IsFloatSet(0));
            Assert.IsTrue(stream2.PassMask.IsFloatSet(2));
            Assert.IsTrue(stream2.PassMask.IsIntSet(1));

            Assert.IsTrue(stream2.FrameMask.IsTranslationSet(0));
            Assert.IsTrue(stream2.FrameMask.IsTranslationSet(1));
            Assert.IsTrue(stream2.FrameMask.IsTranslationSet(30));
            Assert.IsTrue(stream2.FrameMask.IsRotationSet(0));
            Assert.IsTrue(stream2.FrameMask.IsRotationSet(1));
            Assert.IsTrue(stream2.FrameMask.IsRotationSet(30));
            Assert.IsTrue(stream2.FrameMask.IsScaleSet(0));
            Assert.IsTrue(stream2.FrameMask.IsScaleSet(1));
            Assert.IsTrue(stream2.FrameMask.IsScaleSet(30));
            Assert.IsTrue(stream2.FrameMask.IsFloatSet(0));
            Assert.IsTrue(stream2.FrameMask.IsFloatSet(2));
            Assert.IsTrue(stream2.FrameMask.IsIntSet(1));
        }

        void ValidateMixingStreamChannel(ref AnimationStream outputStream)
        {
            Assert.That(outputStream.PassMask.CountChannels(), Is.EqualTo(16));
            Assert.That(outputStream.FrameMask.CountChannels(), Is.EqualTo(16));

            Assert.IsTrue(outputStream.PassMask.IsTranslationSet(0));
            Assert.IsTrue(outputStream.PassMask.IsTranslationSet(1));
            Assert.IsTrue(outputStream.PassMask.IsTranslationSet(4));
            Assert.IsTrue(outputStream.PassMask.IsTranslationSet(30));
            Assert.IsTrue(outputStream.PassMask.IsRotationSet(0));
            Assert.IsTrue(outputStream.PassMask.IsRotationSet(1));
            Assert.IsTrue(outputStream.PassMask.IsRotationSet(6));
            Assert.IsTrue(outputStream.PassMask.IsRotationSet(30));
            Assert.IsTrue(outputStream.PassMask.IsScaleSet(0));
            Assert.IsTrue(outputStream.PassMask.IsScaleSet(1));
            Assert.IsTrue(outputStream.PassMask.IsScaleSet(30));
            Assert.IsTrue(outputStream.PassMask.IsFloatSet(0));
            Assert.IsTrue(outputStream.PassMask.IsFloatSet(1));
            Assert.IsTrue(outputStream.PassMask.IsFloatSet(2));
            Assert.IsTrue(outputStream.PassMask.IsIntSet(0));
            Assert.IsTrue(outputStream.PassMask.IsIntSet(1));

            Assert.IsTrue(outputStream.FrameMask.IsTranslationSet(0));
            Assert.IsTrue(outputStream.FrameMask.IsTranslationSet(1));
            Assert.IsTrue(outputStream.FrameMask.IsTranslationSet(4));
            Assert.IsTrue(outputStream.FrameMask.IsTranslationSet(30));
            Assert.IsTrue(outputStream.FrameMask.IsRotationSet(0));
            Assert.IsTrue(outputStream.FrameMask.IsRotationSet(1));
            Assert.IsTrue(outputStream.FrameMask.IsRotationSet(6));
            Assert.IsTrue(outputStream.FrameMask.IsRotationSet(30));
            Assert.IsTrue(outputStream.FrameMask.IsScaleSet(0));
            Assert.IsTrue(outputStream.FrameMask.IsScaleSet(1));
            Assert.IsTrue(outputStream.FrameMask.IsScaleSet(30));
            Assert.IsTrue(outputStream.FrameMask.IsFloatSet(0));
            Assert.IsTrue(outputStream.FrameMask.IsFloatSet(1));
            Assert.IsTrue(outputStream.FrameMask.IsFloatSet(2));
            Assert.IsTrue(outputStream.FrameMask.IsIntSet(0));
            Assert.IsTrue(outputStream.FrameMask.IsIntSet(1));
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
            Assert.That(stream1.PassMask.CountChannels(), Is.EqualTo(8));
            Assert.That(stream1.FrameMask.CountChannels(), Is.EqualTo(8));

            var clip2 = ClipManager.Instance.GetClipFor(m_Rig, m_Clip2);
            Core.EvaluateClip(clip2, 0, ref stream2, 0);
            Assert.That(stream2.PassMask.CountChannels(), Is.EqualTo(12));
            Assert.That(stream2.FrameMask.CountChannels(), Is.EqualTo(12));

            Core.Blend(ref outputStream, ref stream1, ref stream2, 0.5f);
            ValidateMixingStreamChannel(ref outputStream);
            outputStream.ClearMasks();

            Core.BlendOverrideLayer(ref outputStream, ref stream1, 0.5f);
            Core.BlendOverrideLayer(ref outputStream, ref stream2, 0.5f);
            ValidateMixingStreamChannel(ref outputStream);
            outputStream.ClearMasks();

            Core.BlendAdditiveLayer(ref outputStream, ref stream1, 0.5f);
            Core.BlendAdditiveLayer(ref outputStream, ref stream2, 0.5f);
            ValidateMixingStreamChannel(ref outputStream);
        }
    }
}
