using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    public abstract class RigComputeMatricesSystemBase : SystemBase
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_Marker = new ProfilerMarker("RigComputeMatricesSystemBase");
        static readonly ProfilerMarker k_MarkerComputeWorld = new ProfilerMarker("ComputeWorld");
        static readonly ProfilerMarker k_MarkerComputeRoot = new ProfilerMarker("ComputeRoot");
        static readonly ProfilerMarker k_MarkerComputeWorldAndRoot = new ProfilerMarker("ComputeWorldandRoot");
#endif

        private EntityQuery m_WorldSpaceOnlyQuery;
        private EntityQuery m_RootSpaceOnly_RootManagementEnabled_Query;
        private EntityQuery m_RootSpaceOnly_RootManagementDisabled_Query;
        private EntityQuery m_WorldAndRootSpace_RootManagementEnabled_Query;
        private EntityQuery m_WorldAndRootSpace_RootManagementDisabled_Query;

        protected override void OnCreate()
        {
            m_WorldSpaceOnlyQuery = GetEntityQuery(ComputeWorldSpaceJob.QueryDesc);

            m_RootSpaceOnly_RootManagementEnabled_Query = GetEntityQuery(ComputeRootSpaceJob_RootManagementEnabled.QueryDesc);
            m_RootSpaceOnly_RootManagementDisabled_Query = GetEntityQuery(ComputeRootSpaceJob_RootManagementDisabled.QueryDesc);

            m_WorldAndRootSpace_RootManagementEnabled_Query = GetEntityQuery(ComputeWorldAndRootSpaceJob_RootManagementEnabled.QueryDesc);
            m_WorldAndRootSpace_RootManagementDisabled_Query = GetEntityQuery(ComputeWorldAndRootSpaceJob_RootManagementDisabled.QueryDesc);
        }

        protected override void OnUpdate()
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_Marker.Begin();
#endif
            var rigTypeRO = GetComponentTypeHandle<Rig>(true);
            var rigRootEntityTypeRO = GetComponentTypeHandle<RigRootEntity>(true);
            var animatedDataTypeRO = GetBufferTypeHandle<AnimatedData>(true);

            var entityLocalToParentRO = GetComponentDataFromEntity<LocalToParent>(true);
            var entityLocalToWorldRO = GetComponentDataFromEntity<LocalToWorld>(true);

            var animatedLocalToRootType = GetBufferTypeHandle<AnimatedLocalToRoot>();
            var animatedLocalToWorldType = GetBufferTypeHandle<AnimatedLocalToWorld>();

            JobHandle worldSpaceOnlyHandle = Dependency;
            if (m_WorldSpaceOnlyQuery.CalculateEntityCount() > 0)
            {
                worldSpaceOnlyHandle = new ComputeWorldSpaceJob
                {
                    Rig = rigTypeRO,
                    RigRootEntity = rigRootEntityTypeRO,
                    EntityLocalToWorld = entityLocalToWorldRO,
                    AnimatedData = animatedDataTypeRO,
                    AnimatedLocalToWorld = animatedLocalToWorldType,

#if !UNITY_DISABLE_ANIMATION_PROFILING
                    Marker = k_MarkerComputeWorld,
#endif
                }.ScheduleParallel(m_WorldSpaceOnlyQuery, Dependency);
            }

            JobHandle rootSpaceOnlyHandle = Dependency;
            {
                if (m_RootSpaceOnly_RootManagementEnabled_Query.CalculateEntityCount() > 0)
                {
                    rootSpaceOnlyHandle = new ComputeRootSpaceJob_RootManagementEnabled
                    {
                        Rigs = rigTypeRO,
                        RigRootEntities = rigRootEntityTypeRO,
                        AnimatedData = animatedDataTypeRO,
                        EntityLocalToWorld = entityLocalToWorldRO,
                        EntityLocalToParent = entityLocalToParentRO,
                        AnimatedLocalToRoot = animatedLocalToRootType,

#if !UNITY_DISABLE_ANIMATION_PROFILING
                        Marker = k_MarkerComputeRoot
#endif
                    }.ScheduleParallel(m_RootSpaceOnly_RootManagementEnabled_Query, rootSpaceOnlyHandle);
                }

                if (m_RootSpaceOnly_RootManagementDisabled_Query.CalculateEntityCount() > 0)
                {
                    rootSpaceOnlyHandle = new ComputeRootSpaceJob_RootManagementDisabled
                    {
                        Rigs = rigTypeRO,
                        AnimatedData = animatedDataTypeRO,
                        AnimatedLocalToRoot = animatedLocalToRootType,

#if !UNITY_DISABLE_ANIMATION_PROFILING
                        Marker = k_MarkerComputeRoot
#endif
                    }.ScheduleParallel(m_RootSpaceOnly_RootManagementDisabled_Query, rootSpaceOnlyHandle);
                }
            }

            // TODO : These jobs should ideally all run in parallel since the queries are mutually exclusive.
            //        For now, in order to prevent the safety system from throwing errors schedule
            //        WorldAndRootSpaceJob with a dependency on the two others.
            Dependency = JobHandle.CombineDependencies(worldSpaceOnlyHandle, rootSpaceOnlyHandle);
            {
                if (m_WorldAndRootSpace_RootManagementEnabled_Query.CalculateEntityCount() > 0)
                {
                    Dependency = new ComputeWorldAndRootSpaceJob_RootManagementEnabled
                    {
                        Rig = rigTypeRO,
                        RigRootEntity = rigRootEntityTypeRO,
                        AnimatedData = animatedDataTypeRO,
                        EntityLocalToWorld = entityLocalToWorldRO,
                        EntityLocalToParent = entityLocalToParentRO,
                        AnimatedLocalToWorld = animatedLocalToWorldType,
                        AnimatedLocalToRoot = animatedLocalToRootType,

#if !UNITY_DISABLE_ANIMATION_PROFILING
                        MarkerComputeWorldAndRoot = k_MarkerComputeWorldAndRoot,
#endif
                    }.ScheduleParallel(m_WorldAndRootSpace_RootManagementEnabled_Query, Dependency);
                }

                if (m_WorldAndRootSpace_RootManagementDisabled_Query.CalculateEntityCount() > 0)
                {
                    Dependency = new ComputeWorldAndRootSpaceJob_RootManagementDisabled
                    {
                        Rig = rigTypeRO,
                        RigRootEntity = rigRootEntityTypeRO,
                        AnimatedData = animatedDataTypeRO,
                        EntityLocalToWorld = entityLocalToWorldRO,
                        AnimatedLocalToWorld = animatedLocalToWorldType,
                        AnimatedLocalToRoot = animatedLocalToRootType,

#if !UNITY_DISABLE_ANIMATION_PROFILING
                        MarkerComputeWorldAndRoot = k_MarkerComputeWorldAndRoot,
#endif
                    }.ScheduleParallel(m_WorldAndRootSpace_RootManagementDisabled_Query, Dependency);
                }
            }
