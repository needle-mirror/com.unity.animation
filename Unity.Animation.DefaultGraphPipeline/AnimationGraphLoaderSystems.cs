using Unity.Entities;

namespace Unity.Animation
{
    [UpdateBefore(typeof(ProcessLateAnimationGraph))]
    [UpdateInGroup(typeof(LateAnimationSystemGroup))]
    public class ProcessLateAnimationGraphLoaderSystem :
        AnimationGraphLoaderSystem<ProcessLateAnimationGraph, ProcessLateAnimationGraphLoaderSystem.Tag, ProcessLateAnimationGraphLoaderSystem.AllocatedTag>
    {
        [Phase(description: "Process Late Animation Phase")]
        public struct Tag : IComponentData {}
        public struct AllocatedTag : ISystemStateComponentData {}
    }

    [UpdateBefore(typeof(ProcessDefaultAnimationGraph))]
    [UpdateInGroup(typeof(DefaultAnimationSystemGroup))]
    public class DefaultAnimationGraphLoaderSystem :
        AnimationGraphLoaderSystem<ProcessDefaultAnimationGraph, DefaultAnimationGraphLoaderSystem.Tag, DefaultAnimationGraphLoaderSystem.AllocatedTag>
    {
        [Phase(description: "Default Animation Phase")]
        public struct Tag : IComponentData {}
        public struct AllocatedTag : ISystemStateComponentData {}
    }
}
