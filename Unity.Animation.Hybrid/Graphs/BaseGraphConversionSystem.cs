using Unity.Collections.LowLevel.Unsafe;
#if UNITY_EDITOR
using System;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Profiling;
using UnityEditor;
using Hash128 = Unity.Entities.Hash128;
using TypeHash = Unity.Entities.TypeHash;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion(userName: "Unity.Animation.Hybrid.BaseGraphConversionSystem", version: 3)]
    public class BaseGraphConversionSystem : GameObjectConversionSystem
    {
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("BaseGraphConversionSystem");

        void AddToBuffer<T>(Entity entity, T element)
            where T : struct, IBufferElementData
        {
            DynamicBuffer<T> buffer;
            if (!DstEntityManager.HasComponent<T>(entity))
                buffer = DstEntityManager.AddBuffer<T>(entity);
            else
                buffer = DstEntityManager.GetBuffer<T>(entity);
            buffer.Add(element);
        }

        protected override void OnUpdate()
        {
            k_ProfilerMarker.Begin();
            Entities.ForEach((AnimationGraph animGraph) =>
            {
                var graphProvider = animGraph.Graph as ICompiledGraphProvider;
                if (graphProvider == null || graphProvider.CompiledGraph.Definition == null)
                    return;

                DeclareAssetDependency(animGraph.gameObject, animGraph.Graph);

                if (animGraph.Context == null)
                    throw new ArgumentNullException($"Null Context Object : {graphProvider.CompiledGraph.DisplayName}");
                if (animGraph.Context.gameObject == null)
                    throw new ArgumentNullException($"Null Context GameObject : {graphProvider.CompiledGraph.DisplayName}");

                if (animGraph.gameObject != animGraph.Context.gameObject)
                    DeclareDependency(animGraph, animGraph.Context);

                var graphDefinition = graphProvider.CompiledGraph.Definition;

                var entity = TryGetPrimaryEntity(animGraph);
                if (entity == Entity.Null)
                    throw new Exception($"Something went wrong while creating an Entity for the Animation Graph : {animGraph.name}");

                var registerEntity = CreateAdditionalEntity(animGraph);

                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(animGraph.Graph, out var guid, out long _))
                    throw new Exception($"Something went wrong while fetching guid for : {animGraph.name}");

                // TODO : use BlobAssetComputationContext
                var idAsHash128 = new Hash128(guid);
                var graphId = RegisterForGraphManager(entity, graphProvider.CompiledGraph, idAsHash128);
                foreach (var otherGraph in graphProvider.CompiledGraph.CompiledDependencies)
                {
                    var dependencyGraph = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                        AssetDatabase.GUIDToAssetPath(otherGraph.Guid));
                    if (dependencyGraph == null)
                        throw new Exception($"Something went wrong while fetching dependency {otherGraph.Guid} for graph : {animGraph.name}");
                    RegisterForGraphManager(entity, (dependencyGraph as ICompiledGraphProvider)?.CompiledGraph, new Hash128(otherGraph.Guid));
                }

                //TODO : Do we need to check if the instance is already registered? I guess yes
                DynamicBuffer<GraphParameterRegister> graphInstances;
                if (!DstEntityManager.HasComponent<GraphParameterRegister>(registerEntity))
                    graphInstances = DstEntityManager.AddBuffer<GraphParameterRegister>(registerEntity);
                else
                    graphInstances = DstEntityManager.GetBuffer<GraphParameterRegister>(registerEntity);

                var graphInstanceBlob = GraphBuilder.BuildInstanceSpecificData(graphProvider.CompiledGraph  , animGraph);

                var instanceID = graphInstanceBlob.Value.m_HashCode;
                var graphInstanceId = new Hash128((uint)instanceID, (uint)instanceID, (uint)instanceID, (uint)instanceID); //TODO : need a better way to calculate hash

                if (!BlobAssetStore.Contains<GraphInstanceParameters>(graphInstanceId))
                {
                    if (!BlobAssetStore.TryAdd(graphInstanceId, graphInstanceBlob))
                        throw new Exception($"Something went wrong while registering new instance graph blob asset for compiled graph : {graphProvider.CompiledGraph.DisplayName}");
                }

                graphInstances.Add(
                    new GraphParameterRegister
                    {
                        ID = graphInstanceId,
                        Asset = graphInstanceBlob,
                    });

                DstEntityManager.AddComponentData(entity,
                    new GraphReference { GraphID = graphId, GraphParametersID = graphInstanceId });

                if (animGraph.Context)
                {
                    var contextEntity = TryGetPrimaryEntity(animGraph.Context);
                    if (contextEntity == Entity.Null)
                        throw new Exception($"Something went wrong while creating the Context Entity for the Animation Graph : {animGraph.name}");
                    DstEntityManager.AddComponentData(entity, new ContextReference()
                    {
                        Entity = contextEntity
                    });
                }
                else
                    throw new MissingFieldException("Missing Context for the Animation Graph");

                var inputReferences = DstEntityManager.AddBuffer<InputReference>(entity);

                foreach (var i in animGraph.Inputs)
                {
                    DeclareDependency(animGraph, i.Value);
                    var inputEntity = TryGetPrimaryEntity(i.Value);
                    if (inputEntity == Entity.Null)
                        throw new Exception($"Something went wrong while creating the Input Entity for the Animation Graph input : {i.Identification}");

                    // TODO FB : Need to find a way to patch Identification when MovedFrom is used
                    if (AuthoringComponentService.TryGetComponentByRuntimeAssemblyQualifiedName(i.Identification, out var componentInfo))
                    {
                        var typeHash = TypeHash.CalculateStableTypeHash(componentInfo.RuntimeType);
                        inputReferences.Add(
                            new InputReference()
                            {
                                Entity = inputEntity,
                                TypeHash = typeHash,
                                TypeIndex = TypeManager.GetTypeIndexFromStableTypeHash(typeHash),
                                Size = UnsafeUtility.SizeOf(componentInfo.RuntimeType)
                            });
                    }
                }

                if (ConversionService.TryGetPhaseFromAssemblyQualifiedName(animGraph.PhaseIdentification, out var phase))
                {
                    DstEntityManager.AddComponent(entity,
                        ComponentType.FromTypeIndex(
                            TypeManager.GetTypeIndexFromStableTypeHash(phase.Hash)));
                }
                else
                    throw new Exception($"Something went wrong while setting the phase {animGraph.PhaseIdentification} for the animGraph : {animGraph.name}");

                DstEntityManager.AddComponentData(entity,
                    new GraphExecutionModel { Model = NodeSet.RenderExecutionModel.Islands });
            });
            k_ProfilerMarker.End();
        }

        private GraphID RegisterForGraphManager(Entity targetEntity, CompiledGraph compiledGraph, Hash128 guid)
        {
            var graphId = new GraphID { Value = guid };

            // TODO FB : we need to only rebuild when necessary
            BlobAssetStore.Remove<Graph>(graphId.Value, true);

            var graphBlob = GraphBuilder.Build(compiledGraph);

            BlobAssetReference<StateMachine.StateMachineDefinition> stateMachineBlob = default;
            if (graphBlob.Value.IsStateMachine)
            {
                stateMachineBlob = StateMachine.StateMachineBuilder.BuildStateMachineDefinitionFromGraph(graphBlob);
            }

            if (!BlobAssetStore.TryAdd(graphId.Value, graphBlob))
                throw new InvalidOperationException($"Something went wrong while registering new graph blob asset for compiled graph : {compiledGraph.DisplayName}");

            DynamicBuffer<GraphRegister> graphs;
            if (!DstEntityManager.HasComponent<GraphRegister>(targetEntity))
                graphs = DstEntityManager.AddBuffer<GraphRegister>(targetEntity);
            else
                graphs = DstEntityManager.GetBuffer<GraphRegister>(targetEntity);
            graphs.Add(
                new GraphRegister
                {
                    ID = graphId,
                    Graph = graphBlob,
                    StateMachineDefinition = stateMachineBlob,
                });

            return graphId;
        }
    }
}
#endif
