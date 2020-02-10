using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Profiling;

namespace Unity.Animation
{
    public abstract class RigComputeMatricesSystemBase : JobComponentSystem
    {
        private EntityQuery m_GlobalSpaceOnlyQuery;
        private EntityQuery m_RigSpaceOnlyQuery;
        private EntityQuery m_GlobalAndRigSpaceQuery;

        static readonly ProfilerMarker k_Marker = new ProfilerMarker("RigComputeMatricesSystemBase");

        protected override void OnCreate()
        {
            m_GlobalSpaceOnlyQuery = GetEntityQuery(
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadOnly<AnimatedData>(),
                
                ComponentType.ReadWrite<AnimatedLocalToWorld>(),
                ComponentType.Exclude<AnimatedLocalToRoot>()
                );

            m_RigSpaceOnlyQuery = GetEntityQuery(
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadOnly<AnimatedData>(),
                
                ComponentType.Exclude<AnimatedLocalToWorld>(),
                ComponentType.ReadWrite<AnimatedLocalToRoot>()
                );

            m_GlobalAndRigSpaceQuery = GetEntityQuery(
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadOnly<AnimatedData>(),
                
                ComponentType.ReadWrite<AnimatedLocalToWorld>(),
                ComponentType.ReadWrite<AnimatedLocalToRoot>()
                );
        }

        protected override JobHandle OnUpdate(JobHandle dep)
        {
            k_Marker.Begin();

            var globalSpaceOnlyJob = new ComputeGlobalSpaceJob
            {
                LocalToWorlds = GetArchetypeChunkComponentType<LocalToWorld>(true),
                Rigs = GetArchetypeChunkComponentType<Rig>(true),
                Floats  = GetArchetypeChunkBufferType<AnimatedData>(true),
                LocalToWorld = GetArchetypeChunkBufferType<AnimatedLocalToWorld>()
            };
            var globalSpaceOnlyHandle = globalSpaceOnlyJob.Schedule(m_GlobalSpaceOnlyQuery, dep);

            var rigSpaceOnlyJob = new ComputeRigSpaceJob
            {
                Rigs = GetArchetypeChunkComponentType<Rig>(true),
                Floats = GetArchetypeChunkBufferType<AnimatedData>(true),
                LocalToRoot = GetArchetypeChunkBufferType<AnimatedLocalToRoot>()
            };
            var rigSpaceOnlyHandle = rigSpaceOnlyJob.Schedule(m_RigSpaceOnlyQuery, dep);

            var globalAndRigSpaceJob = new ComputeGlobalAndRigSpaceJob
            {
                LocalToWorlds = GetArchetypeChunkComponentType<LocalToWorld>(true),

                Rigs = GetArchetypeChunkComponentType<Rig>(true),
                Floats = GetArchetypeChunkBufferType<AnimatedData>(true),
                LocalToWorld = GetArchetypeChunkBufferType<AnimatedLocalToWorld>(),
                LocalToRoot = GetArchetypeChunkBufferType<AnimatedLocalToRoot>()
            };

            // TODO : These jobs should ideally all run in parallel since the queries are mutually exclusive.
            //        For now, in order to prevent the safety system from throwing errors schedule
            //        globalAndRigSpaceJob with a dependency on the two others.
            var inputDep = globalAndRigSpaceJob.Schedule(
                m_GlobalAndRigSpaceQuery,
                JobHandle.CombineDependencies(globalSpaceOnlyHandle, rigSpaceOnlyHandle)
                );

            k_Marker.End();

            return inputDep;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeGlobalSpaceJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorlds;

            [ReadOnly] public ArchetypeChunkComponentType<Rig> Rigs;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedData> Floats;
            
            public ArchetypeChunkBufferType<AnimatedLocalToWorld> LocalToWorld;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var eLocalToWorlds = chunk.GetNativeArray(LocalToWorlds);

                var rigs = chunk.GetNativeArray(Rigs);
                var lFloats = chunk.GetBufferAccessor(Floats);
                var localToWorlds = chunk.GetBufferAccessor(LocalToWorld);

                var entityCount = chunk.Count;
                for (int i = 0; i != entityCount; ++i)
                {
                    var stream = AnimationStream.CreateReadOnly(
                        rigs[i].Value,
                        lFloats[i].AsNativeArray()
                        );

                    Core.ComputeLocalToWorld(eLocalToWorlds[i].Value, ref stream, localToWorlds[i].Reinterpret<float4x4>().AsNativeArray());
                }
            }
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeRigSpaceJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<Rig> Rigs;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedData> Floats;
            
            public ArchetypeChunkBufferType<AnimatedLocalToRoot> LocalToRoot;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigs = chunk.GetNativeArray(Rigs);
                var lFloats = chunk.GetBufferAccessor(Floats);
                var localToRoots = chunk.GetBufferAccessor(LocalToRoot);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var stream = AnimationStream.CreateReadOnly(
                        rigs[i].Value,
                        lFloats[i].AsNativeArray()
                        );

                    Core.ComputeLocalToRoot(ref stream, localToRoots[i].Reinterpret<float4x4>().AsNativeArray());
                }
            }
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ComputeGlobalAndRigSpaceJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorlds;

            [ReadOnly] public ArchetypeChunkComponentType<Rig> Rigs;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedData> Floats;
            
            public ArchetypeChunkBufferType<AnimatedLocalToWorld> LocalToWorld;
            public ArchetypeChunkBufferType<AnimatedLocalToRoot> LocalToRoot;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var eLocalToWorlds = chunk.GetNativeArray(LocalToWorlds);

                var rigs = chunk.GetNativeArray(Rigs);
                var lFloats = chunk.GetBufferAccessor(Floats);
                var localToWorlds = chunk.GetBufferAccessor(LocalToWorld);
                var localToRoots = chunk.GetBufferAccessor(LocalToRoot);

                var entityCount = chunk.Count;
                for (int i = 0; i != entityCount; ++i)
                {
                    var stream = AnimationStream.CreateReadOnly(
                        rigs[i].Value,
                        lFloats[i].AsNativeArray()
                        );

                    Core.ComputeLocalToWorldAndRoot(eLocalToWorlds[i].Value, ref stream, localToWorlds[i].Reinterpret<float4x4>().AsNativeArray(), localToRoots[i].Reinterpret<float4x4>().AsNativeArray());
                }
            }
        }
    }
}
