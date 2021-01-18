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
    }
}
