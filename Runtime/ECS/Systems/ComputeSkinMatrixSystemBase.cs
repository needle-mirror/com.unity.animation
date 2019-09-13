using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;

namespace Unity.Animation
{
    public abstract class ComputeSkinMatrixSystemBase : JobComponentSystem
    {
        EntityQuery m_Query;

        protected override void OnCreate()
        {
            m_Query = GetEntityQuery(
                ComponentType.ReadOnly<SkinnedMeshComponentData>(),
                ComponentType.ReadOnly<SkinnedMeshToSkeletonBone>(),
                ComponentType.ReadWrite<SkinMatrix>()
                );
        }

        protected override JobHandle OnUpdate(JobHandle dep)
        {
            var job = new ComputeSkinMatricesJob
            {
                SkinnedMeshComponent = GetArchetypeChunkComponentType<SkinnedMeshComponentData>(true),
                SkinnedMeshToSkeletonBone = GetArchetypeChunkBufferType<SkinnedMeshToSkeletonBone>(true),
                SkinnedMeshBindPoses = GetArchetypeChunkBufferType<BindPose>(true),
                GlobalMatrices = GetBufferFromEntity<AnimatedLocalToWorld>(true),
                SkinMatrices = GetArchetypeChunkBufferType<SkinMatrix>()
            };

            return job.Schedule(m_Query, dep);
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeSkinMatricesJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<SkinnedMeshComponentData> SkinnedMeshComponent;
            [ReadOnly] public ArchetypeChunkBufferType<SkinnedMeshToSkeletonBone> SkinnedMeshToSkeletonBone;
            [ReadOnly] public ArchetypeChunkBufferType<BindPose> SkinnedMeshBindPoses;
            [ReadOnly] public BufferFromEntity<AnimatedLocalToWorld> GlobalMatrices;

            public ArchetypeChunkBufferType<SkinMatrix> SkinMatrices;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var smComponent = chunk.GetNativeArray(SkinnedMeshComponent);
                var smToSkBone = chunk.GetBufferAccessor(SkinnedMeshToSkeletonBone);
                var bindPoses = chunk.GetBufferAccessor(SkinnedMeshBindPoses);
                var outBindMatrices = chunk.GetBufferAccessor(SkinMatrices);

                for (int i = 0; i != smComponent.Length; ++i)
                {
                    if(GlobalMatrices.Exists(smComponent[i].RigEntity))
                    {
                        ComputeSkinMatrix(smToSkBone[i], bindPoses[i],
                            GlobalMatrices[smComponent[i].RigEntity].Reinterpret<float4x4>(),
                            outBindMatrices[i]);
                    }
                }
            }

            void ComputeSkinMatrix(
                DynamicBuffer<SkinnedMeshToSkeletonBone> skeletonToSkinnedMeshBone,
                DynamicBuffer<BindPose> bindPoses,
                DynamicBuffer<float4x4> localToWorlds,
                DynamicBuffer<SkinMatrix> outSkinMatrices
                )
            {
                for (int i = 0; i != outSkinMatrices.Length; ++i)
                {
                    var index = skeletonToSkinnedMeshBone[i].Value;
                    var skinMat = math.mul(localToWorlds[index], bindPoses[i].Value);

                    outSkinMatrices[i] = new SkinMatrix { Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz) };
                }
            }
        }
    }
}
