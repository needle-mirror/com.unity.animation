using Unity.Animation.BoneRenderer;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public abstract class ComputeBoneRenderingMatricesBase : SystemBase
    {
        protected override void OnUpdate()
        {
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
        }
    }
}
