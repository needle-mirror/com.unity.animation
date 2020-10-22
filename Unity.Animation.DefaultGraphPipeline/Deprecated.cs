using System;
using System.ComponentModel;
using Unity.Animation;
using Unity.Entities;
using Unity.Mathematics;

#pragma warning disable 618
[assembly: RegisterGenericJobType(typeof(SortReadTransformComponentJob<PreAnimationGraphSystem.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadTransformComponentJob<PreAnimationGraphSystem.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadRootTransformJob<PreAnimationGraphSystem.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(UpdateRootRemapMatrixJob<PreAnimationGraphSystem.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(WriteRootTransformJob<PreAnimationGraphSystem.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(AccumulateRootTransformJob<PreAnimationGraphSystem.AnimatedRootMotion>))]
[assembly: RegisterGenericJobType(typeof(WriteTransformComponentJob<PreAnimationGraphSystem.WriteTransformHandle>))]

[assembly: RegisterGenericJobType(typeof(SortReadTransformComponentJob<PostAnimationGraphSystem.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(ReadTransformComponentJob<PostAnimationGraphSystem.ReadTransformHandle>))]
[assembly: RegisterGenericJobType(typeof(WriteTransformComponentJob<PostAnimationGraphSystem.WriteTransformHandle>))]
#pragma warning restore 618

namespace Unity.Animation
{
    [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE
    // API updater fails package validation check, but is still source compatible
    [Obsolete("Renamed to InitializeAnimation (RemovedAfter 2020-12-21)")]
#else
    [Obsolete("Renamed to InitializeAnimation (RemovedAfter 2020-12-21) (UnityUpgradable) -> InitializeAnimation", true)]
#endif
    [DisableAutoCreation]
    public class BeginFrameAnimationSystem : BeginFrameAnimationSystemBase
    {
        protected override void OnUpdate() => throw new NotImplementedException();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to InitializeAnimationBase (RemovedAfter 2020-12-21) (UnityUpgradable) -> InitializeAnimationBase", true)]
    public abstract class BeginFrameAnimationSystemBase : SystemBase
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to ComputeDeformationDataBase (RemovedAfter 2020-12-21) (UnityUpgradable) -> ComputeDeformationDataBase", true)]
    public abstract class ComputeDeformationDataSystemBase : SystemBase
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE
    // API updater fails package validation check, but is still source compatible
    [Obsolete("Renamed to ComputeDeformationData (RemovedAfter 2020-12-21)")]
#else
    [Obsolete("Renamed to ComputeDeformationData (RemovedAfter 2020-12-21) (UnityUpgradable) -> ComputeDeformationData", true)]
#endif
    [DisableAutoCreation]
    public class ComputeSkinMatrixSystem : ComputeDeformationDataSystemBase
    {
        protected override void OnUpdate() => throw new NotImplementedException();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to DefaultAnimationSystemGroup (RemovedAfter 2020-12-21) (UnityUpgradable) -> DefaultAnimationSystemGroup", true)]
    [DisableAutoCreation]
    public class PreAnimationSystemGroup : ComponentSystemGroup
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to ProcessDefaultAnimationGraph (RemovedAfter 2020-12-21) (UnityUpgradable) -> ProcessDefaultAnimationGraph")]
    [DisableAutoCreation]
    public class PreAnimationGraphSystem : ProcessAnimationGraphBase<
        PreAnimationGraphSystem.ReadTransformHandle,
        PreAnimationGraphSystem.WriteTransformHandle,
        PreAnimationGraphSystem.AnimatedRootMotion
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

        public struct AnimatedRootMotion : IAnimatedRootMotion
        {
            public RigidTransform Delta { get; set; }
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to ProcessLateAnimationGraph (RemovedAfter 2020-12-21) (UnityUpgradable) -> ProcessLateAnimationGraph")]
    [DisableAutoCreation]
    public class PostAnimationGraphSystem : ProcessAnimationGraphBase<
        PostAnimationGraphSystem.ReadTransformHandle,
        PostAnimationGraphSystem.WriteTransformHandle
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

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to LateAnimationSystemGroup (RemovedAfter 2020-12-21) (UnityUpgradable) -> LateAnimationSystemGroup", true)]
    [DisableAutoCreation]
    public class PostAnimationSystemGroup : ComponentSystemGroup
    {
    }

    public partial class ProcessDefaultAnimationGraph
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ProcessDefaultAnimationGraph.Tag is deprecated. Animation system tags are not required anymore (RemovedAfter 2020-11-04).")]
        public struct Tag : IAnimationSystemTag {}

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ProcessDefaultAnimationGraph.TagComponent is deprecated. Animation system tags are not required anymore (RemovedAfter 2020-11-04).")]
        public Tag TagComponent => new Tag();
    }

    public partial class ProcessLateAnimationGraph
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ProcessLateAnimationGraph.Tag is deprecated. Animation system tags are not required anymore (RemovedAfter 2020-11-04).")]
        public struct Tag : IAnimationSystemTag {}

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("ProcessLateAnimationGraph.TagComponent is deprecated. Animation system tags are not required anymore (RemovedAfter 2020-11-04).")]
        public Tag TagComponent => new Tag();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
#if UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE
    // API updater fails package validation check, but is still source compatible
    [Obsolete("Renamed to ComputeRigMatrices (RemovedAfter 2020-12-21)")]
#else
    [Obsolete("Renamed to ComputeRigMatrices (RemovedAfter 2020-12-21) (UnityUpgradable) -> ComputeRigMatrices", true)]
#endif
    [DisableAutoCreation]
    public class RigComputeMatricesSystem : RigComputeMatricesSystemBase
    {
        protected override void OnUpdate() => throw new NotImplementedException();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to ComputeRigMatricesBase (RemovedAfter 2020-12-21) (UnityUpgradable) -> ComputeRigMatricesBase", true)]
    public abstract class RigComputeMatricesSystemBase : SystemBase
    {
    }
}
