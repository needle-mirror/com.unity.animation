using System;
using System.ComponentModel;
using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation
{
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
