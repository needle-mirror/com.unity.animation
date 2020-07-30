using System;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Animation.Tests
{
    public class VelocityTests : AnimationTestsFixture
    {
        private const float kEulerAnglesTolerance = 1e-5f;
        private const float k_DeltaTime = 0.5f;

        static readonly quaternion k_20DegreesRotationX = quaternion.EulerXYZ(new float3(math.radians(20f), 0f, 0f));
        static readonly float3 k_ForwardTranslation = new float3(1f, 0f, 0f);

        private BlobAssetReference<RigDefinition> m_Rig;

        private NativeArray<AnimatedData> m_PreviousBuffer;
        private NativeArray<AnimatedData> m_Buffer;

        private AnimationStream m_PreviousStream;
        private AnimationStream m_Stream;

        private readonly Float3AbsoluteEqualityComparer EulerAnglesComparer;

        public VelocityTests()
        {
            EulerAnglesComparer = new Float3AbsoluteEqualityComparer(kEulerAnglesTolerance);
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            m_Rig = RigBuilder.CreateRigDefinition(new[]
            {
                new SkeletonNode { ParentIndex = -1, Id = "Root", AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one() },
                new SkeletonNode { ParentIndex = 0, Id = "UpperArm", AxisIndex = -1, LocalTranslationDefaultValue = k_ForwardTranslation, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one()},
                new SkeletonNode { ParentIndex = 1, Id = "LowerArm", AxisIndex = -1, LocalTranslationDefaultValue = k_ForwardTranslation, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one()},
                new SkeletonNode { ParentIndex = 2, Id = "Hand", AxisIndex = -1, LocalTranslationDefaultValue = k_ForwardTranslation, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = mathex.one()},
            });

            m_PreviousBuffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            m_PreviousStream = AnimationStream.Create(m_Rig, m_PreviousBuffer);
            m_PreviousStream.ResetToDefaultValues();

            m_Buffer = new NativeArray<AnimatedData>(m_Rig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            m_Stream = AnimationStream.Create(m_Rig, m_Buffer);
            m_Stream.ResetToDefaultValues();

            m_Stream.SetLocalToParentTranslation(0, k_ForwardTranslation);
            m_Stream.SetLocalToParentRotation(1, k_20DegreesRotationX);
            m_Stream.SetLocalToParentRotation(2, k_20DegreesRotationX);
            m_Stream.SetLocalToParentRotation(3, k_20DegreesRotationX);
        }

        [OneTimeTearDown]
        protected override void OneTimeTearDown()
        {
            base.OneTimeTearDown();

            m_PreviousBuffer.Dispose();
            m_Buffer.Dispose();
        }

        [Test]
        public void ComputeLocalToWorldAngularVelocities_Returns_ExpectedResults()
        {
            var previousLocalToWorld = new float4x4(new RigidTransform(quaternion.identity, float3.zero));
            var localToWorld = new float4x4(new RigidTransform(k_20DegreesRotationX, k_ForwardTranslation));

            // 20 degrees world rotation and on indices 1, 2 and 3 at 2 fps.
            var expectedVelocities = new NativeArray<float3>(new[]
            {
                new float3(math.radians(40f), 0f, 0f),
                new float3(math.radians(80f), 0f, 0f),
                new float3(math.radians(120f), 0f, 0f),
                new float3(math.radians(160f), 0f, 0f),
            }, Allocator.Temp);

            var velocities = new NativeArray<float3>(4, Allocator.Temp);

            try
            {
                Core.ComputeLocalToWorldAngularVelocities(localToWorld, previousLocalToWorld, ref m_Stream, ref m_PreviousStream, k_DeltaTime, velocities);

                for (int i = 0; i < velocities.Length; ++i)
                {
                    Assert.That(velocities[i], Is.EqualTo(expectedVelocities[i]).Using(EulerAnglesComparer), $"Angular velocity at index {i} does not match");
                }
            }
            finally
            {
                expectedVelocities.Dispose();
                velocities.Dispose();
            }
        }

        [Test]
        public void ComputeLocalToParentAngularVelocities_Returns_ExpectedResults()
        {
            // 20 degrees rotation on indices 1, 2 and 3 at 2 fps
            var expectedVelocities = new NativeArray<float3>(new[]
            {
                new float3(math.radians(0f), 0f, 0f),
                new float3(math.radians(40f), 0f, 0f),
                new float3(math.radians(40f), 0f, 0f),
                new float3(math.radians(40f), 0f, 0f),
            }, Allocator.Temp);

            var velocities = new NativeArray<float3>(4, Allocator.Temp);

            try
            {
                Core.ComputeLocalToParentAngularVelocities(ref m_Stream, ref m_PreviousStream, k_DeltaTime, velocities);

                for (int i = 0; i < velocities.Length; ++i)
                {
                    Assert.That(velocities[i], Is.EqualTo(expectedVelocities[i]).Using(EulerAnglesComparer), $"Angular velocity at index {i} does not match");
                }
            }
            finally
            {
                expectedVelocities.Dispose();
                velocities.Dispose();
            }
        }

        [Test]
        public void ComputeLocalToWorldLinearVelocities_Returns_ExpectedResults()
        {
            var previousLocalToWorld = new float4x4(new RigidTransform(quaternion.identity, float3.zero));
            var localToWorld = new float4x4(new RigidTransform(k_20DegreesRotationX, k_ForwardTranslation));

            // 2*ForwardTranslation at 2 fps
            var expectedVelocities = new NativeArray<float3>(new[]
            {
                new float3(4f, 0f, 0f),
                new float3(4f, 0f, 0f),
                new float3(4f, 0f, 0f),
                new float3(4f, 0f, 0f)
            }, Allocator.Temp);

            var velocities = new NativeArray<float3>(4, Allocator.Temp);

            try
            {
                Core.ComputeLocalToWorldLinearVelocities(localToWorld, previousLocalToWorld, ref m_Stream, ref m_PreviousStream, k_DeltaTime, velocities);

                for (int i = 0; i < velocities.Length; ++i)
                {
                    Assert.That(velocities[i], Is.EqualTo(expectedVelocities[i]).Using(TranslationComparer), $"Angular velocity at index {i} does not match");
                }
            }
            finally
            {
                expectedVelocities.Dispose();
                velocities.Dispose();
            }
        }

        [Test]
        public void ComputeLocalToParentLinearVelocities_Returns_ExpectedResults()
        {
            // ForwardTranslation at index 0 at 2 fps
            var expectedVelocities = new NativeArray<float3>(new[]
            {
                new float3(2f, 0f, 0f),
                new float3(0f, 0f, 0f),
                new float3(0f, 0f, 0f),
                new float3(0f, 0f, 0f)
            }, Allocator.Temp);

            var velocities = new NativeArray<float3>(4, Allocator.Temp);

            try
            {
                Core.ComputeLocalToParentLinearVelocities(ref m_Stream, ref m_PreviousStream, k_DeltaTime, velocities);

                for (int i = 0; i < velocities.Length; ++i)
                {
                    Assert.That(velocities[i], Is.EqualTo(expectedVelocities[i]).Using(TranslationComparer), $"Angular velocity at index {i} does not match");
                }
            }
            finally
            {
                expectedVelocities.Dispose();
                velocities.Dispose();
            }
        }
    }
}
