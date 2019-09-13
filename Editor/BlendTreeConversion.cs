using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using Unity.Entities;
using Unity.Animation.Editor;

namespace Unity.Animation.Editor
{
    public static class BlendTreeConversion
    {
        public static WeakAssetReference Convert(BlendTree blendTree)
        {
            if(blendTree.blendType == BlendTreeType.Simple1D)
                return ConvertBlendTree1D(blendTree);
            else if(blendTree.blendType == BlendTreeType.SimpleDirectional2D)
                return ConvertSimpleDirectional2DBlendTree(blendTree);
            else
                throw new System.ArgumentException($"Selected Blend Tree type is not supported.");
        }

        private static WeakAssetReference ConvertBlendTree1D(BlendTree blendTree)
        {
            var blendTree1DMotionData = new BlendTree1DMotionData[blendTree.children.Length];
            for(int i=0;i<blendTree.children.Length;i++)
            {
                blendTree1DMotionData[i].MotionThreshold = blendTree.children[i].threshold;
                blendTree1DMotionData[i].MotionSpeed = blendTree.children[i].timeScale;
                blendTree1DMotionData[i].Motion = Convert(blendTree.children[i].motion);
                blendTree1DMotionData[i].MotionType = GetMotionType(blendTree.children[i].motion);
            }

            var blendTreeBlobAsset = BlendTreeBuilder.CreateBlendTree(blendTree1DMotionData, new StringHash(blendTree.blendParameter) );
            return CreateBlobAsset(blendTree, blendTreeBlobAsset);
        }

        private static WeakAssetReference ConvertSimpleDirectional2DBlendTree(BlendTree blendTree)
        {
            var blendTree2DMotionData = new BlendTree2DMotionData[blendTree.children.Length];
            for(int i=0;i<blendTree.children.Length;i++)
            {
                blendTree2DMotionData[i].MotionPosition = blendTree.children[i].position;
                blendTree2DMotionData[i].MotionSpeed = blendTree.children[i].timeScale;
                blendTree2DMotionData[i].Motion = Convert(blendTree.children[i].motion);
                blendTree2DMotionData[i].MotionType = GetMotionType(blendTree.children[i].motion);
            }

            var blendTreeBlobAsset = BlendTreeBuilder.CreateBlendTree2DSimpleDirectionnal(blendTree2DMotionData, new StringHash(blendTree.blendParameter), new StringHash(blendTree.blendParameterY) );
            return CreateBlobAsset(blendTree, blendTreeBlobAsset);
        }

        public static WeakAssetReference Convert(Motion motion)
        {
            var animationClip = motion as AnimationClip;
            var blendTree = motion as BlendTree;

            if (blendTree != null)
                return Convert(blendTree);
            else if( animationClip != null)
            {
                var clip = ClipBuilder.AnimationClipToDenseClip(animationClip);
                return CreateBlobAsset(animationClip, clip);
            }
            else
                throw new System.ArgumentException($"Selected Motion type is not supported.");
        }

        private static WeakAssetReference CreateBlobAsset<T>(UnityEngine.Object obj, BlobAssetReference<T> blobAsset) where T : struct
        {
            var sourcePath = AssetDatabase.GetAssetPath(obj);
            if (sourcePath == null || sourcePath.Length == 0)
                throw new System.ArgumentException( $"Object '{obj.name}' is currently not managed by the AssetDatabase. Please create an asset for this object before trying to convert it.");

            string guid = AssetDatabase.AssetPathToGUID(sourcePath);

            var path = AnimationImporter.PrepareBlobAssetPath(guid);

            BlobFile.WriteBlobAsset(ref blobAsset, path);

            return new WeakAssetReference(guid);
        }

        private static MotionType GetMotionType(Motion motion)
        {
            var blendTree = motion as BlendTree;

            if (blendTree != null)
            {
                return blendTree.blendType == BlendTreeType.Simple1D ? MotionType.BlendTree1D : MotionType.BlendTree2DSimpleDirectionnal;
            }
            else
                return MotionType.Clip;
        }
    }
}
