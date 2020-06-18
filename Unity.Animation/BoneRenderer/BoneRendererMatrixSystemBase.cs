using Unity.Animation.BoneRenderer;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    public abstract class BoneRendererMatrixSystemBase : SystemBase
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_Marker = new ProfilerMarker("BoneRendererMatrixSystemBase");
#endif

        protected override void OnUpdate()
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_Marker.Begin();
#endif

            var LocalToWorldMatricesFromEntity = GetBufferFromEntity<AnimatedLocalToWorld>(true);
            Dependency = Entities
                .WithReadOnly(LocalToWorldMatricesFromEntity)
                .ForEach((Entity e, DynamicBuffer<BoneWorldMatrix> boneWorldMatrices, DynamicBuffer<RigIndex> rigIndices, DynamicBuffer<RigParentIndex> rigParentIndices, in RigEntity rigEntity, in BoneSize boneSize) =>
                {
                    if (LocalToWorldMatricesFromEntity.HasComponent(rigEntity.Value))
                    {
                        var rigTransforms = LocalToWorldMatricesFromEntity[rigEntity.Value].Reinterpret<float4x4>();

                        for (int i = 0; i != rigIndices.Length; ++i)
                        {
                            var start = rigTransforms[rigParentIndices[i].Value].c3.xyz;
                            var end = rigTransforms[rigIndices[i].Value].c3.xyz;
                            boneWorldMatrices[i] = new BoneWorldMatrix { Value = BoneRendererUtils.ComputeBoneMatrix(start, end, boneSize.Value) };
                        }
                    }
                }).ScheduleParallel(Dependency);

#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_Marker.End();
#endif
        }
    }
}
