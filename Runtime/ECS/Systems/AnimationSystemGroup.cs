using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Animation
{
    // TODO : Eventually the animation pipeline should be defined outside
    // of the core animation package as users can implement
    // their own custom ones.

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
        NotSupportedTransformHandle
        >
    {
        public struct Tag : IAnimationSystemTag { }

        public struct ReadTransformHandle : IReadTransformHandle
        {
            public Entity Entity { get; set; }
            public int Index { get; set; }
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
        public struct Tag : IAnimationSystemTag { }

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
    public class ComputeSkinMatrixSystem : ComputeSkinMatrixSystemBase
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(PostAnimationSystemGroup))]
    [UpdateAfter(typeof(ComputeSkinMatrixSystem))]
    public class PrepareSkinMatrixToRendererSystem : PrepareSkinMatrixToRendererSystemBase
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class FinalizePushSkinMatrixToRendererSystem : FinalizePushSkinMatrixToRendererSystemBase
    {
        protected override PrepareSkinMatrixToRendererSystemBase PrepareSkinMatrixToRenderSystem =>
            World.GetExistingSystem<PrepareSkinMatrixToRendererSystem>();
    }
}
