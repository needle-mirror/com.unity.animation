using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Entities;

namespace Unity.Animation.StateMachine.Tests
{
    internal struct BlackboardValuesAuthoringComponent : IComponentData
    {
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
    }
    internal struct BlackboardValuesAuthoringComponent2 : IComponentData
    {
        public float FloatValue2;
        public int IntValue2;
        public bool BoolValue2;
    }

    internal abstract class StateMachineTestsFixture
    {
        protected BlobAssetReference<Graph> m_Graph;

        protected World World;
        protected EntityManager m_Manager;
        protected EntityManager.EntityManagerDebug m_ManagerDebug;

        protected GraphManagerSystem        m_GraphManagerSystem;
        protected StateMachineSystem        m_StateMachineSystem;
        protected GenerateRenderGraphSystem m_GenerateRenderGraphSystem;

        protected EndInitializationEntityCommandBufferSystem m_EndInitializationEntityCommandBufferSystem;

        [SetUp]
        protected virtual void SetUp()
        {
            World = new World("Test World");

            m_Manager = World.EntityManager;
            m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);

            m_GraphManagerSystem = World.GetOrCreateSystem<GraphManagerSystem>();
            m_StateMachineSystem = World.GetOrCreateSystem<StateMachineSystem>();
            m_GenerateRenderGraphSystem = World.GetOrCreateSystem<GenerateRenderGraphSystem>();
            m_EndInitializationEntityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var asset = ref builder.ConstructRoot<Graph>();
            var arrayBuilder = builder.Allocate(ref asset.ConnectAssetCommands, 1);
            arrayBuilder[0] = new ConnectAssetCommand
            {
                AssetType = AnimationGraphAssetHashes.ClipHash,
                ClipDuration = 2.0f
            };

            m_Graph = builder.CreateBlobAssetReference<Graph>(Allocator.Persistent);
            m_Graph.Value.m_HashCode = 123456789;
            builder.Dispose();

            var graphManager = m_GraphManagerSystem.GetSingleton<GraphManager>();
            graphManager.AddGraph(new GraphRegister { ID = new GraphID { Value = new Hash128(1, 0, 0, 0) }, Graph = m_Graph });
            graphManager.AddGraph(new GraphRegister { ID = new GraphID { Value = new Hash128(1, 0, 0, 1) }, Graph = m_Graph });
            graphManager.AddGraph(new GraphRegister { ID = new GraphID { Value = new Hash128(1, 0, 0, 2) }, Graph = m_Graph });
            graphManager.AddGraph(new GraphRegister { ID = new GraphID { Value = new Hash128(1, 0, 0, 3) }, Graph = m_Graph });
        }

        [TearDown]
        protected virtual void TearDown()
        {
            m_Graph.Dispose();

            if (World != null)
            {
                // Clean up systems before calling CheckInternalConsistency because we might have filters etc
                // holding on SharedComponentData making checks fail
                var system = World.GetExistingSystem<ComponentSystemBase>();
                while (system != null)
                {
                    World.DestroySystem(system);
                    system = World.GetExistingSystem<ComponentSystemBase>();
                }

                m_ManagerDebug.CheckInternalConsistency();

                World.Dispose();
                World = null;
            }
        }

        protected void UpdateStateMachineSystem(float deltaTime)
        {
            World.SetTime(new TimeData(
                elapsedTime: 0,
                deltaTime: deltaTime
            ));

            m_GraphManagerSystem.Update();
            m_StateMachineSystem.Update();
            m_EndInitializationEntityCommandBufferSystem.Update();
            m_Manager.CompleteAllJobs();
        }

        protected void UpdateStateMachineAndGenerateRenderGraphSystem(float deltaTime)
        {
            World.SetTime(new TimeData(
                elapsedTime: 0,
                deltaTime: deltaTime
            ));

            m_GraphManagerSystem.Update();
            m_StateMachineSystem.Update();
            m_GenerateRenderGraphSystem.Update();
            m_EndInitializationEntityCommandBufferSystem.Update();
            m_Manager.CompleteAllJobs();
        }

        protected void SetupBlackboardValues(Entity entity)
        {
            var characterGameplayProperties = new CharacterGameplayPropertiesCopy();
            characterGameplayProperties.GameplayProperties = new UnsafeHashMap<int, UnsafeList<byte>>(2, Allocator.Persistent);
            int size1 = UnsafeUtility.SizeOf(typeof(BlackboardValuesAuthoringComponent));
            int size2 = UnsafeUtility.SizeOf(typeof(BlackboardValuesAuthoringComponent2));
            var referenceByteList1 = new UnsafeList<byte>(size1, Allocator.Persistent);
            var referenceByteList2 = new UnsafeList<byte>(size2, Allocator.Persistent);
            referenceByteList1.Resize(size1);
            referenceByteList2.Resize(size2);
            int typeIndex1 = TypeManager.GetTypeIndex(typeof(BlackboardValuesAuthoringComponent));
            int typeIndex2 = TypeManager.GetTypeIndex(typeof(BlackboardValuesAuthoringComponent2));
            characterGameplayProperties.GameplayProperties.Add(typeIndex1, referenceByteList1);
            characterGameplayProperties.GameplayProperties.Add(typeIndex2, referenceByteList2);
            m_Manager.SetComponentData(entity, characterGameplayProperties);
            m_Manager.AddComponent<BlackboardValuesAuthoringComponent>(entity);
            m_Manager.AddComponent<BlackboardValuesAuthoringComponent2>(entity);
            var inputReferences = m_Manager.AddBuffer<InputReference>(entity);
            inputReferences.Add(new InputReference() { Entity = entity, Size = size1, TypeHash = 1, TypeIndex = typeIndex1 });
            inputReferences.Add(new InputReference() { Entity = entity, Size = size2, TypeHash = 2, TypeIndex = typeIndex2 });
        }

