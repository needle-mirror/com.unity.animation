using NUnit.Framework;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.PerformanceTesting;
using Unity.Animation.Tests;

namespace Unity.Animation.PerformanceTests
{
    [BurstCompile]
    struct BurstedAnimationStream
    {
        public delegate void EvaluateAnimationStreamGetLocalToRootTranslationDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamGetLocalToRootTranslationDelegate EvaluateAnimationStreamGetLocalToRootTranslation;

        public delegate void EvaluateAnimationStreamGetLocalToRootRotationDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamGetLocalToRootRotationDelegate EvaluateAnimationStreamGetLocalToRootRotation;

        public delegate void EvaluateAnimationStreamGetLocalToRootScaleDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamGetLocalToRootScaleDelegate EvaluateAnimationStreamGetLocalToRootScale;

        public delegate void EvaluateAnimationStreamGetLocalToRootTRDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamGetLocalToRootTRDelegate EvaluateAnimationStreamGetLocalToRootTR;

        public delegate void EvaluateAnimationStreamGetLocalToRootTRSDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamGetLocalToRootTRSDelegate EvaluateAnimationStreamGetLocalToRootTRS;

        public delegate void EvaluateAnimationStreamGetLocalToRootMatrixDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamGetLocalToRootMatrixDelegate EvaluateAnimationStreamGetLocalToRootMatrix;

        public delegate void EvaluateAnimationStreamGetLocalToRootInverseMatrixDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamGetLocalToRootInverseMatrixDelegate EvaluateAnimationStreamGetLocalToRootInverseMatrix;

        public delegate void EvaluateAnimationStreamSetLocalToRootTranslationDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamSetLocalToRootTranslationDelegate EvaluateAnimationStreamSetLocalToRootTranslation;

        public delegate void EvaluateAnimationStreamSetLocalToRootRotationDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamSetLocalToRootRotationDelegate EvaluateAnimationStreamSetLocalToRootRotation;

        public delegate void EvaluateAnimationStreamSetLocalToRootScaleDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamSetLocalToRootScaleDelegate EvaluateAnimationStreamSetLocalToRootScale;

        public delegate void EvaluateAnimationStreamSetLocalToRootTRDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamSetLocalToRootTRDelegate EvaluateAnimationStreamSetLocalToRootTR;

        public delegate void EvaluateAnimationStreamSetLocalToRootTRSDelegate(ref AnimationStream stream, int index);
        public static EvaluateAnimationStreamSetLocalToRootTRSDelegate EvaluateAnimationStreamSetLocalToRootTRS;

        static bool Initialized = false;