#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_Marker.End();
#endif
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct ComputeWorldSpaceJob : IJobChunk
        {
            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Rig>(),
                    ComponentType.ReadOnly<RigRootEntity>(),
                    ComponentType.ReadOnly<AnimatedData>(),
                    ComponentType.ReadWrite<AnimatedLocalToWorld>()
                },
                None = new ComponentType[]
                {
                    typeof(AnimatedLocalToRoot)
                }
            };

            [ReadOnly] public ComponentTypeHandle<Rig> Rig;
            [ReadOnly] public ComponentTypeHandle<RigRootEntity> RigRootEntity;
            [ReadOnly] public BufferTypeHandle<AnimatedData> AnimatedData;
            [ReadOnly] public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;

            public BufferTypeHandle<AnimatedLocalToWorld> AnimatedLocalToWorld;

#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker Marker;
#endif

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
#if !UNITY_DISABLE_ANIMATION_PROFILING
                Marker.Begin();
#endif
                var rigs = chunk.GetNativeArray(Rig);
                var rigRoots = chunk.GetNativeArray(RigRootEntity);
                var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedData);
                var animatedLocalToWorldAccessor = chunk.GetBufferAccessor(AnimatedLocalToWorld);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var rootLocalToWorld = EntityLocalToWorld[rigRoots[i].Value].Value;
                    var stream = AnimationStream.CreateReadOnly(rigs[i], animatedDataAccessor[i].AsNativeArray());
                    Core.ComputeLocalToRoot(ref stream, rootLocalToWorld, animatedLocalToWorldAccessor[i].Reinterpret<float4x4>().AsNativeArray());
                }
#if !UNITY_DISABLE_ANIMATION_PROFILING
                Marker.End();
