using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Profiling;
using Unity.Transforms;

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
                ComponentType.ReadWrite<SkinMatrix>(),
                ComponentType.ReadWrite<LocalToWorld>()
                );
        }

        protected override JobHandle OnUpdate(JobHandle inputDep)
        {
            k_Marker.Begin();

            var renderingSkinMatricesJob = new ComputeSkinMatricesJob
            {
                AnimatedLocalToRig = GetBufferFromEntity<AnimatedLocalToRig>(true),
                AnimatedLocalToWorld = GetBufferFromEntity<AnimatedLocalToWorld>(true),
                RigEntities = GetArchetypeChunkComponentType<SkinnedMeshRigEntity>(true),
                SkinnedMeshToRigIndices = GetArchetypeChunkBufferType<SkinnedMeshToRigIndex>(true),
                BindPoses = GetArchetypeChunkBufferType<BindPose>(true),
                SkinMatrices = GetArchetypeChunkBufferType<SkinMatrix>(),
                LocalToWorld = GetArchetypeChunkComponentType<LocalToWorld>(),
            };
            inputDep = renderingSkinMatricesJob.Schedule(m_Query, inputDep);

            k_Marker.End();

            return inputDep;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeSkinMatricesJob : IJobChunk
        {
            [ReadOnly] public BufferFromEntity<AnimatedLocalToRig> AnimatedLocalToRig;
            [ReadOnly] public BufferFromEntity<AnimatedLocalToWorld> AnimatedLocalToWorld;

            [ReadOnly] public ArchetypeChunkComponentType<SkinnedMeshRigEntity> RigEntities;
            [ReadOnly] public ArchetypeChunkBufferType<SkinnedMeshToRigIndex> SkinnedMeshToRigIndices;
            [ReadOnly] public ArchetypeChunkBufferType<BindPose> BindPoses;

            public ArchetypeChunkBufferType<SkinMatrix> SkinMatrices;
            public ArchetypeChunkComponentType<LocalToWorld> LocalToWorld;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigEntities = chunk.GetNativeArray(RigEntities);
                var skinnedMeshToRigIndices = chunk.GetBufferAccessor(SkinnedMeshToRigIndices);
                var bindPoses = chunk.GetBufferAccessor(BindPoses);
                var outSkinMatrices = chunk.GetBufferAccessor(SkinMatrices);
                var outLocalToWorld = chunk.GetNativeArray(LocalToWorld);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var rig = rigEntities[i].Value;
                    if (AnimatedLocalToRig.Exists(rig) && AnimatedLocalToWorld.Exists(rig))
                    {
                        var animatedLocalToRigMatrices = AnimatedLocalToRig[rig];
                        ComputeRenderingSkinMatrix(
                            skinnedMeshToRigIndices[i],
                            bindPoses[i],
                            animatedLocalToRigMatrices,
                            outSkinMatrices[i]
                            );

                        var animatedLocalToWorldMatrices = AnimatedLocalToWorld[rig];
                        outLocalToWorld[i] = new LocalToWorld { Value = animatedLocalToWorldMatrices[0].Value };
                    }
                }
            }

            static void ComputeRenderingSkinMatrix(
                [ReadOnly] DynamicBuffer<SkinnedMeshToRigIndex> skinMeshToRigIndices,
                [ReadOnly] DynamicBuffer<BindPose> bindPoses,
                [ReadOnly] DynamicBuffer<AnimatedLocalToRig> animatedLocalToRigMatrices,
                DynamicBuffer<SkinMatrix> outSkinMatrices
                )
            {
                for (int i = 0; i != outSkinMatrices.Length; ++i)
                {
                    var index = skinMeshToRigIndices[i].Value;
                    var skinMat = math.mul(animatedLocalToRigMatrices[index].Value, bindPoses[i].Value);

                    outSkinMatrices[i] = new SkinMatrix
                    {
                        Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz)
                    };
                }
            }
        }
    }
}