        protected void CleanupBlackboardValues()
        {
            using (var ecb = new EntityCommandBuffer(Allocator.Temp, PlaybackPolicy.SinglePlayback))
            {
                var destroyPropertiesQuery = m_Manager.CreateEntityQuery(ComponentType.ReadWrite<CharacterGameplayPropertiesCopy>());
                var chunks = destroyPropertiesQuery.CreateArchetypeChunkArray(Allocator.Temp);
                for (int i = 0; i < chunks.Length; ++i)
                {
                    var chunk = chunks[i];
                    var entityArray = chunk.GetNativeArray(m_Manager.GetEntityTypeHandle());
                    var writeBufferArray = chunk.GetNativeArray(m_Manager.GetComponentTypeHandle<CharacterGameplayPropertiesCopy>(false));

                    for (int entityIndex = 0; entityIndex < entityArray.Length; ++entityIndex)
                    {
                        var entity = entityArray[entityIndex];
                        var writeBufferForEntity = writeBufferArray[entityIndex];
                        foreach (var inputBinding in writeBufferForEntity.GameplayProperties)
                        {
                            inputBinding.Value.Dispose();
                        }

                        writeBufferForEntity.GameplayProperties.Dispose();
                        ecb.RemoveComponent(entity, ComponentType.ReadWrite<CharacterGameplayPropertiesCopy>());
                    }
                }

                ecb.Playback(m_Manager);
            }
        }
    }

    class StateMachineTests : StateMachineTestsFixture
    {
        [Test]
        public void StateMachineWithoutOnEnterSelectorTransitionShouldTransitionToFirstState()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[0];
            var globalTransitions = new TransitionDefinition[0];
            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[0];

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();

            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.02f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), "Expecting statemachine instance to be in state 0");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void StateMachineWithOnEnterSelectorTransitionShouldTransitionToFirstStateIfAllConditionsAreFalse()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 1 , RootConditionIndex = 0},
                new TransitionDefinition { TargetStateIndex = 2 , RootConditionIndex = 0},
                new TransitionDefinition { TargetStateIndex = 3 , RootConditionIndex = 0}
            };

            var globalTransitions = new TransitionDefinition[0];
            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 10.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 }
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();

            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.02f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), "Expecting statemachine instance to be in state 0");

            stateMachineDefinition.Dispose();
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void StateMachineWithOnEnterSelectorTransitionShouldTransitionToTargetStateIfAllConditionsAreMeet(int targetState)
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 1, RootConditionIndex = targetState == 1 ? 0 : 1},
                new TransitionDefinition { TargetStateIndex = 2, RootConditionIndex = targetState == 2 ? 0 : 1},
                new TransitionDefinition { TargetStateIndex = 3, RootConditionIndex = targetState == 3 ? 0 : 1}
            };

            var globalTransitions = new TransitionDefinition[0];
            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.GroupOr, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}, //true condition
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 10.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 } // false condition
            };


            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.02f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(targetState), $"Expecting statemachine instance to be in state {targetState}");

            stateMachineDefinition.Dispose();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void StateMachineWithDefaultOnEnterSelectorTransitionShouldTransitionToTargetStateIfAllConditionsAreMeet(int targetState)
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition {TargetStateIndex = 0, RootConditionIndex = -1},
                new TransitionDefinition { TargetStateIndex = 1, RootConditionIndex = targetState == 1 ? 0 : 1},
                new TransitionDefinition { TargetStateIndex = 2, RootConditionIndex = targetState == 2 ? 0 : 1},
                new TransitionDefinition { TargetStateIndex = 3, RootConditionIndex = targetState == 3 ? 0 : 1}
            };

            var globalTransitions = new TransitionDefinition[0];
            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.GroupOr, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}, //true condition
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 10.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 } // false condition
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.02f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(targetState), $"Expecting statemachine instance to be in state {targetState}");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanDoGlobalTransitionInstantaneously()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1}
            };

            var globalTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 1 , RootConditionIndex = 0, Duration = 0}
            };

            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.NodeRegistry.Length, Is.EqualTo(2), "Expecting Two node");
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");
            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");

            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a graph instance");
            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            UpdateStateMachineSystem(0.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.NodeRegistry.Length, Is.EqualTo(2), "Expecting Two node");
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");

            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a graph instance");
            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.5f), "Statemachine time should be equal to 0.5");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.5f), "Graph time should be equal to 0.5");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            UpdateStateMachineSystem(0.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.NodeRegistry.Length, Is.EqualTo(2), "Expecting Two node");
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");

            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a graph instance");
            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(graphInstance.ID, Is.EqualTo(1), "State ID should be equals to state index");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            UpdateStateMachineSystem(0.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.NodeRegistry.Length, Is.EqualTo(2), "Expecting Two node");
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");

            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a graph instance");
            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(graphInstance.ID, Is.EqualTo(1), "State ID should be equals to state index");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.5f), "Statemachine time should be equal to 0.5");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.5f), "Graph time should be equal to 0.5");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanDoGlobalTransitionWithDuration()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };

            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1}
            };

            var globalTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 1 , RootConditionIndex = 0, Duration = 1, AdvanceSourceDuringTransition = 1}
            };

            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.NodeRegistry.Length, Is.EqualTo(2), "Expecting Two node");
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");

            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a graph instance");
            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            // Start transition
            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);

            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");

            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a blend instance");
            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, "Blend source child should be a graph instance");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, "Blend target child should be a graph instance");

            var sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            var targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.0f), "Blend time should be equal to 0");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(1.0f), "Source graph time should be equal to 1");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Target graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            // End Transition
            UpdateStateMachineSystem(2.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting 1 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(0), "Expecting 0 blender instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);

            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");

            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a graph instance");
            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(2.0f), "Statemachine time should be equal to 2");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(2.0f), "Target graph time should be equal to 2");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanDoGlobalTransitionWithDurationWithoutAdvanceSourceDuringTransition()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1}
            };

            var globalTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 1 , RootConditionIndex = 0, Duration = 1, AdvanceSourceDuringTransition = 0}
            };

            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");

            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a graph instance");
            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            // Start transition
            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");

            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a blend instance");
            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, "Blend source child should be a graph instance");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, "Blend target child should be a graph instance");

            var sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            var targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.0f), "Blend time should be equal to 0");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(1.0f), "Source graph time should be equal to 1");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Target graph time should be equal to 0");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            // tick to middle of Transition
            UpdateStateMachineSystem(0.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");

            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a blend instance");
            blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, "Blend source child should be a graph instance");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, "Blend target child should be a graph instance");

            sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.5f), "Statemachine time should be equal to 0.5");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.5f), "Blend time should be equal to 0");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(1.0f), "Source graph time should be equal to 1,");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.5f), "Target graph time should be equal to 0");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            // End Transition
            UpdateStateMachineSystem(1.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting 1 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(0), "Expecting 0 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");

            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, "Statemachine child should be a graph instance");
            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(2.0f), "Statemachine time should be equal to 2");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(2.0f), "Target graph time should be equal to 2");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanFinishTransitionOnTime()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[]
            {};

            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 0, TargetStateIndex = 1, RootConditionIndex = 0, Duration = 0.5f, AdvanceSourceDuringTransition = 1 },
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.75f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 },
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            //state 0
            UpdateStateMachineSystem(0.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            //we tick until transition from 0 -> 1 becomes true
            UpdateStateMachineSystem(1.75f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);

            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");
            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(blendInstance.SourceState, Is.EqualTo(0), $"Expecting blend source state to be 0");
            Assert.That(blendInstance.TargetState, Is.EqualTo(1), $"Expecting blend source state to be 1");
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, $"Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, $"Expecting blend target to be a graph");

            //we tick until transition finishes and we go back to target state
            UpdateStateMachineSystem(0.50f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);

            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void RegressionValidateBlendParentIsBeingUpdatedWhenNestedBlendsAreGenerated()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[]
            {
            };

            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 0, TargetStateIndex = 1, RootConditionIndex = 0, Duration = 0.5f, AdvanceSourceDuringTransition = 1},
                new TransitionDefinition { SourceStateIndex = 1, TargetStateIndex = 2, RootConditionIndex = 1, Duration = 1.5f, AdvanceSourceDuringTransition = 1},
                new TransitionDefinition { SourceStateIndex = 2, TargetStateIndex = 3, RootConditionIndex = 2, Duration = 1.2f, AdvanceSourceDuringTransition = 1},
                new TransitionDefinition { SourceStateIndex = 3, TargetStateIndex = 0, RootConditionIndex = 3, Duration = 2.0f, AdvanceSourceDuringTransition = 1}
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.75f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 3.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 3.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 4.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            //state 0
            UpdateStateMachineSystem(0.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            //we tick until transition from 0 -> 1 becomes true
            UpdateStateMachineSystem(1.75f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");

            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");
            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(blendInstance.SourceState, Is.EqualTo(0), $"Expecting blend source state to be 0");
            Assert.That(blendInstance.TargetState, Is.EqualTo(1), $"Expecting blend source state to be 1");
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, $"Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, $"Expecting blend target to be a graph");

            //we tick until transition finishes and we go back to target state
            UpdateStateMachineSystem(0.51f); //@TODO if i put 0.50f here or even 0.50f+float.Epsilon, our condition for exiting transitions (AccumulatedTime + Dt > transitionduration - float.Epsilon returns false)

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            //we tick until transition from 1 -> 2 becomes true
            UpdateStateMachineSystem(2.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(2), $"Expecting statemachine instance to be in state 2");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");
            blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(blendInstance.SourceState, Is.EqualTo(1), $"Expecting blend source state to be 1");
            Assert.That(blendInstance.TargetState, Is.EqualTo(2), $"Expecting blend source state to be 2");
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, $"Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, $"Expecting blend target to be a graph");

            //we tick until transition finishes and we go back to target state
            UpdateStateMachineSystem(1.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(2), $"Expecting statemachine instance to be in state 2");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            //we tick until transition from 2 -> 3 becomes true
            UpdateStateMachineSystem(1.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(3), $"Expecting statemachine instance to be in state 3");

            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");
            blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(blendInstance.SourceState, Is.EqualTo(2), $"Expecting blend source state to be 2");
            Assert.That(blendInstance.TargetState, Is.EqualTo(3), $"Expecting blend source state to be 3");

            //we tick until transition finishes and we go back to target state
            UpdateStateMachineSystem(1.2f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(3), $"Expecting statemachine instance to be in state 3");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            //we tick until transition from 3 -> 0 becomes true
            UpdateStateMachineSystem(2.8f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");
            blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(blendInstance.SourceState, Is.EqualTo(3), $"Expecting blend source state to be 3");
            Assert.That(blendInstance.TargetState, Is.EqualTo(0), $"Expecting blend source state to be 0");
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, $"Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, $"Expecting blend target to be a graph");

            //we tick until transition from 0 -> 1 becomes true
            UpdateStateMachineSystem(1.75f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");
            blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(blendInstance.SourceState, Is.EqualTo(0), $"Expecting blend source state to be 0"); // @TODO it probably shouldnt call this 0 since it's a blend of 3->0. Not sure what to expect
            Assert.That(blendInstance.TargetState, Is.EqualTo(1), $"Expecting blend source state to be 1");
            Assert.That(smAspect.IsBlendInstance(blendInstance.SourceStateNode), Is.True, $"Expecting blend source to be a blend");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, $"Expecting blend target to be a graph");

            var nestedBlendInstance = smAspect.GetBlendInstance(blendInstance.SourceStateNode);
            Assert.That(nestedBlendInstance.SourceState, Is.EqualTo(3), $"Expecting blend source state to be 3");
            Assert.That(nestedBlendInstance.TargetState, Is.EqualTo(0), $"Expecting blend source state to be 0");
            Assert.That(smAspect.IsGraphInstance(nestedBlendInstance.SourceStateNode), Is.True, $"Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(nestedBlendInstance.TargetStateNode), Is.True, $"Expecting blend target to be a graph");

            //we tick until the nested transition finishes and we update the parent transition
            UpdateStateMachineSystem(0.25f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");
            blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(blendInstance.SourceState, Is.EqualTo(0), $"Expecting blend source state to be 0"); //@TODO after collapsing the internal blend, i'd expect it to call the state 0
            Assert.That(blendInstance.TargetState, Is.EqualTo(1), $"Expecting blend source state to be 1");
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, $"Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, $"Expecting blend target to be a graph");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanDoMultipleTransitionInSameFrame()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 1, RootConditionIndex = 0, Duration = 0, AdvanceSourceDuringTransition = 1}
            };

            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 1, TargetStateIndex = 2, RootConditionIndex = 1, Duration = 1, AdvanceSourceDuringTransition = 1}
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.5f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            // Start all transition
            UpdateStateMachineSystem(2.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting only 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting only 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);

            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(2), $"Expecting statemachine instance to be in state 2");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");
            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, $"Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, $"Expecting blend target to be a graph");

            var sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            var targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.5f), "Statemachine time should be equal to 0.5");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.5f), "Blend time should be equal to 0.5");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(1.0f), "Source graph time should be equal to 1");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.5f), "Target graph time should be equal to 0.5");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanDoMultipleTransitionInSameFrameForMultipleSMInParallel()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 1, RootConditionIndex = 0, Duration = 0, AdvanceSourceDuringTransition = 1}
            };

            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 1, TargetStateIndex = 2, RootConditionIndex = 1, Duration = 1, AdvanceSourceDuringTransition = 1}
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.5f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            const int entityCount = 10;

            using (var entities = new NativeList<Entity>(entityCount, Allocator.Temp))
            {
                for (int i = 0; i < entityCount; ++i)
                {
                    var entity = m_Manager.CreateEntity();
                    Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

                    var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

                    Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

                    entities.Add(entity);
                }

                UpdateStateMachineSystem(0.0f);

                for (int i = 0; i < entityCount; ++i)
                {
                    var entity = entities[i];

                    var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

                    Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
                    Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

                    Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

                    var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
                    Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
                    Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

                    var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
                    Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

                    Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
                    Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
                    Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");
                }

                // Start all transition
                UpdateStateMachineSystem(2.0f);

                for (int i = 0; i < entityCount; ++i)
                {
                    var entity = entities[i];

                    var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

                    Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
                    Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
                    Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting only 1 blend instance");

                    Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

                    var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
                    Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(2), $"Expecting statemachine instance to be in state 2");
                    Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");

                    var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);

                    var sourceNodeHandle = blendInstance.SourceStateNode;
                    var targetNodeHandle = blendInstance.TargetStateNode;

                    Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, $"Expecting blend source to be a graph");
                    Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, $"Expecting blend target to be a graph");

                    var sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
                    var targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

                    Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.5f), "Statemachine time should be equal to 0.5");
                    Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.5f), "Blend time should be equal to 0.5");
                    Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(1.0f), "Source graph time should be equal to 1");
                    Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.5f), "Target graph time should be equal to 0.5");
                    Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");
                }
            }
            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanDoMultipleTransitionInSameFrameWithoutAdvanceSourceDuringTransition()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 1, RootConditionIndex = 0, Duration = 0, AdvanceSourceDuringTransition = 0}
            };

            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 1, TargetStateIndex = 2, RootConditionIndex = 1, Duration = 1, AdvanceSourceDuringTransition = 0}
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.5f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            // Start all transition
            UpdateStateMachineSystem(2.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting only 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(2), $"Expecting statemachine instance to be in state 2");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");

            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);

            var sourceNodeHandle = blendInstance.SourceStateNode;
            var targetNodeHandle = blendInstance.TargetStateNode;

            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, $"Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, $"Expecting blend target to be a graph");

            var sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            var targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.5f), "Statemachine time should be equal to 0.5");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.5f), "Blend time should be equal to 0.5");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(0.5f), "Source graph time should be equal to 0.5");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.5f), "Target graph time should be equal to 0.5");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanDoOutgoingTransitionInstantaneously()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[0];

            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 0, TargetStateIndex = 1,  Duration = 0, RootConditionIndex = 0 }
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 }
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(1), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanDoOutgoingTransitionWithDuration()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[0];

            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 0, TargetStateIndex = 1,  Duration = 1, RootConditionIndex = 0 }
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 }
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            // Start transition
            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");

            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, "Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, "Expecting blend target to be a graph");

            var sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            var targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.0f), "Blend time should be equal to 0");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(1.0f), "Source graph time should be equal to 1");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Target graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            // End Transition
            UpdateStateMachineSystem(2.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting 1 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(0), "Expecting 0 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(2.0f), "Statemachine time should be equal to 2");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(2.0f), "Target graph time should be equal to 2");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanDoOutgoingTransitionWithDurationWithoutAdvanceSourceDuringTransition()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1}
            };

            var globalTransitions = new TransitionDefinition[0];


            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 1 , RootConditionIndex = 0, Duration = 1, AdvanceSourceDuringTransition = 0}
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 1.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            // Start transition
            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");

            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, "Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, "Expecting blend target to be a graph");

            var sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            var targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.0f), "Blend time should be equal to 0");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(1.0f), "Source graph time should be equal to 1");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Target graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            // tick to middle of Transition
            UpdateStateMachineSystem(0.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");

            blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, "Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, "Expecting blend target to be a graph");

            sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.5f), "Statemachine time should be equal to 0.5");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.5f), "Blend time should be equal to 0");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(1.0f), "Source graph time should be equal to 1,");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.5f), "Target graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            // End Transition
            UpdateStateMachineSystem(1.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting 1 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(0), "Expecting 0 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(2.0f), "Statemachine time should be equal to 2");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(2.0f), "Target graph time should be equal to 2");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanDoMultipleOutgoingTransitionInSameFrame()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[0];

            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 0, TargetStateIndex = 1, Duration = 3, RootConditionIndex = 0, AdvanceSourceDuringTransition = 1 },
                new TransitionDefinition { SourceStateIndex = 1, TargetStateIndex = 2, Duration = 2, RootConditionIndex = 0, AdvanceSourceDuringTransition = 1 },
                new TransitionDefinition { SourceStateIndex = 2, TargetStateIndex = 3, Duration = 1, RootConditionIndex = 0, AdvanceSourceDuringTransition = 1 }
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.2f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 }
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            // Start 3 transition
            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(4), "Expecting 4 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(3), "Expecting 3 blend instance");


            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(3), $"Expecting statemachine instance to be in state 3");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");

            var blendInstance3 = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(smAspect.IsBlendInstance(blendInstance3.SourceStateNode), Is.True, "Expecting blend source to be a blend");
            Assert.That(smAspect.IsGraphInstance(blendInstance3.TargetStateNode), Is.True, "Expecting blend target to be a graph");

            var blendInstance2 = smAspect.GetBlendInstance(blendInstance3.SourceStateNode);
            var targetGraphInstance4 = smAspect.GetGraphInstance(blendInstance3.TargetStateNode);

            Assert.That(smAspect.IsBlendInstance(blendInstance2.SourceStateNode), Is.True, "Expecting blend source to be a blend");
            Assert.That(smAspect.IsGraphInstance(blendInstance2.TargetStateNode), Is.True, "Expecting blend target to be a graph");

            var blendInstance1 = smAspect.GetBlendInstance(blendInstance2.SourceStateNode);
            var targetGraphInstance3 = smAspect.GetGraphInstance(blendInstance2.TargetStateNode);

            Assert.That(smAspect.IsGraphInstance(blendInstance1.SourceStateNode), Is.True, "Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance1.TargetStateNode), Is.True, "Expecting blend target to be a graph");

            var sourceGraphInstance1 = smAspect.GetGraphInstance(blendInstance1.SourceStateNode);
            var targetGraphInstance2 = smAspect.GetGraphInstance(blendInstance1.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.4f).Within(1).Ulps, "Statemachine time should be equal to 0.4");
            Assert.That(blendInstance3.AccumulatedTime, Is.EqualTo(0.4f).Within(1).Ulps, "Blend time should be equal to 0.4");
            Assert.That(blendInstance2.AccumulatedTime, Is.EqualTo(0.6f).Within(1).Ulps, "Blend time should be equal to 0.6");
            Assert.That(blendInstance1.AccumulatedTime, Is.EqualTo(0.8f).Within(1).Ulps, "Blend time should be equal to 0.8");
            Assert.That(sourceGraphInstance1.AccumulatedTime, Is.EqualTo(1.0f).Within(1).Ulps, "Target graph time should be equal to 1.0");
            Assert.That(targetGraphInstance2.AccumulatedTime, Is.EqualTo(0.8f).Within(1).Ulps, "Target graph time should be equal to 0.8");
            Assert.That(targetGraphInstance3.AccumulatedTime, Is.EqualTo(0.6f).Within(1).Ulps, "Target graph time should be equal to 0.6");
            Assert.That(targetGraphInstance4.AccumulatedTime, Is.EqualTo(0.4f).Within(1).Ulps, "Target graph time should be equal to 0.4");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance4.AccumulatedTime), "Statemachine time should be equal to target graph time");

            // End all Transition by finishing the top most
            UpdateStateMachineSystem(0.6f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting 1 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(0), "Expecting 0 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(3), $"Expecting statemachine instance to be in state 3");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(1.0f).Within(1).Ulps, "Statemachine time should be equal to 1");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(1.0f).Within(1).Ulps, "Target graph time should be equal to 1");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CannotTransitionToSelfWithGlobalTransition()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, Duration = 3, RootConditionIndex = 0, AdvanceSourceDuringTransition = 1 }
            };

            var outgoingTransitions = new TransitionDefinition[0];

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.2f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 }
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            UpdateStateMachineSystem(0.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.5f).Within(1).Ulps, "Statemachine time should be equal to 0.5");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.5f).Within(1).Ulps, "Target graph time should be equal to 0.5");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanTransitionToSelfWithOutgoingTransition()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[0];


            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 0, TargetStateIndex = 0, Duration = 0.2f, RootConditionIndex = 0, AdvanceSourceDuringTransition = 1 }
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.5f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 }
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            UpdateStateMachineSystem(0.6f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");

            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, "Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, "Expecting blend target to be a graph");

            var sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            var targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.1f).Within(3).Ulps, "Statemachine time should be equal to 0.1");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.1f).Within(3).Ulps, "Blend time should be equal to 0.1");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(0.6f).Within(1).Ulps, "Source graph time should be equal to 0.6");
            Assert.That(sourceGraphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index 0");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.1f).Within(3).Ulps, "Target graph time should be equal to 0.1");
            Assert.That(targetGraphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index 0");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void MultipleOutgoingTransitionAlwaysTransitionToFirstValidWithLowerDeltaTime()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[0];


            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 0, TargetStateIndex = 1, Duration = 1.0f, RootConditionIndex = 0, AdvanceSourceDuringTransition = 1 },
                new TransitionDefinition { SourceStateIndex = 0, TargetStateIndex = 2, Duration = 1.0f, RootConditionIndex = 1, AdvanceSourceDuringTransition = 1 }
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.5f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 },
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.2f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 }
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f), "Statemachine time should be equal to 0");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(0.0f), "Graph time should be equal to 0");
            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(graphInstance.AccumulatedTime), "Statemachine time should be equal to graph time");

            UpdateStateMachineSystem(0.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(2), $"Expecting statemachine instance to be in state 2");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");

            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, "Expecting blend source to be a graph");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, "Expecting blend target to be a graph");

            var sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            var targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.3f).Within(1).Ulps, "Statemachine time should be equal to 0.3");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.3f).Within(1).Ulps, "Blend time should be equal to 0.3");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(0.5f).Within(1).Ulps, "Source graph time should be equal to 0.5");
            Assert.That(sourceGraphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index 0");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.3f).Within(1).Ulps, "Target graph time should be equal to 0.3");
            Assert.That(targetGraphInstance.ID, Is.EqualTo(2), "State ID should be equals to state index 2");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetGraphInstance.AccumulatedTime), "Statemachine time should be equal to target graph time");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void OnEnterTransitionAlwaysTransitionToFirstValidTransition()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 3, RootConditionIndex = 0 },
                new TransitionDefinition { TargetStateIndex = 2, RootConditionIndex = -1 },
                new TransitionDefinition { TargetStateIndex = 1, RootConditionIndex = -1 }
            };

            var globalTransitions = new TransitionDefinition[0];

            var outgoingTransitions = new TransitionDefinition[0];

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.5f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 }
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(2), $"Expecting statemachine instance to be in state 2");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(2), "State ID should be equals to state index");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanTransitionToSubStateMachineWithOnEnterSelector()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
                new Hash128(1, 0, 0, 4),
            };

            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 4, RootConditionIndex = -1 },
            };

            var globalTransitions = new TransitionDefinition[0];
            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[0];

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var subStateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
            };

            var subOnEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 },
            };

            var subGlobalTransitions = new TransitionDefinition[0];

            var subOutgoingTransitions = new TransitionDefinition[0];

            var subConditions = new TransitionConditionFragment[0];

            var subStateMachineDefinition = Builder.CreateStateMachineDefinition(subStateGuids, subOnEnterSelectors, subGlobalTransitions, subOutgoingTransitions, subConditions);

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var asset = ref builder.ConstructRoot<Graph>();
            var graph = builder.CreateBlobAssetReference<Graph>(Allocator.Persistent);
            graph.Value.m_HashCode = 234567891;
            graph.Value.IsStateMachine = true;
            builder.Dispose();

            var graphManager = m_GraphManagerSystem.GetSingleton<GraphManager>();
            graphManager.AddGraph(new GraphRegister { ID = new GraphID { Value = new Hash128(1, 0, 0, 4) }, Graph = graph, StateMachineDefinition = subStateMachineDefinition });

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(2), "Expecting only 2 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(4), $"Expecting statemachine instance to be in state 4");
            Assert.That(smAspect.IsStateMachineInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a statemachine");

            var subStateMachineInstance = smAspect.GetStateMachineInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(subStateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(subStateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(subStateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            graph.Dispose();
            stateMachineDefinition.Dispose();
            subStateMachineDefinition.Dispose();
        }

        [Test]
        public void CanTransitionOnEndOfAnimationConditionWithOutgoingTransition()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3)
            };

            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 },
            };

            var globalTransitions = new TransitionDefinition[0];

            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 0, TargetStateIndex = 1, Duration = 0.0f, RootConditionIndex = 0, AdvanceSourceDuringTransition = 1 },
                new TransitionDefinition { SourceStateIndex = 1, TargetStateIndex = 2, Duration = 0.0f, RootConditionIndex = 1, AdvanceSourceDuringTransition = 1 },
                new TransitionDefinition { SourceStateIndex = 2, TargetStateIndex = 0, Duration = 0.3f, RootConditionIndex = 2, AdvanceSourceDuringTransition = 1 },
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.EndOfDominantAnimation, FloatValue = 0.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 },
                new TransitionConditionFragment { Type = ConditionFragmentType.EndOfDominantAnimation, FloatValue = 1.0f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 },
                new TransitionConditionFragment { Type = ConditionFragmentType.EndOfDominantAnimation, FloatValue = 0.3f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 },
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(1.0f).Within(1).Ulps, "Statemachine time should be equal to 1");

            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f).Within(1).Ulps, "Statemachine time should be equal to 0");

            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(2), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f).Within(1).Ulps, "Statemachine time should be equal to 0");

            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(2), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(1.0f).Within(1).Ulps, "Statemachine time should be equal to 1");

            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.3f).Within(3).Ulps, "Statemachine time should be equal to 0.3");

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void CanTransitionToSubStateMachineWithOutgoingTransition()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
                new Hash128(1, 0, 0, 4),
            };

            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 },
            };

            var globalTransitions = new TransitionDefinition[0];

            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 0, TargetStateIndex = 4, Duration = 0.5f, RootConditionIndex = 0, AdvanceSourceDuringTransition = 1 },
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.5f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 }
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var subStateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
            };

            var subOnEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 },
            };

            var subGlobalTransitions = new TransitionDefinition[0];

            var subOutgoingTransitions = new TransitionDefinition[0];

            var subConditions = new TransitionConditionFragment[0];

            var subStateMachineDefinition = Builder.CreateStateMachineDefinition(subStateGuids, subOnEnterSelectors, subGlobalTransitions, subOutgoingTransitions, subConditions);

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var asset = ref builder.ConstructRoot<Graph>();
            var graph = builder.CreateBlobAssetReference<Graph>(Allocator.Persistent);
            graph.Value.m_HashCode = 234567891;
            graph.Value.IsStateMachine = true;
            builder.Dispose();

            var graphManager = m_GraphManagerSystem.GetSingleton<GraphManager>();
            graphManager.AddGraph(new GraphRegister { ID = new GraphID { Value = new Hash128(1, 0, 0, 4) }, Graph = graph, StateMachineDefinition = subStateMachineDefinition });

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            UpdateStateMachineSystem(0.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(2), "Expecting only 2 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance"); // Source state + sub statemachine state
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(4), $"Expecting statemachine instance to be in state 4");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");

            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(smAspect.IsGraphInstance(blendInstance.SourceStateNode), Is.True, "Expecting blend source to be a graph");
            Assert.That(smAspect.IsStateMachineInstance(blendInstance.TargetStateNode), Is.True, "Expecting blend target to be a statemachine");

            var sourceGraphInstance = smAspect.GetGraphInstance(blendInstance.SourceStateNode);
            var targetStateMachineInstance = smAspect.GetStateMachineInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f).Within(1).Ulps, "Statemachine time should be equal to 0");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.0f).Within(1).Ulps, "Blend time should be equal to 0.0");
            Assert.That(sourceGraphInstance.AccumulatedTime, Is.EqualTo(0.5f).Within(1).Ulps, "Source graph time should be equal to 0.5");
            Assert.That(sourceGraphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index 0");
            Assert.That(targetStateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f).Within(1).Ulps, "Target statemachine time should be equal to 0.0");
            Assert.That(targetStateMachineInstance.CurrentState, Is.EqualTo(0), "Expecting sub statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(targetStateMachineInstance.CurrentStateNode), Is.True, $"Expecting sub state machine's current state to be a graph");
            var subGraphInstance = smAspect.GetGraphInstance(targetStateMachineInstance.CurrentStateNode);
            Assert.That(subGraphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(targetStateMachineInstance.AccumulatedTime), "Statemachine time should be equal to target statemachine time");
            Assert.That(subGraphInstance.AccumulatedTime, Is.EqualTo(targetStateMachineInstance.AccumulatedTime), "Sub graph time should be equal to target statemachine time");

            graph.Dispose();
            stateMachineDefinition.Dispose();
            subStateMachineDefinition.Dispose();
        }

        [Test]
        public void CanTransitionOutOfSubStateMachineWithOutgoingTransition()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
                new Hash128(1, 0, 0, 4),
            };

            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 4, RootConditionIndex = -1 },
            };

            var globalTransitions = new TransitionDefinition[0];
            var outgoingTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { SourceStateIndex = 4, TargetStateIndex = 1, Duration = 0.5f, RootConditionIndex = 0, AdvanceSourceDuringTransition = 1 },
            };

            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.ElapsedTime, FloatValue = 0.5f, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1 }
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            var subStateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
            };

            var subOnEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1 },
            };

            var subGlobalTransitions = new TransitionDefinition[0];

            var subOutgoingTransitions = new TransitionDefinition[0];

            var subConditions = new TransitionConditionFragment[0];

            var subStateMachineDefinition = Builder.CreateStateMachineDefinition(subStateGuids, subOnEnterSelectors, subGlobalTransitions, subOutgoingTransitions, subConditions);

            BlobBuilder builder = new BlobBuilder(Allocator.Temp);
            ref var asset = ref builder.ConstructRoot<Graph>();
            var graph = builder.CreateBlobAssetReference<Graph>(Allocator.Persistent);
            graph.Value.m_HashCode = 234567891;
            graph.Value.IsStateMachine = true;
            builder.Dispose();

            var graphManager = m_GraphManagerSystem.GetSingleton<GraphManager>();
            graphManager.AddGraph(new GraphRegister { ID = new GraphID { Value = new Hash128(1, 0, 0, 4) }, Graph = graph, StateMachineDefinition = subStateMachineDefinition });

            UpdateStateMachineSystem(0.0f);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(2), "Expecting only 2 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting only 1 graph instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(4), $"Expecting statemachine instance to be in state 4");
            Assert.That(smAspect.IsStateMachineInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a statemachine");

            var subStateMachineInstance = smAspect.GetStateMachineInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(subStateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(subStateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");

            var graphInstance = smAspect.GetGraphInstance(subStateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            UpdateStateMachineSystem(0.5f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(2), "Expecting only 2 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(2), "Expecting 2 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(1), "Expecting 1 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsBlendInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a blend");

            var blendInstance = smAspect.GetBlendInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(smAspect.IsStateMachineInstance(blendInstance.SourceStateNode), Is.True, "Expecting blend source to be a statemachine");
            Assert.That(smAspect.IsGraphInstance(blendInstance.TargetStateNode), Is.True, "Expecting blend target to be a graph");

            var sourceStateMachineInstance = smAspect.GetStateMachineInstance(blendInstance.SourceStateNode);
            var targetGraphInstance = smAspect.GetGraphInstance(blendInstance.TargetStateNode);

            Assert.That(stateMachineInstance.AccumulatedTime, Is.EqualTo(0.0f).Within(1).Ulps, "Statemachine time should be equal to 0");
            Assert.That(blendInstance.AccumulatedTime, Is.EqualTo(0.0f).Within(1).Ulps, "Blend time should be equal to 0.0");
            Assert.That(targetGraphInstance.AccumulatedTime, Is.EqualTo(0.0f).Within(1).Ulps, "Target graph time should be equal to 0.0");
            Assert.That(targetGraphInstance.ID, Is.EqualTo(1), "State ID should be equals to state index 1");
            Assert.That(sourceStateMachineInstance.AccumulatedTime, Is.EqualTo(0.5f).Within(1).Ulps, "Source statemachine time should be equal to 0.5");
            Assert.That(sourceStateMachineInstance.CurrentState, Is.EqualTo(0), "Expecting sub statemachine instance to be in state 0");
            Assert.That(smAspect.IsGraphInstance(sourceStateMachineInstance.CurrentStateNode), Is.True, $"Expecting sub state machine's current state to be a graph");
            var subGraphInstance = smAspect.GetGraphInstance(sourceStateMachineInstance.CurrentStateNode);
            Assert.That(subGraphInstance.ID, Is.EqualTo(0), "State ID should be equals to state index");

            Assert.That(subGraphInstance.AccumulatedTime, Is.EqualTo(sourceStateMachineInstance.AccumulatedTime), "Sub graph time should be equal to source statemachine time");

            UpdateStateMachineSystem(1.0f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);

            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect.Graphs.Length, Is.EqualTo(1), "Expecting 1 graph instance");
            Assert.That(smAspect.Blenders.Length, Is.EqualTo(0), "Expecting 0 blend instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");

            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");
            Assert.That(smAspect.IsGraphInstance(stateMachineInstance.CurrentStateNode), Is.True, $"Expecting state machine's current state to be a graph");
            graphInstance = smAspect.GetGraphInstance(stateMachineInstance.CurrentStateNode);
            Assert.That(graphInstance.ID, Is.EqualTo(1), "State ID should be equals to state index");

            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(stateMachineInstance.AccumulatedTime), "graph time should be equal to statemachine time");
            Assert.That(graphInstance.AccumulatedTime, Is.EqualTo(1.0f).Within(1).Ulps, "graph time should be equal to 1.0");

            graph.Dispose();
            stateMachineDefinition.Dispose();
            subStateMachineDefinition.Dispose();
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void StateMachineCanTransitionOnEnterSelectorBlackboardValueTransition(int targetState)
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1},
                new TransitionDefinition { TargetStateIndex = 1, RootConditionIndex = 0},
                new TransitionDefinition { TargetStateIndex = 2, RootConditionIndex = 1},
                new TransitionDefinition { TargetStateIndex = 3, RootConditionIndex = 2}
            };

            int typeIndex = TypeManager.GetTypeIndex(typeof(BlackboardValuesAuthoringComponent));
            GraphVariant constantFloat = new GraphVariant();
            constantFloat.Float = 2.0f;
            GraphVariant constantInt = new GraphVariant();
            constantInt.Int = 2;
            GraphVariant constantBool = new GraphVariant();
            constantBool.Bool = true;
            var globalTransitions = new TransitionDefinition[0];
            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantFloat, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 0, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantInt, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 4, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantBool, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 8, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            SetupBlackboardValues(entity);

            var value = new BlackboardValuesAuthoringComponent();
            if (targetState == 1)
            {
                value.FloatValue = 2.0f;
            }
            else if (targetState == 2)
            {
                value.IntValue = 2;
            }
            else if (targetState == 3)
            {
                value.BoolValue = true;
            }
            m_Manager.SetComponentData(entity, value);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.02f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(targetState), $"Expecting statemachine instance to be in state {targetState}");

            CleanupBlackboardValues();

            stateMachineDefinition.Dispose();
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void StateMachineCanTransitionOnEnterSelectorTransitionFrom2DifferentAuthoringComponentInAGroupAndCondition(int targetState)
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1},
                new TransitionDefinition { TargetStateIndex = 1, RootConditionIndex = 0},
                new TransitionDefinition { TargetStateIndex = 2, RootConditionIndex = 1},
                new TransitionDefinition { TargetStateIndex = 3, RootConditionIndex = 2}
            };

            int typeIndex = TypeManager.GetTypeIndex(typeof(BlackboardValuesAuthoringComponent));
            GraphVariant constantFloat = new GraphVariant();
            constantFloat.Float = 2.0f;
            GraphVariant constantInt = new GraphVariant();
            constantInt.Int = 2;
            GraphVariant constantBool = new GraphVariant();
            constantBool.Bool = true;
            int typeIndex2 = TypeManager.GetTypeIndex(typeof(BlackboardValuesAuthoringComponent2));
            GraphVariant constantFloat2 = new GraphVariant();
            constantFloat2.Float = 1.0f;
            GraphVariant constantInt2 = new GraphVariant();
            constantInt2.Int = 1;
            GraphVariant constantBool2 = new GraphVariant();
            constantBool2.Bool = false;
            var globalTransitions = new TransitionDefinition[0];
            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.GroupAnd, FirstChildConditionIndex = 3, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.GroupAnd, FirstChildConditionIndex = 4, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.GroupAnd, FirstChildConditionIndex = 5, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantFloat, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 0, FirstChildConditionIndex = -1, NextSiblingConditionIndex = 6},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantInt, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 4, FirstChildConditionIndex = -1, NextSiblingConditionIndex = 7},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantBool, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 8, FirstChildConditionIndex = -1, NextSiblingConditionIndex = 8},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantFloat2, BlackboardValueComponentDataTypeIndex = typeIndex2, BlackboardValueOffset = 0, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantInt2, BlackboardValueComponentDataTypeIndex = typeIndex2, BlackboardValueOffset = 4, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantBool2, BlackboardValueComponentDataTypeIndex = typeIndex2, BlackboardValueOffset = 8, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            SetupBlackboardValues(entity);

            var value = new BlackboardValuesAuthoringComponent();
            var value2 = new BlackboardValuesAuthoringComponent2();
            if (targetState == 1)
            {
                value.FloatValue = 2.0f;
                value2.FloatValue2 = 1.0f;
            }
            else if (targetState == 2)
            {
                value.IntValue = 2;
                value2.IntValue2 = 1;
            }
            else if (targetState == 3)
            {
                value.BoolValue = true;
                value2.BoolValue2 = false;
            }
            m_Manager.SetComponentData(entity, value);
            m_Manager.SetComponentData(entity, value2);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.02f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(targetState), $"Expecting statemachine instance to be in state {targetState}");

            CleanupBlackboardValues();

            stateMachineDefinition.Dispose();
        }

        [Test]
        public void StateMachineCanTransitionGlobalBlackboardValueTransition()
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1}
            };

            int typeIndex = TypeManager.GetTypeIndex(typeof(BlackboardValuesAuthoringComponent));
            GraphVariant constantFloat = new GraphVariant();
            constantFloat.Float = 2.0f;
            GraphVariant constantInt = new GraphVariant();
            constantInt.Int = 2;
            GraphVariant constantBool = new GraphVariant();
            constantBool.Bool = true;

            var globalTransitions = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 1, RootConditionIndex = 0 },
                new TransitionDefinition { TargetStateIndex = 2, RootConditionIndex = 1 },
                new TransitionDefinition { TargetStateIndex = 3, RootConditionIndex = 2 }
            };
            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantFloat, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 0, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantInt, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 4, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantBool, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 8, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity, stateMachineDefinition);

            SetupBlackboardValues(entity);

            var smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.02f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            var stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(0), $"Expecting statemachine instance to be in state 0");

            var value = new BlackboardValuesAuthoringComponent();
            value.FloatValue = 2.0f;
            m_Manager.SetComponentData(entity, value);

            UpdateStateMachineSystem(0.02f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(1), $"Expecting statemachine instance to be in state 1");

            value.FloatValue = 0.0f;
            value.IntValue = 2;
            m_Manager.SetComponentData(entity, value);

            UpdateStateMachineSystem(0.02f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(2), $"Expecting statemachine instance to be in state 2");

            value.IntValue = 0;
            value.BoolValue = true;
            m_Manager.SetComponentData(entity, value);

            UpdateStateMachineSystem(0.02f);

            smAspect = StateMachineAspect.Create(entity, m_StateMachineSystem, default);
            Assert.That(smAspect.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            Assert.That(smAspect.IsStateMachineInstance(smAspect.RootNodeHandle), Is.True, "First node should be a statemachine");
            stateMachineInstance = smAspect.GetStateMachineInstance(smAspect.RootNodeHandle);
            Assert.That(stateMachineInstance.CurrentState, Is.EqualTo(3), $"Expecting statemachine instance to be in state 3");

            CleanupBlackboardValues();

            stateMachineDefinition.Dispose();
        }

        [TestCase(1, 2)]
        [TestCase(2, 3)]
        [TestCase(3, 1)]
        public void MultipleInstanceOfStateMachineCanTransitionOnEnterSelectorBlackboardValueTransitionInParallel(int targetState1, int targetState2)
        {
            var stateGuids = new Hash128[]
            {
                new Hash128(1, 0, 0, 0),
                new Hash128(1, 0, 0, 1),
                new Hash128(1, 0, 0, 2),
                new Hash128(1, 0, 0, 3),
            };
            var onEnterSelectors = new TransitionDefinition[]
            {
                new TransitionDefinition { TargetStateIndex = 0, RootConditionIndex = -1},
                new TransitionDefinition { TargetStateIndex = 1, RootConditionIndex = 0},
                new TransitionDefinition { TargetStateIndex = 2, RootConditionIndex = 1},
                new TransitionDefinition { TargetStateIndex = 3, RootConditionIndex = 2}
            };

            int typeIndex = TypeManager.GetTypeIndex(typeof(BlackboardValuesAuthoringComponent));
            GraphVariant constantFloat = new GraphVariant();
            constantFloat.Float = 2.0f;
            GraphVariant constantInt = new GraphVariant();
            constantInt.Int = 2;
            GraphVariant constantBool = new GraphVariant();
            constantBool.Bool = true;
            var globalTransitions = new TransitionDefinition[0];
            var outgoingTransitions = new TransitionDefinition[0];
            var conditions = new TransitionConditionFragment[]
            {
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantFloat, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 0, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantInt, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 4, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1},
                new TransitionConditionFragment { Type = ConditionFragmentType.BlackboardValue, Operation = ComparisonOperation.Equal, CompareValue = constantBool, BlackboardValueComponentDataTypeIndex = typeIndex, BlackboardValueOffset = 8, FirstChildConditionIndex = -1, NextSiblingConditionIndex = -1}
            };

            var stateMachineDefinition = Builder.CreateStateMachineDefinition(stateGuids, onEnterSelectors, globalTransitions, outgoingTransitions, conditions);

            var entity1 = m_Manager.CreateEntity();
            var entity2 = m_Manager.CreateEntity();
            Builder.SetupRootStateMachineEntity(m_Manager, entity1, stateMachineDefinition);
            Builder.SetupRootStateMachineEntity(m_Manager, entity2, stateMachineDefinition);

            SetupBlackboardValues(entity1);
            SetupBlackboardValues(entity2);

            m_Manager.SetComponentData(entity1, CreateBlackboardValueAuthoringComponent(targetState1));
            m_Manager.SetComponentData(entity2, CreateBlackboardValueAuthoringComponent(targetState2));

            var smAspect1 = StateMachineAspect.Create(entity1, m_StateMachineSystem, default);
            var smAspect2 = StateMachineAspect.Create(entity2, m_StateMachineSystem, default);
            Assert.That(smAspect1.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect2.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            UpdateStateMachineSystem(0.02f);

            smAspect1 = StateMachineAspect.Create(entity1, m_StateMachineSystem, default);
            smAspect2 = StateMachineAspect.Create(entity2, m_StateMachineSystem, default);
            Assert.That(smAspect1.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");
            Assert.That(smAspect2.StateMachines.Length, Is.EqualTo(1), "Expecting only 1 statemachine instance");

            Assert.That(smAspect1.IsStateMachineInstance(smAspect1.RootNodeHandle), Is.True, "First node should be a statemachine");
            Assert.That(smAspect2.IsStateMachineInstance(smAspect2.RootNodeHandle), Is.True, "First node should be a statemachine");
            ref var stateMachineInstance1 = ref smAspect1.GetStateMachineInstance(smAspect1.RootNodeHandle);
            Assert.That(stateMachineInstance1.CurrentState, Is.EqualTo(targetState1), $"Expecting statemachine instance to be in state {targetState1}");
            ref var stateMachineInstance2 = ref smAspect2.GetStateMachineInstance(smAspect2.RootNodeHandle);
            Assert.That(stateMachineInstance2.CurrentState, Is.EqualTo(targetState2), $"Expecting statemachine instance to be in state {targetState2}");

            CleanupBlackboardValues();

            stateMachineDefinition.Dispose();

            BlackboardValuesAuthoringComponent CreateBlackboardValueAuthoringComponent(int targetState)
            {
                if (targetState == 1)
                    return new BlackboardValuesAuthoringComponent { FloatValue = 2f };
                if (targetState == 2)
                    return new BlackboardValuesAuthoringComponent { IntValue = 2 };

                //if (targetState == 3)
                return new BlackboardValuesAuthoringComponent { BoolValue = true };
            }
        }
    }
}
