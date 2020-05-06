using System;
using Unity.Entities;
using Unity.Transforms;

namespace Unity.Animation
{
    public interface ITransformHandle : IBufferElementData
    {
        Entity Entity { get; set; }
        int Index { get; set; }
    }

    public interface IReadTransformHandle : ITransformHandle {}

    public interface IWriteTransformHandle : ITransformHandle {}

    [WriteGroup(typeof(LocalToParent))]
    [WriteGroup(typeof(LocalToWorld))]
    public struct AnimationTransformOverride : IComponentData {}

    public struct NotSupportedTransformHandle : IReadTransformHandle, IWriteTransformHandle
    {
        public Entity Entity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Index { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
