using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

using System;

namespace Unity.Animation
{
    // TODO : Eventually the animation pipeline should be defined outside
    // of the core animation package as users can implement
    // their own custom ones.

    public struct PreAnimationGraphTag : IGraphTag { }
    public struct PostAnimationGraphTag : IGraphTag { }

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

    [Obsolete("AnimationGraphSystem is obsolete, use either PreAnimationGraphSystem or PostAnimationGraphSystem and component nodes (RemovedAfter 2020-02-18)", false)]
    [ExecuteAlways]
    [UpdateInGroup(typeof(PreAnimationSystemGroup))]
    public class AnimationGraphSystem : AnimationGraphSystemBase<GraphOutput>
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(PreAnimationSystemGroup))]
    public class PreAnimationGraphSystem : GraphSystemBase<PreAnimationGraphTag>
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(PostAnimationSystemGroup))]
    public class PostAnimationGraphSystem : GraphSystemBase<PostAnimationGraphTag>
    {
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
