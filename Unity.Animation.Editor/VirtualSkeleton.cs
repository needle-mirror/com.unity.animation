using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Animation.Authoring.Editor
{
    /// <summary>
    /// Skeleton abstraction that describes a skeleton hierarchy.
    /// </summary>
    class VirtualSkeleton : ScriptableObject
    {
        [SerializeField] private Skeleton m_Skeleton;
        [SerializeField] private List<VirtualBone> m_Roots = new List<VirtualBone>();
        [SerializeField] private List<VirtualBone> m_Bones = new List<VirtualBone>();
        //[SerializeField] private List<VirtualBone> m_Orphans = new List<VirtualBone>();

        public event Action<VirtualBone> VirtualBoneAdded;
        public event Action<VirtualBone> VirtualBoneRemoved;

        /// <summary>
        /// Roots of the skeleton. These are VirtualBone bones without a parent VirtualBone.
        /// </summary>
        public VirtualBone[] Roots
        {
            get => m_Roots.ToArray();
        }

        // TODO. Separate roots into actual roots and orphan hierarchies.
        //public VirtualBone[] Orphans
        //{
        //    get => m_Orphans.ToArray();
        //}

        /// <summary>
        /// Full list of bones associated to the skeleton.
        /// </summary>
        public VirtualBone[] Bones
        {
            get => m_Bones.ToArray();
        }

        /// <summary>
        /// Skeleton asset that is used to build the VirtualSkeleton bone hierarchy.
        /// </summary>
        public Skeleton Skeleton
        {
            get => m_Skeleton;
            private set
            {
                m_Skeleton = value;
                m_Skeleton.BoneAdded += OnAddBone;
                m_Skeleton.BoneRemoved += OnRemoveBone;
                m_Skeleton.GenericPropertyAdded += OnAddGenericProperty;
                m_Skeleton.GenericPropertyRemoved += OnRemoveGenericProperty;
            }
        }

        /// <summary>
        /// Creates a new VirtualSkeleton based on a Skeleton asset.
        /// </summary>
        /// <param name="skeleton">The Skeleton asset.</param>
        /// <param name="hideFlags">HideFlags used for the VirtualSkeleton and nested VirtualBones.</param>
        /// <returns>Returns a new VirtualSkeleton.</returns>
        public static VirtualSkeleton Create(Skeleton skeleton, HideFlags hideFlags = HideFlags.None)
        {
            var transformChannels = skeleton.ActiveTransformChannels;

            var bones = new List<VirtualBone>(transformChannels.Count);

            var roots = new List<VirtualBone>();
            var orphans = new List<VirtualBone>();
            var children = new List<VirtualBone>[transformChannels.Count];

            var virtualSkeleton = ScriptableObject.CreateInstance<VirtualSkeleton>();
            virtualSkeleton.Skeleton = skeleton;
            virtualSkeleton.hideFlags = hideFlags;

            // First pass - create the bones.
            for (int i = 0; i < transformChannels.Count; ++i)
            {
                var newBone = VirtualBone.Create(virtualSkeleton, transformChannels[i].ID);
                bones.Add(newBone);

                children[i] = new List<VirtualBone>();
            }

            // Build parent bones.
            for (int i = 0; i < transformChannels.Count; ++i)
            {
                var parentID = transformChannels[i].ID.GetParent();
                bool hasParent = false;

                for (int j = 0; j < transformChannels.Count; ++j)
                {
                    if (parentID.Equals(transformChannels[j].ID))
                    {
                        bones[i].Parent = bones[j];
                        children[j].Add(bones[i]);
                        hasParent = true;
                        break;
                    }
                }

                if (!hasParent)
                    roots.Add(bones[i]);
            }

            // Assign children.
            for (int i = 0; i < bones.Count; ++i)
            {
                bones[i].Children = children[i];
            }

            virtualSkeleton.m_Roots = roots;
            virtualSkeleton.m_Bones = bones;

            return virtualSkeleton;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            string concatenatedString = string.Empty;
            for (int i = 0; i < m_Roots.Count; ++i)
            {
                concatenatedString += HierarchyToString(m_Roots[i]);
            }
            return concatenatedString;
        }

        private string HierarchyToString(VirtualBone bone, int indent = 0)
        {
            string concatenatedString = string.Empty;
            concatenatedString += "".PadLeft(indent << 2) + bone + "\n";

            for (int i = 0; i < bone.Children.Count; ++i)
            {
                concatenatedString += HierarchyToString(bone.Children[i], indent + 1);
            }

            return concatenatedString;
        }

        private void OnAddBone(TransformBindingID channelID)
        {
            var newBone = VirtualBone.Create(this, channelID);

            var parentID = channelID.GetParent();

            // Find potential children in roots.
            for (int i = m_Roots.Count - 1; i >= 0; --i)
            {
                var parent = m_Roots[i].Parent;
                if (parent != null && parent.Equals(channelID))
                {
                    newBone.Children.Add(m_Roots[i]);
                    m_Roots.RemoveAt(i);
                }
            }

            // Assign a parent bone to this new node.
            for (int i = 0; i < m_Bones.Count; ++i)
            {
                if (m_Bones[i].Channel.ID.Equals(parentID))
                {
                    newBone.Parent = m_Bones[i];
                    m_Bones[i].Children.Add(newBone);
                    break;
                }
            }

            // If no parent bone is found, add the new bones to the roots.
            if (newBone.Parent == null)
            {
                m_Roots.Add(newBone);
            }

            m_Bones.Add(newBone);

            VirtualBoneAdded?.Invoke(newBone);
        }

        private void OnRemoveBone(TransformBindingID channelID)
        {
            var boneToRemove = m_Bones.Find(bone => bone.ChannelID.Equals(channelID));

            // Remove bone from parent's children.
            if (boneToRemove.Parent != null)
            {
                boneToRemove.Parent.Children.Remove(boneToRemove);
            }
            else
            {
                m_Roots.Remove(boneToRemove);
            }

            // Reassign parent to null in children and add children to roots.
            for (int i = 0; i < boneToRemove.Children.Count; ++i)
            {
                var child = boneToRemove.Children[i];
                child.Parent = null;

                m_Roots.Add(child);
            }

            m_Bones.Remove(boneToRemove);

            VirtualBoneRemoved?.Invoke(boneToRemove);
        }

        private void OnAddGenericProperty(GenericBindingID channelID)
        {
            // TODO.
        }

        private void OnRemoveGenericProperty(GenericBindingID channelID)
        {
            // TODO.
        }
    }
}
