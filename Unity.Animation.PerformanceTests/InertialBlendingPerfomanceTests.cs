using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using Unity.Animation.Tests;

namespace Unity.Animation.PerformanceTests
{
    [BurstCompile(CompileSynchronously = true)]
    struct ComputeInertialBlendingCoefficientsJob : IJob
    {
        [ReadOnly]
        public BlobAssetReference<RigDefinition> Rig;
        [ReadOnly]
        public NativeArray<AnimatedData> CurrentInput;
        [ReadOnly]
        public NativeArray<AnimatedData> LastOutput;
        [ReadOnly]
        public NativeArray<AnimatedData> SecondLastOutput;
        [ReadOnly]
        public float DeltaTime;
        [ReadOnly]
        public float Duration;

        [WriteOnly]
        public NativeArray<InertialBlendingCoefficients> OutCoefficients;
        [WriteOnly]
        public NativeArray<float3> OutDirections;

        public void Execute()
        {
            var currentInput = AnimationStream.CreateReadOnly(Rig, CurrentInput);
            var lastOutput = AnimationStream.CreateReadOnly(Rig, LastOutput);
            var secondLastOutput = AnimationStream.CreateReadOnly(Rig, SecondLastOutput);

            Core.ComputeInertialBlendingCoefficients(ref currentInput, ref lastOutput, ref secondLastOutput, DeltaTime, Duration, OutCoefficients, OutDirections);
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    struct InertialBlendJob : IJob
    {
        [ReadOnly]
        public BlobAssetReference<RigDefinition> Rig;
        [ReadOnly]
        public NativeArray<AnimatedData> CurrentInput;
        [WriteOnly]
        public NativeArray<AnimatedData> OutputPose;
        [ReadOnly]
        public NativeArray<InertialBlendingCoefficients> InterpolationFactors;
        [ReadOnly]
        public NativeArray<float3> InterpolationDirections;
        [ReadOnly]
        public float Duration;
        [ReadOnly]
        public float RemainingTime;

        public void Execute()
        {
            var currentInput = AnimationStream.CreateReadOnly(Rig, CurrentInput);
            var outputPose = AnimationStream.Create(Rig, OutputPose);

            Core.InertialBlend(ref currentInput, ref outputPose, InterpolationFactors, InterpolationDirections, Duration, RemainingTime);
        }
    }

    [BurstCompile]
    struct BurstedCore2
    {
        public unsafe delegate void EvaluateInertializeVectorDelegate(
            ref float3 currentInput,
            ref float3 lastOutput,
            ref float3 secondLastOutput,
            float deltaTime,
            float duration,
            float3* outDirections,
            InertialBlendingCoefficients* outCoefficients
        );

        public unsafe delegate void EvaluateInertializeQuaternionDelegate(
            ref quaternion secondLastOutput,
            ref quaternion lastOutput,
            ref quaternion currentInput,
            float deltaTime,
            float duration,
            float3* outDirections,
            InertialBlendingCoefficients* outCoefficients
        );

        public unsafe delegate void EvaluateInertializeFloatDelegate(
            ref float secondLastOutput,
            ref float lastOutput,
            ref float currentInput,
            float deltaTime,
            float duration,
            InertialBlendingCoefficients* outCoefficients
        );

        public static EvaluateInertializeVectorDelegate EvaluateInertializeVector;
        public static EvaluateInertializeQuaternionDelegate EvaluateInertializeQuaternion;
        public static EvaluateInertializeFloatDelegate EvaluateInertializeFloat;

        static bool Initialized = false;

        [BurstCompile]
        static unsafe void EvaluateInertializeVectorExecute(
            ref float3 secondLastOutput,
            ref float3 lastOutput,
            ref float3 currentInput,
            float deltaTime,
            float duration,
            float3* outDirections,
            InertialBlendingCoefficients* outCoefficients
        )
        {
            Core.InertializeVector(currentInput, lastOutput, secondLastOutput, deltaTime, duration, out *outDirections, out *outCoefficients);
        }

        [BurstCompile]
        static unsafe void EvaluateInertializeQuaternionExecute(
            ref quaternion secondLastOutput,
            ref quaternion lastOutput,
            ref quaternion currentInput,
            float deltaTime,
            float duration,
            float3* outDirections,
            InertialBlendingCoefficients* outCoefficients
        )
        {
            Core.InertializeQuaternion(currentInput, lastOutput, secondLastOutput, deltaTime, duration, out *outDirections, out *outCoefficients);
        }

        [BurstCompile]
        static unsafe void EvaluateInertializeFloatExecute(
            ref float secondLastOutput,
            ref float lastOutput,
            ref float currentInput,
            float deltaTime,
            float duration,
            InertialBlendingCoefficients* outCoefficients
        )
        {
            Core.InertializeFloat(currentInput, lastOutput, secondLastOutput, deltaTime, duration, out *outCoefficients);
        }

        public static void Initialize()
        {
            if (Initialized)
                return;

            unsafe
            {
                EvaluateInertializeVector = BurstCompiler.CompileFunctionPointer<EvaluateInertializeVectorDelegate>(EvaluateInertializeVectorExecute).Invoke;
                EvaluateInertializeQuaternion = BurstCompiler.CompileFunctionPointer<EvaluateInertializeQuaternionDelegate>(EvaluateInertializeQuaternionExecute).Invoke;
                EvaluateInertializeFloat = BurstCompiler.CompileFunctionPointer<EvaluateInertializeFloatDelegate>(EvaluateInertializeFloatExecute).Invoke;
            }
        }
    }
    [Category("Performance")]
    public class InertialBlendingPerfomanceTests : AnimationTestsFixture
    {
        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            BurstedCore2.Initialize();
        }

        BlobAssetReference<RigDefinition> BuildRigDefinitionBoneChain(int bones)
        {
            var skeletonNodes = new SkeletonNode[bones];
            // We will arbitrarily create one float channel per bone
            var floatChannels = new IAnimationChannel[bones];
            for (int i = 0; i < bones; ++i)
            {
                skeletonNodes[i] = new SkeletonNode
                {
                    ParentIndex = i == 0 ? -1 : i - 1,
                    Id = ($"Bone{i}"),
                    AxisIndex = -1,
                    LocalTranslationDefaultValue = math.float3(i),
                    LocalRotationDefaultValue = quaternion.AxisAngle(math.float3(0f, 1f, 0f), math.radians(i)),
                    LocalScaleDefaultValue = i == 0 ? 1f : math.float3(i / bones)
                };
                floatChannels[i] = new FloatChannel
                {
                    Id = StringHash.Hash($"float channel #{i}"),
                    DefaultValue = 0,
                };
            }

            return RigBuilder.CreateRigDefinition(skeletonNodes, null, floatChannels);
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void Test_ComputeInertialMotionBlendingCoefficients(int boneCount)
        {
            var rigDefinition = BuildRigDefinitionBoneChain(boneCount);

            using (var secondLastPoseBuffer = new NativeArray<AnimatedData>(rigDefinition.Value.Bindings.StreamSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory))
            using (var lastPoseBuffer = new NativeArray<AnimatedData>(rigDefinition.Value.Bindings.StreamSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory))
            using (var currentInputBuffer = new NativeArray<AnimatedData>(rigDefinition.Value.Bindings.StreamSize,
                Allocator.TempJob, NativeArrayOptions.UninitializedMemory))
            {
                var secondLastPose = AnimationStream.Create(rigDefinition, secondLastPoseBuffer);
                secondLastPose.ResetToDefaultValues();
                var lastPose = AnimationStream.Create(rigDefinition, lastPoseBuffer);
                lastPose.ResetToDefaultValues();
                var currentInput = AnimationStream.Create(rigDefinition, currentInputBuffer);
                currentInput.ResetToDefaultValues();

                for (int i = 0; i < currentInput.TranslationCount; i++)
                {
                    currentInput.SetLocalToParentTranslation(i, currentInput.GetLocalToParentTranslation(i) + 1f);
                    secondLastPose.SetLocalToParentTranslation(i, secondLastPose.GetLocalToParentTranslation(i) - 0.1f);
                }

                var rotation = quaternion.Euler(0.1f, 0.2f, 0.3f);
                var velocity = quaternion.Euler(0.3f, 0.2f, 0.1f);
                for (int i = 0; i < currentInput.TranslationCount; i++)
                {
                    currentInput.SetLocalToParentRotation(i, mathex.mul(rotation, currentInput.GetLocalToParentRotation(i)));
                    secondLastPose.SetLocalToParentRotation(i, mathex.mul(math.inverse(velocity), secondLastPose.GetLocalToParentRotation(i)));
                }

                for (int i = 0; i < currentInput.ScaleCount; i++)
                {
                    currentInput.SetLocalToParentScale(i, currentInput.GetLocalToParentScale(i) + 1f);
                    secondLastPose.SetLocalToParentScale(i, secondLastPose.GetLocalToParentScale(i) - 0.1f);
                }

                for (int i = 0; i < currentInput.FloatCount; i++)
                {
                    currentInput.SetFloat(i, currentInput.GetFloat(i) + 1);
                    secondLastPose.SetFloat(i, secondLastPose.GetFloat(i) - 0.1f);
                }

                var coefficientNumber = rigDefinition.Value.Bindings.BindingCount;
                var directionNumber = coefficientNumber - rigDefinition.Value.Bindings.IntBindings.Length -
                    rigDefinition.Value.Bindings.FloatBindings.Length;

                var coefficients = new NativeArray<InertialBlendingCoefficients>(coefficientNumber, Allocator.TempJob);
                var directions = new NativeArray<float3>(directionNumber, Allocator.TempJob);

                var job = new ComputeInertialBlendingCoefficientsJob
                {
                    Rig = rigDefinition,
                    CurrentInput = currentInputBuffer,
                    LastOutput = lastPoseBuffer,
                    SecondLastOutput = secondLastPoseBuffer,
                    DeltaTime = 0.01f,
                    Duration = 1f,
                    OutCoefficients = coefficients,
                    OutDirections = directions,
                };
                Measure.Method(() =>
                {
                    job.Run();
                })
                    .WarmupCount(10)
                    .MeasurementCount(20)
                    .Run();

                coefficients.Dispose();
                directions.Dispose();
            }
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void Test_InertialBlend(int bones)
        {
            var rig = BuildRigDefinitionBoneChain(bones);
            float duration = 1;
            float time = 0.5f;
            float remainingTime = duration - time;
            float3 direction = new float3(1, 0, 0);

            int coefficientSize = rig.Value.Bindings.TranslationBindings.Length +
                rig.Value.Bindings.RotationBindings.Length +
                rig.Value.Bindings.ScaleBindings.Length +
                rig.Value.Bindings.FloatBindings.Length;
            int directionSize = rig.Value.Bindings.TranslationBindings.Length +
                rig.Value.Bindings.RotationBindings.Length +
                rig.Value.Bindings.ScaleBindings.Length;

            var directions = new NativeArray<float3>(directionSize, Allocator.TempJob);
            var coefficients = new NativeArray<InertialBlendingCoefficients>(coefficientSize, Allocator.TempJob);
            var inputPoseBuffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var outputPoseBuffer = new NativeArray<AnimatedData>(rig.Value.Bindings.StreamSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var inputPose = AnimationStream.Create(rig, inputPoseBuffer);
            inputPose.ResetToDefaultValues();
            var outputPose = AnimationStream.Create(rig, outputPoseBuffer);
            outputPose.ResetToDefaultValues();

            for (int i = 0; i < coefficients.Length; i++)
            {
                coefficients[i] = new InertialBlendingCoefficients(1, 0.01f, duration);
            }

            for (int i = 0; i < directions.Length; i++)
            {
                directions[i] = direction;
            }

            var job = new InertialBlendJob
            {
                CurrentInput = inputPoseBuffer,
                OutputPose = outputPoseBuffer,
                Duration = duration,
                InterpolationDirections = directions,
                InterpolationFactors = coefficients,
                RemainingTime = remainingTime,
                Rig = rig
            };
            Measure.Method(() =>
            {
                job.Run();
            })
                .WarmupCount(10)
                .MeasurementCount(20)
                .Run();

            directions.Dispose();
            coefficients.Dispose();
            inputPoseBuffer.Dispose();
            outputPoseBuffer.Dispose();
        }

        [Test, Performance]
        public void Test_IntertializeVector()
        {
            var secondLast = new float3(1, 2, 3);
            var last = new float3(1.1f, 2.1f, 2.9f);
            var current = new float3(3, 2 , 1);
            var deltaTime = 0.01f;
            var time = 1f;
            InertialBlendingCoefficients outCoefficients;
            float3 outDirection;

            int reps = 100;
            unsafe
            {
                var outCoefficientsPtr = &outCoefficients;
                var outDirectionPtr = &outDirection;
                Measure.Method(() =>
                {
                    for (int i = 0; i < reps; ++i)
                    {
                        BurstedCore2.EvaluateInertializeVector.Invoke(ref secondLast, ref last, ref current, deltaTime, time,
                            outDirectionPtr, outCoefficientsPtr);
                    }
                })
                    .WarmupCount(50)
                    .MeasurementCount(100)
                    .Run();
            }
        }

        [Test, Performance]
        public void Test_IntertializeQuaternion()
        {
            var secondLast = quaternion.Euler(10, 20, 30);
            var last = quaternion.Euler(15, 25, 35);
            var current = quaternion.Euler(30, 20, 10);
            var deltaTime = 0.01f;
            var time = 1f;
            InertialBlendingCoefficients outCoefficients;
            float3 outDirection;

            int reps = 100;
            unsafe
            {
                var outCoefficientsPtr = &outCoefficients;
                var outDirectionPtr = &outDirection;
                Measure.Method(() =>
                {
                    for (int i = 0; i < reps; ++i)
                    {
                        BurstedCore2.EvaluateInertializeQuaternion.Invoke(ref secondLast, ref last, ref current,
                            deltaTime, time,
                            outDirectionPtr, outCoefficientsPtr);
                    }
                })
                    .WarmupCount(50)
                    .MeasurementCount(100)
                    .Run();
            }
        }

        [Test, Performance]
        public void Test_IntertializeFloat()
        {
            var secondLast = 10f;
            var last = 11f;
            var current = 5f;
            var deltaTime = 0.01f;
            var time = 1f;
            InertialBlendingCoefficients outCoefficients;

            int reps = 100;
            unsafe
            {
                var outCoefficientsPtr = &outCoefficients;
                Measure.Method(() =>
                {
                    for (int i = 0; i < reps; ++i)
                    {
                        BurstedCore2.EvaluateInertializeFloat.Invoke(ref secondLast, ref last, ref current,
                            deltaTime, time,
                            outCoefficientsPtr);
                    }
                })
                    .WarmupCount(50)
                    .MeasurementCount(100)
                    .Run();
            }
        }
    }
}
