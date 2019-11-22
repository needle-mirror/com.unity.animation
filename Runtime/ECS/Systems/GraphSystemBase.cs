using System.Collections.Generic;

using Unity.Entities;
using Unity.Jobs;
using Unity.DataFlowGraph;
using Unity.Collections;
using Unity.Profiling;
using Unity.Burst;

[assembly:RegisterGenericComponentType(typeof(NodeMemoryInput<Unity.Animation.NoGraphInput>))]

namespace Unity.Animation
{
    public struct NoGraphInput : IGraphInput
    {
        public NodeHandle Node { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public InputPortID Port { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }

    [NativeAllowReinterpretation]
    public struct GraphInputData : IBufferElementData
    {
        public float Value;
    }

    // AnimationGraphSystemBase class that only outputs its graph evaluation to rig entities holding a TGraphOutput
    public abstract class AnimationGraphSystemBase<TGraphOutput> : AnimationGraphSystemBase<NoGraphInput, TGraphOutput>
        where TGraphOutput : struct, IGraphOutput
    {
    }

    // AnimationGraphSystemBase class that inputs rig entities holding TGraphInput components and outputs
    // its graph evaluation to rig entities holding a TGraphOutput
    public abstract class AnimationGraphSystemBase<TGraphInput, TGraphOutput>
        : MemoryInputSystem<TGraphInput, GraphInputData>
        where TGraphInput : struct, IGraphInput
        where TGraphOutput : struct, IGraphOutput
    {
        internal class AnimationGraphSystemMainThread : ComponentSystem
        {
            static readonly ProfilerMarker k_Marker = new ProfilerMarker("AnimationGraphSystem");

            public AnimationGraphSystemBase<TGraphInput, TGraphOutput> Parent;

            protected override void OnUpdate()
            {
                k_Marker.Begin();

                Entities
                    .WithAll<SharedRigDefinition, TGraphInput>()
                    .WithNone<GraphInputData, NodeMemoryInput<TGraphInput>>()
                    .ForEach(
                        (Entity e, SharedRigDefinition rigDefinition, ref TGraphInput input) =>
                        {
                            PostUpdateCommands.AddComponent(e, new NodeMemoryInput<TGraphInput>(input.Node, input.Port));

                            var buffer = PostUpdateCommands.AddBuffer<GraphInputData>(e);
                            buffer.ResizeUninitialized(rigDefinition.Value.Value.Bindings.CurveCount);
                        }
                    );

                k_Marker.End();
            }
        }

        const int k_SharedRigDefinitionsCount = 10;

        AnimationGraphSystemMainThread m_AnimationGraphSystemMainThread;

        NativeHashMap<int, SharedRigDefinition> m_SharedRigDefinitions;
        List<SharedRigDefinition>               m_SharedValues;
        List<int>                               m_SharedIndex;

        EntityQuery m_GraphInputRigQuery;
        EntityQuery m_GraphOutputRigQuery;

        static readonly ProfilerMarker k_Marker = new ProfilerMarker("AnimationGraphSystemBase");

        public NodeSet Set { get; private set; }
        public int RefCount { get; private set; }

        protected override void OnCreate()
        {
            m_GraphInputRigQuery = GetEntityQuery(
                ComponentType.ReadOnly<SharedRigDefinition>(),
                ComponentType.ReadWrite<AnimatedLocalTranslation>(),
                ComponentType.ReadWrite<AnimatedLocalRotation>(),
                ComponentType.ReadWrite<AnimatedLocalScale>(),
                ComponentType.ReadWrite<AnimatedFloat>(),
                ComponentType.ReadWrite<AnimatedInt>(),
                ComponentType.ReadOnly<TGraphInput>()
                );

            m_GraphOutputRigQuery = GetEntityQuery(
                ComponentType.ReadOnly<SharedRigDefinition>(),
                ComponentType.ReadWrite<AnimatedLocalTranslation>(),
                ComponentType.ReadWrite<AnimatedLocalRotation>(),
                ComponentType.ReadWrite<AnimatedLocalScale>(),
                ComponentType.ReadWrite<AnimatedFloat>(),
                ComponentType.ReadWrite<AnimatedInt>(),
                ComponentType.ReadOnly<TGraphOutput>()
                );

            m_AnimationGraphSystemMainThread = World.GetOrCreateSystem<AnimationGraphSystemMainThread>();
            m_AnimationGraphSystemMainThread.Parent = this;

            m_SharedRigDefinitions = new NativeHashMap<int, SharedRigDefinition>(k_SharedRigDefinitionsCount, Allocator.Persistent);
            m_SharedValues = new List<SharedRigDefinition>(k_SharedRigDefinitionsCount);
            m_SharedIndex = new List<int>(k_SharedRigDefinitionsCount);

            base.OnCreate();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (Set == null)
                return inputDeps;

            k_Marker.Begin();

            m_AnimationGraphSystemMainThread.Update();

            ECSUtils.UpdateSharedComponentDataHashMap(ref m_SharedRigDefinitions, m_SharedValues, m_SharedIndex, EntityManager, Allocator.Persistent);

            var graphInputJob = new ConvertRigBuffersToGraphInput
            {
                SharedRigDefinitions = m_SharedRigDefinitions,
                RigDefinitionType = GetArchetypeChunkSharedComponentType<SharedRigDefinition>(),

                LocalTranslations = GetArchetypeChunkBufferType<AnimatedLocalTranslation>(true),
                LocalRotations = GetArchetypeChunkBufferType<AnimatedLocalRotation>(true),
                LocalScales = GetArchetypeChunkBufferType<AnimatedLocalScale>(true),
                Floats = GetArchetypeChunkBufferType<AnimatedFloat>(true),
                Ints = GetArchetypeChunkBufferType<AnimatedInt>(true),

                GraphBuffer = GetArchetypeChunkBufferType<GraphInputData>()
            };
            inputDeps = graphInputJob.Schedule(m_GraphInputRigQuery, inputDeps);
            m_GraphInputRigQuery.AddDependency(inputDeps);

            inputDeps = base.OnUpdate(inputDeps);

            JobHandle dependencies;
            var job = new ConvertGraphOutputToRigBuffers
            {
                SharedRigDefinitions = m_SharedRigDefinitions,
                SharedRigDefinitionIndex = GetArchetypeChunkSharedComponentType<SharedRigDefinition>(),
                GraphOutputs = GetArchetypeChunkComponentType<TGraphOutput>(true),
                GraphValueResolver = Set.GetGraphValueResolver(out dependencies),

                LocalTranslations = GetArchetypeChunkBufferType<AnimatedLocalTranslation>(),
                LocalRotations = GetArchetypeChunkBufferType<AnimatedLocalRotation>(),
                LocalScales = GetArchetypeChunkBufferType<AnimatedLocalScale>(),
                Floats = GetArchetypeChunkBufferType<AnimatedFloat>(),
                Ints = GetArchetypeChunkBufferType<AnimatedInt>()
            };

            inputDeps = job.Schedule(m_GraphOutputRigQuery, JobHandle.CombineDependencies(dependencies, inputDeps));
            Set.InjectDependencyFromConsumer(inputDeps);

            k_Marker.End();

            return inputDeps;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_SharedRigDefinitions.Dispose();
        }

        public void AddRef()
        {
            if (RefCount++ == 0)
            {
                Set = new NodeSet();
                UnownedSet = Set;
            }
        }

        public void RemoveRef()
        {
            if (RefCount == 0)
                return;

            if (--RefCount == 0)
            {
                Set.Dispose();
                UnownedSet = null;
            }
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ConvertRigBuffersToGraphInput : IJobChunk
        {
            [ReadOnly] public NativeHashMap<int, SharedRigDefinition> SharedRigDefinitions;
            [ReadOnly] public ArchetypeChunkSharedComponentType<SharedRigDefinition> RigDefinitionType;

            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalTranslation> LocalTranslations;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalRotation> LocalRotations;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedLocalScale> LocalScales;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedFloat> Floats;
            [ReadOnly] public ArchetypeChunkBufferType<AnimatedInt> Ints;

            public ArchetypeChunkBufferType<GraphInputData> GraphBuffer;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var rigIdx = chunk.GetSharedComponentIndex(RigDefinitionType);

                var rig = SharedRigDefinitions[rigIdx].Value;
                var translations = chunk.GetBufferAccessor(LocalTranslations);
                var rotations = chunk.GetBufferAccessor(LocalRotations);
                var scales = chunk.GetBufferAccessor(LocalScales);
                var floats = chunk.GetBufferAccessor(Floats);
                var ints = chunk.GetBufferAccessor(Ints);
                var graphBuffers = chunk.GetBufferAccessor(GraphBuffer);

                for (int i = 0; i != chunk.Count; ++i)
                {
                    var graphBuffer = graphBuffers[i].Reinterpret<float>().AsNativeArray();
                    var graphStream = AnimationStreamProvider.Create(rig, graphBuffer);
                    var ecsStream = AnimationStreamProvider.CreateReadOnly(rig, translations[i], rotations[i], scales[i], floats[i], ints[i]);
                    AnimationStreamUtils.MemCpy(ref graphStream, ref ecsStream);
                }
            }
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        struct ConvertGraphOutputToRigBuffers : IJobChunk
        {
            [ReadOnly] public NativeHashMap<int, SharedRigDefinition> SharedRigDefinitions;
            [ReadOnly] public ArchetypeChunkSharedComponentType<SharedRigDefinition> SharedRigDefinitionIndex;
            [ReadOnly] public ArchetypeChunkComponentType<TGraphOutput> GraphOutputs;
            [ReadOnly] public GraphValueResolver GraphValueResolver;

            public ArchetypeChunkBufferType<AnimatedLocalTranslation> LocalTranslations;
            public ArchetypeChunkBufferType<AnimatedLocalRotation> LocalRotations;
            public ArchetypeChunkBufferType<AnimatedLocalScale> LocalScales;
            public ArchetypeChunkBufferType<AnimatedFloat> Floats;
            public ArchetypeChunkBufferType<AnimatedInt> Ints;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var sharedRigDefinitionIndex = chunk.GetSharedComponentIndex(SharedRigDefinitionIndex);
                var rig = SharedRigDefinitions[sharedRigDefinitionIndex].Value;

                var graphOutputArray = chunk.GetNativeArray(GraphOutputs);
                var localTranslationsBuffer = chunk.GetBufferAccessor(LocalTranslations);
                var localRotationsBuffer = chunk.GetBufferAccessor(LocalRotations);
                var localScalesBuffer = chunk.GetBufferAccessor(LocalScales);
                var floatsBuffer = chunk.GetBufferAccessor(Floats);
                var intsBuffer = chunk.GetBufferAccessor(Ints);

                for (int i = 0; i != graphOutputArray.Length; ++i)
                {
                    var ecsStream = AnimationStreamProvider.Create(rig, localTranslationsBuffer[i], localRotationsBuffer[i], localScalesBuffer[i], floatsBuffer[i], intsBuffer[i]);
                    var graphStream = AnimationStreamProvider.CreateReadOnly(rig, GraphValueResolver.Resolve(graphOutputArray[i].Buffer));
                    AnimationStreamUtils.MemCpy(ref ecsStream, ref graphStream);
                }
            }
        }
    }
}
