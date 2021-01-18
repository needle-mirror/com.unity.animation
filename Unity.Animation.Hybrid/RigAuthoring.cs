using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Animation.Authoring;

namespace Unity.Animation.Hybrid
{
    /// <summary>
    /// Bone Override that defines how a skeleton channel should
    /// map to a specific Transform bone in the RigAuthoring prefab hierarchy.
    /// </summary>
    [Serializable]
    struct BoneOverride
    {
        public TransformBindingID BindingID;
        public Transform Bone;
    }

    /// <summary>
    /// The RigAuthoring component allows to map a skeleton
    /// onto a character instance.
    /// </summary>
    public class RigAuthoring : MonoBehaviour, IRigAuthoring
    {
        public Authoring.Skeleton Skeleton
        {
            get => m_Skeleton;
            set
            {
                m_Skeleton = value;
                ClearAllManualOverrides();
            }
        }

        [SerializeField] private Authoring.Skeleton m_Skeleton;

        /// <summary>
        /// Root bone of rig hierarchy.
        /// </summary>
        /// <exception cref="ArgumentException">Transform is not part of the RigAuthoring hierarchy.</exception>
        public Transform TargetSkeletonRoot
        {
            get => m_TargetSkeletonRoot;
            set
            {
                if (value != null)
                {
                    if (!value.IsChildOf(transform))
                    {
                        throw new ArgumentException($"{nameof(value)} is not part of the RigAuthoring hierarchy.");
                    }
                }

                m_TargetSkeletonRoot = value;
            }
        }

        // TODO: Replace this with the first override in the mapping, or default to "this.transform"
        [SerializeField] private Transform m_TargetSkeletonRoot;

        [SerializeField] private List<BoneOverride> m_BoneOverrides = new List<BoneOverride>();

        private static Authoring.Skeleton s_TargetSkeleton;
        private static readonly List<Transform> s_TransformBuffer = new List<Transform>();

        private bool m_BoneCacheDirty = true;
        private List<RigIndexToBone> m_Bones = new List<RigIndexToBone>();
        private List<RigIndexToBone> Bones
        {
            get
            {
                if (m_BoneCacheDirty)
                {
                    var authoring = (IRigAuthoring)this;
                    authoring.GetBones(m_Bones);
                    m_BoneCacheDirty = false;
                }
                return m_Bones;
            }
        }

        protected RigAuthoring()
        {
        }

        private void OnValidate()
        {
            if (m_TargetSkeletonRoot != null)
            {
                if (!m_TargetSkeletonRoot.IsChildOf(transform))
                {
                    m_TargetSkeletonRoot = null;
                }
            }
            m_BoneCacheDirty = true;
        }

        /// <summary>
        /// Adds a manual override for the bone mapping of a skeleton TransformBindingID unto
        /// the RigAuthoring hierarchy.
        /// </summary>
        /// <param name="bindingID">TransformBindingID in the Skeleton.</param>
        /// <param name="bone">Bone in the RigAuthoring hierarchy.</param>
        /// <exception cref="ArgumentException">bindingID is invalid..</exception>
        /// <exception cref="ArgumentNullException">bone is null.</exception>
        public void OverrideTransformBinding(TransformBindingID bindingID, Transform bone)
        {
            if (bindingID.Equals(TransformBindingID.Invalid))
                throw new ArgumentException($"{nameof(bindingID)} is invalid.");

            if (bone != null && !bone.IsChildOf(transform))
                throw new ArgumentException($"{nameof(bone)} is not part of the RigAuthoring hierarchy.");

            var boneIndex = m_BoneOverrides.FindIndex(b => b.BindingID == bindingID);

            if (boneIndex != -1)
                m_BoneOverrides[boneIndex] = new BoneOverride {BindingID = bindingID, Bone = bone};
            else
                m_BoneOverrides.Add(new BoneOverride {BindingID = bindingID, Bone = bone});
            m_BoneCacheDirty = true;
        }

        /// <summary>
        /// Queries whether a manual bone override has been set for the specified bindingID.
        /// </summary>
        /// <param name="bindingID">TransformBindingID in the Skeleton.</param>
        /// <returns>True if a manual override has been found. False otherwise.</returns>
        public bool HasManualOverride(TransformBindingID bindingID)
        {
            return m_BoneOverrides.FindIndex(boneOverride => boneOverride.BindingID.Equals(bindingID)) != -1;
        }

        internal bool TryGetManualOverride(TransformBindingID bindingID, out BoneOverride result)
        {
            var index = m_BoneOverrides.FindIndex(boneOverride => boneOverride.BindingID.Equals(bindingID));
            if (index == -1)
            {
                result = default;
                return false;
            }
            result = m_BoneOverrides[index];
            return true;
        }

        /// <summary>
        /// Removes the bone override associated to bindingID.
        /// </summary>
        /// <param name="bindingID">TransformBindingID in the Skeleton.</param>
        /// <returns>True if manual override was removed. False otherwise.</returns>
        public bool ClearManualOverride(TransformBindingID bindingID)
        {
            m_BoneCacheDirty = true;
            var index = m_BoneOverrides.FindIndex(boneOverride => boneOverride.BindingID.Equals(bindingID));
            if (index != -1)
            {
                m_BoneOverrides.RemoveAt(index);
                return true;
            }

            return false;
        }

