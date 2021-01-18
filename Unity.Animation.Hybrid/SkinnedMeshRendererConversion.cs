using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Deformations;

using UnityEngine;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion("simonbz", 6)]
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [UpdateAfter(typeof(RigConversion))]
    [UpdateAfter(typeof(RigAuthoringConversion))]
    public class SkinnedMeshRendererConversion : GameObjectConversionSystem
    {
        /// <summary>
        /// Returns true when all blendshapes weights are declared by the rig definition in the same order
        /// found on the SkinnedMeshRenderer.
        /// </summary>
        bool AreBlendShapeWeightsContiguous(Mesh mesh, NativeList<BlendShapeToRigIndexMapping> mappings)
        {
            if (mesh.blendShapeCount != mappings.Length)
                return false;

            var testIndices = mappings[0];
            for (int i = 1; i < mappings.Length; ++i)
            {
                testIndices.BlendShapeIndex++;
                testIndices.RigIndex++;

                if (mappings[i].RigIndex != testIndices.RigIndex ||
                    mappings[i].BlendShapeIndex != testIndices.BlendShapeIndex)
                    return false;
            }

            return true;
        }

        static int FindBoneIndex(Transform bone, IReadOnlyList<RigIndexToBone> bones)
        {
            int idx = -1;
            for (int i = 0; i < bones.Count; ++i)
            {
                if (bones[i].Bone == bone)
                {
                    idx = bones[i].Index;
                    break;
                }
            }

            return idx;
        }

        internal static void ValidateSkinnedMeshRendererRootBoneIsExposed(SkinnedMeshRenderer skinnedMeshRenderer, Transform rigComponent, IReadOnlyList<RigIndexToBone> bones)
        {
            var smrRootBone = skinnedMeshRenderer.rootBone;
            if (smrRootBone == null)
                return;

            // Root is always exposed
            int idx = FindBoneIndex(smrRootBone, bones);

            if (idx == 0)
                return;

            if (idx != -1)
            {
                // Warn user that this SMR is referencing a non exposed root to compute its render bounds
                if (!(smrRootBone.GetComponents(typeof(IWriteExposeTransform))?.Length > 0))
                {
                    UnityEngine.Debug.LogWarning(
                        $"SkinnedMeshRenderer [{skinnedMeshRenderer.name}] references a root bone [{smrRootBone.name}] that is not exposed in write which leads to errors when computing render bounds.",
                        smrRootBone
                    );
                }
            }
            else
            {
                // Check to see if any ancestor references a rig transform that should be exposed
                var parent = smrRootBone.parent;
                while (parent != null && parent != rigComponent)
                {
                    idx = FindBoneIndex(parent, bones);

                    if (idx != -1)
                    {
                        if (!(parent.GetComponents(typeof(IWriteExposeTransform))?.Length > 0))
                        {
                            UnityEngine.Debug.LogWarning(
                                $"SkinnedMeshRenderer [{skinnedMeshRenderer.name}] references a root bone [{smrRootBone.name}] parented to a transform [{parent}] that is not exposed in write which leads to errors when computing render bounds.",
                                parent
                            );
                        }
                        break;
                    }
                    else
                        parent = parent.parent;
                }
            }
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((SkinnedMeshRenderer meshRenderer) =>
            {
                // Would need to validate why Component.GetComponentInParent doesn't return the expected results
                //var rigComponent = meshRenderer.GetComponentInParent<IRigAuthoring>();
                var rigAuthoring = RigGenerator.GetComponentInParent<IRigAuthoring>(meshRenderer.gameObject);
                var rigAuthoringComponent = rigAuthoring as Component;
#if UNITY_ENABLE_ANIMATION_ANIMATOR_CONVERSION
                var animatorComponent = GetComponentInParent<Animator>(meshRenderer.gameObject);
#else
                Animator animatorComponent = null;
#endif
                if (rigAuthoring == null && animatorComponent == null)
                {
                    return;
                }

                var rigEntity = rigAuthoringComponent != null ? GetPrimaryEntity(rigAuthoringComponent) : GetPrimaryEntity(animatorComponent);

                var bones = new List<RigIndexToBone>();
                if (rigAuthoring != null)
                    rigAuthoring.GetBones(bones);
                else
                    animatorComponent.ExtractBoneTransforms(bones);

                if (bones == null || bones.Count == 0)
                {
                    return;
                }

                using (var smrMappings = new NativeList<SkinnedMeshToRigIndexMapping>(Allocator.Temp))
                using (var smrIndirectMappings = new NativeList<SkinnedMeshToRigIndexIndirectMapping>(Allocator.Temp))
                {
                    var boneMatchCount = meshRenderer.ExtractMatchingBoneBindings(
                        bones,
                        smrMappings,
                        smrIndirectMappings
                    );

                    if (boneMatchCount > 0)
                    {
                        ValidateSkinnedMeshRendererRootBoneIsExposed(
                            meshRenderer,
                            rigAuthoringComponent != null ? rigAuthoringComponent.transform : animatorComponent.transform,
                            bones
                        );

                        var entity = GetPrimaryEntity(meshRenderer);
                        var animatedSkinMatricesArray = DstEntityManager.AddBuffer<AnimatedLocalToRoot>(rigEntity);
                        animatedSkinMatricesArray.ResizeUninitialized(bones.Count);

                        DstEntityManager.AddComponentData(entity, new RigEntity { Value = rigEntity });
                        DstEntityManager.AddBuffer<BindPose>(entity);

                        var smrMappingBuffer = DstEntityManager.AddBuffer<SkinnedMeshToRigIndexMapping>(entity);
                        smrMappingBuffer.CopyFrom(smrMappings);

                        var smrIndirectMappingBuffer = DstEntityManager.AddBuffer<SkinnedMeshToRigIndexIndirectMapping>(entity);
                        smrIndirectMappingBuffer.CopyFrom(smrIndirectMappings);

                        var smrRootBone = meshRenderer.rootBone != null ? meshRenderer.rootBone : meshRenderer.transform;
                        DstEntityManager.AddComponentData(entity, new SkinnedMeshRootEntity { Value = GetPrimaryEntity(smrRootBone) });

                        if (!DstEntityManager.HasComponent<Deformations.SkinMatrix>(entity))
                            DstEntityManager.AddBuffer<Deformations.SkinMatrix>(entity);

                        var bindPoseArray = DstEntityManager.GetBuffer<BindPose>(entity);
                        var skinMatrices = DstEntityManager.GetBuffer<Deformations.SkinMatrix>(entity);

                        var skinBones = meshRenderer.bones;
                        bindPoseArray.ResizeUninitialized(skinBones.Length);
                        skinMatrices.ResizeUninitialized(skinBones.Length);

                        var invRoot = smrRootBone.localToWorldMatrix.inverse;
                        for (int i = 0; i != skinBones.Length; ++i)
                        {
                            var bindPose = meshRenderer.sharedMesh.bindposes[i];
                            bindPoseArray[i] = new BindPose { Value = bindPose };

                            var skinMat = math.mul(math.mul(invRoot, meshRenderer.bones[i].localToWorldMatrix), bindPose);
                            skinMatrices[i] = new Deformations.SkinMatrix
                            {
                                Value = new float3x4(skinMat.c0.xyz, skinMat.c1.xyz, skinMat.c2.xyz, skinMat.c3.xyz)
                            };
                        }
                    }
                }

                using (var blendShapeToRigIndexMappings = new NativeList<BlendShapeToRigIndexMapping>(Allocator.Temp))
                {
                    var matchCount = meshRenderer.ExtractMatchingBlendShapeBindings(
                        rigAuthoringComponent != null ? rigAuthoringComponent.transform : animatorComponent.transform,
                        DstEntityManager.GetComponentData<Rig>(rigEntity),
                        blendShapeToRigIndexMappings
                    );

                    if (matchCount > 0)
                    {
#if !ENABLE_COMPUTE_DEFORMATIONS
                        UnityEngine.Debug.LogError("DOTS SkinnedMeshRenderer blendshapes are only supported via compute shaders in hybrid renderer. Make sure to add 'ENABLE_COMPUTE_DEFORMATIONS' to your scripting defines in Player settings.");
#endif

                        var sharedMesh = meshRenderer.sharedMesh;
                        var entity = GetPrimaryEntity(meshRenderer);

                        if (!DstEntityManager.HasComponent<RigEntity>(entity))
                            DstEntityManager.AddComponentData(entity, new RigEntity { Value = rigEntity });

                        if (!DstEntityManager.HasComponent<Deformations.BlendShapeWeight>(entity))
                            DstEntityManager.AddBuffer<Deformations.BlendShapeWeight>(entity);

                        var blendShapeWeights = DstEntityManager.GetBuffer<Deformations.BlendShapeWeight>(entity);
                        blendShapeWeights.ResizeUninitialized(sharedMesh.blendShapeCount);
                        for (int i = 0, count = sharedMesh.blendShapeCount; i < count; ++i)
                        {
                            blendShapeWeights[i] = new Deformations.BlendShapeWeight
                            {
                                Value = meshRenderer.GetBlendShapeWeight(i)
                            };
                        }

                        if (AreBlendShapeWeightsContiguous(sharedMesh, blendShapeToRigIndexMappings))
                        {
                            DstEntityManager.AddComponentData(
                                entity,
                                new BlendShapeChunkMapping { RigIndex = blendShapeToRigIndexMappings[0].RigIndex, Size = blendShapeToRigIndexMappings.Length }
                            );
                        }
                        else
                        {
                            DstEntityManager
                                .AddBuffer<BlendShapeToRigIndexMapping>(entity)
                                .CopyFrom(blendShapeToRigIndexMappings.AsArray());
                        }
                    }
                }
            });
        }
    }
}