#endif
            }
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct ComputeRootSpaceJob_RootManagementEnabled : IJobChunk
        {
            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Rig>(),
                    ComponentType.ReadOnly<AnimatedData>(),
                    ComponentType.ReadWrite<AnimatedLocalToRoot>()
                },
                None = new ComponentType[]
                {
                    typeof(AnimatedLocalToWorld),
                    typeof(DisableRootTransformReadWriteTag)
                }
            };

            [ReadOnly] public ComponentTypeHandle<Rig> Rigs;
            [ReadOnly] public BufferTypeHandle<AnimatedData> AnimatedData;

            [ReadOnly] public ComponentTypeHandle<RigRootEntity>     RigRootEntities;
            [ReadOnly] public ComponentDataFromEntity<LocalToWorld>  EntityLocalToWorld;
            [ReadOnly] public ComponentDataFromEntity<LocalToParent> EntityLocalToParent;

            public BufferTypeHandle<AnimatedLocalToRoot> AnimatedLocalToRoot;

#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker Marker;
#endif

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
#if !UNITY_DISABLE_ANIMATION_PROFILING
                Marker.Begin();
#endif
                var rigs = chunk.GetNativeArray(Rigs);
                var rigRoots = chunk.GetNativeArray(RigRootEntities);
                var data = chunk.GetBufferAccessor(AnimatedData);
                var animatedLocalToRoot = chunk.GetBufferAccessor(AnimatedLocalToRoot);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    float4x4 localToParent = EntityLocalToParent.HasComponent(rigRoots[i].Value) ?
                        EntityLocalToParent[rigRoots[i].Value].Value :
                        EntityLocalToWorld[rigRoots[i].Value].Value;

                    var stream = AnimationStream.CreateReadOnly(rigs[i], data[i].AsNativeArray());
                    Core.ComputeLocalToRoot(ref stream, localToParent, animatedLocalToRoot[i].Reinterpret<float4x4>().AsNativeArray());
                }
#if !UNITY_DISABLE_ANIMATION_PROFILING
                Marker.End();
#endif
            }
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct ComputeRootSpaceJob_RootManagementDisabled : IJobChunk
        {
            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Rig>(),
                    ComponentType.ReadOnly<AnimatedData>(),
                    ComponentType.ReadWrite<AnimatedLocalToRoot>(),

                    ComponentType.ReadOnly<DisableRootTransformReadWriteTag>()
                },
                None = new ComponentType[]
                {
                    typeof(AnimatedLocalToWorld)
                }
            };

            [ReadOnly] public ComponentTypeHandle<Rig> Rigs;
            [ReadOnly] public BufferTypeHandle<AnimatedData> AnimatedData;

            public BufferTypeHandle<AnimatedLocalToRoot> AnimatedLocalToRoot;

#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker Marker;
#endif

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
#if !UNITY_DISABLE_ANIMATION_PROFILING
                Marker.Begin();
#endif
                var rigs = chunk.GetNativeArray(Rigs);
                var data = chunk.GetBufferAccessor(AnimatedData);
                var animatedLocalToRoot = chunk.GetBufferAccessor(AnimatedLocalToRoot);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var stream = AnimationStream.CreateReadOnly(rigs[i], data[i].AsNativeArray());
                    Core.ComputeLocalToRoot(ref stream, float4x4.identity, animatedLocalToRoot[i].Reinterpret<float4x4>().AsNativeArray());
                }
#if !UNITY_DISABLE_ANIMATION_PROFILING
                Marker.End();
#endif
            }
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct ComputeWorldAndRootSpaceJob_RootManagementEnabled : IJobChunk
        {
            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Rig>(),
                    ComponentType.ReadOnly<RigRootEntity>(),
                    ComponentType.ReadOnly<AnimatedData>(),
                    ComponentType.ReadWrite<AnimatedLocalToWorld>(),
                    ComponentType.ReadWrite<AnimatedLocalToRoot>()
                },
                None = new ComponentType[]
                {
                    typeof(DisableRootTransformReadWriteTag)
                }
            };

            [ReadOnly] public ComponentTypeHandle<Rig> Rig;
            [ReadOnly] public ComponentTypeHandle<RigRootEntity> RigRootEntity;
            [ReadOnly] public BufferTypeHandle<AnimatedData> AnimatedData;

            [ReadOnly] public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;
            [ReadOnly] public ComponentDataFromEntity<LocalToParent> EntityLocalToParent;

            public BufferTypeHandle<AnimatedLocalToWorld> AnimatedLocalToWorld;
            public BufferTypeHandle<AnimatedLocalToRoot> AnimatedLocalToRoot;

