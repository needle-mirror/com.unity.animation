using Unity.Entities;
using Unity.Jobs;
using Unity.DataFlowGraph;
using Unity.Collections;
using Unity.Profiling;
using Unity.Burst;

using System;

namespace Unity.Animation
{
    public interface IGraphTag : IComponentData { }

    public interface IGraphSystem<T> where T : IGraphTag
    {
        NodeSet Set { get; }
        int RefCount { get; }
        void AddRef();
        void RemoveRef();
        T Tag { get; }
    }

    public abstract class GraphSystemBase<TGraphTag> : JobComponentSystem, IGraphSystem<TGraphTag>
        where TGraphTag : struct, IGraphTag
    {
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("GraphSystemBase");

        EntityQuery m_Query;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Query = GetEntityQuery(ComponentType.ReadOnly<TGraphTag>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            k_ProfilerMarker.Begin();

            if (Set != null)
                inputDeps = Set.Update(inputDeps);

            k_ProfilerMarker.End();

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

        public TGraphTag Tag { get; } = new TGraphTag();
    }


    [Obsolete("AnimationGraphSystemBase is obsolete, use GraphSystemBase instead (RemovedAfter 2020-02-18)", false)]
    public abstract class AnimationGraphSystemBase<TGraphOutput> : JobComponentSystem
        where TGraphOutput : struct, IGraphOutput
    {
        EntityQuery m_GraphOutputRigQuery;

        static readonly ProfilerMarker k_Marker = new ProfilerMarker("AnimationGraphSystemBase");

        public NodeSet Set { get; private set; }
        public int RefCount { get; private set; }

        protected override void OnCreate()
        {
            m_GraphOutputRigQuery = GetEntityQuery(
                ComponentType.ReadOnly<Rig>(),
                ComponentType.ReadWrite<AnimatedData>(),
                ComponentType.ReadOnly<TGraphOutput>()
                );

            base.OnCreate();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (Set == null)
                return inputDeps;

            k_Marker.Begin();

            Set.Update();

            JobHandle dependencies;
            var job = new ConvertGraphOutputToRigBuffers
            {
                GraphOutputs = GetArchetypeChunkComponentType<TGraphOutput>(true),
                GraphValueResolver = Set.GetGraphValueResolver(out dependencies),

                Rigs = GetArchetypeChunkComponentType<Rig>(true),
                Floats = GetArchetypeChunkBufferType<AnimatedData>()
            };

            inputDeps = job.Schedule(m_GraphOutputRigQuery, JobHandle.CombineDependencies(dependencies, inputDeps));
            Set.InjectDependencyFromConsumer(inputDeps);

            k_Marker.End();

            return inputDeps;
        }

        public void AddRef()
        {
            if (RefCount++ == 0)
                Set = new NodeSet();
        }

        public void RemoveRef()
        {
            if (RefCount == 0)
                return;

            if (--RefCount == 0)
                Set.Dispose();
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ConvertGraphOutputToRigBuffers : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<TGraphOutput> GraphOutputs;
            [ReadOnly] public GraphValueResolver GraphValueResolver;

            [ReadOnly] public ArchetypeChunkComponentType<Rig> Rigs;
            public ArchetypeChunkBufferType<AnimatedData> Floats;
            
            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigs = chunk.GetNativeArray(Rigs);
                var graphOutputArray = chunk.GetNativeArray(GraphOutputs);
                var floatsBuffer = chunk.GetBufferAccessor(Floats);
                
                for (int i = 0; i != graphOutputArray.Length; ++i)
                {
                    var ecsStream = AnimationStream.Create(
                        rigs[i].Value,
                        floatsBuffer[i].AsNativeArray()
                        );
                    var graphStream = AnimationStream.CreateReadOnly(rigs[i].Value, GraphValueResolver.Resolve(graphOutputArray[i].Buffer));

                    AnimationStreamUtils.MemCpy(ref ecsStream, ref graphStream);
                }
            }
        }
    }
}
