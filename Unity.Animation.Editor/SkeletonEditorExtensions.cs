using System;
using Unity.Animation.Hybrid;
using UnityEditor;
using UnityEngine;

namespace Unity.Animation.Authoring.Editor
{
    /// <summary>
    /// Utility class for Skeleton
    /// </summary>
    static class SkeletonEditorExtensions
    {
        static readonly string k_BlendShapeBindingPrefix = "blendShape.";

        /// <summary>
        /// Create the Skeleton asset in the Asset database.
        /// </summary>
        /// <param name="skeleton">The Skeleton asset.</param>
        /// <param name="path">The asset path in the project folder.</param>
        internal static void CreateAsset(this Skeleton skeleton, string path)
        {
            AssetDatabase.CreateAsset(skeleton, path);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Build the Skeleton asset using information found in the GameObject hierarchy.
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
        private static void AddTransformsToSkeleton(this Skeleton skeleton, GameObject gameObjectHierarchy)
        {
            var transforms = gameObjectHierarchy.transform.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; ++i)
            {
                var transform = transforms[i];
                var path = RigGenerator.ComputeRelativePath(transform, gameObjectHierarchy.transform);
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
        private static void AddBlendShapesToSkeleton(this Skeleton skeleton, GameObject gameObjectHierarchy)
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
    }
}
