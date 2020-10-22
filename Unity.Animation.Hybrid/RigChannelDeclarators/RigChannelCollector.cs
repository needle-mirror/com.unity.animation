using System.Diagnostics;
using UnityEngine;
using Unity.Collections;

namespace Unity.Animation.Hybrid
{
    /// <summary>
    /// Helper to add custom dynamic rig channel declarations at conversion time.
    /// This class is provided to any GameObject components part of a <see cref="RigComponent"/> hierarchy implementing the
    /// <see cref="IDeclareCustomRigChannels"/> interface
    /// </summary>
    public class RigChannelCollector
    {
        RigBuilderData m_Data;

        RigChannelCollector() {}

        /// <summary>
        /// Internal constructor built at conversion time and passed to all components of a <see cref="RigComponent"/> hierarchy
        /// implementing the <see cref="IDeclareCustomRigChannels"/> interface.
        /// </summary>
        /// <param name="root">RigComponent transform</param>
        /// <param name="data">RigBuilderData</param>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if root is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if root RigBuilderData has not been created.
        /// </exception>
        internal RigChannelCollector(Transform root, ref RigBuilderData data)
        {
            ValidateIsNotNull(root);
            ValidateRigBuilderData(ref data);
            Root = root;
            m_Data = data;
        }

        internal Transform Root { get; private set; }

        /// <summary>
        /// Adds a <see cref="TranslationChannel"/> to the rig definition.
        /// </summary>
        /// <param name="channel">Translation channel</param>
        /// <returns>Index of newly added channel</returns>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if the <see cref="TranslationChannel"/> is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the <see cref="TranslationChannel.Id"/> is null, empty or collides with a SkeletonNode channel.
        /// </exception>
        public int Add(TranslationChannel channel)
        {
            ValidateIsNotNull(channel);
            ValidateChannelId(channel.Id, ref m_Data, true);
            return Add(m_Data.TranslationChannels, new LocalTranslationChannel { Id = channel.Id, DefaultValue = channel.DefaultValue });
        }

        /// <summary>
        /// Adds a <see cref="RotationChannel"/> to the rig definition.
        /// </summary>
        /// <param name="channel">Rotation channel</param>
        /// <returns>Index of newly added channel</returns>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if the <see cref="RotationChannel"/> is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the <see cref="RotationChannel.Id"/> is null, empty or collides with a SkeletonNode channel.
        /// </exception>
        public int Add(RotationChannel channel)
        {
            ValidateIsNotNull(channel);
            ValidateChannelId(channel.Id, ref m_Data, true);
            return Add(m_Data.RotationChannels, new LocalRotationChannel { Id = channel.Id, DefaultValue = channel.DefaultValue });
        }

        /// <summary>
        /// Adds a <see cref="ScaleChannel"/> to the rig definition.
        /// </summary>
        /// <param name="channel">Scale channel</param>
        /// <returns>Index of newly added channel</returns>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if the <see cref="ScaleChannel"/> is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the <see cref="ScaleChannel.Id"/> is null, empty or collides with a SkeletonNode channel.
        /// </exception>
        public int Add(ScaleChannel channel)
        {
            ValidateIsNotNull(channel);
            ValidateChannelId(channel.Id, ref m_Data, true);
            return Add(m_Data.ScaleChannels, new LocalScaleChannel { Id = channel.Id, DefaultValue = channel.DefaultValue });
        }

        /// <summary>
        /// Adds a <see cref="FloatChannel"/> to the rig definition.
        /// </summary>
        /// <param name="channel">Float channel</param>
        /// <returns>Index of newly added channel</returns>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if the <see cref="FloatChannel"/> is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the <see cref="FloatChannel.Id"/> is null or empty.
        /// </exception>
        public int Add(FloatChannel channel)
        {
            ValidateIsNotNull(channel);
            ValidateChannelId(channel.Id, ref m_Data, false);
            return Add(m_Data.FloatChannels, new Animation.FloatChannel { Id = channel.Id, DefaultValue = channel.DefaultValue });
        }

        /// <summary>
        /// Adds an <see cref="IntChannel"/> to the rig definition.
        /// </summary>
        /// <param name="channel">Int channel</param>
        /// <returns>Index of newly added channel</returns>
        /// <exception cref="System.NullReferenceException">
        /// Thrown if the <see cref="IntChannel"/> is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if the <see cref="IntChannel.Id"/> is null or empty.
        /// </exception>
        public int Add(IntChannel channel)
        {
            ValidateIsNotNull(channel);
            ValidateChannelId(channel.Id, ref m_Data, false);
            return Add(m_Data.IntChannels, new Animation.IntChannel { Id = channel.Id, DefaultValue = channel.DefaultValue });
        }

        /// <summary>
        /// Computes the relative path from the <see cref="RigComponent"/> to the target component.
        /// </summary>
        /// <param name="target">Target component transform</param>
        /// <returns>The relative path from the RigComponent to the target component</returns>
        public string ComputeRelativePath(Transform target) =>
            RigGenerator.ComputeRelativePath(target, Root);

        static int Add<T>(NativeList<T> list, in T toAdd)
            where T : struct, IAnimationChannel
        {
            for (int i = 0; i < list.Length; ++i)
                if (list[i].Id == toAdd.Id)
                    return i;

            int count = list.Length;
            list.Add(toAdd);
            return count;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateChannelId(string channelId, ref RigBuilderData data, bool doSkeletonCheck)
        {
            StringHash hash = channelId;
            if (hash == 0)
                throw new System.InvalidOperationException("Custom channel id cannot be null or empty.");

            if (doSkeletonCheck)
            {
                for (int i = 0; i < data.SkeletonNodes.Length; ++i)
                {
                    if (hash == data.SkeletonNodes[i].Id)
                        throw new System.InvalidOperationException($"Custom channel id [{channelId}] is already used by a skeleton bone.");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateIsNotNull(Transform transform)
        {
            if (transform == null)
                throw new System.NullReferenceException("Invalid root transform.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateIsNotNull(System.Object channel)
        {
            if (channel == null)
                throw new System.NullReferenceException("Null channel provided.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ValidateRigBuilderData(ref RigBuilderData data)
        {
            if (!data.IsCreated)
                throw new System.InvalidOperationException("RigBuilderData is invalid.");
        }
    }
}
