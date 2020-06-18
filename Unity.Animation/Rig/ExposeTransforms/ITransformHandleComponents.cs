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
    /// Prevents the transform systems to write into the LocalToParent and LocalToWorld
    /// components of an entity.
    /// </summary>
    /// <remarks>
    /// This component won't work because if a query defines LocalToParent as ReadOnly and LocalToWorld as ReadWrite,
    /// then AnimationTransformOverride will be in the Any field of the query and not the None field,
    /// so the entity will still be selected.
    /// </remarks>
    [Obsolete("AnimationTransformOverride has been deprecated. Use AnimationLocalToParentOverride" +
        "and AnimationLocalToWorldOverride instead. (RemovedAfter 2020-08-26)")]
    [WriteGroup(typeof(LocalToParent))]
    [WriteGroup(typeof(LocalToWorld))]
    public struct AnimationTransformOverride : IComponentData {}

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
        public Entity Entity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Index { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
