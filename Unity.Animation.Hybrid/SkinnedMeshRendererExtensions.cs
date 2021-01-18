using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using System;
using System.Collections.Generic;

namespace Unity.Animation.Hybrid
{
    internal static class SkinnedMeshRendererExtensions
    {
        static readonly string k_BlendShapeBindingPrefix = "blendShape.";

        internal static int ExtractMatchingBoneBindings(
            this SkinnedMeshRenderer skinnedMeshRenderer,
            List<RigIndexToBone> skeletonBones,
            NativeList<SkinnedMeshToRigIndexMapping> outSMRMappings,
            NativeList<SkinnedMeshToRigIndexIndirectMapping> outSMRIndirectMappings
        )
        {
            outSMRMappings.Clear();
            outSMRIndirectMappings.Clear();

            if (skinnedMeshRenderer == null)
                throw new ArgumentNullException("Invalid SkinnedMeshRenderer.");
            if (skeletonBones == null)
                throw new ArgumentNullException($"Invalid ${nameof(skeletonBones)}.");
            if (!outSMRMappings.IsCreated)
                throw new ArgumentException($"Invalid ${nameof(outSMRMappings)}");
            if (!outSMRIndirectMappings.IsCreated)
                throw new ArgumentException($"Invalid ${nameof(outSMRIndirectMappings)}");

            var skinBones = skinnedMeshRenderer.bones;
            if (skinBones == null)
                return 0;

            int matchCount = 0;
            using (var skeletonMap = new NativeHashMap<int, int>(skeletonBones.Count, Allocator.Temp))
            {
                for (int i = 0; i < skeletonBones.Count; ++i)
                {
                    if (skeletonBones[i].Bone != null)
                        skeletonMap.Add(skeletonBones[i].Bone.GetInstanceID(), skeletonBones[i].Index);
                }

                int boneIdx;
                for (int i = 0; i < skinBones.Length; ++i)
                {
                    var smrBone = skinBones[i];
                    if (skeletonMap.TryGetValue(smrBone.GetInstanceID(), out boneIdx))
                    {
                        outSMRMappings.Add(new SkinnedMeshToRigIndexMapping
                        {
                            SkinMeshIndex = i,
                            RigIndex = boneIdx
                        });

                        matchCount++;
                    }
                    else
                    {
                        // Immediate SMR to skeleton mapping not found, walk the hierarchy to find possible parent
                        // and compute static offset
                        var parent = smrBone.parent;
                        while (parent != null)
                        {
                            if (skeletonMap.TryGetValue(parent.GetInstanceID(), out boneIdx))
                            {
                                outSMRIndirectMappings.Add(new SkinnedMeshToRigIndexIndirectMapping
                                {
                                    Offset = mathex.AffineTransform(parent.worldToLocalMatrix * smrBone.localToWorldMatrix),
                                    RigIndex = boneIdx,
                                    SkinMeshIndex = i
                                });

                                matchCount++;
                                break;
                            }
                            else
                                parent = parent.parent;
                        }

                        if (parent == null)
                            Debug.LogWarning($"{skinnedMeshRenderer.ToString()} references bone '{skinBones[i].name}' that cannot be found.");
                    }
                }
            }

            return matchCount;
        }

        internal static int ExtractMatchingBlendShapeBindings(
            this SkinnedMeshRenderer skinnedMeshRenderer,
            Transform root,
            BlobAssetReference<RigDefinition> rigDefinition,
            NativeList<BlendShapeToRigIndexMapping> outBlendShapeToRigIndexMapping
        )
        {
            if (skinnedMeshRenderer == null)
                throw new ArgumentNullException("Invalid SkinnedMeshRenderer.");
            if (root == null)
                throw new ArgumentNullException($"Invalid root transform {nameof(root)}");
            if (!rigDefinition.IsCreated)
                throw new ArgumentNullException($"Invalid {nameof(rigDefinition)}");
            if (!outBlendShapeToRigIndexMapping.IsCreated)
                throw new ArgumentNullException($"Invalid {nameof(outBlendShapeToRigIndexMapping)}");

            var sharedMesh = skinnedMeshRenderer.sharedMesh;
            if (sharedMesh == null)
                throw new ArgumentNullException("SkinnedMeshRenderer contains a null SharedMesh.");

            int count = sharedMesh.blendShapeCount;
            if (count == 0)
                return 0;

            var relativePath = RigGenerator.ComputeRelativePath(skinnedMeshRenderer.transform, root);

            int matchCount = 0;
            ref var rigFloatBindings = ref rigDefinition.Value.Bindings.FloatBindings;
            for (int i = 0; i < count; ++i)
            {
                var id = new GenericBindingID
                {
                    AttributeName = $"{k_BlendShapeBindingPrefix}{sharedMesh.GetBlendShapeName(i)}",
                    ComponentType = typeof(SkinnedMeshRenderer),
                    Path = relativePath
                };

                int idx = Core.FindBindingIndex(ref rigFloatBindings, BindingHashGlobals.DefaultHashGenerator.ToHash(id));
                if (idx != -1)
                {
                    outBlendShapeToRigIndexMapping.Add(new BlendShapeToRigIndexMapping
                    {
                        BlendShapeIndex = i,
                        RigIndex = idx
                    });

                    matchCount++;
                }
            }

            return matchCount;
        }
    }
}
