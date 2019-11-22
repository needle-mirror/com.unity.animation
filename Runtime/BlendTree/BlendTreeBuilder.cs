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
            return BlendTreeBuilder.CreateBlendTree(blendTreeMotionData, blendTreeComponent.BlendParameter );
        }

        public static BlobAssetReference<BlendTree2DSimpleDirectionnal> CreateBlendTree2DFromComponents(BlendTree2DResource blendTreeComponent, EntityManager entityManager,
            Entity entity)
        {
            // Create blendspace
            var motionData =  entityManager.GetBuffer<BlendTree2DMotionData>(entity);
            var blendTree2DMotionData = motionData.ToNativeArray(Allocator.Temp).GetSubArray(blendTreeComponent.MotionStartIndex, blendTreeComponent.MotionCount).ToArray();
            return BlendTreeBuilder.CreateBlendTree2DSimpleDirectionnal(blendTree2DMotionData, blendTreeComponent.BlendParameterX, blendTreeComponent.BlendParameterY );
        }

        public static BlobAssetReference<BlendTree1D> CreateBlendTree(BlendTree1DMotionData[] motionData, StringHash blendParameter)
        {
            if (motionData == null)
                return default;

            var motionDataList = new List<BlendTree1DMotionData>(motionData);
            motionDataList.Sort();

            using (var blobBuilder = new BlobBuilder(Allocator.Temp))
            {

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
                    motionBuilderArray[i].Clip = motionDataList[i].Motion.Clip;
                    motionBuilderArray[i].BlendTree1D = motionDataList[i].Motion.BlendTree1D;
                    motionBuilderArray[i].BlendTree2DSimpleDirectionnal = motionDataList[i].Motion.BlendTree2DSimpleDirectionnal;
                }

                return blobBuilder.CreateBlobAssetReference<BlendTree1D>(Allocator.Persistent);
            }
        }

        public static BlobAssetReference<BlendTree2DSimpleDirectionnal> CreateBlendTree2DSimpleDirectionnal(BlendTree2DMotionData[] motionData, StringHash blendParameterX, StringHash blendParameterY)
        {
            if (motionData == null)
                return default;

            using (var blobBuilder = new BlobBuilder(Allocator.Temp))
            {
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
                    motionBuilderArray[i].Clip = motionData[i].Motion.Clip;
                    motionBuilderArray[i].BlendTree1D = motionData[i].Motion.BlendTree1D;
                    motionBuilderArray[i].BlendTree2DSimpleDirectionnal = motionData[i].Motion.BlendTree2DSimpleDirectionnal;
                }

                return blobBuilder.CreateBlobAssetReference<BlendTree2DSimpleDirectionnal>(Allocator.Persistent);
            }
        }
    }
}
