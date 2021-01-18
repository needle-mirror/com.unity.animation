using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation
{
    public static class BlendTreeBuilder
    {
        public static BlobAssetReference<BlendTree1D> CreateBlendTree1DFromComponents(BlendTree1DResource blendTreeComponent, EntityManager entityManager, Entity entity)
        {
            var motionData =  entityManager.GetBuffer<BlendTree1DMotionData>(entity);
            var blendTreeMotionData = motionData.ToNativeArray(Allocator.Temp).GetSubArray(blendTreeComponent.MotionStartIndex, blendTreeComponent.MotionCount).ToArray();
            return BlendTreeBuilder.CreateBlendTree(blendTreeMotionData);
        }

        public static BlobAssetReference<BlendTree2DSimpleDirectional> CreateBlendTree2DFromComponents(BlendTree2DResource blendTreeComponent, EntityManager entityManager,
            Entity entity)
        {
            // Create blendspace
            var motionData =  entityManager.GetBuffer<BlendTree2DMotionData>(entity);
            var blendTree2DMotionData = motionData.ToNativeArray(Allocator.Temp).GetSubArray(blendTreeComponent.MotionStartIndex, blendTreeComponent.MotionCount).ToArray();
            return BlendTreeBuilder.CreateBlendTree2DSimpleDirectional(blendTree2DMotionData);
        }

        public static BlobAssetReference<BlendTree1D> CreateBlendTree(BlendTree1DMotionData[] motionData)
        {
            if (motionData == null)
                return default;

            var motionDataList = new List<BlendTree1DMotionData>(motionData);
            motionDataList.Sort();

            using (var blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                ref var blendTree = ref blobBuilder.ConstructRoot<BlendTree1D>();

                var length = motionDataList.Count;

                var motionThresholdBuilderArray = blobBuilder.Allocate(ref blendTree.MotionThresholds, length);
                var motionSpeedBuilderArray = blobBuilder.Allocate(ref blendTree.MotionSpeeds, length);
                var motionBuilderArray = blobBuilder.Allocate(ref blendTree.Motions, length);

                for (int i = 0; i < length; i++)
                {
                    motionThresholdBuilderArray[i] = motionDataList[i].MotionThreshold;
                    motionSpeedBuilderArray[i] = motionDataList[i].MotionSpeed;
                    motionBuilderArray[i] = motionDataList[i].Motion;
                }

                return blobBuilder.CreateBlobAssetReference<BlendTree1D>(Allocator.Persistent);
            }
        }

        public static BlobAssetReference<BlendTree2DSimpleDirectional> CreateBlendTree2DSimpleDirectional(BlendTree2DMotionData[] motionData)
        {
            if (motionData == null)
                return default;

            using (var blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                ref var blendTree = ref blobBuilder.ConstructRoot<BlendTree2DSimpleDirectional>();

                var length = motionData.Length;

                var motionPositionBuilderArray = blobBuilder.Allocate(ref blendTree.MotionPositions, length);
                var motionSpeedBuilderArray = blobBuilder.Allocate(ref blendTree.MotionSpeeds, length);
                var motionBuilderArray = blobBuilder.Allocate(ref blendTree.Motions, length);

                for (int i = 0; i < length; i++)
                {
                    motionPositionBuilderArray[i] = motionData[i].MotionPosition;
                    motionSpeedBuilderArray[i] = motionData[i].MotionSpeed;
                    motionBuilderArray[i] = motionData[i].Motion;
                }

                return blobBuilder.CreateBlobAssetReference<BlendTree2DSimpleDirectional>(Allocator.Persistent);
            }
        }
    }
}
