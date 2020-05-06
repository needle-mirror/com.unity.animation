using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.DataFlowGraph;
using Unity.Collections;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    public interface IAnimationSystem
    {
        NodeSet Set { get; }
        int RefCount { get; }
        void AddRef();
        void RemoveRef();

        GraphHandle CreateGraph();
        NodeHandle<T> CreateNode<T>(GraphHandle graph) where T : NodeDefinition, new();
        NodeHandle<ComponentNode> CreateNode(GraphHandle graph, Entity entity);
        void Dispose(GraphHandle graph);
    }

    public interface IAnimationSystemTag : IComponentData {}

    public interface IAnimationSystem<TTag> : IAnimationSystem
        where TTag : struct, IAnimationSystemTag
    {
        TTag TagComponent { get; }
    }

    internal static class AnimationSystemID
    {
        static ushort s_AnimationSystemCounter = 0;

        internal static ushort Generate() => ++ s_AnimationSystemCounter;
    };

    public abstract class AnimationSystemBase<TTag, TReadTransformHandle, TWriteTransformHandle>
        : SystemBase, IAnimationSystem<TTag>
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

        internal readonly ushort m_SystemID;
        ushort m_GraphCounter;

        NativeMultiHashMap<GraphHandle, NodeHandle> m_ManagedNodes;

        protected AnimationSystemBase()
        {
            m_SystemID = AnimationSystemID.Generate();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EvaluateGraphQuery         = GetEntityQuery(ComponentType.ReadOnly<TTag>(), ComponentType.ReadOnly<Rig>());
            m_SortReadComponentDataQuery = GetEntityQuery(SortReadTransformComponentJob<TReadTransformHandle>.QueryDesc);
            m_ReadComponentDataQuery     = GetEntityQuery(ReadTransformComponentJob<TReadTransformHandle>.QueryDesc);
            m_WriteComponentDataQuery    = GetEntityQuery(WriteTransformComponentJob<TWriteTransformHandle>.QueryDesc);
        }

        protected override void OnUpdate()
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_ProfilerMarker.Begin();
#endif
            Dependency = ScheduleReadComponentDataJobs(Dependency);
            Dependency = ScheduleGraphEvaluationJobs(Dependency);
            Dependency = ScheduleWriteComponentDataJobs(Dependency);

#if !UNITY_DISABLE_ANIMATION_PROFILING
            k_ProfilerMarker.End();
#endif
        }

        public NodeSet Set { get; private set; }

        public int RefCount { get; private set; }

        public void AddRef()
        {
            if (RefCount++ == 0)
            {
                Set = new NodeSet(this);
                m_ManagedNodes = new NativeMultiHashMap<GraphHandle, NodeHandle>(64, Allocator.Persistent);
            }
        }

        public void RemoveRef()
        {
            if (RefCount == 0)
                return;

            if (--RefCount == 0)
            {
                var nodes = m_ManagedNodes.GetValueArray(Allocator.Temp);
                for (int i = 0; i < nodes.Length; ++i)
                    Set.Destroy(nodes[i]);
                m_ManagedNodes.Dispose();

                Set.Dispose();
            }
        }

        /// <summary>
        /// Create a new GraphHandle in order to establish a logical grouping of nodes in a NodeSet.
        /// Note that by using <see cref="CreateNode(GraphHandle)"/> or <see cref="CreateNode(GraphHandle, Entity)"/>,
        /// nodes will either be automatically released when disposing the NodeSet
        /// or when you explicitly call <see cref="Dispose(GraphHandle)"/>.
        /// </summary>
        /// <returns>A unique handle to a graph part of this animation system</returns>
        public GraphHandle CreateGraph() =>
            new GraphHandle(m_SystemID, ++m_GraphCounter);

        /// <summary>
        /// Creates a node associated with a GraphHandle. If either the GraphHandle or the animation system NodeSet is disposed, this node
        /// will be automatically released.
        /// </summary>
        /// <typeparam name="T">A known NodeDefinition</typeparam>
        /// <param name="handle">GraphHandle for this animation system created using <see cref="CreateGraph()"/></param>
        /// <returns>The node handle</returns>
        public NodeHandle<T> CreateNode<T>(GraphHandle handle)
            where T : NodeDefinition, new()
        {
            if (!handle.IsValid(m_SystemID))
                return default;

            var node = Set.Create<T>();
            m_ManagedNodes.Add(handle, node);
            return node;
        }

        /// <summary>
        /// Creates a component node associated with a GraphHandle. If either the GraphHandle or the animation system NodeSet is disposed, this node
        /// will be automatically released.
        /// </summary>
        /// <param name="handle">GraphHandle for this animation system created using <see cref="CreateGraph()"/></param>
        /// <param name="entity">Entity</param>
        /// <returns>The component node handle</returns>
        public NodeHandle<ComponentNode> CreateNode(GraphHandle handle, Entity entity)
        {
            if (!handle.IsValid(m_SystemID))
                return default;

            var node = Set.CreateComponentNode(entity);
            m_ManagedNodes.Add(handle, node);
            return node;
        }

        /// <summary>
        /// Disposes all nodes created using <see cref="CreateNode(GraphHandle)"/> or <see cref="CreateNode(GraphHandle, Entity)"/> that
        /// are associated with a GraphHandle.
        /// </summary>
        /// <param name="handle">GraphHandle for this animation system created using <see cref="CreateGraph()"/></param>
        /// <param name="entity">Entity</param>
        public void Dispose(GraphHandle handle)
        {
            if (Set == null || !handle.IsValid(m_SystemID))
                return;

            if (!m_ManagedNodes.ContainsKey(handle))
                return;

            var values = m_ManagedNodes.GetValuesForKey(handle);
            while (values.MoveNext())
                Set.Destroy(values.Current);

            m_ManagedNodes.Remove(handle);
        }

        public TTag TagComponent { get; } = new TTag();

        protected JobHandle ScheduleReadComponentDataJobs(JobHandle inputDeps)
        {
            var sortJob = new SortReadTransformComponentJob<TReadTransformHandle>
            {
                ReadTransforms = GetArchetypeChunkBufferType<TReadTransformHandle>(),
                LastSystemVersion = LastSystemVersion
            }.Schedule(m_SortReadComponentDataQuery, inputDeps);

            var readJob = new ReadTransformComponentJob<TReadTransformHandle>
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
            inputDeps = new WriteTransformComponentJob<TWriteTransformHandle>
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
    }
}
