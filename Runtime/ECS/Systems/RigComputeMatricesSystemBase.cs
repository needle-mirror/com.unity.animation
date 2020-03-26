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
    public abstract class RigComputeMatricesSystemBase : JobComponentSystem
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_Marker = new ProfilerMarker("RigComputeMatricesSystemBase");
        static readonly ProfilerMarker k_MarkerComputeWorld = new ProfilerMarker("ComputeWorld");
        static readonly ProfilerMarker k_MarkerComputeRoot = new ProfilerMarker("ComputeRoot");
        static readonly ProfilerMarker k_MarkerComputeWorldAndRoot = new ProfilerMarker("ComputeWorldandRoot");
#endif

        private EntityQuery m_WorldSpaceOnlyQuery;
        private EntityQuery m_RootSpaceOnlyQuery;
        private EntityQuery m_WorldAndRootSpaceQuery;

        protected override void OnCreate()
        {
            m_WorldSpaceOnlyQuery = GetEntityQuery(ComputeWorldSpaceJob.QueryDesc);
            m_RootSpaceOnlyQuery = GetEntityQuery(ComputeRootSpaceJob.QueryDesc);
            m_WorldAndRootSpaceQuery = GetEntityQuery(ComputeWorldAndRootSpaceJob.QueryDesc);
        }

        protected override JobHandle OnUpdate(JobHandle dep)
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_Marker.Begin();
#endif
            JobHandle worldSpaceOnlyHandle = dep;
            if (m_WorldSpaceOnlyQuery.CalculateEntityCount() > 0)
            {
                worldSpaceOnlyHandle = new ComputeWorldSpaceJob
                {
                    Rig = GetArchetypeChunkComponentType<Rig>(true),
                    LocalToWorld = GetArchetypeChunkComponentType<LocalToWorld>(true),
                    AnimatedData = GetArchetypeChunkBufferType<AnimatedData>(true),
                    AnimatedLocalToWorld = GetArchetypeChunkBufferType<AnimatedLocalToWorld>(),

#if !UNITY_DISABLE_ANIMATION_PROFILING
                    Marker = k_MarkerComputeWorld,
#endif
                    LastSystemVersion = LastSystemVersion
                }.Schedule(m_WorldSpaceOnlyQuery, dep);
            }

            JobHandle rootSpaceOnlyHandle = dep;
            if (m_RootSpaceOnlyQuery.CalculateEntityCount() > 0)
            {
                rootSpaceOnlyHandle = new ComputeRootSpaceJob
                {
                    Rigs = GetArchetypeChunkComponentType<Rig>(true),
                    AnimatedData = GetArchetypeChunkBufferType<AnimatedData>(true),
                    LocalToRoot = GetArchetypeChunkBufferType<AnimatedLocalToRoot>(),

#if !UNITY_DISABLE_ANIMATION_PROFILING
                    Marker = k_MarkerComputeRoot,
#endif
                    LastSystemVersion = LastSystemVersion
                }.Schedule(m_RootSpaceOnlyQuery, dep);
            }

            // TODO : These jobs should ideally all run in parallel since the queries are mutually exclusive.
            //        For now, in order to prevent the safety system from throwing errors schedule
            //        WorldAndRootSpaceJob with a dependency on the two others.
            dep = JobHandle.CombineDependencies(worldSpaceOnlyHandle, rootSpaceOnlyHandle);
            if (m_WorldAndRootSpaceQuery.CalculateEntityCount() > 0)
            {
                dep = new ComputeWorldAndRootSpaceJob
                {
                    Rig = GetArchetypeChunkComponentType<Rig>(true),
                    LocalToWorld = GetArchetypeChunkComponentType<LocalToWorld>(true),
                    AnimatedData = GetArchetypeChunkBufferType<AnimatedData>(true),
                    AnimatedLocalToWorld = GetArchetypeChunkBufferType<AnimatedLocalToWorld>(),
                    AnimatedLocalToRoot = GetArchetypeChunkBufferType<AnimatedLocalToRoot>(),

#if !UNITY_DISABLE_ANIMATION_PROFILING
                    MarkerComputeRoot = k_MarkerComputeRoot,
                    MarkerComputeWorldAndRoot = k_MarkerComputeWorldAndRoot,
#endif
                    LastSystemVersion = LastSystemVersion
                }.Schedule(m_WorldAndRootSpaceQuery, dep);
            }
#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_Marker.End();
#endif

            return dep;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeWorldSpaceJob : IJobChunk
        {
            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Rig>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ReadOnly<AnimatedData>(),
                    ComponentType.ReadWrite<AnimatedLocalToWorld>()
                },
                None = new ComponentType[]
                {
                    typeof(AnimatedLocalToRoot)
                }
            };

            [ReadOnly] public ArchetypeChunkComponentType<Rig> Rig;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorld;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedData> AnimatedData;
            
            public ArchetypeChunkBufferType<AnimatedLocalToWorld> AnimatedLocalToWorld;
            public uint LastSystemVersion;

#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker Marker;
#endif

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                // TODO: Re-enable optimization once https://github.com/Unity-Technologies/dots/pull/4354 lands
                //
                // If the AnimatedData and the rig entity LocalToWorld haven't change in the last frame or
                // If both the AnimatedData and AnimatedLocalToWorld have changed, then the AnimatedLocalToWorld is already up to date.
                //if (!chunk.DidChange(AnimatedData, LastSystemVersion) && !chunk.DidChange(LocalToWorld, LastSystemVersion) ||
                //    chunk.DidChange(AnimatedData, LastSystemVersion) && chunk.DidChange(AnimatedLocalToWorld, LastSystemVersion))
                //    return;

