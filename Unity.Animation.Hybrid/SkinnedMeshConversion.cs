using System;

using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Deformations;

namespace Unity.Animation.Hybrid
{
    [Obsolete("SkinnedMeshConversion has been deprecated. (RemovedAfter 2020-06-24)")]
    [ConverterVersion("Unity.Animation.Hybrid.SkinnedMeshConversion", 2)]
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

                DstEntityManager.AddComponentData(entity, new RigEntity { Value = rigEntity });
                DstEntityManager.AddComponentData(entity, new LocalToWorld());
#if !UNITY_ENTITIES_0_12_OR_NEWER
                DstEntityManager.AddComponent<BoneIndexOffset>(entity);
#endif

                DstEntityManager.AddBuffer<SkinnedMeshToRigIndexMapping>(entity);
                DstEntityManager.AddBuffer<BindPose>(entity);
                DstEntityManager.AddBuffer<Deformations.SkinMatrix>(entity);

                var skinMeshIndexArray = DstEntityManager.GetBuffer<SkinnedMeshToRigIndexMapping>(entity);
                var bindPoseArray = DstEntityManager.GetBuffer<BindPose>(entity);
                var skinMatrices = DstEntityManager.GetBuffer<Deformations.SkinMatrix>(entity);

                var smBones = meshRenderer.SkinnedMeshRenderer.bones;
                var skBones = meshRenderer.Rig.Bones;
                skinMeshIndexArray.ResizeUninitialized(CountBoneMatches(skBones, smBones, meshRenderer.ToString()));
                bindPoseArray.ResizeUninitialized(smBones.Length);
                skinMatrices.ResizeUninitialized(smBones.Length);

                int mappingIndex = 0;
                for (int rigIndex = 0; rigIndex != skBones.Length; ++rigIndex)
                {
                    var remap = new SkinnedMeshToRigIndexMapping { SkinMeshIndex = -1, RigIndex = -1 };
                    for (int skinMeshIndex = 0; skinMeshIndex != smBones.Length; ++skinMeshIndex)
                    {
                        if (skBones[rigIndex] == smBones[skinMeshIndex])
                        {
                            remap.SkinMeshIndex = skinMeshIndex;
                            remap.RigIndex = rigIndex;
                            break;
                        }
                    }
                    if (remap.SkinMeshIndex == -1 || remap.RigIndex == -1)
                    {
                        continue;
                    }
                    skinMeshIndexArray[mappingIndex] = remap;
                    mappingIndex++;
                }
                for (int j = 0; j != smBones.Length; ++j)
                {
                    var bindPose = meshRenderer.SkinnedMeshRenderer.sharedMesh.bindposes[j];
                    bindPoseArray[j] = new BindPose { Value = bindPose };

                    var skinMat = math.mul(meshRenderer.SkinnedMeshRenderer.bones[j].localToWorldMatrix, bindPose);
                    skinMatrices[j] = new Deformations.SkinMatrix { Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz) };
                }
            });
        }

        internal static int CountBoneMatches(Transform[] rigBones, Transform[] skinMeshBones, string componentName)
        {
            var matchCount = 0;
            for (int j = 0; j != skinMeshBones.Length; ++j)
            {
                var currentBoneMatch = false;
                for (int k = 0; !currentBoneMatch && k != rigBones.Length; ++k)
                {
                    currentBoneMatch = skinMeshBones[j] == rigBones[k];
                }
                if (!currentBoneMatch)
                {
                    UnityEngine.Debug.LogWarning($"{componentName} references bone '{skinMeshBones[j].name}' that cannot be found.");
                }
                else
                {
                    matchCount += 1;
                }
            }
            return matchCount;
        }
    }
}
