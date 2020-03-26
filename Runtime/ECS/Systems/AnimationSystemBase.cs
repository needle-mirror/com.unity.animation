using System;

using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.DataFlowGraph;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    public interface IAnimationSystemTag : IComponentData { }

    public interface IAnimationSystem
    {
        NodeSet Set { get; }
        int RefCount { get; }
        void AddRef();
        void RemoveRef();
    }

    public struct NotSupportedTransformHandle : IReadTransformHandle, IWriteTransformHandle
    {
        public Entity Entity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Index { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    public interface IAnimationSystem<TTag> : IAnimationSystem
        where TTag : struct, IAnimationSystemTag
    {
        TTag TagComponent { get; }
    }

    public abstract class AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle>
        : JobComponentSystem, IAnimationSystem<TTag>
        where TTag : struct, IAnimationSystemTag
        where TReadTransformHandle : struct, IReadTransformHandle
        where TWriteTransformHandle : struct, IWriteTransformHandle
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("AnimationSystemBase");
#endif

        EntityQuery m_EvaluateGraphQuery;
        EntityQuery m_SortReadComponentDataQuery;
        EntityQuery m_ReadComponentDataQuery;
        EntityQuery m_WriteComponentDataQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EvaluateGraphQuery         = GetEntityQuery(ComponentType.ReadOnly<TTag>(), ComponentType.ReadOnly<Rig>());
            m_SortReadComponentDataQuery = GetEntityQuery(SortReadTransformComponentJob.QueryDesc);
            m_ReadComponentDataQuery     = GetEntityQuery(ReadTransformComponentJob.QueryDesc);
            m_WriteComponentDataQuery    = GetEntityQuery(WriteTransformComponentJob.QueryDesc);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_ProfilerMarker.Begin();
#endif

            inputDeps = ScheduleReadComponentDataJobs(inputDeps);
            inputDeps = ScheduleGraphEvaluationJobs(inputDeps);
            inputDeps = ScheduleWriteComponentDataJobs(inputDeps);

#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_ProfilerMarker.End();
#endif

            return inputDeps;
        }

        public NodeSet Set { get; private set; }

        public int RefCount { get; private set; }

        public void AddRef()
        {
            if (RefCount++ == 0)
                Set = new NodeSet(this);
        }

        public void RemoveRef()
        {
            if (RefCount == 0)
                return;

            if (--RefCount == 0)
                Set.Dispose();
        }

        public TTag TagComponent { get; } = new TTag();

        protected JobHandle ScheduleReadComponentDataJobs(JobHandle inputDeps)
        {
            var sortJob = new SortReadTransformComponentJob
            {
                ReadTransforms = GetArchetypeChunkBufferType<TReadTransformHandle>(),
                LastSystemVersion = LastSystemVersion
            }.Schedule(m_SortReadComponentDataQuery, inputDeps);

            var readJob = new ReadTransformComponentJob
            {
                Rigs = GetArchetypeChunkComponentType<Rig>(true),
                RigLocalToWorlds = GetArchetypeChunkComponentType<LocalToWorld>(true),
                ReadTransforms = GetArchetypeChunkBufferType<TReadTransformHandle>(true),
                EntityLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(true),
                AnimatedData = GetArchetypeChunkBufferType<AnimatedData>()
            }.Schedule(m_ReadComponentDataQuery, sortJob);

            return readJob;
        }

        protected JobHandle ScheduleWriteComponentDataJobs(JobHandle inputDeps)
        {
            inputDeps = new WriteTransformComponentJob
            {
                Rigs = GetArchetypeChunkComponentType<Rig>(true),
                RigLocalToWorlds = GetArchetypeChunkComponentType<LocalToWorld>(true),
                AnimatedData = GetArchetypeChunkBufferType<AnimatedData>(true),
                WriteTransforms = GetArchetypeChunkBufferType<TWriteTransformHandle>(true),
                AnimatedLocalToWorlds = GetArchetypeChunkBufferType<AnimatedLocalToWorld>(),
                EntityLocalToWorld = GetComponentDataFromEntity<LocalToWorld>()
            }.Schedule(m_WriteComponentDataQuery, inputDeps);

            return inputDeps;
        }

        protected JobHandle ScheduleGraphEvaluationJobs(JobHandle inputDeps)
        {
            if (Set == null || m_EvaluateGraphQuery.CalculateEntityCount() == 0)
                return inputDeps;

            return Set.Update(inputDeps);
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        protected struct SortReadTransformComponentJob : IJobChunk
        {
            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<TReadTransformHandle>()
                }
            };

            public ArchetypeChunkBufferType<TReadTransformHandle> ReadTransforms;
            public uint LastSystemVersion;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var didChange = chunk.DidChange(ReadTransforms, LastSystemVersion);
                if (!didChange)
                    return;

                var readTransformAccessor = chunk.GetBufferAccessor(ReadTransforms);

                for (int i = 0; i < chunk.Count; ++i)
                {
                    var readTransforms = readTransformAccessor[i].AsNativeArray();
                    readTransforms.Sort(new RigEntityBuilder.TransformHandleComparer<TReadTransformHandle>());
                    var end = readTransforms.Unique(new RigEntityBuilder.TransformHandleComparer<TReadTransformHandle>());
                    if(end < readTransforms.Length)
                    {
                        throw new InvalidOperationException($"Cannot have multiple entity targeting the same transform index. Ignoring Entity '{readTransforms[end].Entity}' targeting transform index '{readTransforms[end].Index}'.");
                    }
                }
            }
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        protected struct ReadTransformComponentJob : IJobChunk
        {
            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Rig>(),
                    ComponentType.ReadOnly<TReadTransformHandle>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ReadWrite<AnimatedData>()
                }
            };

            [ReadOnly] public ArchetypeChunkComponentType<Rig> Rigs;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> RigLocalToWorlds;
            [ReadOnly] public ArchetypeChunkBufferType<TReadTransformHandle> ReadTransforms;
            [ReadOnly] public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;

            public ArchetypeChunkBufferType<AnimatedData> AnimatedData;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigs = chunk.GetNativeArray(Rigs);
                var rigLocalToWorlds = chunk.GetNativeArray(RigLocalToWorlds);
                var readTransformAccessor = chunk.GetBufferAccessor(ReadTransforms);
                var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedData);

                for (int i = 0; i < chunk.Count; ++i)
                {
                    var readTransformArray = readTransformAccessor[i].AsNativeArray();
                    var stream = AnimationStream.Create(rigs[i], animatedDataAccessor[i].AsNativeArray());
                    var invRigLocalToWorld = math.inverse(rigLocalToWorlds[i].Value);

                    var localToRootCache = new NativeArray<float4x4>(stream.Rig.Value.Skeleton.BoneCount, Allocator.Temp);

                    var readTransformIndex = 0;
                    var b = 0;

                    var readTransform = readTransformArray[readTransformIndex];
                    var parentIdx = stream.Rig.Value.Skeleton.ParentIndexes[b];
                    var parentLocalToRoot = parentIdx == -1 ? float4x4.identity : localToRootCache[parentIdx];

                    // Assumes the ReadtTransformHandles are sorted, so that we need to pass through the bones only once.
                    // This does not work if the bones and the read handles are not sorted in the same order.
                    while (b < stream.Rig.Value.Skeleton.BoneCount)
                    {
                        // Early exit if all read transforms have been processed,
                        // no need to compute the rest of the LocalToRoots.
                        if (readTransformIndex >= readTransformArray.Length)
                            break;

                        readTransform = readTransformArray[readTransformIndex];

                        // If the target index is smaller or equal than the previous one, it means we reached the
                        // end of the buffer, that has the duplicates (for when several read transform target the
                        // same index in the rig).
                        if (readTransformIndex > 0
                            && readTransformArray[readTransformIndex - 1].Index >= readTransform.Index)
                        {
                            break;
                        }

                        parentIdx = stream.Rig.Value.Skeleton.ParentIndexes[b];
                        parentLocalToRoot = parentIdx == -1 ? float4x4.identity : localToRootCache[parentIdx];

                        if (readTransform.Index == b)
                        {
                            if (EntityLocalToWorld.HasComponent(readTransform.Entity))
                            {
                                var localToRoot = math.mul(invRigLocalToWorld, EntityLocalToWorld[readTransform.Entity].Value);

                                if (b > 0)
                                {
                                    var tx = math.mul(math.inverse(parentLocalToRoot), localToRoot);

                                    stream.SetLocalToParentTranslation(b, tx.c3.xyz);
                                    stream.SetLocalToParentRotation(b, new quaternion(tx));
                                }
                                else
                                {
                                    stream.SetLocalToParentTranslation(b, localToRoot.c3.xyz);
                                    stream.SetLocalToParentRotation(b, new quaternion(localToRoot));
                                }

                                localToRootCache[b] = localToRoot;
                                readTransformIndex++;
                            }
                        }
                        else
                        {
                            localToRootCache[b] = math.mul(parentLocalToRoot, stream.GetLocalToParentMatrix(b));
                        }

                        ++b;
                    }
                }
            }
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        protected struct WriteTransformComponentJob : IJobChunk
        {
            static public EntityQueryDesc QueryDesc => new EntityQueryDesc()
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<Rig>(),
                    ComponentType.ReadOnly<LocalToWorld>(),
                    ComponentType.ReadOnly<AnimatedData>(),
                    ComponentType.ReadOnly<TWriteTransformHandle>(),
                    ComponentType.ReadWrite<AnimatedLocalToWorld>()
                }
            };

            [ReadOnly] public ArchetypeChunkComponentType<Rig> Rigs;
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> RigLocalToWorlds;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedData> AnimatedData;
            [ReadOnly] public ArchetypeChunkBufferType<TWriteTransformHandle> WriteTransforms;

            public ArchetypeChunkBufferType<AnimatedLocalToWorld> AnimatedLocalToWorlds;

            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<LocalToWorld> EntityLocalToWorld;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigs = chunk.GetNativeArray(Rigs);
                var rigLocalToWorlds = chunk.GetNativeArray(RigLocalToWorlds);
                var animatedDataAccessor = chunk.GetBufferAccessor(AnimatedData);
                var writeTransformAccessor = chunk.GetBufferAccessor(WriteTransforms);
                var animatedLocalToWorldAccessor = chunk.GetBufferAccessor(AnimatedLocalToWorlds);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var animatedLocalToWorlds = animatedLocalToWorldAccessor[i].Reinterpret<float4x4>().AsNativeArray();
                    var stream = AnimationStream.CreateReadOnly(rigs[i], animatedDataAccessor[i].AsNativeArray());
                    Core.ComputeLocalToWorld(rigLocalToWorlds[i].Value, ref stream, animatedLocalToWorlds);

                    var writeTransforms = writeTransformAccessor[i].AsNativeArray();
                    for (int j = 0; j < writeTransforms.Length; ++j)
                    {
                        if (EntityLocalToWorld.HasComponent(writeTransforms[j].Entity))
                        {
                            EntityLocalToWorld[writeTransforms[j].Entity] = new LocalToWorld
                            {
                                Value = animatedLocalToWorlds[writeTransforms[j].Index]
                            };
                        }
                    }
                }
            }
        }
    }
}
