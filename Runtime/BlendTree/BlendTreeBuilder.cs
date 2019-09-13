using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation
{
    public static class BlendTreeBuilder
    {
        public static BlobAssetReference<BlendTree1D> CreateBlendTree(BlendTree1DMotionData[] motionData, StringHash blendParameter)
        {
            if (motionData == null)
                return default;

            var motionDataList = new List<BlendTree1DMotionData>(motionData);
            motionDataList.Sort();

            var blobBuilder = new BlobBuilder(Allocator.Temp);

            ref var blendTree = ref blobBuilder.ConstructRoot<BlendTree1D>();

            blendTree.BlendParameter = blendParameter;

            var length = motionDataList.Count;

            var motionThresholdBuilderArray = blobBuilder.Allocate(ref blendTree.MotionThresholds, length);
            var motionSpeedBuilderArray = blobBuilder.Allocate(ref blendTree.MotionSpeeds, length);
            var motionTypeBuilderArray = blobBuilder.Allocate(ref blendTree.MotionTypes, length);
            var motionBuilderArray = blobBuilder.Allocate(ref blendTree.Motions, length);

            for(int i=0;i<length;i++)
            {
                motionThresholdBuilderArray[i] = motionDataList[i].MotionThreshold;
                motionSpeedBuilderArray[i] = motionDataList[i].MotionSpeed;
                motionTypeBuilderArray[i] = motionDataList[i].MotionType;
                motionBuilderArray[i] = motionDataList[i].Motion;
            }

            var blendTreeAsset = blobBuilder.CreateBlobAssetReference<BlendTree1D>(Allocator.Persistent);

            blobBuilder.Dispose();

            return blendTreeAsset;
        }

        public static BlobAssetReference<BlendTree2DSimpleDirectionnal> CreateBlendTree2DSimpleDirectionnal(BlendTree2DMotionData[] motionData, StringHash blendParameterX, StringHash blendParameterY)
        {
            if (motionData == null)
                return default;

            var blobBuilder = new BlobBuilder(Allocator.Temp);

            ref var blendTree = ref blobBuilder.ConstructRoot<BlendTree2DSimpleDirectionnal>();

            blendTree.BlendParameterX = blendParameterX;
            blendTree.BlendParameterY = blendParameterY;

            var length = motionData.Length;

            var motionPositionBuilderArray = blobBuilder.Allocate(ref blendTree.MotionPositions, length);
            var motionSpeedBuilderArray = blobBuilder.Allocate(ref blendTree.MotionSpeeds, length);
            var motionTypeBuilderArray = blobBuilder.Allocate(ref blendTree.MotionTypes, length);
            var motionBuilderArray = blobBuilder.Allocate(ref blendTree.Motions, length);

            for(int i=0;i<length;i++)
            {
                motionPositionBuilderArray[i] = motionData[i].MotionPosition;
                motionSpeedBuilderArray[i] = motionData[i].MotionSpeed;
                motionTypeBuilderArray[i] = motionData[i].MotionType;
                motionBuilderArray[i] = motionData[i].Motion;
            }

            var blendTreeAsset = blobBuilder.CreateBlobAssetReference<BlendTree2DSimpleDirectionnal>(Allocator.Persistent);

            blobBuilder.Dispose();

            return blendTreeAsset;
        }
    }
}
