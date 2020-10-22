using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using System;

namespace Unity.Animation.Hybrid
{
    internal static class SkinnedMeshRendererExtensions
    {
        static readonly string k_BlendShapeBindingPrefix = "blendShape.";

        internal static int ExtractMatchingBoneBindings(
            this SkinnedMeshRenderer skinnedMeshRenderer,
            Transform[] skeletonBones,
            NativeList<SkinnedMeshToRigIndexMapping> outSkinnedMeshToRigIndexMappings
        )
        {
            if (skinnedMeshRenderer == null)
                throw new ArgumentNullException("Invalid SkinnedMeshRenderer.");
            if (skeletonBones == null)
                throw new ArgumentNullException($"Invalid ${nameof(skeletonBones)}.");
            if (!outSkinnedMeshToRigIndexMappings.IsCreated)
                throw new ArgumentNullException($"Invalid ${nameof(outSkinnedMeshToRigIndexMappings)}");

            var skinBones = skinnedMeshRenderer.bones;
            if (skinBones == null)
                return 0;

            var matchCount = 0;
            for (int i = 0; i != skinBones.Length; ++i)
            {
                int j = 0;
                for (; j != skeletonBones.Length; ++j)
                {
                    if (skinBones[i] == skeletonBones[j])
                    {
                        outSkinnedMeshToRigIndexMappings.Add(new SkinnedMeshToRigIndexMapping
                        {
                            SkinMeshIndex = i,
                            RigIndex = j
                        });

                        matchCount++;
                        break;
                    }
                }

                if (j == skeletonBones.Length)
                {
                    Debug.LogWarning($"{skinnedMeshRenderer.ToString()} references bone '{skinBones[i].name}' that cannot be found.");
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
                int idx = Core.FindBindingIndex(
                    ref rigFloatBindings,
                    BindingHashUtils.BuildPath(relativePath, k_BlendShapeBindingPrefix + sharedMesh.GetBlendShapeName(i))
                );

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
