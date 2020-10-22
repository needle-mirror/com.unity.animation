using System;
using System.ComponentModel;
using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("IAnimationSystem has been renamed to IAnimationGraphSystem (RemovedAfter 2020-11-04). (UnityUpgradable) -> IAnimationGraphSystem", false)]
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

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("IAnimationSystemTag has been deprecated and is no longer necessary (RemovedAfter 2020-11-04).", false)]
    public interface IAnimationSystem<TTag> : IAnimationSystem
        where TTag : struct, IAnimationSystemTag
    {
        TTag TagComponent { get; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("AnimationSystemBase<TTag> has been deprecated, use ProcessAnimationGraphBase instead. (RemovedAfter 2020-11-04).", false)]
    public abstract class AnimationSystemBase<TTag>
        : AnimationSystemBase<TTag, NotSupportedTransformHandle, NotSupportedTransformHandle, NotSupportedRootMotion>
        where TTag : struct, IAnimationSystemTag
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle> has been deprecated, use ProcessAnimationGraphBase<TReadTransformHandle, TWriteTransformHandle> instead. (RemovedAfter 2020-11-04).", false)]
    public abstract class AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle>
        : AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle, NotSupportedRootMotion>
        where TTag : struct, IAnimationSystemTag
        where TReadTransformHandle : struct, IReadTransformHandle
        where TWriteTransformHandle : struct, IWriteTransformHandle
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion> has been deprecated, use ProcessAnimationGraphBase<TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion> instead. (RemovedAfter 2020-11-04).", false)]
    public abstract class AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion>
        : ProcessAnimationGraphBase<TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion>, IAnimationSystemTag
        where TTag : struct, IAnimationSystemTag
        where TReadTransformHandle : struct, IReadTransformHandle
        where TWriteTransformHandle : struct, IWriteTransformHandle
        where TAnimatedRootMotion : struct, IAnimatedRootMotion
    {
        public TTag TagComponent { get; } = new TTag();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE
    // API updater fails package validation check, but is still source compatible
    [Obsolete("AnimationGraphSystemBase has been deprecated. Use ProcessAnimationGraphBase instead. (RemovedAfter 2020-12-21)")]
#else
    [Obsolete("AnimationGraphSystemBase has been deprecated. Use ProcessAnimationGraphBase instead. (RemovedAfter 2020-12-21) (UnityUpgradable) -> ProcessAnimationGraphBase", true)]
#endif
    public abstract class AnimationGraphSystemBase
        : ProcessAnimationGraphBase<NotSupportedTransformHandle, NotSupportedTransformHandle, NotSupportedRootMotion>
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE
    // API updater fails package validation check, but is still source compatible
    [Obsolete("AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle> has been deprecated. Use ProcessAnimationGraphBase instead. (RemovedAfter 2020-12-21)")]
#else
    [Obsolete("AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle> has been deprecated. Use ProcessAnimationGraphBase instead. (RemovedAfter 2020-12-21) (UnityUpgradable) -> ProcessAnimationGraphBase", true)]
#endif
    public abstract class AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle>
        : AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle, NotSupportedRootMotion>
        where TReadTransformHandle : struct, IReadTransformHandle
        where TWriteTransformHandle : struct, IWriteTransformHandle
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE
    // API updater fails package validation check, but is still source compatible
    [Obsolete("AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion> has been deprecated. Use ProcessAnimationGraphBase instead. (RemovedAfter 2020-12-21)")]
#else
    [Obsolete("AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion> has been deprecated. Use ProcessAnimationGraphBase instead. (RemovedAfter 2020-12-21) (UnityUpgradable) -> ProcessAnimationGraphBase", true)]
#endif
    public abstract class AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion>
        : SystemBase, IAnimationGraphSystem
        where TReadTransformHandle : struct, IReadTransformHandle
        where TWriteTransformHandle : struct, IWriteTransformHandle
        where TAnimatedRootMotion : struct, IAnimatedRootMotion
    {
        public NodeSet Set { get; }
        public int RefCount { get; }
        public void AddRef() => throw new NotImplementedException();
        public void RemoveRef() => throw new NotImplementedException();
        public GraphHandle CreateGraph() => throw new NotImplementedException();
        public NodeHandle<T> CreateNode<T>(GraphHandle handle) where T : NodeDefinition, new() => throw new NotImplementedException();
        public NodeHandle<ComponentNode> CreateNode(GraphHandle handle, Entity entity) => throw new NotImplementedException();
        public void Dispose(GraphHandle handle) => throw new NotImplementedException();
    }
}
