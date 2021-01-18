using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation.StateMachine
{
    static internal partial class Builder
    {
        public static readonly ComponentType[] m_StateMachineComponentTypes =
        {
            typeof(StateMachineVersion),
            typeof(TimeControl),
            typeof(Node),
            typeof(NodeHandle),
            typeof(GraphInstance),
            typeof(BlendInstance),
            typeof(StateMachineInstance),
            typeof(DFGGraphToFree),
            typeof(CharacterGameplayPropertiesCopy)
        };

        public static ComponentTypes StateMachineComponentTypes => new ComponentTypes(m_StateMachineComponentTypes);

        internal static BlobAssetReference<StateMachineDefinition> CreateStateMachineDefinition(
            Hash128[] states,
            TransitionDefinition[] onEnterSelectors,
            TransitionDefinition[] globalTransitions,
            TransitionDefinition[] outgoingTransitions,
            TransitionConditionFragment[] conditionFragments)
        {
            BlobAssetReference<StateMachineDefinition> definitionAsset;
            using (var blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                ref var sm = ref blobBuilder.ConstructRoot<StateMachineDefinition>();

                var statesArray = blobBuilder.Allocate(ref sm.States, states.Length);
                var outgoingTransitionsStartIndicesArray = blobBuilder.Allocate(ref sm.OutgoingTransitionsStartIndices, states.Length);
                var onEnterSelectorsArray = blobBuilder.Allocate(ref sm.OnEnterSelectors, onEnterSelectors.Length);
                var globalTransitionsArray = blobBuilder.Allocate(ref sm.GlobalTransitions, globalTransitions.Length);
                var outgoingTransitionsArray = blobBuilder.Allocate(ref sm.OutgoingTransitions, outgoingTransitions.Length);
                var conditionFragmentsArray = blobBuilder.Allocate(ref sm.ConditionFragments, conditionFragments.Length);

                for (int i = 0; i < states.Length; i++)
                {
                    statesArray[i] = states[i];
                    outgoingTransitionsStartIndicesArray[i] = outgoingTransitions.Length;
                }

                for (int i = 0; i < onEnterSelectors.Length; i++)
                {
                    onEnterSelectorsArray[i].TransitionDefinition = onEnterSelectors[i];
                    onEnterSelectorsArray[i].PropertiesOverride = new TransitionPropertiesOverride(onEnterSelectorsArray[i].TransitionDefinition);
                }

                for (int i = 0; i < globalTransitions.Length; i++)
                    globalTransitionsArray[i] = globalTransitions[i];

                for (int i = 0; i < outgoingTransitions.Length; i++)
                {
                    var transition = outgoingTransitions[i];
                    outgoingTransitionsArray[i] = transition;
                    if (transition.SourceStateIndex != -1 && outgoingTransitionsStartIndicesArray[transition.SourceStateIndex] == outgoingTransitions.Length)
                    {
                        outgoingTransitionsStartIndicesArray[transition.SourceStateIndex] = i;
                    }
                }

                for (int i = 0; i < conditionFragments.Length; ++i)
                {
                    conditionFragmentsArray[i] = conditionFragments[i];
                }

                definitionAsset = blobBuilder.CreateBlobAssetReference<StateMachineDefinition>(Allocator.Persistent);
            }
            return definitionAsset;
        }

        internal static NodeHandle SetupStateMachineNode(StateMachineAspect smAspect, int stateIndex, BlobAssetReference<StateMachineDefinition> stateMachineDefinition)
        {
            var index = smAspect.StateMachines.Add(new StateMachine.StateMachineInstance
            {
                Definition = stateMachineDefinition,
                CurrentState = -1
            });

            var nodeIndex = smAspect.FindFirstFreeNode();
            if (nodeIndex == smAspect.NodeRegistry.Length)
            {
                smAspect.NodeRegistry.Add(new Node {});
            }

            smAspect.NodeRegistry[nodeIndex] = new Node
            {
                Version = smAspect.Version,
                Type = NodeType.StateMachine,
                Index = index
            };

            var handle = new NodeHandle
            {
                Version = smAspect.Version,
                Index = nodeIndex,
            };
            return handle;
        }

        internal static NodeHandle SetupGraphNode(StateMachineAspect smAspect, int stateIndex, Hash128 graphReference)
        {
            var index = smAspect.Graphs.Add(new GraphInstance
            {
                ID = stateIndex,
                GraphReference = graphReference,
                AccumulatedTime = 0.0f,
                SingleAnimationDuration = -1.0f
            });

            var nodeIndex = smAspect.FindFirstFreeNode();
            if (nodeIndex == smAspect.NodeRegistry.Length)
            {
                smAspect.NodeRegistry.Add(new Node {});
            }

            smAspect.NodeRegistry[nodeIndex] = new Node
            {
                Version = smAspect.Version,
                Type = NodeType.Graph,
                Index = index
            };

            var handle = new NodeHandle
            {
                Version = smAspect.Version,
                Index = nodeIndex,
            };
            return handle;
        }

        internal static NodeHandle SetupBlendNode(StateMachineAspect smAspect, BlendInstance blendInstance)
        {
            var index = smAspect.Blenders.Add(blendInstance);

            var nodeIndex = smAspect.FindFirstFreeNode();
            if (nodeIndex == smAspect.NodeRegistry.Length)
            {
                smAspect.NodeRegistry.Add(new Node {});
            }

            smAspect.NodeRegistry[nodeIndex] = new Node
            {
                Version = smAspect.Version,
                Type = NodeType.Blend,
                Index = index
            };

            var handle = new NodeHandle
            {
                Version = smAspect.Version,
                Index = nodeIndex,
            };
            return handle;
        }

        internal static void SetupRootStateMachineEntity(EntityManager dstManager, Entity entity, BlobAssetReference<StateMachineDefinition> stateMachineDefinition)
        {
            dstManager.AddComponents(entity, StateMachineComponentTypes);

            var nodeRegistry = dstManager.GetBuffer<Node>(entity);
            var orderedNodes = dstManager.GetBuffer<NodeHandle>(entity);
            var graphs = dstManager.GetBuffer<GraphInstance>(entity);
            var blenders = dstManager.GetBuffer<BlendInstance>(entity);
            var stateMachines = dstManager.GetBuffer<StateMachineInstance>(entity);

            var index = stateMachines.Add(new StateMachineInstance
            {
                Definition = stateMachineDefinition,
                CurrentState = -1
            });

            var nodeIndex = nodeRegistry.Add(new Node
            {
                Type = NodeType.StateMachine,
                Index = index
            });
        }
    }
}
