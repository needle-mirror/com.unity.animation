using System;

using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;

using UnityEngine;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion("Unity.Animation.Hybrid.SkinnedMeshRendererConversion", 2)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    public class SkinnedMeshRendererConversion : GameObjectConversionSystem
    {
        T GetComponentInParent<T>(GameObject gameObject) where T : Component
        {
            T queryComponent = null;

            for (var transform = gameObject.transform; queryComponent == null && transform != null; transform = transform.parent)
            {
                transform.TryGetComponent<T>(out queryComponent);
            }

            return queryComponent;
        }

        protected override void OnUpdate()
        {
#pragma warning disable 0618
            Entities.WithNone<SkinnedMesh>().ForEach((SkinnedMeshRenderer meshRenderer) =>
            {
                // Would need to validate why Component.GetComponentInParent doesn't return the expected results
                //var rigComponent = meshRenderer.GetComponentInParent<RigComponent>();
                var rigComponent = GetComponentInParent<RigComponent>(meshRenderer.gameObject);
                if (rigComponent == null)
                    return;

                var hasMatch = false;
                var smBones = meshRenderer.bones;
                var skBones = rigComponent.Bones;
                for (int j = 0; !hasMatch && j != smBones.Length; ++j)
                {
                    for (int k = 0; !hasMatch && k != skBones.Length; ++k)
                    {
                        hasMatch = smBones[j] == skBones[k];
                    }
                }

                if (hasMatch)
                {
                    var entity = GetPrimaryEntity(meshRenderer);
                    var rigEntity = GetPrimaryEntity(rigComponent);
                    var animatedSkinMatricesArray = DstEntityManager.AddBuffer<AnimatedLocalToRoot>(rigEntity);
                    animatedSkinMatricesArray.ResizeUninitialized(rigComponent.Bones.Length);

                    DstEntityManager.AddComponentData(entity, new SkinnedMeshRigEntity { Value = rigEntity });
                    DstEntityManager.AddComponentData(entity, new LocalToWorld());
                    DstEntityManager.AddComponentData(entity, new BoneIndexOffset());

                    DstEntityManager.AddBuffer<SkinnedMeshToRigIndex>(entity);
                    DstEntityManager.AddBuffer<BindPose>(entity);
                    DstEntityManager.AddBuffer<SkinMatrix>(entity);

                    var skeletonIndexArray = DstEntityManager.GetBuffer<SkinnedMeshToRigIndex>(entity);
                    var bindPoseArray = DstEntityManager.GetBuffer<BindPose>(entity);
                    var skinMatrices = DstEntityManager.GetBuffer<SkinMatrix>(entity);

                    skeletonIndexArray.ResizeUninitialized(smBones.Length);
                    bindPoseArray.ResizeUninitialized(smBones.Length);
                    skinMatrices.ResizeUninitialized(smBones.Length);

                    for (int j = 0; j != smBones.Length; ++j)
                    {
                        var remap = new SkinnedMeshToRigIndex { Value = -1 };
                        for (int k = 0; k != skBones.Length; ++k)
                        {
                            if (smBones[j] == skBones[k])
                            {
                                remap.Value = k;
                                break;
                            }
                        }
                        skeletonIndexArray[j] = remap;

                        var bindPose = meshRenderer.sharedMesh.bindposes[j];
                        bindPoseArray[j] = new BindPose { Value = bindPose };

                        var skinMat = math.mul(meshRenderer.bones[j].localToWorldMatrix, bindPose);
                        skinMatrices[j] = new SkinMatrix { Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz) };
                    }
                }
            });
#pragma warning restore 0618
        }
    }
}
