using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation
{
    [System.Obsolete("IAnimationSystem has been renamed to IAnimationGraphSystem (RemovedAfter 2020-11-04). (UnityUpgradable) -> IAnimationGraphSystem", false)]
    public interface IAnimationSystem
    {
        NodeSet Set { get; }
        int RefCount { get; }
        void AddRef();
        void RemoveRef();

        GraphHandle CreateGraph();
        NodeHandle<T> CreateNode<T>(GraphHandle graph) where T : NodeDefinition, new();
        NodeHandle<ComponentNode> CreateNode(GraphHandle graph, Entity entity);
        void Dispose(GraphHandle graph);
    }

    [System.Obsolete("IAnimationSystemTag has been deprecated and is no longer necessary (RemovedAfter 2020-11-04).", false)]
    public interface IAnimationSystem<TTag> : IAnimationSystem
        where TTag : struct, IAnimationSystemTag
    {
        TTag TagComponent { get; }
    }

    [System.Obsolete("AnimationSystemBase<TTag> has been deprecated, use AnimationGraphSystemBase instead. (RemovedAfter 2020-11-04).", false)]
    public abstract class AnimationSystemBase<TTag>
        : AnimationSystemBase<TTag, NotSupportedTransformHandle, NotSupportedTransformHandle, NotSupportedRootMotion>
        where TTag : struct, IAnimationSystemTag
    {
    }

    [System.Obsolete("AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle> has been deprecated, use AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle> instead. (RemovedAfter 2020-11-04).", false)]
    public abstract class AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle>
        : AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle, NotSupportedRootMotion>
        where TTag : struct, IAnimationSystemTag
        where TReadTransformHandle : struct, IReadTransformHandle
        where TWriteTransformHandle : struct, IWriteTransformHandle
    {
    }

    [System.Obsolete("AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion> has been deprecated, use AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion> instead. (RemovedAfter 2020-11-04).", false)]
    public abstract class AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion>
        : AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion>, IAnimationSystemTag
        where TTag : struct, IAnimationSystemTag
        where TReadTransformHandle : struct, IReadTransformHandle
        where TWriteTransformHandle : struct, IWriteTransformHandle
        where TAnimatedRootMotion : struct, IAnimatedRootMotion
    {
        public TTag TagComponent { get; } = new TTag();
    }
}
