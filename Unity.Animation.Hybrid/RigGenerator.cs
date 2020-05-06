using System.Collections.Generic;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    public static class RigGenerator
    {
        public static int FindTransformIndex(Transform transform, Transform[] transforms)
        {
            if (transform == null || transforms == null)
                return -1;

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].GetInstanceID() == transform.GetInstanceID())
                    return i;
            }

            return -1;
        }

        public static string ComputeRelativePath(Transform target, Transform ancestor)
        {
            var stack = new List<Transform>(10);
            var cur = target;
            while (cur != ancestor && cur != null)
            {
                stack.Add(cur);
                cur = cur.parent;
            }

            var res = "";
            if (stack.Count > 0)
            {
                for (var i = stack.Count - 1; i > 0; --i)
                    res += stack[i].name + "/";
                res += stack[0].name;
            }

            return res;
        }

        public static SkeletonNode[] ExtractSkeletonNodesFromTransforms(Transform root, Transform[] transforms, BindingHashDelegate bindingHash = null)
        {
            var skeletonNodes = new List<SkeletonNode>();

            var hasher = bindingHash ?? BindingHashUtils.DefaultBindingHash;
            for (int i = 0; i < transforms.Length; i++)
            {
                var skeletonNode = new SkeletonNode
                {
                    Id = hasher(ComputeRelativePath(transforms[i], root)),
                    AxisIndex = -1,
                    LocalTranslationDefaultValue = transforms[i].localPosition,
                    LocalRotationDefaultValue = transforms[i].localRotation,
                    LocalScaleDefaultValue = transforms[i].localScale,
                    ParentIndex = FindTransformIndex(transforms[i].parent, transforms)
                };
                skeletonNodes.Add(skeletonNode);
            }

            return skeletonNodes.ToArray();
        }

        public static SkeletonNode[] ExtractSkeletonNodesFromGameObject(GameObject root, BindingHashDelegate bindingHash = null)
        {
            var transforms = root.GetComponentsInChildren<Transform>();

            return ExtractSkeletonNodesFromTransforms(root.transform, transforms, bindingHash);
        }

        public static SkeletonNode[] ExtractSkeletonNodesFromRigComponent(RigComponent rigComponent, BindingHashDelegate bindingHash = null)
        {
            return ExtractSkeletonNodesFromTransforms(rigComponent.transform, rigComponent.Bones, bindingHash);
        }

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
}