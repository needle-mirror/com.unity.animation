using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animation.Tests
{
    public class InertialBlendingTests : AnimationTestsFixture
    {
        BlobAssetReference<RigDefinition> m_TestRig;

        SkeletonNode MakeSkeletonNode(string id, int parentIndex)
        {
            return new SkeletonNode
            {
                Id = id,
                ParentIndex = parentIndex,
                AxisIndex = -1,
                LocalRotationDefaultValue = Quaternion.identity,
                LocalScaleDefaultValue = new float3(1, 1, 1)
            };
        }

        BlobAssetReference<RigDefinition> MakeRig(string prefix)
        {
            var skeletonNodes = new[]
            {
                MakeSkeletonNode(prefix + "Root", -1),
                MakeSkeletonNode(prefix + "Hips", 0),
                MakeSkeletonNode(prefix + "LeftUpLeg", 1),
                MakeSkeletonNode(prefix + "RightUpLeg", 1)
            };
            var animationChannels = new IAnimationChannel[]
            {
                new FloatChannel()
                {
                    Id = StringHash.Hash(prefix + "MyFloatChannel"),
                    DefaultValue = 0,
                },
                new IntChannel
                {
                    Id = StringHash.Hash(prefix + "MyIntChannel"),
                    DefaultValue = 1,
                },
            };
            // TODO: Generate a rig with different number of bindings for rotation, translation & scale
            return RigBuilder.CreateRigDefinition(skeletonNodes, null, animationChannels);
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            m_TestRig = MakeRig("test_");
        }

        [OneTimeTearDown]
        protected override void OneTimeTearDown()
        {
            base.OneTimeTearDown();
            m_TestRig.Dispose();
        }

        private struct ComputeInertialBlendingCoefficientsParameters : IDisposable
        {
            NativeArray<AnimatedData> m_CurrentInputBuffer;
            NativeArray<AnimatedData> m_LastPoseBuffer;
            NativeArray<AnimatedData> m_SecondLastPoseBuffer;

            public AnimationStream CurrentInput;
            public AnimationStream LastPose;
            public AnimationStream SecondLastPose;

            public NativeArray<InertialBlendingCoefficients> Coefficients;
            public NativeArray<float3> Directions;

            public ComputeInertialBlendingCoefficientsParameters(BlobAssetReference<RigDefinition> rig)
            {
                m_SecondLastPoseBuffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize,
                    Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                m_LastPoseBuffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
                m_CurrentInputBuffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);

                SecondLastPose = AnimationStream.Create(rig, m_SecondLastPoseBuffer);
                SecondLastPose.ResetToDefaultValues();
                LastPose = AnimationStream.Create(rig, m_LastPoseBuffer);
                LastPose.ResetToDefaultValues();
                CurrentInput = AnimationStream.Create(rig, m_CurrentInputBuffer);
                CurrentInput.ResetToDefaultValues();

                var coefficientNumber = rig.Value.Bindings.BindingCount - rig.Value.Bindings.IntBindings.Length;
                var directionNumber = coefficientNumber - rig.Value.Bindings.FloatBindings.Length;

                Coefficients = new NativeArray<InertialBlendingCoefficients>(coefficientNumber, Allocator.Temp);
                Directions = new NativeArray<float3>(directionNumber, Allocator.Temp);
            }

            public void Dispose()
            {
                m_CurrentInputBuffer.Dispose();
                m_LastPoseBuffer.Dispose();
                m_SecondLastPoseBuffer.Dispose();
                Coefficients.Dispose();
                Directions.Dispose();
            }
        }

        [TestCase(5, 0, .5f, -960, 1200, -400, 0, 0, 5)]
        [TestCase(0, 0, .5f, 0, 0, 0, 0, 0, 0)]
        [TestCase(1e10f, 0, 1, -6e10f, 15e10f, -10e10f,  0, 0, 1e10f)]
        [TestCase(5, 0, 0f, 0, 0, 0, 0, 0, 0)]
        [TestCase(5, 0, 2.595e-8f, 0, 0, 0, 0, 0, 0)] // Duration is small enough so that duration^5 is normal
        [TestCase(5, 0, 1e4f, -3e-19f, 7.5e-15f, -5e-11f, 0, 0, 5)]
        [TestCase(5, -0.2f, .5f, -960, 1200, -400, 0, 0, 5)]
        [TestCase(5, 0.01f, .5f, -912, 1136, -376, 0, -1, 5)]
        [TestCase(5, -1e4f, .5f, -960, 1200, -400, 0, 0, 5)]
        [TestCase(5, 1e4f, .5f, -5.119999e23f, 64e18f, -32e14f, 8e10f, -1e6f, 5)]
        public void InertialBlendingCoefficientTranslationTest(float tX, float vX, float duration, float expectedA, float expectedB, float expectedC
            , float expectedD, float expectedE, float expectedF)
        {
            float3 translation = new float3(tX, 0, 0);
            float3 velocity = new float3(vX, 0, 0);

            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);
            for (int i = 0; i < parameters.CurrentInput.TranslationCount; i++)
            {
                parameters.CurrentInput.SetLocalToParentTranslation(i, parameters.CurrentInput.GetLocalToParentTranslation(i) - translation);
                parameters.SecondLastPose.SetLocalToParentTranslation(i, parameters.SecondLastPose.GetLocalToParentTranslation(i) + velocity);
            }

            Core.ComputeInertialBlendingCoefficients(
                ref parameters.CurrentInput,
                ref parameters.LastPose,
                ref parameters.SecondLastPose,
                0.01f,
                duration,
                parameters.Coefficients,
                parameters.Directions);

            var relativeTolerance = 0.001f;
            for (int i = 0; i < parameters.CurrentInput.TranslationCount; ++i)
            {
                Assert.That(parameters.Coefficients[i].m_ABCD.x, Is.EqualTo(expectedA).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.y, Is.EqualTo(expectedB).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.z, Is.EqualTo(expectedC).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.w, Is.EqualTo(expectedD).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_E, Is.EqualTo(expectedE).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_F, Is.EqualTo(expectedF).Within(relativeTolerance * 100).Percent);
            }
            parameters.Dispose();
        }

        [TestCase(0, 0, .5f, 0, 0, 0, 0, 0, 0)]
        [TestCase(2, 0, .5f, -384, 480, -160, 0, 0, 2)]
        [TestCase(math.PI * 2 + 2, 0, .5f, -384, 480, -160, 0, 0, 2)]
        [TestCase(-2, 0, .5f, -384, 480, -160, 0, 0, 2)]
        // TODO: What do we do in the case of a PI rotation?
        // [TestCase(math.PI, 0, 0, 0, 1, -603.18578f, 753.98223f, -251.32741f,  0, 0, math.PI)]
        [TestCase(5, 0, 0f, 0, 0, 0, 0, 0, 0)]
        [TestCase(5, 0, 2.595e-8f, 0, 0, 0, 0, 0, 0)] // Duration is small enough so that duration^5 is normal
        public void InertialBlendingCoefficientRotationTest(float angle, float angularSpeed, float duration, float expectedA, float expectedB, float expectedC
            , float expectedD, float expectedE, float expectedF)
        {
            quaternion rotation = quaternion.AxisAngle(new float3(1, 0, 0), angle);
            quaternion velocity = quaternion.AxisAngle(new float3(1, 0, 0), angularSpeed);

            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);

            for (int i = 0; i < parameters.CurrentInput.RotationCount; i++)
            {
                parameters.CurrentInput.SetLocalToParentRotation(i, mathex.mul(rotation, parameters.CurrentInput.GetLocalToParentRotation(i)));
                parameters.SecondLastPose.SetLocalToParentRotation(i, mathex.mul(math.inverse(velocity), parameters.SecondLastPose.GetLocalToParentRotation(i)));
            }

            Core.ComputeInertialBlendingCoefficients(
                ref parameters.CurrentInput,
                ref parameters.LastPose,
                ref parameters.SecondLastPose,
                0.01f,
                duration,
                parameters.Coefficients,
                parameters.Directions);

            var relativeTolerance = 0.001f;
            var offset = m_TestRig.Value.Bindings.TranslationBindings.Length;
            for (int i = offset; i < offset + parameters.CurrentInput.RotationCount; ++i)
            {
                Assert.That(parameters.Coefficients[i].m_ABCD.x, Is.EqualTo(expectedA).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.y, Is.EqualTo(expectedB).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.z, Is.EqualTo(expectedC).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.w, Is.EqualTo(expectedD).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_E, Is.EqualTo(expectedE).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_F, Is.EqualTo(expectedF).Within(relativeTolerance * 100).Percent);
            }
            parameters.Dispose();
        }

        [TestCase(5, 0, .5f, -960, 1200, -400, 0, 0, 5)]
        [TestCase(0, 0, .5f, 0, 0, 0, 0, 0, 0)]
        [TestCase(1e10f, 0, 1, -6e10f, 15e10f, -10e10f,  0, 0, 1e10f)]
        [TestCase(5, 0, 0f, 0, 0, 0, 0, 0, 0)]
        [TestCase(5, 0, 2.595e-8f, 0, 0, 0, 0, 0, 0)] // Duration is small enough so that duration^5 is normal
        [TestCase(5, 0, 1e4f, -3e-19f, 7.5e-15f, -5e-11f, 0, 0, 5)]
        [TestCase(5, -0.2f, .5f, -960, 1200, -400, 0, 0, 5)]
        [TestCase(5, 0.01f, .5f, -912, 1136, -376, 0, -1, 5)]
        [TestCase(5, -1e4f, .5f, -960, 1200, -400, 0, 0, 5)]
        [TestCase(5, 1e4f, .5f, -5.119999e23f, 64e18f, -32e14f, 8e10f, -1e6f, 5)]
        public void InertialBlendingCoefficientScaleTest(float tX, float vX, float duration, float expectedA, float expectedB, float expectedC
            , float expectedD, float expectedE, float expectedF)
        {
            float3 scale = new float3(tX, 0, 0);
            float3 velocity = new float3(vX, 0, 0);

            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);

            for (int i = 0; i < parameters.CurrentInput.ScaleCount; i++)
            {
                parameters.CurrentInput.SetLocalToParentScale(i, parameters.CurrentInput.GetLocalToParentScale(i) + scale);
                parameters.SecondLastPose.SetLocalToParentScale(i, parameters.SecondLastPose.GetLocalToParentScale(i) - velocity);
            }


            Core.ComputeInertialBlendingCoefficients(
                ref parameters.CurrentInput,
                ref parameters.LastPose,
                ref parameters.SecondLastPose,
                0.01f,
                duration,
                parameters.Coefficients,
                parameters.Directions);

            var relativeTolerance = 0.001f;
            var offset = InertialBlendingCoefficients.GetScalesOffset(m_TestRig);
            for (int i = offset; i < offset + parameters.CurrentInput.ScaleCount; ++i)
            {
                Assert.That(parameters.Coefficients[i].m_ABCD.x, Is.EqualTo(expectedA).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.y, Is.EqualTo(expectedB).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.z, Is.EqualTo(expectedC).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.w, Is.EqualTo(expectedD).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_E, Is.EqualTo(expectedE).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_F, Is.EqualTo(expectedF).Within(relativeTolerance * 100).Percent);
            }
            parameters.Dispose();
        }

        [TestCase(5, 0, .5f, -960, 1200, -400, 0, 0, 5)]
        [TestCase(0, 0, .5f, 0, 0, 0, 0, 0, 0)]
        [TestCase(1e10f, 0, 1, -6e10f, 15e10f, -10e10f,  0, 0, 1e10f)]
        [TestCase(5, 0, 0f, 0, 0, 0, 0, 0, 0)]
        [TestCase(5, 0, 2.595e-8f, 0, 0, 0, 0, 0, 0)] // Duration is small enough so that duration^5 is normal
        [TestCase(5, 0, 1e4f, -3e-19f, 7.5e-15f, -5e-11f, 0, 0, 5)]
        [TestCase(5, -0.2f, .5f, 0, -80, 80, 0, -20, 5)]
        [TestCase(5, 0.01f, .5f, -960, 1200, -400, 0, 0, 5)]
        [TestCase(5, 1e4f, .5f, -960, 1200, -400, 0, 0, 5)]
        [TestCase(5, -1e4f, .5f, -5.119999e23f, 64e18f, -32e14f, 8e10f, -1e6f, 5)]
        [TestCase(-5, 0, .5f, 960, -1200, 400, 0, 0, -5)]
        [TestCase(-5, 0.2f, .5f, 0, 80, -80, 0, 20, -5)]
        [TestCase(-5, -0.01f, .5f, 960, -1200, 400, 0, 0, -5)]
        public void InertialBlendingCoefficientFloatTest(float delta, float velocity, float duration, float expectedA, float expectedB, float expectedC
            , float expectedD, float expectedE, float expectedF)
        {
            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);

            for (int i = 0; i < parameters.CurrentInput.FloatCount; i++)
            {
                parameters.CurrentInput.SetFloat(i, parameters.CurrentInput.GetFloat(i) - delta);
                parameters.SecondLastPose.SetFloat(i, parameters.SecondLastPose.GetFloat(i) - velocity);
            }

            Core.ComputeInertialBlendingCoefficients(
                ref parameters.CurrentInput,
                ref parameters.LastPose,
                ref parameters.SecondLastPose,
                0.01f,
                duration,
                parameters.Coefficients,
                parameters.Directions);

            var relativeTolerance = 0.001f;
            var offset = InertialBlendingCoefficients.GetFloatsOffset(m_TestRig);
            Assert.That(parameters.CurrentInput.FloatCount, Is.Not.EqualTo(0));
            for (int i = offset; i < offset + parameters.CurrentInput.FloatCount; ++i)
            {
                Assert.That(parameters.Coefficients[i].m_ABCD.x, Is.EqualTo(expectedA).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.y, Is.EqualTo(expectedB).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.z, Is.EqualTo(expectedC).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_ABCD.w, Is.EqualTo(expectedD).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_E, Is.EqualTo(expectedE).Within(relativeTolerance * 100).Percent);
                Assert.That(parameters.Coefficients[i].m_F, Is.EqualTo(expectedF).Within(relativeTolerance * 100).Percent);
            }
            parameters.Dispose();
        }

        [TestCase(0, 0, 0)]
        [TestCase(0, 1, 0)]
        [TestCase(20, -5, 71)]
        public void InertialBlendingCoefficientTranslationAndScaleDirectionTest(float tX, float tY, float tZ)
        {
            float3 translation = new float3(tX, tY, tZ);
            float3 expectedDirection = math.normalizesafe(translation);

            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);

            for (int i = 0; i < parameters.CurrentInput.TranslationCount; i++)
            {
                parameters.CurrentInput.SetLocalToParentTranslation(i, parameters.CurrentInput.GetLocalToParentTranslation(i) + translation);
                parameters.SecondLastPose.SetLocalToParentTranslation(i, parameters.SecondLastPose.GetLocalToParentTranslation(i));
            }

            for (int i = 0; i < parameters.CurrentInput.ScaleCount; i++)
            {
                parameters.CurrentInput.SetLocalToParentScale(i, parameters.CurrentInput.GetLocalToParentScale(i) + translation);
                parameters.SecondLastPose.SetLocalToParentScale(i, parameters.SecondLastPose.GetLocalToParentScale(i));
            }

            Core.ComputeInertialBlendingCoefficients(
                ref parameters.CurrentInput,
                ref parameters.LastPose,
                ref parameters.SecondLastPose,
                0.01f,
                1f,
                parameters.Coefficients,
                parameters.Directions);

            int translationOffset = InertialBlendingCoefficients.GetTranslationsOffset(m_TestRig);
            for (int i = translationOffset; i < translationOffset + m_TestRig.Value.Bindings.TranslationBindings.Length; ++i)
            {
                Assert.That(parameters.Directions[i], Is.EqualTo(-expectedDirection).Using(TranslationComparer));
            }
            int scaleOffset = InertialBlendingCoefficients.GetScalesOffset(m_TestRig);
            for (int i = scaleOffset; i < scaleOffset + m_TestRig.Value.Bindings.ScaleBindings.Length; ++i)
            {
                Assert.That(parameters.Directions[i], Is.EqualTo(-expectedDirection).Using(TranslationComparer));
            }
            parameters.Dispose();
        }

        [TestCase(0, 0, 0, -1, 0, 0)] // The expected direction is not null, but the angle will be
        [TestCase(0, 1, 0, 0, 1, 0)]
        [TestCase(20, -5, 71, -0.2705174f, 0.06762935f, -0.9603367f)]
        public void InertialBlendingCoefficientRotationDirectionTest(float tX, float tY, float tZ, float expectedX, float expectedY, float expectedZ)
        {
            float3 angleAxis = new float3(tX, tY, tZ);
            quaternion rotation = quaternion.AxisAngle(math.normalizesafe(angleAxis), math.length(angleAxis));

            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);

            for (int i = 0; i < parameters.CurrentInput.RotationCount; i++)
            {
                parameters.CurrentInput.SetLocalToParentRotation(i, mathex.mul(rotation, parameters.CurrentInput.GetLocalToParentRotation(i)));
                parameters.SecondLastPose.SetLocalToParentRotation(i, parameters.SecondLastPose.GetLocalToParentRotation(i));
            }

            Core.ComputeInertialBlendingCoefficients(
                ref parameters.CurrentInput,
                ref parameters.LastPose,
                ref parameters.SecondLastPose,
                0.01f,
                1f,
                parameters.Coefficients,
                parameters.Directions);

            int offset = InertialBlendingCoefficients.GetRotationsOffset(m_TestRig);
            for (int i = offset; i < offset + m_TestRig.Value.Bindings.RotationBindings.Length; ++i)
            {
                Assert.That(parameters.Directions[i], Is.EqualTo(new float3(-expectedX, -expectedY, -expectedZ)).Using(TranslationComparer));
            }
            parameters.Dispose();
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS

        [TestCase(0.01f, -1)]
        [TestCase(0f, 1)]
        [TestCase(-1f, 1)]
        public void InertialBlendingCoefficientsInvalidParametersTest(float deltaTime, float duration)
        {
            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    ref parameters.SecondLastPose,
                    deltaTime,
                    duration,
                    parameters.Coefficients,
                    parameters.Directions);
            });
            parameters.Dispose();
        }

        [Test]
        public void InertialBlendingCoefficientsInvalidAnimationStreamTest()
        {
            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);

            var nullStream = AnimationStream.Null;

            var otherRig = MakeRig("temp_");
            var otherRigData = new NativeArray<AnimatedData>(otherRig.Value.Bindings.StreamSize, Allocator.Temp);
            var stramWithOtherRig = AnimationStream.Create(otherRig, otherRigData);

            Assert.Throws<NullReferenceException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref nullStream,
                    ref parameters.LastPose,
                    ref parameters.SecondLastPose,
                    0.001f,
                    1f,
                    parameters.Coefficients,
                    parameters.Directions);
            });

            Assert.Throws<NullReferenceException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref parameters.CurrentInput,
                    ref nullStream,
                    ref parameters.SecondLastPose,
                    0.001f,
                    1f,
                    parameters.Coefficients,
                    parameters.Directions);
            });

            Assert.Throws<NullReferenceException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    ref nullStream,
                    0.001f,
                    1f,
                    parameters.Coefficients,
                    parameters.Directions);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref stramWithOtherRig,
                    ref parameters.LastPose,
                    ref parameters.SecondLastPose,
                    0.001f,
                    1f,
                    parameters.Coefficients,
                    parameters.Directions);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref parameters.CurrentInput,
                    ref stramWithOtherRig,
                    ref parameters.SecondLastPose,
                    0.001f,
                    1f,
                    parameters.Coefficients,
                    parameters.Directions);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    ref stramWithOtherRig,
                    0.001f,
                    1f,
                    parameters.Coefficients,
                    parameters.Directions);
            });

            otherRig.Dispose();
            parameters.Dispose();
        }

        [Test]
        public void InertialBlendingCoefficientsInvalidNativeArrayTest()
        {
            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);
            var uninitializedDirectionArray = new NativeArray<float3>();
            var uninitializedCoefficientsArray = new NativeArray<InertialBlendingCoefficients>();

            var wrongLengthDirectionArray = new NativeArray<float3>(10, Allocator.Temp);
            var wrongLengthCoefficientsArray = new NativeArray<InertialBlendingCoefficients>(10, Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    ref parameters.SecondLastPose,
                    0.001f,
                    1f,
                    uninitializedCoefficientsArray,
                    parameters.Directions);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    ref parameters.SecondLastPose,
                    0.001f,
                    1f,
                    parameters.Coefficients,
                    uninitializedDirectionArray);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    ref parameters.SecondLastPose,
                    0.001f,
                    1f,
                    wrongLengthCoefficientsArray,
                    parameters.Directions);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.ComputeInertialBlendingCoefficients(
                    ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    ref parameters.SecondLastPose,
                    0.001f,
                    1f,
                    parameters.Coefficients,
                    wrongLengthDirectionArray);
            });

            wrongLengthDirectionArray.Dispose();
            wrongLengthCoefficientsArray.Dispose();
            parameters.Dispose();
        }

        [TestCase(-1, 0)]
        [TestCase(10, -1)]
        [TestCase(1, 2)]
        public void InertialBlendInvalidTimeTest(float duration, float remainingTime)
        {
            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.InertialBlend(ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    parameters.Coefficients,
                    parameters.Directions,
                    duration,
                    remainingTime);
            });
            parameters.Dispose();
        }

        [Test]
        public void InertialBlendInvalidAnimationStreamTest()
        {
            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);

            var nullStream = AnimationStream.Null;

            var otherRig = MakeRig("temp_");
            var otherRigData = new NativeArray<AnimatedData>(otherRig.Value.Bindings.StreamSize, Allocator.Temp);
            var stramWithOtherRig = AnimationStream.Create(otherRig, otherRigData);

            Assert.Throws<NullReferenceException>(() =>
            {
                Core.InertialBlend(ref nullStream,
                    ref parameters.LastPose,
                    parameters.Coefficients,
                    parameters.Directions,
                    1,
                    0.5f);
            });

            Assert.Throws<NullReferenceException>(() =>
            {
                Core.InertialBlend(ref parameters.CurrentInput,
                    ref nullStream,
                    parameters.Coefficients,
                    parameters.Directions,
                    1,
                    0.5f);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.InertialBlend(ref stramWithOtherRig,
                    ref parameters.LastPose,
                    parameters.Coefficients,
                    parameters.Directions,
                    1,
                    0.5f);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.InertialBlend(ref parameters.CurrentInput,
                    ref stramWithOtherRig,
                    parameters.Coefficients,
                    parameters.Directions,
                    1,
                    0.5f);
            });

            otherRigData.Dispose();
            parameters.Dispose();
        }

        [Test]
        public void InertialBlendInvalidNativeArrayTest()
        {
            var parameters = new ComputeInertialBlendingCoefficientsParameters(m_TestRig);

            var uninitializedDirectionArray = new NativeArray<float3>();
            var uninitializedCoefficientsArray = new NativeArray<InertialBlendingCoefficients>();

            var wrongLengthDirectionArray = new NativeArray<float3>(10, Allocator.Temp);
            var wrongLengthCoefficientsArray = new NativeArray<InertialBlendingCoefficients>(10, Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.InertialBlend(ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    uninitializedCoefficientsArray,
                    parameters.Directions,
                    1,
                    0.5f);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.InertialBlend(ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    parameters.Coefficients,
                    uninitializedDirectionArray,
                    1,
                    0.5f);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.InertialBlend(ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    wrongLengthCoefficientsArray,
                    parameters.Directions,
                    1,
                    0.5f);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                Core.InertialBlend(ref parameters.CurrentInput,
                    ref parameters.LastPose,
                    parameters.Coefficients,
                    wrongLengthDirectionArray,
                    1,
                    0.5f);
            });

            wrongLengthCoefficientsArray.Dispose();
            wrongLengthDirectionArray.Dispose();
            parameters.Dispose();
        }

