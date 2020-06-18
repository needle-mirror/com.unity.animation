using System;
using NUnit.Framework;

using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

using Unity.Animation.Tests;
using Unity.PerformanceTesting;


namespace Unity.Animation.PerformanceTests
{
    [BurstCompile]
    public struct BurstedCore
    {
        public delegate void EvaluateClipDelegate(ref BlobAssetReference<ClipInstance> clipInstance, float time, ref AnimationStream stream, int additive);
        public static EvaluateClipDelegate EvaluateClip;

        [BurstCompile]
        static void EvaluateClipExecute(ref BlobAssetReference<ClipInstance> clipInstance, float time, ref AnimationStream stream, int additive)
        {
            Core.EvaluateClip(clipInstance, time, ref stream, additive);
        }

        public static void Initialize()
        {
            if (EvaluateClip != null)
                return;

            EvaluateClip = BurstCompiler.CompileFunctionPointer<EvaluateClipDelegate>(EvaluateClipExecute).Invoke;
        }
    }

    [Category("performance"), Category("animation")]
    public class EvaluateClipPerformanceTests : AnimationTestsFixture
    {
        [Flags]
        public enum ChannelType
        {
            TranslationChannel = 1 << 0,
            RotationChannel = 1 << 1,
            ScaleChannel = 1 << 2,
            FloatChannel = 1 << 3,
            IntChannel = 1 << 4
        };
        protected static int[] s_ChannelCount =
        {
            30, 100, 200, 500, 1000, 2000
        };
        protected static ChannelType[] s_ChannelType =
        {
            ChannelType.TranslationChannel, ChannelType.RotationChannel, ChannelType.ScaleChannel, ChannelType.FloatChannel, ChannelType.IntChannel,
            ChannelType.TranslationChannel | ChannelType.RotationChannel,
            ChannelType.TranslationChannel | ChannelType.RotationChannel | ChannelType.ScaleChannel,
            ChannelType.TranslationChannel | ChannelType.RotationChannel | ChannelType.ScaleChannel | ChannelType.FloatChannel | ChannelType.IntChannel
        };

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            BurstedCore.Initialize();
        }

        BlobAssetReference<RigDefinition> CreateRigDefinition(int channelCount, ChannelType channelType)
        {
            var animationChannel = new IAnimationChannel[channelCount * 5];

            for (int i = 0; i < channelCount; i++)
            {
                if (channelType.HasFlag(ChannelType.TranslationChannel))
                    animationChannel[(i * 5) + 0] = new LocalTranslationChannel { Id = $"{i}" };

                if (channelType.HasFlag(ChannelType.RotationChannel))
                    animationChannel[(i * 5) + 1] = new LocalRotationChannel { Id = $"{i}" };

                if (channelType.HasFlag(ChannelType.ScaleChannel))
                    animationChannel[(i * 5) + 2] = new LocalScaleChannel { Id = $"{i}" };

                if (channelType.HasFlag(ChannelType.FloatChannel))
                    animationChannel[(i * 5) + 3] = new FloatChannel { Id = $"{i}" };

                if (channelType.HasFlag(ChannelType.IntChannel))
                    animationChannel[(i * 5) + 4] = new IntChannel { Id = $"{i}" };
            }
            ;

            return RigBuilder.CreateRigDefinition(animationChannel);
        }

