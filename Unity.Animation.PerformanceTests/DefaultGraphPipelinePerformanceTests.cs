using NUnit.Framework;

using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

using Unity.Animation.Tests;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace Unity.Animation.PerformanceTests
{
    [Category("Performance"), Category("Animation")]
    public class DefaultGraphPipelinePerformanceTests : AnimationTestsFixture, IPrebuildSetup
    {
        public enum RigType
        {
            Rig5Bone = 5,
            Rig30Bone = 30,
            Rig70Bone = 70,
            Rig130Bone = 130,
            Rig250Bone = 250
        }

        readonly static string[] blobPath = new string[]
        {
            "DefaultGraphPipelineRig5Bone.blob",
            "DefaultGraphPipelineRig30Bone.blob",
            "DefaultGraphPipelineRig70Bone.blob",
            "DefaultGraphPipelineRig130Bone.blob",
            "DefaultGraphPipelineRig250Bone.blob"
        };

        public void Setup()
        {
#if UNITY_EDITOR
            PlaymodeTestsEditorSetup.CreateStreamingAssetsDirectory();
            var rigTypes = RigType.GetValues(typeof(RigType));
            for (int i = 0; i < rigTypes.Length; i++)
            {
                var rig = CreateTestRigDefinition((int)rigTypes.GetValue(i) , null);
                BlobFile.WriteBlobAsset(ref rig, blobPath[i]);
                rig.Dispose();
            }
#endif
        }

        void UpdateSystem()
        {
            World.GetOrCreateSystem<InitializeAnimation>().Update();
            World.GetOrCreateSystem<ProcessDefaultAnimationGraph>().Update();
            World.GetOrCreateSystem<ComputeRigMatrices>().Update();
        }

        int GetFileIndex(RigType rigType)
        {
            switch (rigType)
            {
                default:
                case RigType.Rig5Bone: return 0;
                case RigType.Rig30Bone: return 1;
                case RigType.Rig70Bone: return 2;
                case RigType.Rig130Bone: return 3;
                case RigType.Rig250Bone: return 4;
            }
        }

        [Test, Performance]
        public void EvaluateDefaultGraphPipelineWithUniqueRig([Values(RigType.Rig5Bone, RigType.Rig30Bone, RigType.Rig70Bone, RigType.Rig130Bone, RigType.Rig250Bone)] RigType rigType, [Values(30, 130, 255)] int instanceCount, [Values(true, false)] bool withRootMotion, [Values(true, false)] bool withSharedComponent)
        {
            var rig = BlobFile.ReadBlobAsset<RigDefinition>(blobPath[GetFileIndex(rigType)]);

            var rigEntity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(rigEntity, new LocalToWorld { Value = float4x4.identity });
            m_Manager.AddComponentData(rigEntity, new Translation { Value = new float3(0) });
            m_Manager.AddComponentData(rigEntity, new Rotation { Value = quaternion.identity });
            m_Manager.AddComponentData(rigEntity, new NonUniformScale { Value = new float3(1) });

            SetupRigEntity(rigEntity, rig, rigEntity);
            if (!withRootMotion)
                m_Manager.AddComponent<DisableRootTransformReadWriteTag>(rigEntity);
            if (!withSharedComponent)
                m_Manager.RemoveComponent<SharedRigHash>(rigEntity);

            using (var entities = m_Manager.Instantiate(rigEntity, instanceCount, Allocator.Persistent))
            {
                Measure.Method(
                    () =>
                    {
                        UpdateSystem();
                        m_Manager.CompleteAllJobs();
                    })
                    .WarmupCount(30)
                    .MeasurementCount(200)
                    .Run();

                m_Manager.DestroyEntity(entities);
            }

            m_Manager.DestroyEntity(rigEntity);
            rig.Dispose();
        }

        [Test, Performance]
        public void EvaluateDefaultGraphPipelineWithMultipleRig([Values(30, 130, 255)] int instanceCount, [Values(true, false)] bool withRootMotion, [Values(true, false)] bool withSharedComponent)
        {
            using (var allEntities = new NativeList<Entity>(instanceCount, Allocator.Persistent))
            {
                var rigs = new BlobAssetReference<RigDefinition>[blobPath.Length];
                for (int i = 0; i < blobPath.Length; ++i)
                    rigs[i] = BlobFile.ReadBlobAsset<RigDefinition>(blobPath[i]);

                for (int j = 0; j < instanceCount / 5; j++)
                {
                    for (int i = 0; i < blobPath.Length; i++)
                    {
                        var rigEntity = m_Manager.CreateEntity();
                        m_Manager.AddComponentData(rigEntity, new LocalToWorld { Value = float4x4.identity });
                        m_Manager.AddComponentData(rigEntity, new Translation { Value = new float3(0) });
                        m_Manager.AddComponentData(rigEntity, new Rotation { Value = quaternion.identity });
                        m_Manager.AddComponentData(rigEntity, new NonUniformScale { Value = new float3(1) });

                        SetupRigEntity(rigEntity, rigs[i], rigEntity);
                        if (!withRootMotion)
                            m_Manager.AddComponent<DisableRootTransformReadWriteTag>(rigEntity);

                        if (!withSharedComponent)
                            m_Manager.RemoveComponent<SharedRigHash>(rigEntity);

                        allEntities.Add(rigEntity);
                    }
                }

                Measure.Method(
                    () =>
                    {
                        UpdateSystem();
                        m_Manager.CompleteAllJobs();
                    })
                    .WarmupCount(30)
                    .MeasurementCount(200)
                    .Run();

                m_Manager.DestroyEntity(allEntities);

                for (int i = 0; i < rigs.Length; ++i)
                    rigs[i].Dispose();
            }
        }
    }
}
