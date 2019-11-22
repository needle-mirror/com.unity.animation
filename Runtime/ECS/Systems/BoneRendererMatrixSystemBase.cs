using Unity.Animation.BoneRenderer;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;

namespace Unity.Animation
{
    public abstract class BoneRendererMatrixSystemBase : JobComponentSystem
    {
        static readonly ProfilerMarker k_Marker = new ProfilerMarker("BoneRendererMatrixSystemBase");

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            k_Marker.Begin();

            inputDeps = new ComputeBoneMatricesJob {
                GlobalMatrices = GetBufferFromEntity<AnimatedLocalToWorld>(true)
            }.Schedule(this, inputDeps);

            k_Marker.End();
            return inputDeps;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeBoneMatricesJob : IJobForEachWithEntity_EBBBCC<
            BoneWorldMatrix,
            RigIndex,
            RigParentIndex,
            RigEntity,
            BoneSize
            >
        {
            [ReadOnly] public BufferFromEntity<AnimatedLocalToWorld> GlobalMatrices;

            public void Execute(
                Entity entity,
                int index,
                DynamicBuffer<BoneWorldMatrix> boneWorldMatrices,
                [ReadOnly] DynamicBuffer<RigIndex> rigIndices,
                [ReadOnly] DynamicBuffer<RigParentIndex> rigParentIndices,
                [ReadOnly] ref RigEntity rigEntity,
                [ReadOnly] ref BoneSize boneSize
                )
            {
                if (GlobalMatrices.Exists(rigEntity.Value))
                {
                    var rigTransforms = GlobalMatrices[rigEntity.Value].Reinterpret<float4x4>();

                    for (int i = 0; i != rigIndices.Length; ++i)
                    {
                        var start = rigTransforms[rigParentIndices[i].Value].c3.xyz;
                        var end = rigTransforms[rigIndices[i].Value].c3.xyz;
                        boneWorldMatrices[i] = new BoneWorldMatrix { Value = BoneRendererUtils.ComputeBoneMatrix(start, end, boneSize.Value) };
                    }
                }
            }
        }
    }
}
