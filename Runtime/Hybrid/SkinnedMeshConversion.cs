using UnityEngine;
using Unity.Mathematics;

namespace Unity.Animation.Hybrid
{
    // Converts the GameObject SkinnedMeshRenderer into SkinRenderer, SkinMatrix, SkinTransformBinding
    public class SkinnedMeshConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((SkinnedMesh meshRenderer) =>
            {
                var srcMaterials = meshRenderer.SkinnedMeshRenderer.sharedMaterials;

                var entity = GetPrimaryEntity(meshRenderer);

                //@TODO: DIRTY DIRTy hack until we get a proper integration into culling code
                meshRenderer.SkinnedMeshRenderer.sharedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);

                // Setup basic SkinRenderer data
                var skin = new SkinRenderer();
                skin.Material0 = srcMaterials.Length >= 1 ? srcMaterials[0] : null;
                skin.Material1 = srcMaterials.Length >= 2 ? srcMaterials[1] : null;
                skin.Mesh = meshRenderer.SkinnedMeshRenderer.sharedMesh;
                skin.Layer = meshRenderer.gameObject.layer;
                skin.CastShadows = meshRenderer.SkinnedMeshRenderer.shadowCastingMode;
                skin.ReceiveShadows = meshRenderer.SkinnedMeshRenderer.receiveShadows;
                DstEntityManager.AddSharedComponentData(entity, skin);

                DstEntityManager.AddComponentData(entity, new SkinnedMeshComponentData { RigEntity = GetPrimaryEntity(meshRenderer.Skeleton) });
                DstEntityManager.AddBuffer<SkinnedMeshToSkeletonBone>(entity);
                DstEntityManager.AddBuffer<BindPose>(entity);
                DstEntityManager.AddBuffer<SkinMatrix>(entity);

                var skeletonIndexArray = DstEntityManager.GetBuffer<SkinnedMeshToSkeletonBone>(entity);
                var bindPoseArray = DstEntityManager.GetBuffer<BindPose>(entity);
                var skinMatrices = DstEntityManager.GetBuffer<SkinMatrix>(entity);

                var smBones = meshRenderer.SkinnedMeshRenderer.bones;
                skeletonIndexArray.ResizeUninitialized(smBones.Length);
                bindPoseArray.ResizeUninitialized(smBones.Length);
                skinMatrices.ResizeUninitialized(smBones.Length);

                var skBones = meshRenderer.Skeleton.Bones;
                for (int j = 0; j != smBones.Length; ++j)
                {
                    var remap = new SkinnedMeshToSkeletonBone { Value = -1 };
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
