using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    [System.Serializable]
    public class TranslationChannel : System.Object
    {
        public string Id;
        public Vector3 DefaultValue;
    }

    [System.Serializable]
    public class RotationChannel : System.Object
    {
        public string Id;
        public Quaternion DefaultValue;
    }

    [System.Serializable]
    public class ScaleChannel : System.Object
    {
        public string Id;
        public Vector3 DefaultValue;
    }

    [System.Serializable]
    public class FloatChannel : System.Object
    {
        public string Id;
        public float DefaultValue;
    }

    [System.Serializable]
    public class IntChannel : System.Object
    {
        public string Id;
        public int DefaultValue;
    }

    // TODO: figure out a better name for RigComponent
    public class RigComponent : MonoBehaviour
    {
        internal const int LatestVersion = 1;
        [HideInInspector][SerializeField] int m_Version;
        internal int Version { get => m_Version; set { m_Version = value; } }
        public Transform[] Bones = Array.Empty<Transform>();

        internal IReadOnlyList<Transform> ExcludeBones => m_ExcludeBones;
        internal IReadOnlyList<Transform> InvalidBones => m_InvalidBones;
        [SerializeField]
        List<Transform> m_ExcludeBones = new List<Transform>();
        [SerializeField]
        List<Transform> m_InvalidBones = new List<Transform>();


        [SerializeField]
        Transform m_SkeletonRootBone;
        public Transform SkeletonRootBone
        {
            get { return m_SkeletonRootBone; }
            set
            {
                if (value && !value.IsChildOf(transform))
                {
                    throw new ArgumentException($"{nameof(SkeletonRootBone)} can only be set to a child of, or the transform of this Rig Component");
                }
                m_SkeletonRootBone = value;
            }
        }
        internal Transform RootBone
        {
            get { return m_SkeletonRootBone == null ? transform : m_SkeletonRootBone; }
        }

        public TranslationChannel[] TranslationChannels = Array.Empty<TranslationChannel>();
        public RotationChannel[] RotationChannels = Array.Empty<RotationChannel>();
        public ScaleChannel[] ScaleChannels = Array.Empty<ScaleChannel>();
        public FloatChannel[] FloatChannels = Array.Empty<FloatChannel>();
        public IntChannel[] IntChannels = Array.Empty<IntChannel>();


        internal void Reset()
        {
            var root = RootBone;
            root.GetComponentsInChildren(true, s_TransformBuffer);
            s_TransformBuffer.Remove(root);
            Bones = s_TransformBuffer.ToArray();
            s_TransformBuffer.Clear();
            m_Version = LatestVersion;
        }

        void OnValidate() { UpdateHierarchyCache(); }

        void CheckValidTransformBone(Transform bone)
        {
            if (bone == null)
                throw new ArgumentNullException($"The Argument {nameof(bone)} cannot be null");
        }

        internal bool IsBoneIncluded(Transform bone)
        {
            return bone != null && Bones.Contains(bone) && !m_ExcludeBones.Contains(bone);
        }

        internal int FindTransformIndex(Transform bone)
        {
            if (bone == null)
                return -1;
            return RigGenerator.FindTransformIndex(bone, Bones);
        }

        public void IncludeBoneAndAncestors(Transform bone)
        {
            CheckValidTransformBone(bone);

            var root = RootBone;
            if (!bone.IsChildOf(root))
                throw new ArgumentException($"Bone must be a child transform of {root}", nameof(bone));

            var rootParent = (root == null) ? root : root.parent;

            // TODO: optimize
            while (bone && bone != rootParent)
            {
                m_ExcludeBones.Remove(bone);
                bone = bone.parent;
            }

            UpdateHierarchyCache();
        }

        static readonly List<Transform> s_TransformBuffer = new List<Transform>(64);
        public void ExcludeBoneAndDescendants(Transform bone)
        {
            CheckValidTransformBone(bone);

            var root = RootBone;
            if (!bone.IsChildOf(root))
                throw new ArgumentException($"Bone must be a child transform of {root}", nameof(bone));

            bone.GetComponentsInChildren(true, s_TransformBuffer);
            foreach (var child in s_TransformBuffer)
            {
                if (!m_ExcludeBones.Contains(child))
                    m_ExcludeBones.Add(child);
            }
            s_TransformBuffer.Clear(); // remove dangling references

            UpdateHierarchyCache();
        }

        public void IncludeBoneAndDescendants(Transform bone)
        {
            CheckValidTransformBone(bone);

            var root = RootBone;
            if (!bone.IsChildOf(root))
                throw new ArgumentException($"Bone must be a child transform of {root}", nameof(bone));

            bone.GetComponentsInChildren(true, s_TransformBuffer);
            foreach (var child in s_TransformBuffer)
            {
                if (m_ExcludeBones.Contains(child))
                    m_ExcludeBones.Remove(child);
            }
            s_TransformBuffer.Clear(); // remove dangling references

            UpdateHierarchyCache();
        }

        static readonly List<Transform> s_UsedBoneTransforms = new List<Transform>(64);
        internal void UpdateHierarchyCache()
        {
            UpgradeWhenNecessary();

            var root = RootBone;
            root.GetComponentsInChildren(true, s_TransformBuffer);

            s_UsedBoneTransforms.Clear();
            foreach (var bone in s_TransformBuffer)
            {
                // Note: s_TransformBuffer is in order of traversal, so this will
                //       automatically exclude children of an excluded parent
                if (bone.parent &&
                    m_ExcludeBones.Contains(bone.parent) &&
                    !m_ExcludeBones.Contains(bone))
                {
                    m_ExcludeBones.Add(bone);
                    continue;
                }

                if (!m_ExcludeBones.Contains(bone))
                    s_UsedBoneTransforms.Add(bone);
            }

            Bones = s_UsedBoneTransforms.ToArray();

            // Remove dangling references
            s_UsedBoneTransforms.Clear();
            s_TransformBuffer.Clear();
        }

        internal void UpgradeWhenNecessary()
        {
            if (m_Version == LatestVersion)
                return;

            m_InvalidBones.Clear();

            // Remove all Bones that are not a child of the RigComponent transform
            var topTransform = this.transform;
            topTransform.GetComponentsInChildren(false, s_TransformBuffer);
            var originalBoneCount = Bones.Length;
            var newBones = new List<Transform>();
            for (int i = Bones.Length - 1; i >= 0; i--)
            {
                var bone = Bones[i];
                if (bone &&                 // Invalid Transforms cannot be valid
                    bone.parent != null &&  // Root transforms cannot be valid (prevents possible infinite loops further on)
                    s_TransformBuffer.Contains(bone))
                {
                    if (!newBones.Contains(bone))   // Remove duplicates
                        newBones.Add(bone);
                }
                else if (!ReferenceEquals(bone, null) && // Prevent adding an actual null pointer to invalid bones list
                         bone != topTransform)           // Skip the top Transform (never consider it an invalid bone)
                {
                    // Note that a destroyed transform is still added b/c it might become valid
                    // again if a change is reverted. This allows the user to still find the bone
                    // in the RigComponent invalid bones list in the inspector.
                    m_InvalidBones.Add(bone);
                }
            }

            if (newBones.Count < originalBoneCount)
            {
                newBones.Reverse();
                Bones = newBones.ToArray();
            }


            // Find our RootBone
            s_TransformBuffer.Clear();
            s_TransformBuffer.AddRange(Bones);
            while (s_TransformBuffer.Count > 1)
            {
                int newCount = s_TransformBuffer.Count;

                // Remove all transforms from s_TransformBuffer that have a parent in s_TransformBuffer
                for (int i = s_TransformBuffer.Count - 1; i >= 0 && s_TransformBuffer.Count > 1; i--)
                {
                    var childTransform = s_TransformBuffer[i];

                    if (!s_TransformBuffer.Contains(childTransform.parent))
                        continue;

                    newCount--;
                    if (i < newCount)
                        s_TransformBuffer[i] = s_TransformBuffer[newCount];
                }

                if (newCount < s_TransformBuffer.Count)
                    s_TransformBuffer.RemoveRange(newCount, s_TransformBuffer.Count - newCount);


                // If we only have one transforms left, that's our parent
                if (s_TransformBuffer.Count == 1)
                    break;


                // Add all parents of all our transforms and see if we can find a common parent there
                for (int i = s_TransformBuffer.Count - 1; i >= 0; i--)
                {
                    var parent = s_TransformBuffer[i].parent;

                    // The topTransform is the highest possible transform, if we find it, don't bother looking further
                    if (parent == topTransform)
                    {
                        s_TransformBuffer.Clear();
                        s_TransformBuffer.Add(parent);
                        break;
                    }

                    if (parent != null &&
                        !s_TransformBuffer.Contains(parent))
                        s_TransformBuffer.Add(parent);
                }
            }

            if (s_TransformBuffer.Count == 1)
                m_SkeletonRootBone = s_TransformBuffer[0];
            else
                m_SkeletonRootBone = null;


            // Find all the excluded bones
            var root = RootBone;
            root.GetComponentsInChildren(false, s_TransformBuffer);
            foreach (var childTransform in s_TransformBuffer)
            {
                if (childTransform == root) // never exclude the root bone
                    continue;

                if (Bones.Contains(childTransform))
                {
                    m_ExcludeBones.Remove(childTransform);
                    continue;
                }

                if (!m_ExcludeBones.Contains(childTransform))
                    m_ExcludeBones.Add(childTransform);
            }

            s_TransformBuffer.Clear(); // Remove dangling references
            m_Version = LatestVersion;
        }
    }
}
