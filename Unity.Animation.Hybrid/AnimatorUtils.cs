using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Unity.Animation.Hybrid
{
    static class AnimatorUtils
    {
        internal static Transform FindDescendant(Transform root, string name)
        {
            foreach (Transform transform in root)
            {
                if (transform.name == name)
                {
                    return transform;
                }
                else
                {
                    var result = FindDescendant(transform, name);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        internal static SkeletonBone[] FilterNonExsistantBones(Transform root, SkeletonBone[] skeleton)
        {
            var filteredSkeleton = new List<SkeletonBone>();

            if (skeleton.Length == 0)
                return filteredSkeleton.ToArray();

            // The root can have a name change, but it is ok.
            filteredSkeleton.Add(skeleton[0]);

            for (int i = 1; i < skeleton.Length; ++i)
            {
                var tr = FindDescendant(root, skeleton[i].name);
                if (tr == null)
                    continue;

                // Make sure that there are bones until the root.
                // TODO: figure out what to do when two transforms have the same name.
                bool hirearchyExsists = true;
                while (hirearchyExsists && tr.parent != root)
                {
                    tr = tr.parent;
                    if (Array.FindIndex(skeleton, (bone) => bone.name == tr.name) == -1)
                    {
                        hirearchyExsists = false;
                    }
                }

                if (!hirearchyExsists)
                    continue;
                filteredSkeleton.Add(skeleton[i]);
            }
            return filteredSkeleton.ToArray();
        }

        internal static Transform[] GetTransformsFromSkeleton(Transform root, SkeletonBone[] skeleton)
        {
            var skeletonTransforms = new Transform[skeleton.Length];
            skeletonTransforms[0] = root;
            for (int i = 1; i < skeleton.Length; ++i)
            {
                skeletonTransforms[i] = FindDescendant(root, skeleton[i].name);
            }
            return skeletonTransforms;
        }

        internal static int[] GetBoneParentIndicies(Transform root, SkeletonBone[] skeleton)
        {
            if (skeleton.Length == 0)
                return new int[] {};

            var skeletonTransforms = GetTransformsFromSkeleton(root, skeleton);

            int[] parentIndicies = new int[skeleton.Length];
            // We assume that the root is at index 0
            parentIndicies[0] = -1;
            for (int i = 1; i < parentIndicies.Length; i++)
            {
                var transform = FindDescendant(root, skeleton[i].name);
                if (transform == null)
                {
                    parentIndicies[i] = -1;
                }
                else
                {
                    // Since we are getting bones from an avatar, the parent is guarenteed to be in the SkeletonBone array
                    parentIndicies[i] = RigGenerator.FindTransformIndex(transform.parent, skeletonTransforms);
                }
            }

            return parentIndicies;
        }
    }
}