#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker MarkerComputeWorldAndRoot;
#endif
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigs = chunk.GetNativeArray(Rig);
                var rigRoots = chunk.GetNativeArray(RigRootEntity);
                var animatedLocalToRootAccessor = chunk.GetBufferAccessor(AnimatedLocalToRoot);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                MarkerComputeWorldAndRoot.Begin();
#endif
                var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedData);
                var animatedLocalToWorldAccessor = chunk.GetBufferAccessor(AnimatedLocalToWorld);
                for (int i = 0; i != chunk.Count; ++i)
                {
                    var rootLocalToWorld = EntityLocalToWorld[rigRoots[i].Value].Value;
                    var rootLocalToParent = EntityLocalToParent.HasComponent(rigRoots[i].Value) ?
                        EntityLocalToParent[rigRoots[i].Value].Value :
                        rootLocalToWorld;

                    var stream = AnimationStream.CreateReadOnly(rigs[i].Value, animatedDataAccessor[i].AsNativeArray());
                    Core.ComputeLocalToRoot(
                        ref stream,
                        rootLocalToParent,
                        animatedLocalToRootAccessor[i].Reinterpret<float4x4>().AsNativeArray(),
                        rootLocalToWorld,
                        animatedLocalToWorldAccessor[i].Reinterpret<float4x4>().AsNativeArray()
                    );
                }
#if !UNITY_DISABLE_ANIMATION_PROFILING
                MarkerComputeWorldAndRoot.End();
#endif
            }
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct ComputeWorldAndRootSpaceJob_RootManagementDisabled : IJobChunk
        {
            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Rig>(),
                    ComponentType.ReadOnly<RigRootEntity>(),
                    ComponentType.ReadOnly<AnimatedData>(),
                    ComponentType.ReadWrite<AnimatedLocalToWorld>(),
                    ComponentType.ReadWrite<AnimatedLocalToRoot>(),

                    ComponentType.ReadOnly<DisableRootTransformReadWriteTag>()
                }
            };

            [ReadOnly] public ComponentTypeHandle<Rig> Rig;
            [ReadOnly] public ComponentTypeHandle<RigRootEntity> RigRootEntity;
            [ReadOnly] public BufferTypeHandle<AnimatedData> AnimatedData;

            [ReadOnly] public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;

            public BufferTypeHandle<AnimatedLocalToWorld> AnimatedLocalToWorld;
            public BufferTypeHandle<AnimatedLocalToRoot> AnimatedLocalToRoot;

#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker MarkerComputeWorldAndRoot;
#endif
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigs = chunk.GetNativeArray(Rig);
                var rigRoots = chunk.GetNativeArray(RigRootEntity);
                var animatedLocalToRootAccessor = chunk.GetBufferAccessor(AnimatedLocalToRoot);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                MarkerComputeWorldAndRoot.Begin();
#endif
                var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedData);
                var animatedLocalToWorldAccessor = chunk.GetBufferAccessor(AnimatedLocalToWorld);
                for (int i = 0; i != chunk.Count; ++i)
                {
                    var rootLocalToWorld = EntityLocalToWorld[rigRoots[i].Value].Value;
                    var stream = AnimationStream.CreateReadOnly(rigs[i].Value, animatedDataAccessor[i].AsNativeArray());
                    Core.ComputeLocalToRoot(
                        ref stream,
                        float4x4.identity,
                        animatedLocalToRootAccessor[i].Reinterpret<float4x4>().AsNativeArray(),
                        rootLocalToWorld,
                        animatedLocalToWorldAccessor[i].Reinterpret<float4x4>().AsNativeArray()
                    );
                }

#if !UNITY_DISABLE_ANIMATION_PROFILING
                MarkerComputeWorldAndRoot.End();
#endif
            }
        }

#if !UNITY_ENTITIES_0_12_OR_NEWER
        ComponentTypeHandle<T> GetComponentTypeHandle<T>(bool readOnly = false) where T : struct, IComponentData => new ComponentTypeHandle<T> { Value = GetArchetypeChunkComponentType<T>(readOnly) };
        BufferTypeHandle<T> GetBufferTypeHandle<T>(bool readOnly = false) where T : struct, IBufferElementData => new BufferTypeHandle<T>() {Value = GetArchetypeChunkBufferType<T>(readOnly)};
#endif
    }
}