        BlobAssetReference<Clip> CreateClip(int channelCount, float duration)
        {
            var translations = new LinearBinding<float3>[channelCount];
            var rotations = new LinearBinding<quaternion>[channelCount];
            var scales = new LinearBinding<float3>[channelCount];
            var floats = new LinearBinding<float>[channelCount];
            var ints = new LinearBinding<int>[channelCount];

            for (int i = 0; i < channelCount; i++)
            {
                translations[i] = new LinearBinding<float3> { Path = $"{i}", ValueStart = float3.zero, ValueEnd = new float3(i, i, i) };
                rotations[i] = new LinearBinding<quaternion> { Path = $"{i}", ValueStart = quaternion.identity, ValueEnd = new quaternion(1, 0, 0, 0) };
                scales[i] = new LinearBinding<float3> { Path = $"{i}", ValueStart = float3.zero, ValueEnd = new float3(i, i, i) };
                floats[i] = new LinearBinding<float> { Path = $"{i}", ValueStart = 0.0f, ValueEnd = i };
                ints[i] = new LinearBinding<int> { Path = $"{i}", ValueStart = 0, ValueEnd = i };
            }

            return CreateLinearDenseClip(translations, rotations, scales, floats, ints, 0, duration);
        }

        [Test, Performance]
        public void EvaluateClipForward([ValueSource("s_ChannelCount")] int channelCount, [ValueSource("s_ChannelType")] ChannelType channelType)
        {
            var duration = 1.0f;
            var rig = CreateRigDefinition(channelCount, channelType);
            var clip = CreateClip(channelCount, duration);
            var clipInstance = ClipInstanceBuilder.Create(rig, clip);

            // Validate that the clips match the rig
            Assert.That(clipInstance.Value.RotationBindingMap.Length, Is.EqualTo(rig.Value.Bindings.RotationBindings.Length));
            Assert.That(clipInstance.Value.TranslationBindingMap.Length, Is.EqualTo(rig.Value.Bindings.TranslationBindings.Length));
            Assert.That(clipInstance.Value.ScaleBindingMap.Length, Is.EqualTo(rig.Value.Bindings.ScaleBindings.Length));
            Assert.That(clipInstance.Value.FloatBindingMap.Length, Is.EqualTo(rig.Value.Bindings.FloatBindings.Length));
            Assert.That(clipInstance.Value.IntBindingMap.Length, Is.EqualTo(rig.Value.Bindings.IntBindings.Length));

            var entity = m_Manager.CreateEntity();
            SetupRigEntity(entity, rig, Entity.Null);

            var stream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            var time = 0.0f;
            var deltaTime = 0.02f;

            Measure.Method(
                () =>
                {
                    BurstedCore.EvaluateClip(ref clipInstance, time, ref stream, 0);
                })
                .SampleGroup("Core.EvaluateClip")
                .WarmupCount(100)
                .MeasurementCount(500)
                .SetUp(() =>
                {
                    time += deltaTime;
                    time = time > duration ? 0 : time;
                })
                .Run();

            var expectedStream = AnimationStream.Create(
                rig,
                m_Manager.GetBuffer<AnimatedData>(entity).AsNativeArray()
            );

            Core.EvaluateClip(clipInstance, time, ref expectedStream, 0);

            for (int i = 0; i < rig.Value.Bindings.RotationBindings.Length; i++)
            {
                Assert.That(stream.GetLocalToParentRotation(i), Is.EqualTo(expectedStream.GetLocalToParentRotation(i)).Using(RotationComparer));
            }

            for (int i = 0; i < rig.Value.Bindings.TranslationBindings.Length; i++)
            {
                Assert.That(stream.GetLocalToParentTranslation(i), Is.EqualTo(expectedStream.GetLocalToParentTranslation(i)).Using(TranslationComparer));
            }

            for (int i = 0; i < rig.Value.Bindings.ScaleBindings.Length; i++)
            {
                Assert.That(stream.GetLocalToParentScale(i), Is.EqualTo(expectedStream.GetLocalToParentScale(i)).Using(ScaleComparer));
            }

            for (int i = 0; i < rig.Value.Bindings.FloatBindings.Length; i++)
            {
                Assert.That(stream.GetFloat(i), Is.EqualTo(expectedStream.GetFloat(i)));
            }

            for (int i = 0; i < rig.Value.Bindings.IntBindings.Length; i++)
            {
                Assert.That(stream.GetInt(i), Is.EqualTo(expectedStream.GetInt(i)));
            }
        }
    }
}
