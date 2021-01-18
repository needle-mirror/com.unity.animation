using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Deformations;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Animation
{
    public abstract class ComputeDeformationDataBase : SystemBase
    {
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
            var computeSkinMatrixJob = new ComputeSkinMatrixJob
            {
                EntityAnimatedLocalToRoot = GetBufferFromEntity<AnimatedLocalToRoot>(true),
                EntityRigRootBone = GetComponentDataFromEntity<RigRootEntity>(true),
                EntityLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(true),
                RigEntityType = GetComponentTypeHandle<RigEntity>(true),
                SkinnedMeshRootEntityType = GetComponentTypeHandle<SkinnedMeshRootEntity>(true),
                SkinnedMeshToRigIndexMappingType = GetBufferTypeHandle<SkinnedMeshToRigIndexMapping>(true),
                SkinnedMeshToRigIndexIndirectMappingType = GetBufferTypeHandle<SkinnedMeshToRigIndexIndirectMapping>(true),
                BindPoseType = GetBufferTypeHandle<BindPose>(true),
                SkinMatriceType = GetBufferTypeHandle<SkinMatrix>()
            }.ScheduleParallel(m_ComputeSkinMatrixQuery, 1, Dependency);

            var copySparseBlendShapeJob = new CopySparseBlendShapeWeightJob
            {
                Rigs = GetComponentDataFromEntity<Rig>(true),
                AnimatedData = GetBufferFromEntity<AnimatedData>(true),
                RigEntityType = GetComponentTypeHandle<RigEntity>(true),
                BlendShapeToRigIndexMappingType = GetBufferTypeHandle<BlendShapeToRigIndexMapping>(true),
                BlendShapeWeightType = GetBufferTypeHandle<BlendShapeWeight>()
            }.ScheduleParallel(m_CopySparseBlendShapeWeightQuery, 1, Dependency);

            var copyContiguousBlendShapeJob = new CopyContiguousBlendShapeWeightJob
            {
                Rigs = GetComponentDataFromEntity<Rig>(true),
                AnimatedData = GetBufferFromEntity<AnimatedData>(true),
                RigEntityType = GetComponentTypeHandle<RigEntity>(true),
                BlendShapeChunkMappingType = GetComponentTypeHandle<BlendShapeChunkMapping>(true),
                BlendShapeWeightType = GetBufferTypeHandle<BlendShapeWeight>()
            }.ScheduleParallel(m_CopyContiguousBlendShapeWeightQuery, 1, copySparseBlendShapeJob);

            Dependency = JobHandle.CombineDependencies(computeSkinMatrixJob, copyContiguousBlendShapeJob);
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct ComputeSkinMatrixJob : IJobEntityBatch
        {
            [ReadOnly] public BufferFromEntity<AnimatedLocalToRoot>  EntityAnimatedLocalToRoot;
            [ReadOnly] public ComponentDataFromEntity<RigRootEntity> EntityRigRootBone;
            [ReadOnly] public ComponentDataFromEntity<LocalToWorld>  EntityLocalToWorld;

            [ReadOnly] public ComponentTypeHandle<RigEntity> RigEntityType;
            [ReadOnly] public ComponentTypeHandle<SkinnedMeshRootEntity> SkinnedMeshRootEntityType;
            [ReadOnly] public BufferTypeHandle<SkinnedMeshToRigIndexMapping> SkinnedMeshToRigIndexMappingType;
            [ReadOnly] public BufferTypeHandle<SkinnedMeshToRigIndexIndirectMapping> SkinnedMeshToRigIndexIndirectMappingType;
            [ReadOnly] public BufferTypeHandle<BindPose> BindPoseType;

            public BufferTypeHandle<SkinMatrix> SkinMatriceType;

            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<RigEntity>(),
                    ComponentType.ReadOnly<SkinnedMeshRootEntity>(),
                    ComponentType.ReadOnly<SkinnedMeshToRigIndexMapping>(),
                    ComponentType.ReadOnly<SkinnedMeshToRigIndexIndirectMapping>(),
                    ComponentType.ReadOnly<BindPose>(),
                    ComponentType.ReadWrite<SkinMatrix>()
                }
            };

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var rigEntities = batchInChunk.GetNativeArray(RigEntityType);
                var skinnedMeshRootEntities = batchInChunk.GetNativeArray(SkinnedMeshRootEntityType);
                var skinnedMeshToRigIndexMappings = batchInChunk.GetBufferAccessor(SkinnedMeshToRigIndexMappingType);
                var skinnedMeshToRigIndexIndirectMappings = batchInChunk.GetBufferAccessor(SkinnedMeshToRigIndexIndirectMappingType);
                var bindPoses = batchInChunk.GetBufferAccessor(BindPoseType);
                var outSkinMatrices = batchInChunk.GetBufferAccessor(SkinMatriceType);

                for (int i = 0; i != batchInChunk.Count; ++i)
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
                            skinnedMeshToRigIndexIndirectMappings[i],
                            bindPoses[i],
                            animatedLocalToRootMatrices,
                            outSkinMatrices[i]
                        );
                    }
                }
            }

            static void ComputeSkinMatrices(
                float4x4 smrToRigRootOffset,
                [ReadOnly] DynamicBuffer<SkinnedMeshToRigIndexMapping> smrToRigMappings,
                [ReadOnly] DynamicBuffer<SkinnedMeshToRigIndexIndirectMapping> smrToRigIndirectMappings,
                [ReadOnly] DynamicBuffer<BindPose> bindPoses,
                [ReadOnly] DynamicBuffer<AnimatedLocalToRoot> animatedLocalToRootMatrices,
                DynamicBuffer<SkinMatrix> outSkinMatrices
            )
            {
                for (int i = 0; i != smrToRigMappings.Length; ++i)
                {
                    var mapping = smrToRigMappings[i];

                    var skinMat = math.mul(math.mul(smrToRigRootOffset, animatedLocalToRootMatrices[mapping.RigIndex].Value), bindPoses[mapping.SkinMeshIndex].Value);
                    outSkinMatrices[mapping.SkinMeshIndex] = new SkinMatrix
                    {
                        Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz)
                    };
                }

                for (int i = 0; i != smrToRigIndirectMappings.Length; ++i)
                {
                    var mapping = smrToRigIndirectMappings[i];

                    var skinMat = math.mul(math.mul(smrToRigRootOffset, math.mul(animatedLocalToRootMatrices[mapping.RigIndex].Value, mapping.Offset)), bindPoses[mapping.SkinMeshIndex].Value);
                    outSkinMatrices[mapping.SkinMeshIndex] = new SkinMatrix
                    {
                        Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz)
                    };
                }
            }
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct CopyContiguousBlendShapeWeightJob : IJobEntityBatch
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

            public unsafe void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var rigEntities = batchInChunk.GetNativeArray(RigEntityType);
                var blendShapeChunkMappings = batchInChunk.GetNativeArray(BlendShapeChunkMappingType);
                var blendShapeWeightAccessor = batchInChunk.GetBufferAccessor(BlendShapeWeightType);

                for (int i = 0; i != batchInChunk.Count; ++i)
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
        struct CopySparseBlendShapeWeightJob : IJobEntityBatch
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

            public unsafe void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var rigEntities = batchInChunk.GetNativeArray(RigEntityType);
                var blendShapeToRigIndexMappings = batchInChunk.GetBufferAccessor(BlendShapeToRigIndexMappingType);
                var blendShapeWeightAccessor = batchInChunk.GetBufferAccessor(BlendShapeWeightType);

                for (int i = 0; i != batchInChunk.Count; ++i)
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
}
