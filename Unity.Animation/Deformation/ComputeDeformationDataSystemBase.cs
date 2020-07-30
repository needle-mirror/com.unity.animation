using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Deformations;
using Unity.Collections.LowLevel.Unsafe;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    public abstract class ComputeDeformationDataSystemBase : SystemBase
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_Marker = new ProfilerMarker("ComputeDeformationDataSystemBase");
#endif

        EntityQuery m_ComputeSkinMatrixQuery;
        EntityQuery m_CopySparseBlendShapeWeightQuery;
        EntityQuery m_CopyContiguousBlendShapeWeightQuery;

        protected override void OnCreate()
        {
            m_ComputeSkinMatrixQuery = GetEntityQuery(ComputeSkinMatrixJob.QueryDesc);
            m_CopySparseBlendShapeWeightQuery = GetEntityQuery(CopySparseBlendShapeWeightJob.QueryDesc);
            m_CopyContiguousBlendShapeWeightQuery = GetEntityQuery(CopyContiguousBlendShapeWeightJob.QueryDesc);
        }

        protected override void OnUpdate()
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_Marker.Begin();
#endif
            var computeSkinMatrixJob = new ComputeSkinMatrixJob
            {
                EntityAnimatedLocalToRoot = GetBufferFromEntity<AnimatedLocalToRoot>(true),
                EntityRigRootBone = GetComponentDataFromEntity<RigRootEntity>(true),
                EntityLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(true),
                RigEntityType = GetComponentTypeHandle<RigEntity>(true),
                SkinnedMeshRootEntityType = GetComponentTypeHandle<SkinnedMeshRootEntity>(true),
                SkinnedMeshToRigIndexMappingType = GetBufferTypeHandle<SkinnedMeshToRigIndexMapping>(true),
                BindPoseType = GetBufferTypeHandle<BindPose>(true),
                SkinMatriceType = GetBufferTypeHandle<Deformations.SkinMatrix>()
            }.ScheduleParallel(m_ComputeSkinMatrixQuery, Dependency);

            var copySparseBlendShapeJob = new CopySparseBlendShapeWeightJob
            {
                Rigs = GetComponentDataFromEntity<Rig>(true),
                AnimatedData = GetBufferFromEntity<AnimatedData>(true),
                RigEntityType = GetComponentTypeHandle<RigEntity>(true),
                BlendShapeToRigIndexMappingType = GetBufferTypeHandle<BlendShapeToRigIndexMapping>(true),
                BlendShapeWeightType = GetBufferTypeHandle<BlendShapeWeight>()
            }.ScheduleParallel(m_CopySparseBlendShapeWeightQuery, Dependency);

            var copyContiguousBlendShapeJob = new CopyContiguousBlendShapeWeightJob
            {
                Rigs = GetComponentDataFromEntity<Rig>(true),
                AnimatedData = GetBufferFromEntity<AnimatedData>(true),
                RigEntityType = GetComponentTypeHandle<RigEntity>(true),
                BlendShapeChunkMappingType = GetComponentTypeHandle<BlendShapeChunkMapping>(true),
                BlendShapeWeightType = GetBufferTypeHandle<BlendShapeWeight>()
            }.ScheduleParallel(m_CopyContiguousBlendShapeWeightQuery, copySparseBlendShapeJob);

            Dependency = JobHandle.CombineDependencies(computeSkinMatrixJob, copyContiguousBlendShapeJob);