#endif

        [TestCase(0, 0, 0, 0, 0)]
        [TestCase(10, 0, 10, -1, 10)]
        [TestCase(10, 5, 10, -1, 3.4375f)]
        [TestCase(10, 10, 10, -1, 0)]
        public void InertialBlendTest(float duration, float time, float translationX0, float translationV0, float expectedValue)
        {
            float remainingTime = duration - time;
            float3 direction = new float3(1, 0, 0);

            int coefficientSize = m_TestRig.Value.Bindings.TranslationBindings.Length +
                m_TestRig.Value.Bindings.RotationBindings.Length +
                m_TestRig.Value.Bindings.ScaleBindings.Length +
                m_TestRig.Value.Bindings.FloatBindings.Length;
            int directionSize = m_TestRig.Value.Bindings.TranslationBindings.Length +
                m_TestRig.Value.Bindings.RotationBindings.Length +
                m_TestRig.Value.Bindings.ScaleBindings.Length;

            var directions = new NativeArray<float3>(directionSize, Allocator.Temp);
            var coefficients = new NativeArray<InertialBlendingCoefficients>(coefficientSize, Allocator.Temp);
            var inputPoseBuffer = new NativeArray<AnimatedData>(m_TestRig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var outputPoseBuffer = new NativeArray<AnimatedData>(m_TestRig.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            var inputPose = AnimationStream.Create(m_TestRig, inputPoseBuffer);
            inputPose.ResetToDefaultValues();
            var outputPose = AnimationStream.Create(m_TestRig, outputPoseBuffer);
            outputPose.ResetToDefaultValues();

            for (int i = 0; i < coefficients.Length; i++)
            {
                coefficients[i] = new InertialBlendingCoefficients(translationX0, translationV0, duration);
            }

            for (int i = 0; i < directions.Length; i++)
            {
                directions[i] = direction;
            }

            for (int i = 0; i < m_TestRig.Value.Bindings.IntBindings.Length; ++i)
            {
                inputPose.SetInt(i, 1);
            }

            Core.InertialBlend(ref inputPose, ref outputPose, coefficients, directions, duration, remainingTime);

            for (int i = 0; i < outputPose.TranslationCount; ++i)
            {
                Assert.That(outputPose.GetLocalToParentTranslation(i) - inputPose.GetLocalToParentTranslation(i),
                    Is.EqualTo(direction * expectedValue).Using(TranslationComparer));
            }
            for (int i = 0; i < outputPose.ScaleCount; ++i)
            {
                Assert.That(outputPose.GetLocalToParentScale(i) - inputPose.GetLocalToParentScale(i),
                    Is.EqualTo(direction * expectedValue).Using(ScaleComparer));
            }
            for (int i = 0; i < outputPose.RotationCount; ++i)
            {
                Assert.That(mathex.mul(outputPose.GetLocalToParentRotation(i), math.inverse(inputPose.GetLocalToParentRotation(i))),
                    Is.EqualTo(quaternion.AxisAngle(direction, expectedValue)).Using(RotationComparer));
            }
            for (int i = 0; i < outputPose.FloatCount; ++i)
            {
                Assert.That(outputPose.GetFloat(i) - inputPose.GetFloat(i),
                    Is.EqualTo(expectedValue).Using(FloatComparer));
            }
            for (int i = 0; i < outputPose.IntCount; ++i)
            {
                Assert.That(outputPose.GetInt(i), Is.EqualTo(inputPose.GetInt(i)));
            }

            coefficients.Dispose();
            inputPoseBuffer.Dispose();
            outputPoseBuffer.Dispose();
        }

        [TestCase(0f, 0f, 0f, 1f)]
        [TestCase(0f, 0f, 0f, -1f)]
        [TestCase(0f, 0f, 1f, 0f)]
        [TestCase(0f, 0f, -1f, 0f)]
        [TestCase(0f, 1f, 0f, 0f)]
        [TestCase(0f, -1f, 0f, 0f)]
        [TestCase(1f, 0f, 0f, 0f)]
        [TestCase(-1f, 0f, 0f, 0f)]
        [TestCase(-0.383f, 0.881f,  0.145f, -0.2370f)]
        [TestCase(0.331f,  0.281f,   0.637f,  0.637f)]
        public void Test_AngleAxis(float qx, float qy, float qz, float qw)
        {
            var quaternionComparer = new QuaternionAbsoluteEqualityComparer(1e-4f);
            quaternion q = math.quaternion(qx, qy, qz, qw);

            float angleValue = mathex.angle(q);
            float3 axisValue = mathex.axis(q);

            Assert.That(quaternion.AxisAngle(angleValue, angleValue), Is.EqualTo(q).Using(quaternionComparer));
        }

        [TestCase(0f, 0f)]
        [TestCase(45f, 45f)]
        [TestCase(-45f, -45f)]
        [TestCase(181f, -179f)]
        [TestCase(-181f, 179f)]
        [TestCase(360f, 0f)]
        [TestCase(-360f, 0f)]
        [TestCase(361f, 1f)]
        [TestCase(-361f, -1f)]
        [TestCase(540, 180f)]
        [TestCase(-540, -180f)]
        public void UnwindOnceRotationTest(float angle, float expected)
        {
            // Need to relax floating point constraint a bit for this test
            var comparer = new FloatAbsoluteEqualityComparer(1e-4f);
            Assert.That(math.degrees(mathex.unwind_once(math.radians(angle))), Is.EqualTo(expected).Using(comparer));
        }

        [TestCase(0f, 0f)]
        [TestCase(45f, 45f)]
        [TestCase(-45f, -45f)]
        [TestCase(181f, -179f)]
        [TestCase(-181f, 179f)]
        [TestCase(360f, 0f)]
        [TestCase(-360f, 0f)]
        [TestCase(361f, 1f)]
        [TestCase(-361f, -1f)]
        [TestCase(720f, 0f)]
        [TestCase(-720f, 0f)]
        [TestCase(725f, 5f)]
        [TestCase(-725f, -5f)]
        public void UnwindRotationTest(float angle, float expected)
        {
            // Need to relax floating point constraint a bit for this test
            var comparer = new FloatAbsoluteEqualityComparer(1e-4f);
            Assert.That(math.degrees(mathex.unwind(math.radians(angle))), Is.EqualTo(expected).Using(comparer));
        }
    }
}
