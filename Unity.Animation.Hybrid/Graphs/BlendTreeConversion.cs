#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.Animations;
using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    public struct BakeOptions
    {
        public BlobAssetReference<RigDefinition>   RigDefinition;
        public ClipConfiguration                   ClipConfiguration;
        public float                               SampleRate;

        public bool NeedBaking => RigDefinition.IsCreated && ClipConfiguration.Mask != 0 && SampleRate > 0;
    }

    public static class BlendTreeConversion
    {
        public static int Convert(BlendTree blendTree, Entity entity, EntityManager entityManager, BakeOptions bakeOptions = default)
        {
            AssertBlendTreeIsNotNested(blendTree);

            ValidateBakeOptions(bakeOptions);

            if (blendTree.blendType == BlendTreeType.Simple1D)
                return ConvertBlendTree1D(blendTree, entity, entityManager, bakeOptions);
            else if (blendTree.blendType == BlendTreeType.SimpleDirectional2D)
                return ConvertSimpleDirectional2DBlendTree(blendTree, entity, entityManager, bakeOptions);
            else
                throw new System.ArgumentException($"Selected Blend Tree type is not supported.");
        }

        private static int ConvertBlendTree1D(BlendTree blendTree, Entity entity, EntityManager entityManager, BakeOptions bakeOptions)
        {
            if (!entityManager.HasComponent<BlendTree1DResource>(entity))
                entityManager.AddBuffer<BlendTree1DResource>(entity);

            if (!entityManager.HasComponent<BlendTree1DMotionData>(entity))
                entityManager.AddBuffer<BlendTree1DMotionData>(entity);

            var blendTreeResources = entityManager.GetBuffer<BlendTree1DResource>(entity);
            var blendTreeMotionData = entityManager.GetBuffer<BlendTree1DMotionData>(entity);

            var blendTreeIndex = blendTreeResources.Length;

            blendTreeResources.Add(new BlendTree1DResource
            {
                MotionCount = blendTree.children.Length,
                MotionStartIndex = blendTreeMotionData.Length
            });

            for (int i = 0; i < blendTree.children.Length; i++)
            {
                var motionData = new BlendTree1DMotionData
                {
                    MotionThreshold = blendTree.children[i].threshold,
                    MotionSpeed = blendTree.children[i].timeScale,
                };

                var clip = (blendTree.children[i].motion as AnimationClip).ToDenseClip();
                if (bakeOptions.NeedBaking)
                {
                    clip = UberClipNode.Bake(bakeOptions.RigDefinition, clip, bakeOptions.ClipConfiguration, bakeOptions.SampleRate);
                }

                motionData.Motion = clip;

                blendTreeMotionData.Add(motionData);
            }

            return blendTreeIndex;
        }

        private static int ConvertSimpleDirectional2DBlendTree(BlendTree blendTree, Entity entity, EntityManager entityManager, BakeOptions bakeOptions)
        {
            if (!entityManager.HasComponent<BlendTree2DResource>(entity))
                entityManager.AddBuffer<BlendTree2DResource>(entity);

            if (!entityManager.HasComponent<BlendTree2DMotionData>(entity))
                entityManager.AddBuffer<BlendTree2DMotionData>(entity);

            var blendTreeResources = entityManager.GetBuffer<BlendTree2DResource>(entity);
            var blendTreeMotionData = entityManager.GetBuffer<BlendTree2DMotionData>(entity);

            var blendTreeIndex = blendTreeResources.Length;

            blendTreeResources.Add(new BlendTree2DResource
            {
                MotionCount = blendTree.children.Length,
                MotionStartIndex = blendTreeMotionData.Length
            });

            for (int i = 0; i < blendTree.children.Length; i++)
            {
                var motionData = new BlendTree2DMotionData
                {
                    MotionPosition = blendTree.children[i].position,
                    MotionSpeed = blendTree.children[i].timeScale,
                };

                var clip = (blendTree.children[i].motion as AnimationClip).ToDenseClip();
                if (bakeOptions.NeedBaking)
                {
                    clip = UberClipNode.Bake(bakeOptions.RigDefinition, clip, bakeOptions.ClipConfiguration, bakeOptions.SampleRate);
                }

                motionData.Motion = clip;

                blendTreeMotionData.Add(motionData);
            }

            return blendTreeIndex;
        }

        private static void AssertBlendTreeIsNotNested(BlendTree blendTree)
        {
            for (int i = 0; i < blendTree.children.Length; i++)
            {
                var nestedBlendTree = blendTree.children[i].motion as BlendTree;
                if (nestedBlendTree != null)
                    throw new System.NotSupportedException($"BlendTree:{blendTree.name} have nested blendtree:{nestedBlendTree.name}.");
            }
        }

        private static void ValidateBakeOptions(BakeOptions bakeOptions)
        {
            if (bakeOptions.RigDefinition == BlobAssetReference<RigDefinition>.Null ||
                bakeOptions.ClipConfiguration.Mask == 0 ||
                bakeOptions.SampleRate == 0)
            {
                return;
            }

            if (bakeOptions.SampleRate < 0)
                throw new System.ArgumentOutOfRangeException("bakeOptions.SampleRate", bakeOptions.SampleRate, "SampleRate cannot be negative.");
        }
    }
}
#endif
