using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animation.Authoring.Editor
{
    /// <summary>
    /// Bone abstraction that describes a transform binding.
    /// </summary>
    [Serializable]
    class VirtualBone : ScriptableObject
    {
        [SerializeField] private VirtualSkeleton m_Skeleton;
        [SerializeField] private TransformBindingID m_ChannelID;

        [SerializeField] private VirtualBone m_Parent;
        [SerializeField] private List<VirtualBone> m_Children = new List<VirtualBone>();

        /// <summary>
        /// Create a VirtualBone abstraction from a transform binding.
        /// </summary>
        /// <param name="skeleton">VirtualSkeleton owning the VirtualBone.</param>
        /// <param name="bindingID">Transform channel binding ID.</param>
        /// <returns>New VirtualBone.</returns>
        public static VirtualBone Create(VirtualSkeleton skeleton, TransformBindingID bindingID)
        {
            var bone = ScriptableObject.CreateInstance<VirtualBone>();
            bone.hideFlags = skeleton.hideFlags;
            bone.name = bindingID.Name;
            bone.m_Skeleton = skeleton;
            bone.m_ChannelID = bindingID;
            return bone;
        }

        /// <summary>
        /// The parent VirtualBone to this bone.
        /// </summary>
        public VirtualBone Parent
        {
            get => m_Parent;
            set => m_Parent = value;
        }

        /// <summary>
        /// The list of children VirtualBone for this bone.
        /// </summary>
        public List<VirtualBone> Children
        {
            get => m_Children;
            set => m_Children = value;
        }

        /// <summary>
        /// The unique TransformBindingID associated to this bone.
        /// </summary>
        public TransformBindingID ChannelID
        {
            get => m_ChannelID;
        }

        /// <summary>
        /// The TransformChannel data associated to this bone.
        /// </summary>
        public TransformChannel Channel
        {
            get => new TransformChannel { ID = m_ChannelID, Properties = m_Skeleton.Skeleton[m_ChannelID] };
            private set => m_Skeleton.Skeleton.AddOrSetTransformChannel(value);
        }

        /// <summary>
        /// The VirtualBone name.
        /// </summary>
        public string Name
        {
            get => Channel.ID.Name;
        }

        /// <summary>
        /// The VirtualBone path in the hierarchy.
        /// </summary>
        public string Path
        {
            get => Channel.ID.Path;
        }

        /// <summary>
        /// The local translation value of the Transform channel.
        /// </summary>
        public float3 DefaultTranslation
        {
            get => Channel.Properties.DefaultTranslationValue;
            set
            {
                var channelCopy = Channel;
                channelCopy.Properties.DefaultTranslationValue = value;
                Channel = channelCopy;
            }
        }

        /// <summary>
        /// The local rotation value of the Transform channel.
        /// </summary>
        public quaternion DefaultRotation
        {
            get => Channel.Properties.DefaultRotationValue;
            set
            {
                var channelCopy = Channel;
                channelCopy.Properties.DefaultRotationValue = value;
                Channel = channelCopy;
            }
        }

        /// <summary>
        /// The local scale value of the Transform channel.
        /// </summary>
        public float3 DefaultScale
        {
            get => Channel.Properties.DefaultScaleValue;
            set
            {
                var channelCopy = Channel;
                channelCopy.Properties.DefaultScaleValue = value;
                Channel = channelCopy;
            }
        }

        /// <summary>
        /// Transform labels associated to this bone.
        /// </summary>
        public TransformLabel[] TransformLabels
        {
            get
            {
                var labels = new List<TransformLabel>();
                m_Skeleton.Skeleton.QueryTransformLabels(m_ChannelID, labels);
                return labels.ToArray();
            }
            set
            {
                var currentLabels = TransformLabels;
                var newLabels = value;
                foreach (var label in newLabels)
                {
                    if (Array.IndexOf(currentLabels, label) == -1)
                    {
                        m_Skeleton.Skeleton.AddTransformLabel(m_ChannelID, label);
                    }
                }

                foreach (var label in currentLabels)
                {
                    if (Array.IndexOf(newLabels, label) == -1)
                    {
                        m_Skeleton.Skeleton.RemoveTransformLabel(label);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }
}
