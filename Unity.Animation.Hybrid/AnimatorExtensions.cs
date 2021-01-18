using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Animation.Hybrid
{
    static class AnimatorExtensions
    {
        internal static void ExtractSkeletonNodes(this Animator animator, NativeList<SkeletonNode> extractedNodes, BindingHashGenerator hasher = default)
        {
            if (!hasher.IsValid)
                hasher = BindingHashGlobals.DefaultHashGenerator;

            if (animator.avatar == null || !animator.avatar.isHuman)
            {
                var nodes = RigGenerator.ExtractSkeletonNodesFromGameObject(animator.gameObject, hasher);
                unsafe
                {
                    fixed(void* nodesPtr = &nodes[0])
                    {
                        extractedNodes.AddRange(nodesPtr, nodes.Length);
                    }
                }
                return;
            }
            else if (!animator.avatar.isValid)
            {
                throw new System.ArgumentException($"Avatar ({animator.avatar.ToString()}) is not valid, so no bones could be extracted.");
            }

            var skeleton = AnimatorUtils.FilterNonExsistantBones(animator.transform, animator.avatar.humanDescription.skeleton);
            if (skeleton.Length != animator.avatar.humanDescription.skeleton.Length)
            {
                Debug.LogWarning($"Animator ({animator.ToString()}) is missing {animator.avatar.humanDescription.skeleton.Length - skeleton.Length} bones.");
            }
            var parentIndicies = AnimatorUtils.GetBoneParentIndicies(animator.transform, skeleton);

            for (int i = 0; i < skeleton.Length; ++i)
            {
                var boneTransform = AnimatorUtils.FindDescendant(animator.transform, skeleton[i].name) ?? animator.transform;
                extractedNodes.Add(new SkeletonNode()
                {
                    Id = hasher.ToHash(RigGenerator.ToTransformBindingID(boneTransform, animator.transform)),
                    LocalTranslationDefaultValue = skeleton[i].position,
                    LocalScaleDefaultValue = skeleton[i].scale,
                    LocalRotationDefaultValue = skeleton[i].rotation,
                    ParentIndex = parentIndicies[i],
                    AxisIndex = -1,
                });
            }
        }

        internal static void ExtractBoneTransforms(this Animator animatorComponent, List<RigIndexToBone> bones)
        {
            bones.Clear();

            Transform[] transforms = null;
            if (animatorComponent.avatar == null || !animatorComponent.avatar.isValid || !animatorComponent.avatar.isHuman)
            {
                transforms =  animatorComponent.gameObject.GetComponentsInChildren<Transform>();
            }
            else
            {
                var skeleton = AnimatorUtils.FilterNonExsistantBones(animatorComponent.transform, animatorComponent.avatar.humanDescription.skeleton);

                if (skeleton.Length == 0)
                {
                    transforms = new Transform[0];
                }
                else
                {
                    transforms = AnimatorUtils.GetTransformsFromSkeleton(animatorComponent.transform, skeleton);
                }
            }

            for (int i = 0; i < transforms.Length; ++i)
            {
                bones.Add(new RigIndexToBone {Index = i, Bone = transforms[i]});
            }
        }
    }
}
