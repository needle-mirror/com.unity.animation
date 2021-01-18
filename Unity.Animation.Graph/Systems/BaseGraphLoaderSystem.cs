using Unity.DataFlowGraph;
using Unity.Collections;
using Unity.Entities;
using System.Collections.Generic;
using Unity.Profiling;

namespace Unity.Animation
{
    public struct GraphExecutionModel : IComponentData
    {
        public NodeSet.RenderExecutionModel Model;
    }

    public struct GraphEntityState : ISystemStateComponentData
    {
        public NodeHandle m_Root;
        public Entity m_ContextEntity; //Since we are referencing the ComponentNode through the entity as a key, we need to store the entity in the GraphState
        public NodeHandle<ComponentNode> m_ContextEntityComponentNodeHandle;
    }

    public abstract class BaseGraphLoaderSystem<TNode, TTag, TAllocatedTag> : ComponentSystem
        where TNode : NodeDefinition, IGraphHandler, IGraphInstanceHandler, IEntityManagerHandler, IInputReferenceHandler, IComponentNodeHandler, new()
        where TTag : struct, IComponentData
        where TAllocatedTag : struct, ISystemStateComponentData
    {
        public abstract NodeSet Set { get; }

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker($"Unity.Animation.CompositorSystem {typeof(TTag).DeclaringType.Name}");

        private Dictionary<Entity, NodeHandle<ComponentNode>> m_ComponentNodes = new Dictionary<Entity, NodeHandle<ComponentNode>>();

        protected NodeHandle<ComponentNode> GetOrCreateComponentNode(Entity entity)
        {
            if (m_ComponentNodes.TryGetValue(entity, out var handle))
                return handle;
            handle = Set.CreateComponentNode(entity);
            m_ComponentNodes[entity] = handle;
            return handle;
        }

        protected bool DeleteComponentNode(Entity entity)
        {
            if (m_ComponentNodes.TryGetValue(entity, out var handle))
            {
                Set.Destroy(handle);
                m_ComponentNodes.Remove(entity);
                return true;
            }
            return false;
        }

        EntityQuery m_ExecutionModelQuery;
        EntityQuery m_CreateQuery;
        EntityQuery m_SetupQuery;
        EntityQuery m_ReleaseQuery;
        EntityQuery m_DestroyQuery;
        EntityQuery m_RegisterQuery;
        EntityQuery m_RegisterInstancesQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ExecutionModelQuery = GetEntityQuery(ComponentType.ReadOnly<GraphExecutionModel>(), ComponentType.ReadOnly<TTag>());
            m_CreateQuery = GetEntityQuery(
                ComponentType.Exclude<GraphEntityState>(),
                ComponentType.ReadOnly<GraphReference>(),
                ComponentType.ReadOnly<ContextReference>(),
                ComponentType.ReadOnly<InputReference>(),
                ComponentType.ReadOnly<TTag>());
            m_SetupQuery = GetEntityQuery(
                ComponentType.Exclude<GraphReady>(),
                ComponentType.ReadOnly<GraphEntityState>(),
                ComponentType.ReadOnly<InputReference>(),
                ComponentType.ReadOnly<GraphReference>(),
                ComponentType.ReadOnly<ContextReference>(),
                ComponentType.ReadOnly<TTag>());
            m_ReleaseQuery = GetEntityQuery(ComponentType.ReadOnly<GraphEntityState>(), ComponentType.ReadOnly<TAllocatedTag>(), ComponentType.Exclude<GraphReady>());
            m_DestroyQuery = GetEntityQuery(ComponentType.ReadOnly<GraphEntityState>(), ComponentType.ReadOnly<TAllocatedTag>());
            m_RegisterQuery =
                GetEntityQuery(
                    ComponentType.ReadOnly<GraphRegister>());
            m_RegisterInstancesQuery =
                GetEntityQuery(
                    ComponentType.ReadOnly<GraphParameterRegister>());

            RequireSingletonForUpdate<GraphManager>();
        }

