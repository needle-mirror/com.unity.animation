using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Animation
{
    //This is a workaround for blob assets being able to store blob asset references.
    //TODO: Once blob asset references can be stored in blob assets these assets should be created at conversion time
    //and not modified at runtime
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Motion
    {
        [FieldOffset(0)]
        private long m_BlobAssetRefStorage;
        public ref Clip Value => ref UnsafeUtility.As<long, BlobAssetReference<Clip>>(ref m_BlobAssetRefStorage).Value;
        public static implicit operator Motion(BlobAssetReference<Clip> assetRef)
        {
            Motion ret = default;
            UnsafeUtility.As<long, BlobAssetReference<Clip>>(ref ret.m_BlobAssetRefStorage) = assetRef;
            return ret;
        }

        public static implicit operator BlobAssetReference<Clip>(Motion clip)
        {
            return UnsafeUtility.As<long, BlobAssetReference<Clip>>(ref clip.m_BlobAssetRefStorage);
        }
    }

    public struct BlendTree1DMotionData : IComparable<BlendTree1DMotionData>, IBufferElementData
    {
        public float                               MotionThreshold;
        public float                               MotionSpeed;
        public BlobAssetReference<Clip>            Motion;

        public int CompareTo(BlendTree1DMotionData other)
        {
            return MotionThreshold.CompareTo(other.MotionThreshold);
        }

        public static bool operator>(BlendTree1DMotionData operand1, BlendTree1DMotionData operand2)
        {
            return operand1.CompareTo(operand2) == 1;
        }

        public static bool operator<(BlendTree1DMotionData operand1, BlendTree1DMotionData operand2)
        {
            return operand1.CompareTo(operand2) == -1;
        }

        public static bool operator>=(BlendTree1DMotionData operand1, BlendTree1DMotionData operand2)
        {
            return operand1.CompareTo(operand2) >= 0;
        }

        public static bool operator<=(BlendTree1DMotionData operand1, BlendTree1DMotionData operand2)
        {
            return operand1.CompareTo(operand2) <= 0;
        }
    }

    [BurstCompatible]
    public struct BlendTree1D
    {
        public StringHash                       BlendParameter;

        public BlobArray<float>                 MotionThresholds;
        public BlobArray<float>                 MotionSpeeds;
        public BlobArray<Motion>                Motions;
    }
}