        internal TransformBindingID GetBindingForTransform(Transform transform)
        {
            if (transform == null)
                return TransformBindingID.Invalid;

            var bones = this.Bones;
            var boneIndex = bones.FindIndex(b => b.Bone == transform);
            if (boneIndex == -1)
                return TransformBindingID.Invalid;

            return Skeleton.GetBindingIDByRuntimeIndex(boneIndex);
        }

        internal Transform GetTransformForBindingID(TransformBindingID bindingID)
        {
            if (bindingID == TransformBindingID.Invalid)
                return null;

            var runtimeIndex = Skeleton.QueryTransformIndex(bindingID);

            var bones = this.Bones;
            var boneIndex = bones.FindIndex(b => b.Index == runtimeIndex);
            if (boneIndex == -1)
                return null;

            return bones[boneIndex].Bone;
        }

        internal void AutoSetChildOrSiblingTransforms(TransformBindingID bindingID, Transform parentTransform)
        {
            // TODO: implement
            m_BoneCacheDirty = true;
        }

        internal void SetChildrenManualOverrideToNull(TransformBindingID bindingID)
        {
            // TODO: implement
            m_BoneCacheDirty = true;
        }

        internal void ClearChildrenManualOverride(TransformBindingID bindingID)
        {
            // TODO: implement
            m_BoneCacheDirty = true;
        }

        internal Transform GetFirstSetAncestorTransform(TransformBindingID bindingID, Transform root)
        {
            if (bindingID == TransformBindingID.Invalid)
                return null;

            var currentBindingID = bindingID;
            do
            {
                currentBindingID = GetParentBindingID(currentBindingID);
                if (currentBindingID == TransformBindingID.Invalid)
                    return null;

                if (currentBindingID == TransformBindingID.Root)
                    return root;

                var currentTransform = GetTransformForBindingID(currentBindingID);
                if (currentTransform == null)
                    continue;

                return currentTransform;
            }
            while (true);
        }

        internal Transform GetTransformForPrecedingBindingID(TransformBindingID bindingID)
        {
            if (bindingID == TransformBindingID.Invalid)
                return null;
            var previousBindingID = GetPrecedingBindingID(bindingID);
            if (previousBindingID == TransformBindingID.Invalid)
                return null;
            return GetTransformForBindingID(previousBindingID);
        }

        internal bool IsHierarchyValidForTransform(TransformBindingID bindingID, Transform root, Transform transform)
        {
            if (transform == null)
                return false;

            // If the root is invalid, the transform cannot be in a valid hierarchy
            if (root == null)
                return false;

            // If this is the root, then it is valid
            if (root == transform)
                return true;

            if (!transform.IsChildOf(root))
                // If our binding is the root, we always allow it as long as it's a child transform of RigAuthoring
                return (bindingID.ID == this.Skeleton.Root.ID &&
                    transform.IsChildOf(this.transform));

            // Is transform a child of the first found ancestor (higher level branch)?
            var ancestorTransform = GetFirstSetAncestorTransform(bindingID, root);
            if (ancestorTransform != null)
                return transform.IsChildOf(ancestorTransform);

            // Is it a sibling to the binding id right in front of this binding id?
            var siblingTransform = GetTransformForPrecedingBindingID(bindingID);
            if (siblingTransform != null &&
                transform.parent == siblingTransform.parent)
                return true;

            return false;
        }

        internal void GetAllowedTransformsForBinding(TransformBindingID bindingID, List<Transform> transforms)
        {
            var root = (TargetSkeletonRoot != null) ? TargetSkeletonRoot : this.transform;
            var ancestorTransform = GetFirstSetAncestorTransform(bindingID, root);

            transforms.Clear();
            if (ancestorTransform == null)
                return;

            ancestorTransform.GetComponentsInChildren(transforms);
            transforms.Remove(ancestorTransform);

            var selfIndex = Skeleton.QueryTransformIndex(bindingID);

            // Remove all transforms, and their children, of transforms that have already been set somewhere else
            var bones = Bones;
            for (int b = 0; b < bones.Count; b++)
            {
                var bone = bones[b].Bone;
                if (bone == null ||
                    bones[b].Index == selfIndex) // make sure we do not remove our currently set value
                    continue;

                if (transforms.IndexOf(bone) == -1)
                    continue;

                for (int t = transforms.Count - 1; t >= 0; t--)
                {
                    var transform = transforms[t];
                    if (transform == bone ||
                        transform.IsChildOf(bone))
                        transforms.RemoveAt(t);
                }
            }
        }

