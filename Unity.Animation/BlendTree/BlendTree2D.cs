using System;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public enum BlendTree2DType
    {
        SimpleDirectionnal2D,
        FreeformDirectionnal2D,
        FreeformCartesian2D
    }

    public struct BlendTree2DMotionData : IBufferElementData
    {
        public float2                              MotionPosition;
        public float                               MotionSpeed;
        public BlobAssetReference<Clip>            Motion;
    }

    [BurstCompatible]
    public struct BlendTree2DSimpleDirectional
    {
        public StringHash                       BlendParameterX;
        public StringHash                       BlendParameterY;

        public BlobArray<float2>                MotionPositions;
        public BlobArray<float>                 MotionSpeeds;
        public BlobArray<Motion>                Motions;
    }
}
