using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion("simonbz", 2)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [UpdateAfter(typeof(RigConversion))]
    [UpdateAfter(typeof(RigAuthoringConversion))]
    public class BoneRendererConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((BoneRendererComponent boneRenderer) =>
            {
                var rigAuthoring = RigGenerator.GetComponentInParent<IRigAuthoring>(boneRenderer.gameObject);
                if (rigAuthoring == null)
                {
                    Debug.LogWarning($"BoneRendererComponent on '{boneRenderer.gameObject.name}' has a null RigComponent");
                }
                else
                {
                    var rigAuthoringComponent = rigAuthoring as Component;
                    var rigEntity = GetPrimaryEntity(rigAuthoringComponent);
                    if (boneRenderer.RenderBones && DstEntityManager.HasComponent<Rig>(rigEntity))
                    {
                        var bones = new List<RigIndexToBone>();
                        rigAuthoring.GetBones(bones);

                        var transformIndices = new NativeList<int>(boneRenderer.Transforms.Length, Allocator.Temp);

                        for (int i = 0; i < bones.Count; ++i)
                        {
                            if (bones[i].Bone == null)
                                continue;

                            if (RigGenerator.FindTransformIndex(bones[i].Bone, boneRenderer.Transforms) != -1)
                                transformIndices.Add(bones[i].Index);
                        }

                        if (!transformIndices.IsEmpty)
                        {
                            var props = new BoneRendererProperties
                            {
                                BoneShape = boneRenderer.BoneShape,
                                Color = Convert(boneRenderer.Color),
                                Size = boneRenderer.Size
                            };

                            var rigDefinition = DstEntityManager.GetComponentData<Rig>(rigEntity);

                            BoneRendererEntityBuilder.SetupBoneRendererEntities(
                                rigEntity,
                                CreateAdditionalEntity(boneRenderer),
                                CreateAdditionalEntity(boneRenderer),
                                DstEntityManager,
                                rigDefinition.Value,
                                in props,
                                transformIndices
                            );
                        }

                        transformIndices.Dispose();
                    }
                }
            });
        }

        static float4 Convert(Color color) => new float4(color.r, color.g, color.b, color.a);
    }
}