        internal bool CanTransformBeSet(TransformBindingID bindingID, Transform root, Transform transform)
        {
            // Is the transform already set to another binding?
            var foundBindingID = GetBindingForTransform(transform);
            if (foundBindingID != TransformBindingID.Invalid && foundBindingID != bindingID)
                return false;

            // Is the transform position in its hierarchy compatible with the skeleton hierarchy?
            if (IsHierarchyValidForTransform(bindingID, root, transform))
                return true;

            return false;
        }

        internal TransformBindingID GetPrecedingBindingID(TransformBindingID bindingID)
        {
            if (Skeleton == null)
                return TransformBindingID.Invalid;
            return Skeleton.GetPrecedingBindingID(bindingID);
        }

        internal TransformBindingID GetParentBindingID(TransformBindingID bindingID)
        {
            if (Skeleton == null)
                return TransformBindingID.Invalid;
            return Skeleton.GetParentBindingID(bindingID);
        }

        /// <summary>
        /// Clears all bone overrides.
        /// </summary>
        public void ClearAllManualOverrides()
        {
            m_BoneCacheDirty = true;
            m_BoneOverrides.Clear();
        }

        /// <summary>
        /// Clears bone overrides that can't be found in the skeleton.
        /// </summary>
        public void ClearInvalidManualOverrides()
        {
            m_BoneCacheDirty = true;
            m_BoneOverrides.RemoveAll(boneOverride => !m_Skeleton.Contains(boneOverride.BindingID));
        }

        void IRigAuthoring.GetBones(List<RigIndexToBone> bones)
        {
            bones.Clear();

            if (m_Skeleton == null)
                return;

            // Map by path.
            {
                GetComponentsInChildren(s_TransformBuffer);

                if (s_TargetSkeleton == null)
                {
                    s_TargetSkeleton = ScriptableObject.CreateInstance<Authoring.Skeleton>();
                    s_TargetSkeleton.hideFlags = HideFlags.HideAndDontSave;
                }

                s_TargetSkeleton.Clear();
                s_TargetSkeleton.AddTransformsToSkeleton(s_TransformBuffer, transform);

                if (TargetSkeletonRoot != null && TargetSkeletonRoot.IsChildOf(transform))
                    s_TargetSkeleton.Root = new TransformBindingID { Path = RigGenerator.ComputeRelativePath(TargetSkeletonRoot, transform) };
                else
                    s_TargetSkeleton.Root = TransformBindingID.Invalid;

                var sourceSkeletonNamespace = m_Skeleton.ExtractNamespace();
                var targetSkeletonNamespace = s_TargetSkeleton.ExtractNamespace();

                var sourceHasher = new MapByPathHashGenerator {RootID = Skeleton.Root, BonePrefix = sourceSkeletonNamespace};
                var targetHasher = new MapByPathHashGenerator {RootID = s_TargetSkeleton.Root, BonePrefix = targetSkeletonNamespace};

                using (var boneMappingTable = RigRemapUtils.CreateRemapTable(
                    m_Skeleton,
                    s_TargetSkeleton,
                    Animation.RigRemapUtils.ChannelFilter.All,
                    default,
                    sourceHasher,
                    targetHasher))
                {
                    var dstSkeletonChannels = new List<TransformChannel>();
                    s_TargetSkeleton.GetAllTransforms(dstSkeletonChannels);

                    // Rebuild list of RigIndexToBone.
                    bones.Capacity = m_Skeleton.ActiveTransformChannelCount;
                    for (int i = 0; i < m_Skeleton.ActiveTransformChannelCount; ++i)
                        bones.Add(new RigIndexToBone { Index = i, Bone = null });


                    var fullPathHashGenerator = BindingHashGlobals.DefaultHashGenerator;
                    var hashes = new StringHash[s_TransformBuffer.Count];
                    for (int i = 0; i < s_TransformBuffer.Count; ++i)
                    {
                        var path = RigGenerator.ComputeRelativePath(s_TransformBuffer[i], s_TransformBuffer[0]);
                        var bindingID = new TransformBindingID { Path = path };

                        hashes[i] = fullPathHashGenerator.ToHash(bindingID);
                    }

                    var boneMappingCount = boneMappingTable.Value.TranslationMappings.Length;
                    for (int i = 0; i < boneMappingCount; ++i)
                    {
                        var entry = boneMappingTable.Value.TranslationMappings[i];

                        var channel = dstSkeletonChannels[entry.DestinationIndex];

                        var hash = fullPathHashGenerator.ToHash(channel.ID);
                        var boneIndex = Array.FindIndex(hashes, h => h.Equals(hash));

                        var bone = s_TransformBuffer[boneIndex];
                        bones[entry.SourceIndex] = new RigIndexToBone { Index = entry.SourceIndex, Bone = bone };
                    }
                }
            }

            foreach (var overrideBone in m_BoneOverrides)
            {
                int rigIndex = m_Skeleton.QueryTransformIndex(overrideBone.BindingID);
                if (rigIndex != -1)
                {
                    var boneIndex = bones.FindIndex(b => b.Index == rigIndex);
                    if (boneIndex != -1)
                        bones[boneIndex] = new RigIndexToBone { Index = rigIndex, Bone = overrideBone.Bone };
                }
            }
        }
    }
}
