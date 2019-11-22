using System;
using Unity.Entities;

namespace Unity.Animation
{
    public enum MotionType
    {
        Clip,
        BlendTree1D,
        BlendTree2DSimpleDirectionnal,
    }

    public struct Motion
    {
        public BlobAssetReference<Clip>                             Clip;
        public BlobAssetReference<BlendTree1D>                      BlendTree1D;
        public BlobAssetReference<BlendTree2DSimpleDirectionnal>    BlendTree2DSimpleDirectionnal;
    }

    public struct BlendTree1DMotionData : IComparable<BlendTree1DMotionData>, IBufferElementData
    {
        public float                               MotionThreshold;
        public float                               MotionSpeed;
        public Motion                              Motion;
        public MotionType                          MotionType;

        public int CompareTo(BlendTree1DMotionData other)
        {
            return MotionThreshold.CompareTo(other.MotionThreshold);
        }

        public static bool operator >  (BlendTree1DMotionData operand1, BlendTree1DMotionData operand2)
        {
           return operand1.CompareTo(operand2) == 1;
        }

        public static bool operator <  (BlendTree1DMotionData operand1, BlendTree1DMotionData operand2)
        {
           return operand1.CompareTo(operand2) == -1;
        }

        public static bool operator >=  (BlendTree1DMotionData operand1, BlendTree1DMotionData operand2)
        {
           return operand1.CompareTo(operand2) >= 0;
        }

        public static bool operator <=  (BlendTree1DMotionData operand1, BlendTree1DMotionData operand2)
        {
           return operand1.CompareTo(operand2) <= 0;
        }
    }

    public struct BlendTree1D
    {
        public StringHash                       BlendParameter;

        public BlobArray<float>                 MotionThresholds;
        public BlobArray<float>                 MotionSpeeds;
        public BlobArray<MotionType>            MotionTypes;
        public BlobArray<Motion>                Motions;
    }
}