        protected override void OnUpdate()
        {
            var graphManager = GetSingleton<GraphManager>();

            k_ProfilerMarker.Begin();
            Entities.With(m_RegisterQuery).ForEach((Entity entity, DynamicBuffer<GraphRegister> graphs) =>
            {
                foreach (var g in graphs)
                {
                    graphManager.AddGraph(g);

                    // TODO : Had to move this outside of GraphManager due to assemblies conflicts
                    for (int i = 0; i < g.Graph.Value.TypesUsed.Length; ++i)
                    {
                        TypeRegistry.Instance.RegisterType(g.Graph.Value.TypesUsed[i].ToString());
                    }
                }
                PostUpdateCommands.RemoveComponent<GraphRegister>(entity);
            });

            Entities.With(m_RegisterInstancesQuery).ForEach((Entity entity, DynamicBuffer<GraphParameterRegister> graphsInstances) =>
            {
                foreach (var g in graphsInstances)
                    GenericAssetManager<GraphInstanceParameters, GraphParameterRegister>.Instance.AddAsset(g);
                PostUpdateCommands.RemoveComponent<GraphParameterRegister>(entity);
            });

            Entities.With(m_ExecutionModelQuery).ForEach((Entity entity, ref GraphExecutionModel model) =>
            {
                Set.RendererModel = model.Model;
                PostUpdateCommands.RemoveComponent<GraphExecutionModel>(entity);
            });

            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entities.With(m_CreateQuery).ForEach((Entity e, ref ContextReference context, ref GraphReference graph) =>
            {
                NodeHandle<TNode> root = Set.Create<TNode>();
                var state = new GraphEntityState
                {
                    m_Root = root,
                    m_ContextEntity = context.Entity,
                    m_ContextEntityComponentNodeHandle = GetOrCreateComponentNode(context.Entity)
                };

                var loadedGraph = graphManager.GetGraph(graph.GraphID).Graph;

                if (loadedGraph.Value.IsStateMachine)
                {
                    SetupStateMachine(EntityManager, e, loadedGraph);
                }
                Set.SendMessage(
                    Set.Adapt(root).To<IGraphHandler>(),
                    loadedGraph);

                cmdBuffer.AddComponent(e, state);
            });

            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();
            cmdBuffer = new EntityCommandBuffer(Allocator.Temp);

            Entities.With(m_SetupQuery).ForEach((Entity e, DynamicBuffer<InputReference> inputReferences, ref GraphEntityState state, ref ContextReference context, ref GraphReference graph) =>
            {
                var inputArray = inputReferences.ToNativeArray(Allocator.Temp);
                Set.SendMessage(
                    Set.Adapt(state.m_Root).To<IGraphInstanceHandler>(),
                    GenericAssetManager<GraphInstanceParameters, GraphParameterRegister>.Instance.GetAsset(graph.GraphParametersID));
                Set.SendMessage(
                    Set.Adapt(state.m_Root).To<IEntityManagerHandler>(), EntityManager);
                Set.SendMessage(
                    Set.Adapt(state.m_Root).To<IInputReferenceHandler>(),
                    inputArray);

                inputArray.Dispose();

                cmdBuffer.AddComponent<GraphReady>(e);
                cmdBuffer.AddComponent<TAllocatedTag>(e);
                cmdBuffer.RemoveComponent<GraphReference>(e);
                cmdBuffer.RemoveComponent<ContextReference>(e);
            });

            Entities.With(m_ReleaseQuery).ForEach((Entity entity, ref GraphEntityState state) =>
            {
                DestroyGraph(entity, ref state, ref cmdBuffer);
            });
            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();
            k_ProfilerMarker.End();
        }

        protected virtual void SetupStateMachine(EntityManager entityManager, Entity animComponentEntity, BlobAssetReference<Graph> loadedGraph)
        {
        }

        protected virtual void DestroyGraph(Entity entity, ref GraphEntityState state, ref EntityCommandBuffer cmdBuffer)
        {
            DeleteComponentNode(state.m_ContextEntity);
            Set.Destroy(state.m_Root);
            cmdBuffer.RemoveComponent<GraphEntityState>(entity);
            cmdBuffer.RemoveComponent<TAllocatedTag>(entity);
        }

        protected override void OnDestroy()
        {
            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);

            Entities.With(m_DestroyQuery).ForEach((Entity entity, ref GraphEntityState state) =>
            {
                DestroyGraph(entity, ref state, ref cmdBuffer);
            });
            cmdBuffer.Playback(EntityManager);
            cmdBuffer.Dispose();
        }
    }
}
