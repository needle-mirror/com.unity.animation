using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animation
{
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
    public class PreAnimationGraphSystem : AnimationSystemBase<
        PreAnimationGraphSystem.Tag,
        PreAnimationGraphSystem.ReadTransformHandle,
        PreAnimationGraphSystem.WriteTransformHandle,
        PreAnimationGraphSystem.AnimatedRootMotion
    >
    {
        public struct Tag : IAnimationSystemTag {}

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
    public class PostAnimationGraphSystem : AnimationSystemBase<
        PostAnimationGraphSystem.Tag,
        PostAnimationGraphSystem.ReadTransformHandle,
        PostAnimationGraphSystem.WriteTransformHandle
    >
    {
        public struct Tag : IAnimationSystemTag {}

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

#if !UNITY_ENTITIES_0_12_OR_NEWER
    [ExecuteAlways]
    [UpdateInGroup(typeof(PostAnimationSystemGroup))]
    [UpdateAfter(typeof(ComputeSkinMatrixSystem))]
    internal class PrepareSkinMatrixToRendererSystem : PrepareSkinMatrixToRendererSystemBase
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    internal class FinalizePushSkinMatrixToRendererSystem : FinalizePushSkinMatrixToRendererSystemBase
    {
        protected override PrepareSkinMatrixToRendererSystemBase PrepareSkinMatrixToRenderSystem =>
            World.GetExistingSystem<PrepareSkinMatrixToRendererSystem>();
    }
#endif
}
