using Unity.Entities;

namespace Unity.Animation
{
    /// <summary>
    /// This system is the base system that initializes all the components for the animation frame.
    /// </summary>
    public abstract class InitializeAnimationBase : SystemBase
    {
        EntityQuery m_ClearFrameMaskQuery;

        protected override void OnCreate()
        {
            m_ClearFrameMaskQuery = GetEntityQuery(ComponentType.ReadOnly<Rig>(), ComponentType.ReadWrite<AnimatedData>());
        }

        protected override void OnUpdate()
        {
            Dependency = new ClearMasksJob
            {
                RigType = GetComponentTypeHandle<Rig>(true),
                AnimatedDataType = GetBufferTypeHandle<AnimatedData>()
            }.ScheduleParallel(m_ClearFrameMaskQuery, 1, Dependency);
        }
    }
}
