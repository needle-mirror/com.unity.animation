using NUnit.Framework;

using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;

using Unity.Animation.Tests;
using Unity.PerformanceTesting;
using System.Collections.Generic;

namespace Unity.Animation.PerformanceTests
{
    [Category("Performance"), Category("Animation")]
    public class ExposeTransformPerformanceTests : AnimationTestsFixture
    {
        BlobAssetReference<RigDefinition> CreateRigDefinition(int boneCount)
        {
            var skeleton = new SkeletonNode[boneCount];

            skeleton[0] = new SkeletonNode { Id = "Root", ParentIndex = -1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };
            for (int i = 1; i < boneCount; i++)
            {
                skeleton[i] = new SkeletonNode { Id = $"{i}", ParentIndex = i - 1, AxisIndex = -1, LocalTranslationDefaultValue = float3.zero, LocalRotationDefaultValue = quaternion.identity, LocalScaleDefaultValue = new float3(1) };
            }
            ;

            return RigBuilder.CreateRigDefinition(skeleton);
        }

        List<Entity> CreateRigEntityTransforms(BlobAssetReference<RigDefinition> rig)
        {
            var list = new List<Entity>(rig.Value.Skeleton.BoneCount);

            for (int i = 0; i < rig.Value.Skeleton.BoneCount; i++)
            {
                list.Add(m_Manager.CreateEntity());

                m_Manager.AddComponentData(list[i], new LocalToWorld { Value = float4x4.identity });
                m_Manager.AddComponentData(list[i], new Translation { Value = new float3(i) });
                m_Manager.AddComponentData(list[i], new Rotation { Value = quaternion.identity });
                m_Manager.AddComponentData(list[i], new NonUniformScale { Value = new float3(1) });

                if (rig.Value.Skeleton.ParentIndexes[i] != -1)
                {
                    var parent = list[rig.Value.Skeleton.ParentIndexes[i]];
                    m_Manager.AddComponentData(list[i], new Parent { Value = parent });
                    m_Manager.AddComponentData(list[i], new LocalToParent());
                }
            }
            return list;
        }

        [Test, Performance]
        [TestCase(200)]
        [TestCase(400)]
        public void EvaluateReadExposedTransformSystemWithAllEntityWithReadAccess(int boneCount)
        {
            var rig = CreateRigDefinition(boneCount);

            var entityTransforms = CreateRigEntityTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);

            for (int i = 0; i < entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                );
            }

            Measure.Method(
                () =>
                {
                    World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
                    m_Manager.CompleteAllJobs();
                })
                .SampleGroup("EvaluateReadExposedTransformSystemWithAllEntityWithReadAccess")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }

        [Test, Performance]
        [TestCase(200)]
        [TestCase(400)]
        public void EvaluateReadExposedTransformSystemWithEntityWithReadAccessChangingEveryFrame(int boneCount)
        {
            var rig = CreateRigDefinition(boneCount);

            var entityTransforms = CreateRigEntityTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);

            var entityIter = 0;
            for (; entityIter < 20; entityIter++)
            {
                RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[entityIter], entityIter
                );
            }

            Measure.Method(
                () =>
                {
                    World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
                    m_Manager.CompleteAllJobs();
                })
                .SampleGroup("EvaluateReadExposedTransformSystemWithAllEntityWithReadAccess")
                .SetUp(() =>
                {
                    if (entityIter < entityTransforms.Count)
                    {
                        RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                            m_Manager, rigEntity, entityTransforms[entityIter], entityIter);
                        entityIter++;
                    }
                }
                )
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }

        [Test, Performance]
        [TestCase(200)]
        [TestCase(400)]
        public void EvaluateReadExposedTransformSystemWithHalfEntityWithReadAccess(int boneCount)
        {
            var rig = CreateRigDefinition(boneCount);

            var entityTransforms = CreateRigEntityTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);

            for (int i = 0; i < entityTransforms.Count; i++)
            {
                if (i % 2 == 0)
                {
                    RigEntityBuilder.AddReadTransformHandle<ProcessDefaultAnimationGraph.ReadTransformHandle>(
                        m_Manager, rigEntity, entityTransforms[i], i
                    );
                }
            }

            Measure.Method(
                () =>
                {
                    World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
                    m_Manager.CompleteAllJobs();
                })
                .SampleGroup("EvaluateReadExposedTransformSystemWithHalfEntityWithReadAccess")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }

        [Test, Performance]
        [TestCase(200)]
        [TestCase(400)]
        public void EvaluateReadExposedTransformSystemWithNoEntityWithReadAccess(int boneCount)
        {
            var rig = CreateRigDefinition(boneCount);

            var entityTransforms = CreateRigEntityTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);

            Measure.Method(
                () =>
                {
                    World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
                    m_Manager.CompleteAllJobs();
                })
                .SampleGroup("EvaluateReadExposedTransformSystemWithNoEntityWithReadAccess")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }

        [Test, Performance]
        [TestCase(200)]
        [TestCase(400)]
        public void EvaluateWriteExposedTransformSystemWithAllEntityWithWriteAccess(int boneCount)
        {
            var rig = CreateRigDefinition(boneCount);

            var entityTransforms = CreateRigEntityTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);

            for (int i = 0; i < entityTransforms.Count; i++)
            {
                RigEntityBuilder.AddWriteTransformHandle<ProcessLateAnimationGraph.WriteTransformHandle>(
                    m_Manager, rigEntity, entityTransforms[i], i
                );
            }

            Measure.Method(
                () =>
                {
                    World.GetOrCreateSystem<ProcessLateAnimationGraph>().Update();
                    m_Manager.CompleteAllJobs();
                })
                .SampleGroup("EvaluateWriteExposedTransformSystemWithAllEntityWithWriteAccess")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }

        [Test, Performance]
        [TestCase(200)]
        [TestCase(400)]
        public void EvaluateWriteExposedTransformSystemWithHalfEntityWithWriteAccess(int boneCount)
        {
            var rig = CreateRigDefinition(boneCount);

            var entityTransforms = CreateRigEntityTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);

            for (int i = 0; i < entityTransforms.Count; i++)
            {
                if (i % 2 == 0)
                {
                    RigEntityBuilder.AddWriteTransformHandle<ProcessLateAnimationGraph.WriteTransformHandle>(
                        m_Manager, rigEntity, entityTransforms[i], i
                    );
                }
            }

            Measure.Method(
                () =>
                {
                    World.GetOrCreateSystem<ProcessLateAnimationGraph>().Update();
                    m_Manager.CompleteAllJobs();
                })
                .SampleGroup("EvaluateWriteExposedTransformSystemWithHalfEntityWithWriteAccess")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }

        [Test, Performance]
        [TestCase(200)]
        [TestCase(400)]
        public void EvaluateWriteExposedTransformSystemWithNoEntityWithWriteAccess(int boneCount)
        {
            var rig = CreateRigDefinition(boneCount);

            var entityTransforms = CreateRigEntityTransforms(rig);

            var rigEntity = m_Manager.CreateEntity();
            SetupRigEntity(rigEntity, rig, entityTransforms[0]);

            Measure.Method(
                () =>
                {
                    World.GetOrCreateSystem<ProcessLateAnimationGraph>().Update();
                    m_Manager.CompleteAllJobs();
                })
                .SampleGroup("EvaluateWriteExposedTransformSystemWithNoEntityWithWriteAccess")
                .WarmupCount(100)
                .MeasurementCount(500)
                .Run();
        }
    }
}
