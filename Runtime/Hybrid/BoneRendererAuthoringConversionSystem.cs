using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    [UpdateAfter(typeof(RigConversion))]
    public class BoneRendererAuthoringConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((BoneRendererAuthoring boneRenderer) =>
            {
                if(boneRenderer.RigComponent == null)
                {
                    Debug.LogWarning( $"BoneRendererAuthoring component on '{boneRenderer.gameObject.name}' has a null RigComponent");
                }
                else
                {
                    var rigEntity = GetPrimaryEntity(boneRenderer.RigComponent);
                    if (boneRenderer.RenderBones && DstEntityManager.HasComponent<SharedRigDefinition>(rigEntity))
                    {
                        var transformIndices = new NativeList<int>(boneRenderer.Transforms.Length, Allocator.Temp);
                        for (int i = 0; i < boneRenderer.Transforms.Length; ++i)
                        {
                            int idx = RigGenerator.FindTransformIndex(boneRenderer.Transforms[i], boneRenderer.RigComponent.Bones);
                            if (idx != -1)
                                transformIndices.Add(idx);
                        }

                        var props = new BoneRendererProperties
                        {
                            BoneShape = boneRenderer.BoneShape,
                            Color = Convert(boneRenderer.Color),
                            Size = boneRenderer.Size
                        };

                        var sharedRigDefinition = DstEntityManager.GetSharedComponentData<SharedRigDefinition>(rigEntity);

                        BoneRendererEntityBuilder.SetupBoneRendererEntities(
                            rigEntity,
                            CreateAdditionalEntity(boneRenderer),
                            CreateAdditionalEntity(boneRenderer),
                            DstEntityManager,
                            sharedRigDefinition.Value,
                            in props,
                            transformIndices
                            );

                        transformIndices.Dispose();
                    }
                }
            });
        }

        static float4 Convert(Color color) => new float4(color.r, color.g, color.b, color.a);
    }
}
