using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public struct AnimatedLocalTranslation : IBufferElementData
    {
        public float3 Value;
    }

    public struct AnimatedLocalRotation : IBufferElementData
    {
        public quaternion Value;
    }

    public struct AnimatedLocalScale : IBufferElementData
    {
        public float3 Value;
    }

    public struct AnimatedFloat : IBufferElementData
    {
        public float Value;
    }

    public struct AnimatedInt : IBufferElementData
    {
        public int Value;
    }

    public struct AnimatedChannelMask: IBufferElementData
    {
        public byte Value;
    }

    public struct AnimatedLocalToWorld : IBufferElementData
    {
        public float4x4 Value;
    }

    public struct AnimatedLocalToRig : IBufferElementData
    {
        public float4x4 Value;
    }
}
