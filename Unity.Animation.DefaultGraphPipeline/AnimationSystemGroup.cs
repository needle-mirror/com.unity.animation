using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animation
{
    [ExecuteAlways]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class BeginFrameAnimationSystem : BeginFrameAnimationSystemBase
    {
    }

    [ExecuteAlways]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class PreAnimationSystemGroup : ComponentSystemGroup
    {
    }

    [ExecuteAlways]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public class PostAnimationSystemGroup : ComponentSystemGroup
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(PreAnimationSystemGroup))]
    public class PreAnimationGraphSystem : AnimationGraphSystemBase<
        PreAnimationGraphSystem.ReadTransformHandle,
        PreAnimationGraphSystem.WriteTransformHandle,
        PreAnimationGraphSystem.AnimatedRootMotion
    >
    {
        [System.Obsolete("PreAnimationGraphSystem.Tag is deprecated. Animation system tags are not required anymore (RemovedAfter 2020-11-04).")]
        public struct Tag : IAnimationSystemTag {}

        [System.Obsolete("PreAnimationGraphSystem.TagComponent is deprecated. Animation system tags are not required anymore (RemovedAfter 2020-11-04).")]
        public Tag TagComponent { get => new Tag(); }

        public struct ReadTransformHandle : IReadTransformHandle
        {
            public Entity Entity { get; set; }
            public int Index { get; set; }
        }

        public struct WriteTransformHandle : IWriteTransformHandle
        {
            public Entity Entity { get; set; }
            public int Index { get; set; }
        }

        /// <summary>
        /// PreAnimationGraphSystem root motion component to add in order to
        /// accumulate the delta root transform evaluated at graph rendering
        /// in the entity transform components.
        /// This works when the rig entity does not hold a DisableRootTransformReadWriteTag component.
        /// </summary>
        public struct AnimatedRootMotion : IAnimatedRootMotion
        {
            public RigidTransform Delta { get; set; }
        }
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(PostAnimationSystemGroup))]
    public class PostAnimationGraphSystem : AnimationGraphSystemBase<
        PostAnimationGraphSystem.ReadTransformHandle,
        PostAnimationGraphSystem.WriteTransformHandle
    >
    {
        [System.Obsolete("PostAnimationGraphSystem.Tag is deprecated. Animation system tags are not required anymore (RemovedAfter 2020-11-04).")]
        public struct Tag : IAnimationSystemTag {}

        [System.Obsolete("PostAnimationGraphSystem.TagComponent is deprecated. Animation system tags are not required anymore (RemovedAfter 2020-11-04).")]
        public Tag TagComponent { get => new Tag(); }

        public struct ReadTransformHandle : IReadTransformHandle
        {
            public Entity Entity { get; set; }
            public int Index { get; set; }
        }

        public struct WriteTransformHandle : IWriteTransformHandle
        {
            public Entity Entity { get; set; }
            public int Index { get; set; }
        }
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(PostAnimationSystemGroup))]
    [UpdateAfter(typeof(PostAnimationGraphSystem))]
    public class RigComputeMatricesSystem : RigComputeMatricesSystemBase
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(PostAnimationSystemGroup))]
    [UpdateAfter(typeof(RigComputeMatricesSystem))]
    public class ComputeSkinMatrixSystem : ComputeDeformationDataSystemBase
    {
    }
}
