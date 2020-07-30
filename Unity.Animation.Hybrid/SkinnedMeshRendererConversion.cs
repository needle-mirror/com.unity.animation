using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Deformations;

using UnityEngine;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion("Unity.Animation.Hybrid.SkinnedMeshRendererConversion", 5)]
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

        internal static void ValidateSkinnedMeshRendererRootBoneIsExposed(SkinnedMeshRenderer skinnedMeshRenderer, Transform rigComponent, Transform[] bones)
        {
            var smrRootBone = skinnedMeshRenderer.rootBone;
            if (smrRootBone == null)
                return;

            // Root is always exposed
            var idx = RigGenerator.FindTransformIndex(smrRootBone, bones);
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
                    idx = RigGenerator.FindTransformIndex(parent, bones);
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
                //var rigComponent = meshRenderer.GetComponentInParent<RigComponent>();
                var rigComponent = GetComponentInParent<RigComponent>(meshRenderer.gameObject);
#if UNITY_ENABLE_ANIMATION_ANIMATOR_CONVERSION
                var animatorComponent = GetComponentInParent<Animator>(meshRenderer.gameObject);
#else
                Animator animatorComponent = null;
#endif
                if (rigComponent == null && animatorComponent == null)
                {
                    return;
                }

                var rigEntity = rigComponent != null ? GetPrimaryEntity(rigComponent) : GetPrimaryEntity(animatorComponent);
                var skBones = rigComponent != null ? rigComponent.Bones : animatorComponent.ExtractBoneTransforms();
                using (var skinnedMeshToRigIndexMappings = new NativeList<SkinnedMeshToRigIndexMapping>(Allocator.Temp))
                {
                    var boneMatchCount = meshRenderer.ExtractMatchingBoneBindings(skBones, skinnedMeshToRigIndexMappings);
                    if (boneMatchCount > 0)
                    {
                        ValidateSkinnedMeshRendererRootBoneIsExposed(
                            meshRenderer,
                            rigComponent != null ? rigComponent.transform : animatorComponent.transform,
                            skBones
                        );

                        var entity = GetPrimaryEntity(meshRenderer);
                        var animatedSkinMatricesArray = DstEntityManager.AddBuffer<AnimatedLocalToRoot>(rigEntity);
                        animatedSkinMatricesArray.ResizeUninitialized(skBones.Length);

                        DstEntityManager.AddComponentData(entity, new RigEntity { Value = rigEntity });
                        DstEntityManager.AddBuffer<SkinnedMeshToRigIndexMapping>(entity);
                        DstEntityManager.AddBuffer<BindPose>(entity);

                        var smrRootBone = meshRenderer.rootBone != null ? meshRenderer.rootBone : meshRenderer.transform;
                        DstEntityManager.AddComponentData(entity, new SkinnedMeshRootEntity { Value = GetPrimaryEntity(smrRootBone) });

                        if (!DstEntityManager.HasComponent<Deformations.SkinMatrix>(entity))
                            DstEntityManager.AddBuffer<Deformations.SkinMatrix>(entity);

                        var skinMeshMappingArray = DstEntityManager.GetBuffer<SkinnedMeshToRigIndexMapping>(entity);
                        var bindPoseArray = DstEntityManager.GetBuffer<BindPose>(entity);
                        var skinMatrices = DstEntityManager.GetBuffer<Deformations.SkinMatrix>(entity);

                        skinMeshMappingArray.CopyFrom(skinnedMeshToRigIndexMappings.AsArray());

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
                        rigComponent != null ? rigComponent.transform : animatorComponent.transform,
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
