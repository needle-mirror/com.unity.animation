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

    /// <summary>
    /// Prevents the transform systems to write into the LocalToParent component of an entity.
    /// </summary>
    /// <remarks>
    /// When adding a write exposed transform to a rig, this component is added
    /// to the entity of the exposed transform to prevent the LocalToParent component
    /// to be overriden by the transform systems.
    /// The LocalToParent component is updated by the the animation systems.
    /// </remarks>
    [WriteGroup(typeof(LocalToParent))]
    public struct AnimationLocalToParentOverride : IComponentData {}

    /// <summary>
    /// Prevents the transform systems to write into the LocalToWorld component of an entity.
    /// </summary>
    /// <remarks>
    /// When adding a write exposed transform to a rig, this component is added
    /// to the entity of the exposed transform to prevent the LocalToWorld component
    /// to be overriden by the transform systems.
    /// The LocalToWorld component is updated by the the animation systems.
    /// </remarks>
    [WriteGroup(typeof(LocalToWorld))]
    public struct AnimationLocalToWorldOverride : IComponentData {}

    public struct NotSupportedTransformHandle : IReadTransformHandle, IWriteTransformHandle
    {
        public Entity Entity { get { Core.NotImplementedException(); return Entity.Null; } set => Core.NotImplementedException(); }
        public int Index { get { Core.NotImplementedException(); return 0; } set => Core.NotImplementedException(); }
    }
}
