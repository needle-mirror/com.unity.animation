using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.DataFlowGraph;
using Unity.Collections;

namespace Unity.Animation
{
    /// <summary>
    /// Base interface of an AnimationGraphSystem which gives access to the DataFlowGraph NodeSet.
    /// </summary>
    public interface IAnimationGraphSystem
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

    internal static class AnimationSystemID
    {
        static ushort s_AnimationSystemCounter = 0;

        internal static ushort Generate() => ++ s_AnimationSystemCounter;
    };

    public abstract class AnimationGraphSystemBase
        : AnimationGraphSystemBase<NotSupportedTransformHandle, NotSupportedTransformHandle, NotSupportedRootMotion>
    {
    }

    public abstract class AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle>
        : AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle, NotSupportedRootMotion>
        where TReadTransformHandle : struct, IReadTransformHandle
        where TWriteTransformHandle : struct, IWriteTransformHandle
    {
    }

    /// <summary>
    /// This is the base animation graph system which can used to create an animation pass in your system pipeline.
    /// This system uses a DataFlowGraph NodeSet to evaluate an animation graph and should be populated with animation nodes.
    /// </summary>
    public abstract class AnimationGraphSystemBase<TReadTransformHandle, TWriteTransformHandle, TAnimatedRootMotion>
        : SystemBase, IAnimationGraphSystem
        where TReadTransformHandle  : struct, IReadTransformHandle
        where TWriteTransformHandle : struct, IWriteTransformHandle
        where TAnimatedRootMotion   : struct, IAnimatedRootMotion
    {
        EntityQuery m_EvaluateGraphQuery;
        EntityQuery m_ReadRootTransformQuery;
        EntityQuery m_SortReadComponentDataQuery;
        EntityQuery m_ReadComponentDataQuery;
        EntityQuery m_UpdateRootRemapJobDataQuery;
        EntityQuery m_ClearPassMaskQuery;
        EntityQuery m_WriteComponentDataQuery;
        EntityQuery m_WriteRootTransformQuery;
        EntityQuery m_AccumulateRootTransformQuery;

        internal readonly ushort m_SystemID;
        ushort m_GraphCounter;

        NativeMultiHashMap<GraphHandle, NodeHandle> m_ManagedNodes;

        protected AnimationGraphSystemBase()
        {
            m_SystemID = AnimationSystemID.Generate();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EvaluateGraphQuery           = GetEntityQuery(ComponentType.ReadOnly<Rig>(), ComponentType.ReadOnly<AnimatedData>());
            m_ReadRootTransformQuery       = GetEntityQuery(ReadRootTransformJob<TAnimatedRootMotion>.QueryDesc);
            m_SortReadComponentDataQuery   = GetEntityQuery(SortReadTransformComponentJob<TReadTransformHandle>.QueryDesc);
            m_ReadComponentDataQuery       = GetEntityQuery(ReadTransformComponentJob<TReadTransformHandle>.QueryDesc);
            m_UpdateRootRemapJobDataQuery  = GetEntityQuery(UpdateRootRemapMatrixJob<TAnimatedRootMotion>.QueryDesc);
            m_ClearPassMaskQuery           = GetEntityQuery(ComponentType.ReadOnly<Rig>(), ComponentType.ReadWrite<AnimatedData>());
            m_WriteComponentDataQuery      = GetEntityQuery(WriteTransformComponentJob<TWriteTransformHandle>.QueryDesc);
            m_WriteRootTransformQuery      = GetEntityQuery(WriteRootTransformJob<TAnimatedRootMotion>.QueryDesc);
            m_AccumulateRootTransformQuery = GetEntityQuery(AccumulateRootTransformJob<TAnimatedRootMotion>.QueryDesc);
        }

        protected override void OnUpdate()
        {
            Dependency = ScheduleReadComponentDataJobs(Dependency);
            Dependency = ScheduleGraphEvaluationJobs(Dependency);
            Dependency = ScheduleWriteComponentDataJobs(Dependency);
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

        protected JobHandle ScheduleReadComponentDataJobs(JobHandle inputDeps)
        {
            var sortJob = new SortReadTransformComponentJob<TReadTransformHandle>
            {
                ReadTransforms = GetBufferTypeHandle<TReadTransformHandle>(),
                LastSystemVersion = LastSystemVersion
            }.ScheduleParallel(m_SortReadComponentDataQuery, inputDeps);

            var readJob = new ReadTransformComponentJob<TReadTransformHandle>
            {
                Rigs = GetComponentTypeHandle<Rig>(true),
                RigRoots = GetComponentTypeHandle<RigRootEntity>(true),
                ReadTransforms = GetBufferTypeHandle<TReadTransformHandle>(true),
                EntityLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(true),
                AnimatedData = GetBufferTypeHandle<AnimatedData>()
            }.ScheduleParallel(m_ReadComponentDataQuery, sortJob);

            var readRootJob = new ReadRootTransformJob<TAnimatedRootMotion>
            {
                EntityTranslation = GetComponentDataFromEntity<Translation>(true),
                EntityRotation = GetComponentDataFromEntity<Rotation>(true),
                EntityScale = GetComponentDataFromEntity<Scale>(true),
                EntityNonUniformScale = GetComponentDataFromEntity<NonUniformScale>(true),
                RigType = GetComponentTypeHandle<Rig>(true),
                RigRootEntityType = GetComponentTypeHandle<RigRootEntity>(true),
                AnimatedDataType = GetBufferTypeHandle<AnimatedData>()
            }.ScheduleParallel(m_ReadRootTransformQuery, readJob);

            var clearPassMaskJob = new ClearPassMaskJob
            {
                RigType = GetComponentTypeHandle<Rig>(true),
                AnimatedDataType = GetBufferTypeHandle<AnimatedData>()
            }.ScheduleParallel(m_ClearPassMaskQuery, readRootJob);

            var updateRigRemapJob = new UpdateRootRemapMatrixJob<TAnimatedRootMotion>
            {
                EntityLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(true),
                Parent = GetComponentDataFromEntity<Parent>(true),
                DisableRootTransformType = GetComponentTypeHandle<DisableRootTransformReadWriteTag>(true),
                AnimatedRootMotionType = GetComponentTypeHandle<TAnimatedRootMotion>(true),
                RigRootEntityType = GetComponentTypeHandle<RigRootEntity>(),
            }.ScheduleParallel(m_UpdateRootRemapJobDataQuery, readRootJob);

            return JobHandle.CombineDependencies(updateRigRemapJob, clearPassMaskJob);
        }

        protected JobHandle ScheduleWriteComponentDataJobs(JobHandle inputDeps)
        {
            var writeRootJob = new WriteRootTransformJob<TAnimatedRootMotion>
            {
                EntityLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(),
                EntityLocalToParent = GetComponentDataFromEntity<LocalToParent>(),
                EntityTranslation = GetComponentDataFromEntity<Translation>(),
                EntityRotation = GetComponentDataFromEntity<Rotation>(),
                EntityScale = GetComponentDataFromEntity<Scale>(),
                EntityNonUniformScale = GetComponentDataFromEntity<NonUniformScale>(),
                RigType = GetComponentTypeHandle<Rig>(true),
                RigRootEntityType = GetComponentTypeHandle<RigRootEntity>(true),
                AnimatedDataType = GetBufferTypeHandle<AnimatedData>(),
            }.ScheduleParallel(m_WriteRootTransformQuery, inputDeps);

            var accumulateRootJob = new AccumulateRootTransformJob<TAnimatedRootMotion>
            {
                EntityLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(),
                EntityLocalToParent = GetComponentDataFromEntity<LocalToParent>(),
                EntityTranslation = GetComponentDataFromEntity<Translation>(),
                EntityRotation = GetComponentDataFromEntity<Rotation>(),
                EntityScale = GetComponentDataFromEntity<Scale>(),
                EntityNonUniformScale = GetComponentDataFromEntity<NonUniformScale>(),
                RootMotionOffsetType = GetComponentTypeHandle<RootMotionOffset>(),
                RootMotionType = GetComponentTypeHandle<TAnimatedRootMotion>(),
                RigType = GetComponentTypeHandle<Rig>(true),
                RigRootEntityType = GetComponentTypeHandle<RigRootEntity>(true),
                AnimatedDataType = GetBufferTypeHandle<AnimatedData>()
            }.ScheduleParallel(m_AccumulateRootTransformQuery, writeRootJob);

            var writeComponentJob = new WriteTransformComponentJob<TWriteTransformHandle>
            {
                Rigs = GetComponentTypeHandle<Rig>(true),
                RigRoots = GetComponentTypeHandle<RigRootEntity>(true),
                AnimatedData = GetBufferTypeHandle<AnimatedData>(true),
                WriteTransforms = GetBufferTypeHandle<TWriteTransformHandle>(true),
                AnimatedLocalToWorlds = GetBufferTypeHandle<AnimatedLocalToWorld>(),
                EntityLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(),
                EntityLocalToParent = GetComponentDataFromEntity<LocalToParent>(),
                EntityTranslation = GetComponentDataFromEntity<Translation>(),
                EntityRotation = GetComponentDataFromEntity<Rotation>(),
                EntityScale = GetComponentDataFromEntity<Scale>(),
                EntityNonUniformScale = GetComponentDataFromEntity<NonUniformScale>()
            }.ScheduleParallel(m_WriteComponentDataQuery, accumulateRootJob);

            return writeComponentJob;
        }

        protected JobHandle ScheduleGraphEvaluationJobs(JobHandle inputDeps)
        {
            if (Set == null || m_EvaluateGraphQuery.CalculateEntityCount() == 0)
                return inputDeps;

            return Set.Update(inputDeps);
        }
    }
}
