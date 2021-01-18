using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;

namespace Unity.Animation
{
    public abstract class ComputeRigMatricesBase : SystemBase
    {
        private EntityQuery m_WorldSpaceOnlyQuery;
        private EntityQuery m_RootSpaceOnlyQuery;
        private EntityQuery m_WorldAndRootSpaceQuery;

        protected override void OnCreate()
        {
            m_WorldSpaceOnlyQuery = GetEntityQuery(ComputeWorldSpaceJob.QueryDesc);
            m_RootSpaceOnlyQuery = GetEntityQuery(ComputeRootSpaceJob.QueryDesc);
            m_WorldAndRootSpaceQuery = GetEntityQuery(ComputeWorldAndRootSpaceJob.QueryDesc);
        }

        protected override void OnUpdate()
        {
            var rigTypeRO = GetComponentTypeHandle<Rig>(true);
            var rigRootEntityTypeRO = GetComponentTypeHandle<RigRootEntity>(true);
            var animatedDataTypeRO = GetBufferTypeHandle<AnimatedData>(true);
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
                }.ScheduleParallel(m_WorldSpaceOnlyQuery, 1, Dependency);
            }

            JobHandle rootSpaceOnlyHandle = Dependency;
            if (m_RootSpaceOnlyQuery.CalculateEntityCount() > 0)
            {
                rootSpaceOnlyHandle = new ComputeRootSpaceJob
                {
                    Rigs = rigTypeRO,
                    AnimatedData = animatedDataTypeRO,
                    AnimatedLocalToRoot = animatedLocalToRootType,
                }.ScheduleParallel(m_RootSpaceOnlyQuery, 1, Dependency);
            }

            // TODO : These jobs should ideally all run in parallel since the queries are mutually exclusive.
            //        For now, in order to prevent the safety system from throwing errors schedule
            //        WorldAndRootSpaceJob with a dependency on the two others.
            Dependency = JobHandle.CombineDependencies(worldSpaceOnlyHandle, rootSpaceOnlyHandle);
            if (m_WorldAndRootSpaceQuery.CalculateEntityCount() > 0)
            {
                Dependency = new ComputeWorldAndRootSpaceJob
                {
                    Rig = rigTypeRO,
                    RigRootEntity = rigRootEntityTypeRO,
                    AnimatedData = animatedDataTypeRO,
                    EntityLocalToWorld = entityLocalToWorldRO,
                    AnimatedLocalToWorld = animatedLocalToWorldType,
                    AnimatedLocalToRoot = animatedLocalToRootType,
                }.ScheduleParallel(m_WorldAndRootSpaceQuery, 1, Dependency);
            }
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct ComputeWorldSpaceJob : IJobEntityBatch
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

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var rigs = batchInChunk.GetNativeArray(Rig);
                var rigRoots = batchInChunk.GetNativeArray(RigRootEntity);
                var animatedDataAccessor = batchInChunk.GetBufferAccessor(AnimatedData);
                var animatedLocalToWorldAccessor = batchInChunk.GetBufferAccessor(AnimatedLocalToWorld);

                for (int i = 0; i != batchInChunk.Count; ++i)
                {
                    var rootLocalToWorld = EntityLocalToWorld[rigRoots[i].Value].Value;
                    var stream = AnimationStream.CreateReadOnly(rigs[i], animatedDataAccessor[i].AsNativeArray());
                    Core.ComputeLocalToRoot(ref stream, rootLocalToWorld, animatedLocalToWorldAccessor[i].Reinterpret<float4x4>().AsNativeArray());
                }
            }
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct ComputeRootSpaceJob : IJobEntityBatch
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

            [ReadOnly] public ComponentTypeHandle<Rig> Rigs;
            [ReadOnly] public BufferTypeHandle<AnimatedData> AnimatedData;

            public BufferTypeHandle<AnimatedLocalToRoot> AnimatedLocalToRoot;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var rigs = batchInChunk.GetNativeArray(Rigs);
                var data = batchInChunk.GetBufferAccessor(AnimatedData);
                var animatedLocalToRoot = batchInChunk.GetBufferAccessor(AnimatedLocalToRoot);

                for (int i = 0; i != batchInChunk.Count; ++i)
                {
                    var stream = AnimationStream.CreateReadOnly(rigs[i], data[i].AsNativeArray());
                    Core.ComputeLocalToRoot(ref stream, float4x4.identity, animatedLocalToRoot[i].Reinterpret<float4x4>().AsNativeArray());
                }
            }
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct ComputeWorldAndRootSpaceJob : IJobEntityBatch
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
                }
            };

            [ReadOnly] public ComponentTypeHandle<Rig> Rig;
            [ReadOnly] public ComponentTypeHandle<RigRootEntity> RigRootEntity;
            [ReadOnly] public BufferTypeHandle<AnimatedData> AnimatedData;

            [ReadOnly] public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;

            public BufferTypeHandle<AnimatedLocalToWorld> AnimatedLocalToWorld;
            public BufferTypeHandle<AnimatedLocalToRoot> AnimatedLocalToRoot;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var rigs = batchInChunk.GetNativeArray(Rig);
                var rigRoots = batchInChunk.GetNativeArray(RigRootEntity);
                var animatedLocalToRootAccessor = batchInChunk.GetBufferAccessor(AnimatedLocalToRoot);

                var animatedDataAccessor = batchInChunk.GetBufferAccessor(AnimatedData);
                var animatedLocalToWorldAccessor = batchInChunk.GetBufferAccessor(AnimatedLocalToWorld);
                for (int i = 0; i != batchInChunk.Count; ++i)
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
            }
        }
    }
}