#if !UNITY_DISABLE_ANIMATION_PROFILING
                Marker.Begin();
#endif
                var rigs = chunk.GetNativeArray(Rig);
                var localToWorlds = chunk.GetNativeArray(LocalToWorld);
                var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedData);
                var animatedLocalToWorldAccessor = chunk.GetBufferAccessor(AnimatedLocalToWorld);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var stream = AnimationStream.CreateReadOnly(rigs[i], animatedDataAccessor[i].AsNativeArray());
                    Core.ComputeLocalToWorld(localToWorlds[i].Value, ref stream, animatedLocalToWorldAccessor[i].Reinterpret<float4x4>().AsNativeArray());
                }
#if !UNITY_DISABLE_ANIMATION_PROFILING
                Marker.End();
#endif
            }
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeRootSpaceJob : IJobChunk
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
                    typeof(AnimatedLocalToWorld)
                }
            };

            [ReadOnly] public ArchetypeChunkComponentType<Rig> Rigs;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedData> AnimatedData;
            
            public ArchetypeChunkBufferType<AnimatedLocalToRoot> LocalToRoot;
            public uint LastSystemVersion;

#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker Marker;
#endif

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                // TODO: Re-enable optimization once https://github.com/Unity-Technologies/dots/pull/4354 lands
                //
                //if (!chunk.DidChange(AnimatedData, LastSystemVersion))
                //    return;
#if !UNITY_DISABLE_ANIMATION_PROFILING
                Marker.Begin();
#endif
                var rigs = chunk.GetNativeArray(Rigs);
                var data = chunk.GetBufferAccessor(AnimatedData);
                var localToRoots = chunk.GetBufferAccessor(LocalToRoot);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var stream = AnimationStream.CreateReadOnly(rigs[i], data[i].AsNativeArray());
                    Core.ComputeLocalToRoot(ref stream, localToRoots[i].Reinterpret<float4x4>().AsNativeArray());
                }
#if !UNITY_DISABLE_ANIMATION_PROFILING
                Marker.End();
#endif
            }
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeWorldAndRootSpaceJob : IJobChunk
        {
            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Rig>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ReadOnly<AnimatedData>(),
                    ComponentType.ReadWrite<AnimatedLocalToWorld>(),
                    ComponentType.ReadWrite<AnimatedLocalToRoot>()
                }
            };

            [ReadOnly] public ArchetypeChunkComponentType<Rig> Rig;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorld;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedData> AnimatedData;
            
            public ArchetypeChunkBufferType<AnimatedLocalToWorld> AnimatedLocalToWorld;
            public ArchetypeChunkBufferType<AnimatedLocalToRoot> AnimatedLocalToRoot;
            public uint LastSystemVersion;

#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker MarkerComputeWorldAndRoot;
            public ProfilerMarker MarkerComputeRoot;
#endif
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                // TODO: Re-enable optimization once https://github.com/Unity-Technologies/dots/pull/4354 lands
                //
                // If the animated data and the rig entity LocalToWorld haven't change in the last frame, do not update
                //var animatedDataDidChange = chunk.DidChange(AnimatedData, LastSystemVersion);
                //if (!animatedDataDidChange && !chunk.DidChange(LocalToWorld, LastSystemVersion))
                //    return;

                var rigs = chunk.GetNativeArray(Rig);
                var animatedLocalToRootAccessor = chunk.GetBufferAccessor(AnimatedLocalToRoot);

                // TODO: Re-enable optimization once https://github.com/Unity-Technologies/dots/pull/4354 lands
                //
                // If both the AnimatedData and AnimatedLocalToWorld have changed, then the AnimatedLocalToWorld is already up to date.
                // For first frame (LastSystemVersion == 0) force AnimatedLocalToWorld and AnimatedLocalToRoot update
                //if (LastSystemVersion != 0 && animatedDataDidChange && chunk.DidChange(AnimatedLocalToWorld, LastSystemVersion))
                //{
#if !UNITY_DISABLE_ANIMATION_PROFILING
                //    MarkerComputeRoot.Begin();
#endif
                //    var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedData);
                //    for (int i = 0; i != chunk.Count; ++i)
                //    {
                //        var stream = AnimationStream.CreateReadOnly(rigs[i], animatedDataAccessor[i].AsNativeArray());
                //        Core.ComputeLocalToRoot(ref stream, animatedLocalToRootAccessor[i].Reinterpret<float4x4>().AsNativeArray());
                //    }
#if !UNITY_DISABLE_ANIMATION_PROFILING
                //    MarkerComputeRoot.End();
#endif
                //}
                //else
                //{
#if !UNITY_DISABLE_ANIMATION_PROFILING
                    MarkerComputeWorldAndRoot.Begin();
#endif
                    var localToWorlds = chunk.GetNativeArray(LocalToWorld);
                    var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedData);
                    var animatedLocalToWorldAccessor = chunk.GetBufferAccessor(AnimatedLocalToWorld);
                    for (int i = 0; i != chunk.Count; ++i)
                    {
                        var stream = AnimationStream.CreateReadOnly(rigs[i].Value, animatedDataAccessor[i].AsNativeArray());
                        Core.ComputeLocalToWorldAndRoot(
                            localToWorlds[i].Value,
                            ref stream,
                            animatedLocalToWorldAccessor[i].Reinterpret<float4x4>().AsNativeArray(),
                            animatedLocalToRootAccessor[i].Reinterpret<float4x4>().AsNativeArray()
                            );
                    }
#if !UNITY_DISABLE_ANIMATION_PROFILING
                    MarkerComputeWorldAndRoot.End();
#endif
                //}
            }
        }
    }
}
