using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animation
{
    [ExecuteAlways]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class InitializeAnimation : InitializeAnimationBase
    {
    }

    [ExecuteAlways]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class DefaultAnimationSystemGroup : ComponentSystemGroup
    {
    }

    [ExecuteAlways]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public class LateAnimationSystemGroup : ComponentSystemGroup
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(DefaultAnimationSystemGroup))]
    public partial class ProcessDefaultAnimationGraph : ProcessAnimationGraphBase<
        ProcessDefaultAnimationGraph.ReadTransformHandle,
        ProcessDefaultAnimationGraph.WriteTransformHandle,
        ProcessDefaultAnimationGraph.AnimatedRootMotion
    >
    {
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
        /// ProcessDefaultAnimationGraph root motion component to add in order to
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
    [UpdateInGroup(typeof(LateAnimationSystemGroup))]
    public partial class ProcessLateAnimationGraph : ProcessAnimationGraphBase<
        ProcessLateAnimationGraph.ReadTransformHandle,
        ProcessLateAnimationGraph.WriteTransformHandle
    >
    {
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
    [UpdateInGroup(typeof(LateAnimationSystemGroup))]
    [UpdateAfter(typeof(ProcessLateAnimationGraph))]
    public class ComputeRigMatrices : ComputeRigMatricesBase
    {
    }

    [ExecuteAlways]
    [UpdateInGroup(typeof(LateAnimationSystemGroup))]
    [UpdateAfter(typeof(ComputeRigMatrices))]
    public class ComputeDeformationData : ComputeDeformationDataBase
    {
    }
}
