using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    /// The root transform needs to be dealt differently than other transforms in the animation systems.
    /// By default, when the transform of the GameObject holding the RigComponent is found in the list of bones
    /// we make sure to update the root entity transform component values and the animation stream following this
    /// logic:
    /// 1) Before evaluating the animation graph, root entity transform components are copied to the animation stream.
    /// 2) Once the graph is evaluated, the new root transform values from the animation stream are copied back to the entity
    ///    transform components and the root transform values in the stream are reset to identity.
    ///
    /// Root handling is disabled when the root transform is not declared in the list of bones of the RigComponent. In this case, the tag
    /// <see cref="DisableRootTransformReadWriteTag"/> will be added to the rig entity (the entity holding
    /// the Rig and AnimatedData components) at conversion time in order to prevent systems from doing any sort of root handling.
    /// In other words, the root transform values will remain in the animation stream and not be copied back to the
    /// entity transform components.
    /// This tag can also be used to override and customize the overall behavior of root transform handling.
    ///
    /// Root motion (or accumulation of delta root transforms) is a special case in animation. This requires a AnimationSystem specific <see cref="IAnimatedRootMotion"/>
    /// component on the rig entity. When the <see cref="IAnimatedRootMotion"/> component is present, this
    /// disables default root transform handling as described above. Any root displacement evaluated in an animation graph
    /// is accumulated on the entity transform components. Once the accumulation computed, the root transform values in the animation stream are reset to identity.
    /// The major difference here is that the entity root transform component values are never copied to the animation stream prior to graph evaluation.
    /// Any root values changed in the animation stream during graph evaluation is accumulated later in the entity transform components.
    /// An optional user defined offset to root motion can be specified using <see cref="RootMotionOffset"/>


    /// <summary>
    /// IAnimatedRootMotion contains the computed delta root motion value by a given AnimationSystem.
    /// </summary>
    public interface IAnimatedRootMotion : IComponentData
    {
        RigidTransform Delta { get; set; }
    }

    /// <summary>
    /// When an AnimationSystem does not support root motion this struct can be used.
    /// </summary>
    public struct NotSupportedRootMotion : IAnimatedRootMotion
    {
        public RigidTransform Delta { get { Core.NotImplementedException(); return new RigidTransform(); } set => Core.NotImplementedException(); }
    }

    /// <summary>
    /// RootMotionOffset is an optional component that can be used to add an extra user controlled offset to
    /// the absolute root motion displacement.
    /// Once consumed, the offset is reset to identity to prevent a feedback loop in transformations.
    /// </summary>
    public struct RootMotionOffset : IComponentData
    {
        public RigidTransform Value;
    }

    /// <summary>
    /// Tag to disable reading from and writing to root entity transform components from the animation stream.
    /// </summary>
    public struct DisableRootTransformReadWriteTag : IComponentData
    {
    }
}