#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_Marker.End();
#endif
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct ComputeSkinMatrixJob : IJobChunk
        {
            [ReadOnly] public BufferFromEntity<AnimatedLocalToRoot>  EntityAnimatedLocalToRoot;
            [ReadOnly] public ComponentDataFromEntity<RigRootEntity> EntityRigRootBone;
            [ReadOnly] public ComponentDataFromEntity<LocalToWorld>  EntityLocalToWorld;

            [ReadOnly] public ComponentTypeHandle<RigEntity> RigEntityType;
            [ReadOnly] public ComponentTypeHandle<SkinnedMeshRootEntity> SkinnedMeshRootEntityType;
            [ReadOnly] public BufferTypeHandle<SkinnedMeshToRigIndexMapping> SkinnedMeshToRigIndexMappingType;
            [ReadOnly] public BufferTypeHandle<BindPose> BindPoseType;

            public BufferTypeHandle<Deformations.SkinMatrix> SkinMatriceType;

            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<RigEntity>(),
                    ComponentType.ReadOnly<SkinnedMeshRootEntity>(),
                    ComponentType.ReadOnly<SkinnedMeshToRigIndexMapping>(),
                    ComponentType.ReadOnly<BindPose>(),
                    ComponentType.ReadWrite<Deformations.SkinMatrix>()
                }
            };

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigEntities = chunk.GetNativeArray(RigEntityType);
                var skinnedMeshRootEntities = chunk.GetNativeArray(SkinnedMeshRootEntityType);
                var skinnedMeshToRigIndexMappings = chunk.GetBufferAccessor(SkinnedMeshToRigIndexMappingType);
                var bindPoses = chunk.GetBufferAccessor(BindPoseType);
                var outSkinMatrices = chunk.GetBufferAccessor(SkinMatriceType);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var rig = rigEntities[i].Value;
                    if (EntityAnimatedLocalToRoot.HasComponent(rig) && EntityRigRootBone.HasComponent(rig))
                    {
                        var animatedLocalToRootMatrices = EntityAnimatedLocalToRoot[rig];
                        var rigRootL2W = EntityLocalToWorld[EntityRigRootBone[rig].Value].Value;
                        var smrRootL2W = EntityLocalToWorld[skinnedMeshRootEntities[i].Value].Value;

                        ComputeSkinMatrices(
                            math.mul(math.inverse(smrRootL2W), rigRootL2W),
                            skinnedMeshToRigIndexMappings[i],
                            bindPoses[i],
                            animatedLocalToRootMatrices,
                            outSkinMatrices[i]
                        );
                    }
                }
            }

            static void ComputeSkinMatrices(
                float4x4 smr2RigRootOffset,
                [ReadOnly] DynamicBuffer<SkinnedMeshToRigIndexMapping> skinnedMeshToRigIndexMappings,
                [ReadOnly] DynamicBuffer<BindPose> bindPoses,
                [ReadOnly] DynamicBuffer<AnimatedLocalToRoot> animatedLocalToRootMatrices,
                DynamicBuffer<Deformations.SkinMatrix> outSkinMatrices
            )
            {
                for (int i = 0; i != skinnedMeshToRigIndexMappings.Length; ++i)
                {
                    var mapping = skinnedMeshToRigIndexMappings[i];

                    var skinMat = math.mul(math.mul(smr2RigRootOffset, animatedLocalToRootMatrices[mapping.RigIndex].Value), bindPoses[mapping.SkinMeshIndex].Value);
                    outSkinMatrices[mapping.SkinMeshIndex] = new Deformations.SkinMatrix
                    {
                        Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz)
                    };
                }
            }
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct CopyContiguousBlendShapeWeightJob : IJobChunk
        {
            [ReadOnly] public ComponentDataFromEntity<Rig> Rigs;
            [ReadOnly] public BufferFromEntity<AnimatedData> AnimatedData;

            [ReadOnly] public ComponentTypeHandle<RigEntity> RigEntityType;
            [ReadOnly] public ComponentTypeHandle<BlendShapeChunkMapping> BlendShapeChunkMappingType;

            public BufferTypeHandle<BlendShapeWeight> BlendShapeWeightType;

            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<RigEntity>(),
                    ComponentType.ReadOnly<BlendShapeChunkMapping>(),
                    ComponentType.ReadWrite<BlendShapeWeight>()
                }
            };

            public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigEntities = chunk.GetNativeArray(RigEntityType);
                var blendShapeChunkMappings = chunk.GetNativeArray(BlendShapeChunkMappingType);
                var blendShapeWeightAccessor = chunk.GetBufferAccessor(BlendShapeWeightType);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var rigEntity = rigEntities[i].Value;
                    if (Rigs.HasComponent(rigEntity) && AnimatedData.HasComponent(rigEntity))
                    {
                        var mapping = blendShapeChunkMappings[i];
                        var stream = AnimationStream.CreateReadOnly(Rigs[rigEntity], AnimatedData[rigEntity].AsNativeArray());
                        var blendShapeWeights = blendShapeWeightAccessor[i].AsNativeArray();

                        UnsafeUtility.MemCpy(
                            blendShapeWeights.GetUnsafePtr(),
                            (float*)stream.GetUnsafePtr() + stream.Rig.Value.Bindings.FloatSamplesOffset + mapping.RigIndex,
                            mapping.Size * UnsafeUtility.SizeOf<float>()
                        );
                    }
                }
            }
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct CopySparseBlendShapeWeightJob : IJobChunk
        {
            [ReadOnly] public ComponentDataFromEntity<Rig> Rigs;
            [ReadOnly] public BufferFromEntity<AnimatedData> AnimatedData;

            [ReadOnly] public ComponentTypeHandle<RigEntity> RigEntityType;
            [ReadOnly] public BufferTypeHandle<BlendShapeToRigIndexMapping> BlendShapeToRigIndexMappingType;

            public BufferTypeHandle<BlendShapeWeight> BlendShapeWeightType;

            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<RigEntity>(),
                    ComponentType.ReadOnly<BlendShapeToRigIndexMapping>(),
                    ComponentType.ReadWrite<BlendShapeWeight>()
                }
            };

            public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigEntities = chunk.GetNativeArray(RigEntityType);
                var blendShapeToRigIndexMappings = chunk.GetBufferAccessor(BlendShapeToRigIndexMappingType);
                var blendShapeWeightAccessor = chunk.GetBufferAccessor(BlendShapeWeightType);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var rigEntity = rigEntities[i].Value;
                    if (Rigs.HasComponent(rigEntity) && AnimatedData.HasComponent(rigEntity))
                    {
                        var mappings = blendShapeToRigIndexMappings[i];
                        var stream = AnimationStream.CreateReadOnly(Rigs[rigEntity], AnimatedData[rigEntity].AsNativeArray());
                        var blendShapeWeights = blendShapeWeightAccessor[i].AsNativeArray();

                        for (int j = 0; j != mappings.Length; ++j)
                        {
                            blendShapeWeights[mappings[j].BlendShapeIndex] = new BlendShapeWeight
                            {
                                Value = stream.GetFloat(mappings[j].RigIndex)
                            };
                        }
                    }
                }
            }
        }
    }

    [System.Obsolete("ComputeSkinMatrixSystemBase is deprecated use ComputeDeformationDataSystemBase instead. (RemovedAfter 2020-08-19). ComputeDeformationDataSystemBase (UnityUpgradable)", false)]
    public abstract class ComputeSkinMatrixSystemBase : SystemBase
    {
        protected override void OnUpdate()
        {
            throw new System.NotImplementedException();
        }
    }
}