        [BurstCompile]
        static void EvaluateAnimationStreamGetLocalToRootTranslationExecute(ref AnimationStream stream, int index)
        {
            stream.GetLocalToRootTranslation(index);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamGetLocalToRootRotationExecute(ref AnimationStream stream, int index)
        {
            stream.GetLocalToRootRotation(index);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamGetLocalToRootScaleExecute(ref AnimationStream stream, int index)
        {
            stream.GetLocalToRootScale(index);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamGetLocalToRootTRExecute(ref AnimationStream stream, int index)
        {
            stream.GetLocalToRootTR(index, out float3 t, out quaternion r);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamGetLocalToRootTRSExecute(ref AnimationStream stream, int index)
        {
            stream.GetLocalToRootTRS(index, out float3 t, out quaternion r, out float3 s);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamGetLocalToRootMatrixExecute(ref AnimationStream stream, int index)
        {
            stream.GetLocalToRootMatrix(index);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamGetLocalToRootInverseMatrixExecute(ref AnimationStream stream, int index)
        {
            stream.GetLocalToRootInverseMatrix(index);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamSetLocalToRootTranslationExecute(ref AnimationStream stream, int index)
        {
            stream.SetLocalToRootTranslation(index, 1f);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamSetLocalToRootRotationExecute(ref AnimationStream stream, int index)
        {
            stream.SetLocalToRootRotation(index, quaternion.identity);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamSetLocalToRootScaleExecute(ref AnimationStream stream, int index)
        {
            stream.SetLocalToRootScale(index, 1f);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamSetLocalToRootTRExecute(ref AnimationStream stream, int index)
        {
            stream.SetLocalToRootTR(index, 1f, quaternion.identity);
        }

        [BurstCompile]
        static void EvaluateAnimationStreamSetLocalToRootTRSExecute(ref AnimationStream stream, int index)
        {
            stream.SetLocalToRootTRS(index, 1f, quaternion.identity, 1f);
        }

        public static void Initialize()
        {
            if (Initialized)
                return;

            EvaluateAnimationStreamGetLocalToRootTranslation = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamGetLocalToRootTranslationDelegate>(EvaluateAnimationStreamGetLocalToRootTranslationExecute).Invoke;
            EvaluateAnimationStreamGetLocalToRootRotation = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamGetLocalToRootRotationDelegate>(EvaluateAnimationStreamGetLocalToRootRotationExecute).Invoke;
            EvaluateAnimationStreamGetLocalToRootScale = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamGetLocalToRootScaleDelegate>(EvaluateAnimationStreamGetLocalToRootScaleExecute).Invoke;
            EvaluateAnimationStreamGetLocalToRootTR = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamGetLocalToRootTRDelegate>(EvaluateAnimationStreamGetLocalToRootTRExecute).Invoke;
            EvaluateAnimationStreamGetLocalToRootTRS = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamGetLocalToRootTRSDelegate>(EvaluateAnimationStreamGetLocalToRootTRSExecute).Invoke;
            EvaluateAnimationStreamGetLocalToRootMatrix = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamGetLocalToRootMatrixDelegate>(EvaluateAnimationStreamGetLocalToRootMatrixExecute).Invoke;
            EvaluateAnimationStreamGetLocalToRootInverseMatrix = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamGetLocalToRootInverseMatrixDelegate>(EvaluateAnimationStreamGetLocalToRootInverseMatrixExecute).Invoke;

            EvaluateAnimationStreamSetLocalToRootTranslation = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamSetLocalToRootTranslationDelegate>(EvaluateAnimationStreamSetLocalToRootTranslationExecute).Invoke;
            EvaluateAnimationStreamSetLocalToRootRotation = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamSetLocalToRootRotationDelegate>(EvaluateAnimationStreamSetLocalToRootRotationExecute).Invoke;
            EvaluateAnimationStreamSetLocalToRootScale = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamSetLocalToRootScaleDelegate>(EvaluateAnimationStreamSetLocalToRootScaleExecute).Invoke;
            EvaluateAnimationStreamSetLocalToRootTR = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamSetLocalToRootTRDelegate>(EvaluateAnimationStreamSetLocalToRootTRExecute).Invoke;
            EvaluateAnimationStreamSetLocalToRootTRS = BurstCompiler.CompileFunctionPointer<EvaluateAnimationStreamSetLocalToRootTRSDelegate>(EvaluateAnimationStreamSetLocalToRootTRSExecute).Invoke;
        }
    }

    [Category("Performance")]
    public class AnimationStreamPerformanceTests : AnimationTestsFixture
    {
        BlobAssetReference<RigDefinition> BuildRigDefinitionBoneChain(int bones)
        {
            var skeletonNodes = new SkeletonNode[bones];
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
            }

            return RigBuilder.CreateRigDefinition(skeletonNodes);
        }

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            BurstedAnimationStream.Initialize();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_GetLocalToRootTranslation_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            var stream = AnimationStream.FromDefaultValues(rigDefintion);

            Measure.Method(() =>
            {
                BurstedAnimationStream.EvaluateAnimationStreamGetLocalToRootTranslation(ref stream, boneCount - 1);
            })
                .WarmupCount(50)
                .MeasurementCount(100)
                .Run();

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_GetLocalToRootRotation_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            var stream = AnimationStream.FromDefaultValues(rigDefintion);

            Measure.Method(() =>
            {
                BurstedAnimationStream.EvaluateAnimationStreamGetLocalToRootRotation(ref stream, boneCount - 1);
            })
                .WarmupCount(50)
                .MeasurementCount(100)
                .Run();

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_GetLocalToRootScale_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            var stream = AnimationStream.FromDefaultValues(rigDefintion);

            Measure.Method(() =>
            {
                BurstedAnimationStream.EvaluateAnimationStreamGetLocalToRootScale(ref stream, boneCount - 1);
            })
                .WarmupCount(50)
                .MeasurementCount(100)
                .Run();

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_GetLocalToRootTR_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            var stream = AnimationStream.FromDefaultValues(rigDefintion);

            Measure.Method(() =>
            {
                BurstedAnimationStream.EvaluateAnimationStreamGetLocalToRootTR(ref stream, boneCount - 1);
            })
                .WarmupCount(50)
                .MeasurementCount(100)
                .Run();

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_GetLocalToRootTRS_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            var stream = AnimationStream.FromDefaultValues(rigDefintion);

            Measure.Method(() =>
            {
                BurstedAnimationStream.EvaluateAnimationStreamGetLocalToRootTRS(ref stream, boneCount - 1);
            })
                .WarmupCount(50)
                .MeasurementCount(100)
                .Run();

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_GetLocalToRootMatrix_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            var stream = AnimationStream.FromDefaultValues(rigDefintion);

            Measure.Method(() =>
            {
                BurstedAnimationStream.EvaluateAnimationStreamGetLocalToRootMatrix(ref stream, boneCount - 1);
            })
                .WarmupCount(50)
                .MeasurementCount(100)
                .Run();

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_GetLocalToRootInverseMatrix_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            var stream = AnimationStream.FromDefaultValues(rigDefintion);

            Measure.Method(() =>
            {
                BurstedAnimationStream.EvaluateAnimationStreamGetLocalToRootInverseMatrix(ref stream, boneCount - 1);
            })
                .WarmupCount(50)
                .MeasurementCount(100)
                .Run();

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_SetLocalToRootTranslation_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            using (var buffer = new NativeArray<AnimatedData>(rigDefintion.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory))
            {
                var stream = AnimationStream.Create(rigDefintion, buffer);
                stream.ResetToDefaultValues();

                Measure.Method(() =>
                {
                    BurstedAnimationStream.EvaluateAnimationStreamSetLocalToRootTranslation(ref stream, boneCount - 1);
                })
                    .WarmupCount(50)
                    .MeasurementCount(100)
                    .Run();
            }

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_SetLocalToRootRotation_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            using (var buffer = new NativeArray<AnimatedData>(rigDefintion.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory))
            {
                var stream = AnimationStream.Create(rigDefintion, buffer);
                stream.ResetToDefaultValues();

                Measure.Method(() =>
                {
                    BurstedAnimationStream.EvaluateAnimationStreamSetLocalToRootRotation(ref stream, boneCount - 1);
                })
                    .WarmupCount(50)
                    .MeasurementCount(100)
                    .Run();
            }

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_SetLocalToRootScale_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            using (var buffer = new NativeArray<AnimatedData>(rigDefintion.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory))
            {
                var stream = AnimationStream.Create(rigDefintion, buffer);
                stream.ResetToDefaultValues();

                Measure.Method(() =>
                {
                    BurstedAnimationStream.EvaluateAnimationStreamSetLocalToRootScale(ref stream, boneCount - 1);
                })
                    .WarmupCount(50)
                    .MeasurementCount(100)
                    .Run();
            }

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_SetLocalToRootTR_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            using (var buffer = new NativeArray<AnimatedData>(rigDefintion.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory))
            {
                var stream = AnimationStream.Create(rigDefintion, buffer);
                stream.ResetToDefaultValues();

                Measure.Method(() =>
                {
                    BurstedAnimationStream.EvaluateAnimationStreamSetLocalToRootTR(ref stream, boneCount - 1);
                })
                    .WarmupCount(50)
                    .MeasurementCount(100)
                    .Run();
            }

            rigDefintion.Dispose();
        }

        [Test, Performance]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(400)]
        [TestCase(800)]
        public void Test_SetLocalToRootTRS_OfLastBone(int boneCount)
        {
            var rigDefintion = BuildRigDefinitionBoneChain(boneCount);
            using (var buffer = new NativeArray<AnimatedData>(rigDefintion.Value.Bindings.StreamSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory))
            {
                var stream = AnimationStream.Create(rigDefintion, buffer);
                stream.ResetToDefaultValues();

                Measure.Method(() =>
                {
                    BurstedAnimationStream.EvaluateAnimationStreamSetLocalToRootTRS(ref stream, boneCount - 1);
                })
                    .WarmupCount(50)
                    .MeasurementCount(100)
                    .Run();
            }

            rigDefintion.Dispose();
        }
    }
}
