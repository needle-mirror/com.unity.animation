using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Profiling;

namespace Unity.Animation
{
    public abstract class ComputeSkinMatrixSystemBase : JobComponentSystem
    {
        static readonly ProfilerMarker k_Marker = new ProfilerMarker("ComputeSkinMatrixSystemBase");

        EntityQuery m_Query;

        protected override void OnCreate()
        {
             m_Query = GetEntityQuery(
                ComponentType.ReadOnly<SkinnedMeshRigEntity>(),
                ComponentType.ReadOnly<SkinnedMeshToRigIndex>(),
                ComponentType.ReadOnly<BindPose>(),
                ComponentType.ReadWrite<SkinMatrix>()
                );
        }

        protected override JobHandle OnUpdate(JobHandle inputDep)
        {
            k_Marker.Begin();

            var renderingSkinMatricesJob = new ComputeSkinMatricesJob
            {
                AnimatedLocalToRoot = GetBufferFromEntity<AnimatedLocalToRoot>(true),
                RigEntities = GetArchetypeChunkComponentType<SkinnedMeshRigEntity>(true),
                SkinnedMeshToRigIndices = GetArchetypeChunkBufferType<SkinnedMeshToRigIndex>(true),
                BindPoses = GetArchetypeChunkBufferType<BindPose>(true),
                SkinMatrices = GetArchetypeChunkBufferType<SkinMatrix>()
            };
            inputDep = renderingSkinMatricesJob.Schedule(m_Query, inputDep);

            k_Marker.End();

            return inputDep;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeSkinMatricesJob : IJobChunk
        {
            [ReadOnly] public BufferFromEntity<AnimatedLocalToRoot> AnimatedLocalToRoot;

            [ReadOnly] public ArchetypeChunkComponentType<SkinnedMeshRigEntity> RigEntities;
            [ReadOnly] public ArchetypeChunkBufferType<SkinnedMeshToRigIndex> SkinnedMeshToRigIndices;
            [ReadOnly] public ArchetypeChunkBufferType<BindPose> BindPoses;

            public ArchetypeChunkBufferType<SkinMatrix> SkinMatrices;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigEntities = chunk.GetNativeArray(RigEntities);
                var skinnedMeshToRigIndices = chunk.GetBufferAccessor(SkinnedMeshToRigIndices);
                var bindPoses = chunk.GetBufferAccessor(BindPoses);
                var outSkinMatrices = chunk.GetBufferAccessor(SkinMatrices);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var rig = rigEntities[i].Value;
                    if (AnimatedLocalToRoot.Exists(rig))
                    {
                        var animatedLocalToRootMatrices = AnimatedLocalToRoot[rig];
                        ComputeRenderingSkinMatrix(
                            skinnedMeshToRigIndices[i],
                            bindPoses[i],
                            animatedLocalToRootMatrices,
                            outSkinMatrices[i]
                            );
                    }
                }
            }

            static void ComputeRenderingSkinMatrix(
                [ReadOnly] DynamicBuffer<SkinnedMeshToRigIndex> skinMeshToRigIndices,
                [ReadOnly] DynamicBuffer<BindPose> bindPoses,
                [ReadOnly] DynamicBuffer<AnimatedLocalToRoot> animatedLocalToRootMatrices,
                DynamicBuffer<SkinMatrix> outSkinMatrices
                )
            {
                for (int i = 0; i != outSkinMatrices.Length; ++i)
                {
                    var index = skinMeshToRigIndices[i].Value;
                    var skinMat = math.mul(animatedLocalToRootMatrices[index].Value, bindPoses[i].Value);

                    outSkinMatrices[i] = new SkinMatrix
                    {
                        Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz)
                    };
                }
            }
        }
    }
}
