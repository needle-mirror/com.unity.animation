using System;
using System.Collections.Generic;
using Unity.Animation.Hybrid;
using UnityEngine;

namespace Unity.Animation.Authoring
{
    /// <summary>
    /// Extension methods for Skeleton
    /// </summary>
    static class SkeletonExtensions
    {
        static readonly string k_BlendShapeBindingPrefix = "blendShape.";

        /// <summary>
        /// Builds the Skeleton asset using information found in the GameObject hierarchy.
        /// </summary>
        /// <param name="skeleton">The Skeleton asset.</param>
        /// <param name="gameObjectHierarchy">The GameObject hierarchy to build from.</param>
        internal static void PopulateFromGameObjectHierarchy(this Skeleton skeleton, GameObject gameObjectHierarchy)
        {
            skeleton.Clear();
            skeleton.AddTransformsToSkeleton(gameObjectHierarchy);
            skeleton.AddBlendShapesToSkeleton(gameObjectHierarchy);
        }

        /// <summary>
        /// Appends transform channels to the Skeleton asset.
        /// </summary>
        /// <param name="skeleton">The Skeleton asset.</param>
        /// <param name="gameObjectHierarchy">The GameObject hierarchy to build from.</param>
        internal static void AddTransformsToSkeleton(this Skeleton skeleton, GameObject gameObjectHierarchy)
        {
            var transforms = new List<Transform>();
            gameObjectHierarchy.transform.GetComponentsInChildren(true, transforms);

            skeleton.AddTransformsToSkeleton(transforms, gameObjectHierarchy.transform);
        }

        /// <summary>
        /// Appends transform channels to the Skeleton asset.
        /// </summary>
        /// <param name="skeleton">The Skeleton asset.</param>
        /// <param name="transforms">List of transforms to add to the Skeleton.</param>
        /// <param name="rootTransform">Root transform of the hierarchy.</param>
        internal static void AddTransformsToSkeleton(this Skeleton skeleton, IReadOnlyList<Transform> transforms, Transform rootTransform)
        {
            for (int i = 0; i < transforms.Count; ++i)
            {
                var transform = transforms[i];
                var path = RigGenerator.ComputeRelativePath(transform, rootTransform);
                var id = new TransformBindingID {Path = path};

                skeleton[id] = new TransformChannelProperties
                {
                    DefaultTranslationValue = transform.localPosition,
                    DefaultRotationValue = transform.localRotation,
                    DefaultScaleValue = transform.localScale
                };
            }
        }

        /// <summary>
        /// Appends blend shapes to the Skeleton asset.
        /// </summary>
        /// <param name="skeleton">The Skeleton asset.</param>
        /// <param name="gameObjectHierarchy">The GameObject hierarchy to build from.</param>
        internal static void AddBlendShapesToSkeleton(this Skeleton skeleton, GameObject gameObjectHierarchy)
        {
            var skinnedMeshRenderers = gameObjectHierarchy.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in skinnedMeshRenderers)
            {
                var sharedMesh = smr.sharedMesh;
                if (sharedMesh == null)
                    throw new ArgumentNullException("SkinnedMeshRenderer contains a null SharedMesh.");

                int count = sharedMesh.blendShapeCount;
                if (count > 0)
                {
                    var transform = smr.transform;
                    var path = RigGenerator.ComputeRelativePath(transform, gameObjectHierarchy.transform);

                    for (int i = 0; i < count; ++i)
                    {
                        var id = new GenericBindingID
                        {
                            Path = path,
                            AttributeName = $"{k_BlendShapeBindingPrefix}{sharedMesh.GetBlendShapeName(i)}",
                            ComponentType = typeof(SkinnedMeshRenderer)
                        };

                        skeleton.AddOrSetGenericProperty(id, new GenericPropertyVariant {Float = smr.GetBlendShapeWeight(i)});
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a common namespace in the Skeleton hierarchy.
        /// </summary>
        /// <param name="skeleton">Skeleton</param>
        /// <returns>Common namespace, or empty string if none can be found.</returns>
        internal static string ExtractNamespace(this Skeleton skeleton)
        {
            var allTransforms = new List<TransformChannel>();
            skeleton.GetAllTransforms(allTransforms);

            if (allTransforms.Count < 2)
                return String.Empty;

            // Root channel name should not be considered in calculation.
            var firstChannelID = allTransforms[1].ID;
            var firstChannelName = firstChannelID.Name;

            bool foundMismatch = false;
            int prefixIndex = 0;
            while (prefixIndex < firstChannelName.Length)
            {
                for (int i = 2; i < allTransforms.Count; ++i)
                {
                    var channelName = allTransforms[i].ID.Name;
                    if (prefixIndex >= channelName.Length)
                    {
                        foundMismatch = true;
                        break;
                    }

                    if (channelName[prefixIndex] != firstChannelName[prefixIndex])
                    {
                        foundMismatch = true;
                        break;
                    }
                }

                if (foundMismatch)
                    break;

                ++prefixIndex;
            }

            if (prefixIndex == 0)
                return String.Empty;

            return firstChannelID.Name.Substring(0, prefixIndex);
        }
    }
}
