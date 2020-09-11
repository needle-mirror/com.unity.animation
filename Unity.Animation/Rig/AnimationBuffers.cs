using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public struct AnimatedData : IBufferElementData
    {
        public float Value;
    }

    public struct WeightData : IBufferElementData
    {
        public float Value;
    }

    public struct AnimatedLocalToWorld : IBufferElementData
    {
        public float4x4 Value;
    }

    public struct AnimatedLocalToRoot : IBufferElementData
    {
        public float4x4 Value;
    }

    [System.Obsolete("IAnimationSystemTag has been deprecated and is no longer necessary (RemovedAfter 2020-11-04).", false)]
    public interface IAnimationSystemTag : IComponentData
    {
    }
}
