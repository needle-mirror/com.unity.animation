using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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

    public interface ITransformHandle : IBufferElementData
    {
        Entity Entity { get; set; }
        int Index { get; set; }
    }

    public interface IReadTransformHandle : ITransformHandle { }

    public interface IWriteTransformHandle : ITransformHandle { }

    [WriteGroup(typeof(LocalToParent))]
    [WriteGroup(typeof(LocalToWorld))]
    public struct AnimationTransformOverride : IComponentData { }
}
