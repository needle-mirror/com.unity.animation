using Unity.Entities;
using UnityEngine;

namespace Unity.Animation
{
    // TODO : Eventually the animation pipeline should be defined outside
    // of the core animation package as users can implement
    // their own custom ones.

    [ExecuteAlways]
    public class AnimationSystemGroup : ComponentSystemGroup
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    public class AnimationGraphSystem : AnimationGraphSystemBase<GraphOutput>
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    [UpdateAfter(typeof(AnimationGraphSystem))]
    public class RigComputeMatricesSystem : RigComputeMatricesSystemBase
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    [UpdateAfter(typeof(RigComputeMatricesSystem))]
    public class ComputeSkinMatrixSystem : ComputeSkinMatrixSystemBase
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(AnimationSystemGroup))]
    [UpdateAfter(typeof(ComputeSkinMatrixSystem))]
    public class PushSkinMatrixToRendererSystem : PushSkinMatrixToRendererSystemBase
    {
    }
}
