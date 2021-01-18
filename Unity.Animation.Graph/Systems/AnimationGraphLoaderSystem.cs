using System;
using System.Diagnostics;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Profiling;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    internal struct GraphEntityStateProcessed : IComponentData
    {
    }

    internal struct GraphGameplayPropertiesSpaceReserved : IComponentData
    {
    }

    public abstract class AnimationGraphLoaderSystem<TAnimationSystem, TTag, TReleaseTag> : BaseGraphLoaderSystem<AnimationGraphNodeDefinition, TTag, TReleaseTag>
        where TAnimationSystem : ComponentSystemBase, IAnimationGraphSystem
        where TTag : struct, IComponentData
        where TReleaseTag : struct, ISystemStateComponentData
    {
        IAnimationGraphSystem AnimationSystem { get; set; }
        public override NodeSet Set => AnimationSystem.Set;

        static readonly string ProfilerMarkerName = $"Unity.Animation.AnimationGraphLoaderSystem : {typeof(TTag).DeclaringType.Name}";

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker(ProfilerMarkerName);
        static readonly ProfilerMarker k_ProfilerRigMarker = new ProfilerMarker($"{ProfilerMarkerName} : Rig");
        static readonly ProfilerMarker k_ProfilerClipsMarker = new ProfilerMarker($"{ProfilerMarkerName} : Clips");
        static readonly ProfilerMarker k_ProfilerBlendTree1DMarker = new ProfilerMarker($"{ProfilerMarkerName} : Blend Tree 1D");
        static readonly ProfilerMarker k_ProfilerBlendTree2DMarker = new ProfilerMarker($"{ProfilerMarkerName} : Blend Tree 2D");
        static readonly ProfilerMarker k_ProfilerCopyBlackboardPropertiesMarker = new ProfilerMarker($"{ProfilerMarkerName} : Copy Blackboard Properties");

        EntityQuery m_RigQuery;
        EntityQuery m_ClipRegisterQuery;
        EntityQuery m_BlendTree1DQuery;
        EntityQuery m_BlendTree2DQuery;
        EntityQuery m_GameplayPropertiesSetup;
        EntityQuery m_GameplayPropertiesDestroy;

        protected override void OnCreate()
        {
            base.OnCreate();
            AnimationSystem = World.GetOrCreateSystem<TAnimationSystem>();
            AnimationSystem.AddRef();

            m_RigQuery =
                GetEntityQuery(
                    ComponentType.ReadWrite<GraphEntityState>(),
                    ComponentType.ReadOnly<GraphReady>(),
                    ComponentType.ReadOnly<TTag>(),
                    ComponentType.Exclude<GraphEntityStateProcessed>());
            m_ClipRegisterQuery =
                GetEntityQuery(
                    ComponentType.ReadOnly<ClipRegister>());
            m_BlendTree1DQuery =
                GetEntityQuery(
                    ComponentType.ReadOnly<GraphEntityState>(),
                    ComponentType.ReadOnly<BlendTree1DAsset>(),
                    ComponentType.ReadOnly<GraphReady>(),
                    ComponentType.ReadOnly<TTag>());
            m_BlendTree2DQuery =
                GetEntityQuery(
                    ComponentType.ReadOnly<GraphEntityState>(),
                    ComponentType.ReadOnly<BlendTree2DAsset>(),
                    ComponentType.ReadOnly<GraphReady>(),
                    ComponentType.ReadOnly<TTag>());

            m_GameplayPropertiesSetup =
                GetEntityQuery(
                    ComponentType.ReadWrite<StateMachine.CharacterGameplayPropertiesCopy>(),
                    ComponentType.ReadOnly<InputReference>(),
                    ComponentType.ReadOnly<GraphReady>(),
                    ComponentType.Exclude<GraphGameplayPropertiesSpaceReserved>());

            m_GameplayPropertiesDestroy =
                GetEntityQuery(
                    ComponentType.ReadWrite<StateMachine.CharacterGameplayPropertiesCopy>(),
                    ComponentType.ReadOnly<GraphGameplayPropertiesSpaceReserved>());
        }

        protected override void OnUpdate()
        {
            k_ProfilerMarker.Begin();

            //TODO : Could we move this elsewhere? Maybe a bootstrap system?
            Entities.With(m_ClipRegisterQuery).ForEach((Entity entity, DynamicBuffer<ClipRegister> assets) =>
            {
                foreach (var g in assets)
                    GenericAssetManager<Clip, ClipRegister>.Instance.AddAsset(g);
                PostUpdateCommands.RemoveComponent<ClipRegister>(entity);
            });

            base.OnUpdate();

            Entities.With(m_RigQuery).ForEach((Entity entity, ref GraphEntityState state) =>
            {
                k_ProfilerRigMarker.Begin();
                if (EntityManager.HasComponent<Rig>(state.m_ContextEntity))
                {
                    var ctx = EntityManager.GetComponentData<Rig>(state.m_ContextEntity);
                    Set.SendMessage(
                        Set.Adapt(state.m_Root).To<IRigContextHandler>(),
                        ctx);

                    Set.SendMessage(
                        Set.Adapt(state.m_Root).To<IComponentNodeHandler>(),
                        state.m_ContextEntityComponentNodeHandle);
                    PostUpdateCommands.AddComponent<GraphEntityStateProcessed>(entity);
                }
                else
                    throw new OperationCanceledException($"Missing Rig Component in entity {state.m_ContextEntity}");
                k_ProfilerRigMarker.End();
            });

            Entities.With(m_BlendTree1DQuery).ForEach((Entity entity, ref GraphEntityState state, DynamicBuffer<BlendTree1DAsset> assets) =>
            {
                k_ProfilerBlendTree1DMarker.Begin();
                var resourceBuffer = EntityManager.GetBuffer<BlendTree1DResource>(entity);
                for (int i = 0; i < assets.Length; ++i)
                {
                    var asset = assets[i];
                    asset.Value = BlendTreeBuilder.CreateBlendTree1DFromComponents(resourceBuffer[asset.Index], EntityManager, entity);
                    Set.SendMessage(
                        state.m_Root,
                        (InputPortID)AnimationGraphNodeDefinition.SimulationPorts.BlendTree1DAsset,
                        asset);
                }
                PostUpdateCommands.RemoveComponent<BlendTree1DAsset>(entity);
                k_ProfilerBlendTree1DMarker.End();
            });

            Entities.With(m_BlendTree2DQuery).ForEach((Entity entity, ref GraphEntityState state, DynamicBuffer<BlendTree2DAsset> assets) =>
            {
                k_ProfilerBlendTree2DMarker.Begin();
                var resourceBuffer = EntityManager.GetBuffer<BlendTree2DResource>(entity);
                for (int i = 0; i < assets.Length; ++i)
                {
                    var asset = assets[i];
                    asset.Value = BlendTreeBuilder.CreateBlendTree2DFromComponents(resourceBuffer[asset.Index], EntityManager, entity);
                    Set.SendMessage(
                        state.m_Root,
                        (InputPortID)AnimationGraphNodeDefinition.SimulationPorts.BlendTree2DAsset,
                        asset);
                }
                PostUpdateCommands.RemoveComponent<BlendTree2DAsset>(entity);
                k_ProfilerBlendTree2DMarker.End();
            });

            Entities.With(m_GameplayPropertiesSetup).ForEach((Entity entity, DynamicBuffer<InputReference> inputReferences, ref StateMachine.CharacterGameplayPropertiesCopy gameplayPropertiesCopy) =>
            {
                k_ProfilerCopyBlackboardPropertiesMarker.Begin();
                gameplayPropertiesCopy.GameplayProperties = new UnsafeHashMap<int, UnsafeList<byte>>(inputReferences.Length, Allocator.Persistent);
                for (int i = 0; i < inputReferences.Length; ++i)
                {
                    var inputReference = inputReferences[i];
                    var referenceByteList = new UnsafeList<byte>(inputReference.Size, Allocator.Persistent);
                    referenceByteList.Resize(inputReference.Size);
                    if (!gameplayPropertiesCopy.GameplayProperties.TryAdd(inputReference.TypeIndex, referenceByteList))
                    {
                        ThrowBlackboardValueAlreadyExist(inputReference.TypeIndex);
                    }
                }
                PostUpdateCommands.AddComponent<GraphGameplayPropertiesSpaceReserved>(entity);
                k_ProfilerCopyBlackboardPropertiesMarker.End();
            });

            k_ProfilerMarker.End();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void ThrowBlackboardValueAlreadyExist(int typeIndex)
        {
            throw new InvalidOperationException($"Blackboard value included in type {typeIndex} already exists.");
        }

        protected override void SetupStateMachine(EntityManager entityManager, Entity animComponentEntity, BlobAssetReference<Graph> loadedGraph)
        {
            base.SetupStateMachine(entityManager, animComponentEntity, loadedGraph);

            entityManager.AddComponents(animComponentEntity, StateMachine.Builder.StateMachineComponentTypes);

            entityManager.SetComponentData(animComponentEntity, new StateMachine.StateMachineVersion {});

            var nodeRegistry = entityManager.GetBuffer<StateMachine.Node>(animComponentEntity);
            var stateMachines = entityManager.GetBuffer<StateMachine.StateMachineInstance>(animComponentEntity);

            var index = stateMachines.Add(new StateMachine.StateMachineInstance
            {
                Definition = StateMachine.StateMachineBuilder.BuildStateMachineDefinitionFromGraph(loadedGraph),
                CurrentState = -1
            });

            var nodeIndex = nodeRegistry.Add(new StateMachine.Node
            {
                Type = StateMachine.NodeType.StateMachine,
                Index = index
            });
        }

        protected override void DestroyGraph(Entity entity, ref GraphEntityState state, ref EntityCommandBuffer cmdBuffer)
        {
            base.DestroyGraph(entity, ref state, ref cmdBuffer);
            Entities.With(m_GameplayPropertiesDestroy).ForEach((Entity e, ref StateMachine.CharacterGameplayPropertiesCopy gameplayProperties) =>
            {
                if (e != entity)
                    return;
                foreach (var inputBinding in gameplayProperties.GameplayProperties)
                {
                    inputBinding.Value.Dispose();
                }

                gameplayProperties.GameplayProperties.Dispose();
                gameplayProperties.GameplayProperties = default;
            });
            cmdBuffer.RemoveComponent<StateMachine.CharacterGameplayPropertiesCopy>(entity);
            cmdBuffer.RemoveComponent<GraphGameplayPropertiesSpaceReserved>(entity);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AnimationSystem.RemoveRef();
        }
    }
}
