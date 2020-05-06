using System;

using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;

namespace Unity.Animation.Hybrid
{
    [Obsolete("SkinnedMeshConversion has been deprecated. (RemovedAfter 2020-06-24)")]
    [ConverterVersion("Unity.Animation.Hybrid.SkinnedMeshConversion", 1)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    public class SkinnedMeshConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((SkinnedMesh meshRenderer) =>
            {
                var entity = GetPrimaryEntity(meshRenderer.SkinnedMeshRenderer);
                Entity rigEntity = GetPrimaryEntity(meshRenderer.Rig);
                var animatedSkinMatricesArray = DstEntityManager.AddBuffer<AnimatedLocalToRoot>(rigEntity);
                animatedSkinMatricesArray.ResizeUninitialized(meshRenderer.Rig.Bones.Length);

                DstEntityManager.AddComponentData(entity, new SkinnedMeshRigEntity { Value = rigEntity });
                DstEntityManager.AddComponentData(entity, new LocalToWorld());
                DstEntityManager.AddComponentData(entity, new BoneIndexOffset());

                DstEntityManager.AddBuffer<SkinnedMeshToRigIndex>(entity);
                DstEntityManager.AddBuffer<BindPose>(entity);
                DstEntityManager.AddBuffer<SkinMatrix>(entity);

                var skeletonIndexArray = DstEntityManager.GetBuffer<SkinnedMeshToRigIndex>(entity);
                var bindPoseArray = DstEntityManager.GetBuffer<BindPose>(entity);
                var skinMatrices = DstEntityManager.GetBuffer<SkinMatrix>(entity);

                var smBones = meshRenderer.SkinnedMeshRenderer.bones;
                skeletonIndexArray.ResizeUninitialized(smBones.Length);
                bindPoseArray.ResizeUninitialized(smBones.Length);
                skinMatrices.ResizeUninitialized(smBones.Length);

                var skBones = meshRenderer.Rig.Bones;
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

                    var bindPose = meshRenderer.SkinnedMeshRenderer.sharedMesh.bindposes[j];
                    bindPoseArray[j] = new BindPose { Value = bindPose };

                    var skinMat = math.mul(meshRenderer.SkinnedMeshRenderer.bones[j].localToWorldMatrix, bindPose);
                    skinMatrices[j] = new SkinMatrix { Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz) };
                }
            });
        }
    }
}
