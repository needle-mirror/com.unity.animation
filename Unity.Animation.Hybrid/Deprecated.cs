using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
#if UNITY_EDITOR
    public static partial class ClipConversion
    {
        [Obsolete("ToDenseClip(BindingHashDelegate) has been deprecated. Use ToDenseClip(BindingHashGenerator) instead. (RemovedAfter 2020-12-27).", false)]
        public static BlobAssetReference<Clip> ToDenseClip(this AnimationClip sourceClip, BindingHashDelegate bindingHash) =>
            sourceClip.ToDenseClip(BindingHashDeprecationHelper.Convert(bindingHash));
    }

    public static partial class ClipBuilderUtils
    {
        [Obsolete("ToClipBuilder(Allocator, BindingHashDelegate) has been deprecated. Use ToClipBuilder(Allocator BindingHashGenerator) instead. (RemovedAfter 2020-12-27).", false)]
        public static ClipBuilder ToClipBuilder(this AnimationClip sourceClip, Allocator allocator, BindingHashDelegate bindingHash) =>
            sourceClip.ToClipBuilder(allocator, BindingHashDeprecationHelper.Convert(bindingHash));
    }
#endif

    public static partial class RigGenerator
    {
        [Obsolete("ExtractSkeletonNodesFromTransforms(Transform, Transform[], BindingHashDelegate) has been deprecated. Use ExtractSkeletonNodesFromTransforms(Transform, Transform[], BindingHashGenerator) instead. (RemovedAfter 2020-12-27).", false)]
        public static SkeletonNode[] ExtractSkeletonNodesFromTransforms(Transform root, Transform[] transforms, BindingHashDelegate bindingHash) =>
            ExtractSkeletonNodesFromTransforms(root, transforms, BindingHashDeprecationHelper.Convert(bindingHash));

        [Obsolete("ExtractSkeletonNodesFromGameObject(GameObject, BindingHashDelegate) has been deprecated. Use ExtractSkeletonNodesFromGameObject(GameObject, BindingHashGenerator) instead. (RemovedAfter 2020-12-27).", false)]
        public static SkeletonNode[] ExtractSkeletonNodesFromGameObject(GameObject root, BindingHashDelegate bindingHash) =>
            ExtractSkeletonNodesFromGameObject(root, BindingHashDeprecationHelper.Convert(bindingHash));

        [Obsolete("ExtractSkeletonNodesFromRigComponent has been deprecated. Use RigComponent.ExtractRigBuilderData. (RemovedAfter 2020-12-01).", false)]
        public static SkeletonNode[] ExtractSkeletonNodesFromRigComponent(RigComponent rigComponent, BindingHashDelegate bindingHash = null)
        {
            return ExtractSkeletonNodesFromTransforms(rigComponent.transform, rigComponent.Bones, bindingHash);
        }

        [Obsolete("RigGeneratorExtractAnimationChannelFromRigComponent has been deprecated. Use RigComponent.ExtractRigBuilderData. (RemovedAfter 2020-12-01).", false)]
        public static IAnimationChannel[] ExtractAnimationChannelFromRigComponent(RigComponent rigComponent, BindingHashDelegate bindingHash = null)
        {
            var channels = new List<IAnimationChannel>();
            var hasher = bindingHash ?? BindingHashUtils.DefaultBindingHash;
            for (int i = 0; i < rigComponent.TranslationChannels.Length; i++)
            {
                var channel = new LocalTranslationChannel { Id = hasher(rigComponent.TranslationChannels[i].Id), DefaultValue = rigComponent.TranslationChannels[i].DefaultValue };
                channels.Add(channel);
            }

            for (int i = 0; i < rigComponent.RotationChannels.Length; i++)
            {
                var channel = new LocalRotationChannel { Id = hasher(rigComponent.RotationChannels[i].Id), DefaultValue = rigComponent.RotationChannels[i].DefaultValue };
                channels.Add(channel);
            }

            for (int i = 0; i < rigComponent.ScaleChannels.Length; i++)
            {
                var channel = new LocalScaleChannel { Id = hasher(rigComponent.ScaleChannels[i].Id), DefaultValue = rigComponent.ScaleChannels[i].DefaultValue };
                channels.Add(channel);
            }

            for (int i = 0; i < rigComponent.FloatChannels.Length; i++)
            {
                var channel = new Unity.Animation.FloatChannel { Id = hasher(rigComponent.FloatChannels[i].Id), DefaultValue = rigComponent.FloatChannels[i].DefaultValue };
                channels.Add(channel);
            }

            for (int i = 0; i < rigComponent.IntChannels.Length; i++)
            {
                var channel = new Unity.Animation.IntChannel { Id = hasher(rigComponent.IntChannels[i].Id), DefaultValue = rigComponent.IntChannels[i].DefaultValue };
                channels.Add(channel);
            }

            return channels.ToArray();
        }
    }

    public static partial class RigRemapUtils
    {
        [Obsolete("CreateRemapTable(RigComponent, RigComponent, ChannelFilter, OffsetOverrides, BindingHashDelegate) has been deprecated. Use CreateRemapTable(RigComponent, RigComponent, ChannelFilter, OffsetOverrides, BindingHashGenerator) instead. (RemovedAfter 2020-12-27).", false)]
        public static BlobAssetReference<RigRemapTable> CreateRemapTable(
            RigComponent srcRig,
            RigComponent dstRig,
            Animation.RigRemapUtils.ChannelFilter filter,
            Animation.RigRemapUtils.OffsetOverrides offsetOverrides,
            BindingHashDelegate bindingHash
        ) => CreateRemapTable(srcRig, dstRig, filter, offsetOverrides, BindingHashDeprecationHelper.Convert(bindingHash));
    }

    public partial class RigComponent : MonoBehaviour, IRigAuthoring
    {
        [Obsolete("ToRigDefinition(BindingHashDelegate) has been deprecated. Use ToRigDefinition(BindingHashGenerator) instead. (RemovedAfter 2020-12-27).", false)]
        public BlobAssetReference<RigDefinition> ToRigDefinition(BindingHashDelegate bindingHash) =>
            ToRigDefinition(BindingHashDeprecationHelper.Convert(bindingHash));

        [Obsolete("ExtractRigBuilderData(BindingHashDelegate) has been deprecated. Use ExtractRigBuilderData(BindingHashGenerator) instead. (RemovedAfter 2020-12-27).", false)]
        public RigBuilderData ExtractRigBuilderData(BindingHashDelegate bindingHash) =>
            ExtractRigBuilderData(BindingHashDeprecationHelper.Convert(bindingHash));
    }

    [Obsolete("BindingHashDelegate has been deprecated. Use BindingHashGenerator instead. (RemovedAfter 2020-12-27).", false)]
    public delegate uint BindingHashDelegate(string path);

    [Obsolete("BindingHashUtils has been deprecated. Use BindingHashGenerator and BindingHashGlobals instead. (RemovedAfter 2020-12-27).", false)]
    public static class BindingHashUtils
    {
        public static BindingHashDelegate DefaultBindingHash = HashFullPath;

        public static uint HashFullPath(string path) =>
            StringHash.Hash(path);

        public static uint HashName(string path) =>
            StringHash.Hash(System.IO.Path.GetFileName(path));
    }

    internal static class BindingHashDeprecationHelper
    {
#pragma warning disable 0618
        internal static BindingHashGenerator Convert(BindingHashDelegate deprecatedDelegate)
        {
            return new BindingHashGenerator
            {
                TransformBindingHashFunction = id =>
                {
                    var hasher = deprecatedDelegate ?? BindingHashUtils.DefaultBindingHash;
                    return hasher(id.Path);
                },
                GenericBindingHashFunction = id =>
                {
                    var hasher = deprecatedDelegate ?? BindingHashUtils.DefaultBindingHash;
                    return hasher(BuildPath(id.Path, id.AttributeName));
                }
            };
        }

#pragma warning restore 0618

        internal static string BuildPath(string path, string property)
        {
            bool nullPath = string.IsNullOrEmpty(path);
            bool nullProperty = string.IsNullOrEmpty(property);

            if (nullPath && nullProperty)
                return string.Empty;
            if (nullPath)
                return property;
            if (nullProperty)
                return path;

            return path + "/" + property;
        }
    }

    public partial struct TransformBindingID : IBindingID, IEquatable<TransformBindingID>, IComparable<TransformBindingID>
    {
        [Obsolete("IsDecendantOf has been deprecated. Use TransformBindingID.IsDescendantOf. (RemovedAfter 2021-03-03). (UnityUpgradable) -> IsDescendantOf(*)", false)]
        public bool IsDecendantOf(TransformBindingID ancestor)
        {
            return IsDescendantOf(ancestor);
        }
    }
}
