using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation.StateMachine
{
    static internal partial class Core
    {
        public struct StateMachinesContext
        {
            static public StateMachinesContext Create(SystemBase systemBase, EntityCommandBuffer.ParallelWriter ECB)
            {
                return new StateMachinesContext
                {
                    StateMachineVersion = systemBase.GetComponentDataFromEntity<StateMachineVersion>(),
                    StateMachineSystemState = systemBase.GetComponentDataFromEntity<StateMachineSystemState>(),
                    NodeRegistry = systemBase.GetBufferFromEntity<Node>(),
                    OrderedNode = systemBase.GetBufferFromEntity<NodeHandle>(),
                    StateMachines = systemBase.GetBufferFromEntity<StateMachineInstance>(),
                    Blenders = systemBase.GetBufferFromEntity<BlendInstance>(),
                    Graphs = systemBase.GetBufferFromEntity<GraphInstance>(),
                    DFGGraphToFree = systemBase.GetBufferFromEntity<DFGGraphToFree>(),
                    GameplayPropertiesCopy = systemBase.GetComponentDataFromEntity<CharacterGameplayPropertiesCopy>(true),
                    TimeControls = systemBase.GetComponentDataFromEntity<TimeControl>(true),
                    ECB = ECB,
                    GraphManager = systemBase.GetSingleton<GraphManager>()
                };
            }

            public StateMachineAspect GetStateMachineAspect(Entity entity)
            {
                return new StateMachineAspect
                {
                    Entity = entity,
                    StateMachineVersion = StateMachineVersion,
                    StateMachineSystemState = StateMachineSystemState,
                    NodeRegistry = NodeRegistry[entity],
                    OrderedNode = OrderedNode[entity],
                    StateMachines = StateMachines[entity],
                    Blenders = Blenders[entity],
                    Graphs = Graphs[entity],
                    DFGGraphToFree = DFGGraphToFree[entity],
                    GameplayPropertiesCopy = GameplayPropertiesCopy[entity],
                    TimeControl = TimeControls[entity],
                    ECB = ECB,
                    GraphManager = GraphManager
                };
            }

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<StateMachineVersion>         StateMachineVersion;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<StateMachineSystemState>     StateMachineSystemState;
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<Node>                               NodeRegistry;
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<NodeHandle>                         OrderedNode;
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<StateMachineInstance>               StateMachines;
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<BlendInstance>                      Blenders;
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<GraphInstance>                      Graphs;
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<DFGGraphToFree>                     DFGGraphToFree;

            [ReadOnly] public ComponentDataFromEntity<CharacterGameplayPropertiesCopy>    GameplayPropertiesCopy;

            [ReadOnly] public ComponentDataFromEntity<TimeControl>      TimeControls;

            public EntityCommandBuffer.ParallelWriter                   ECB;
            [ReadOnly] public GraphManager                              GraphManager;
        }

        static public void StateMachineInit(StateMachineAspect smAspect, ref StateMachineInstance stateMachineInstance, ref TransitionDefinition transitionDefinition)
        {
            if (stateMachineInstance.CurrentState == -1)
            {
                SelectEnterState(smAspect, ref stateMachineInstance, ref transitionDefinition);
            }
        }

        static void SelectEnterState(StateMachineAspect smAspect, ref StateMachineInstance stateMachineInstance, ref TransitionDefinition transitionDefinition)
        {
            if (smAspect.ErrorCode != Error.NoEnterSelectorTransition)
            {
                // we want to skip the default enter selector unless none of the others are true
                int indexDefaultEnterSelector = -1;
                for (int i = 0; i < stateMachineInstance.Definition.Value.OnEnterSelectors.Length; i++)
                {
                    var enterSelectorTransition = stateMachineInstance.Definition.Value.OnEnterSelectors[i];
                    if (enterSelectorTransition.TransitionDefinition.RootConditionIndex == -1)
                    {
                        //if indexDefaultEnterSelector is not -1 when we get here, it means we have more than one Default OnEnterSelectors. We should detect it at compilation time or SM runtime build time.
                        // for now we will take the first default OnEnterSelector
                        if (indexDefaultEnterSelector == -1)
                            indexDefaultEnterSelector = i;
                        continue;
                    }
                    if (NaiveStackBustingEvaluateConditionFragment(enterSelectorTransition.TransitionDefinition.RootConditionIndex, ref stateMachineInstance, new ConditionFragmentEvaluationContext()
                    {
                        SMAspect = smAspect,
                        DeltaTimeAbsolute = 0.0f
                    }) >= 0.0f)
                    {
                        enterSelectorTransition.UpdateTransitionProperties(ref transitionDefinition);

                        stateMachineInstance.CurrentState = enterSelectorTransition.TransitionDefinition.TargetStateIndex;
                        stateMachineInstance.CurrentStateNode = CreateInstance(smAspect, ref stateMachineInstance, stateMachineInstance.CurrentState, ref transitionDefinition);
                        return;
                    }
                }

                if (indexDefaultEnterSelector != -1)
                {
                    var enterSelectorTransition = stateMachineInstance.Definition.Value.OnEnterSelectors[indexDefaultEnterSelector];
                    enterSelectorTransition.UpdateTransitionProperties(ref transitionDefinition);
                    stateMachineInstance.CurrentState = enterSelectorTransition.TransitionDefinition.TargetStateIndex;
                    stateMachineInstance.CurrentStateNode = CreateInstance(smAspect, ref stateMachineInstance, stateMachineInstance.CurrentState, ref transitionDefinition);
                    return;
                }
            }

            smAspect.ErrorCode = Error.NoEnterSelectorTransition;
            stateMachineInstance.CurrentState = 0;
            stateMachineInstance.CurrentStateNode = CreateInstance(smAspect, ref stateMachineInstance, stateMachineInstance.CurrentState, ref transitionDefinition);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static internal void UnknownGraphID(GraphID graphID)
        {
            throw new InvalidOperationException($"StateMachine: GraphID '{graphID.Value}' not found.");
        }

        static NodeHandle CreateInstance(StateMachineAspect smAspect, ref StateMachineInstance stateMachineInstance, int stateIndex, ref TransitionDefinition transitionDefinition)
        {
            Hash128 stateDefinitionId = stateMachineInstance.Definition.Value.States[stateIndex];
            var graphID = new GraphID
            {
                Value = stateDefinitionId
            };

            if (!smAspect.GraphManager.TryGetGraph(graphID, out var graph))
            {
                UnknownGraphID(graphID);
                smAspect.ErrorCode = Error.GraphIDNotFound;
            }
            NodeHandle nodeHandle;
            if (graph.Graph.IsCreated && graph.Graph.Value.IsStateMachine)
            {
                nodeHandle = CreateStateMachineStateInstance(smAspect, stateIndex, stateDefinitionId, graph.StateMachineDefinition);
            }
            else
            {
                nodeHandle = CreateGraphStateInstance(smAspect, stateIndex, stateDefinitionId);
            }
            Init(smAspect, nodeHandle, ref transitionDefinition);
            return nodeHandle;
        }

        static NodeHandle CreateStateMachineStateInstance(StateMachineAspect smAspect, int stateIndex, Hash128 definitionId, BlobAssetReference<StateMachineDefinition> stateMachineDefinition)
        {
            return Builder.SetupStateMachineNode(smAspect, stateIndex, stateMachineDefinition);
        }

        static NodeHandle CreateGraphStateInstance(StateMachineAspect smAspect, int stateIndex, Hash128 definitionId)
        {
            var newGraphStateNode = Builder.SetupGraphNode(smAspect, stateIndex, definitionId);
            var graphDefinitionHandle = new GraphMessage { GraphId = definitionId, GraphManager = smAspect.GraphManager };
            ref var graphInstance = ref smAspect.GetGraphInstance(newGraphStateNode);
            graphInstance.SingleAnimationDuration = GetAnimationDurationFromGraphID(graphDefinitionHandle);
            return newGraphStateNode;
        }

        static NodeHandle CreateBlendStateInstance(StateMachineAspect smAspect, ref StateMachineInstance stateMachineInstance, BlendInstance blendInstance)
        {
            return Builder.SetupBlendNode(smAspect, blendInstance);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static internal void InvalidNodeInstanceType()
        {
            throw new InvalidOperationException("Invalid NodeInstance Type.");
        }

        static void TransitionToState(StateMachineAspect smAspect, ref StateMachineInstance stateMachineInstance, int transitionTargetStateIndex, ref TransitionDefinition transitionDefinition, float startTimeDT)
        {
            var sourceState = stateMachineInstance.CurrentState;
            var sourceStateNode = stateMachineInstance.CurrentStateNode;

            stateMachineInstance.CurrentState = transitionTargetStateIndex;
            stateMachineInstance.AccumulatedTime = 0;

            //@TODO watchout, by using dt here, we'd assume it will be completely consumed. Perhaps we are supposed to transition before we get to that point
            if (transitionDefinition.Duration > float.Epsilon)
            {
                var targetStateNode = CreateInstance(smAspect, ref stateMachineInstance, transitionTargetStateIndex, ref transitionDefinition);

                var blendInstance = new BlendInstance
                {
                    ID = transitionDefinition.ID,
                    SourceState = sourceState,
                    TargetState = transitionTargetStateIndex,
                    ParentStateNode = smAspect.CurrentNodeHandle,
                    SourceStateNode = sourceStateNode,
                    TargetStateNode = targetStateNode,
                    TransitionDefinition = transitionDefinition,
                    StartTimeDT = startTimeDT
                };


                var blendNode = CreateBlendStateInstance(smAspect, ref stateMachineInstance, blendInstance);
                smAspect.SetParentStateNode(blendNode, sourceStateNode);
                smAspect.SetParentStateNode(blendNode, targetStateNode);
                stateMachineInstance.CurrentStateNode = blendNode;
            }
            else
            {
                smAspect.RemoveNodeRecursive(sourceStateNode);

                var targetStateNode = CreateInstance(smAspect, ref stateMachineInstance, transitionTargetStateIndex, ref transitionDefinition);
                smAspect.SetParentStateNode(smAspect.CurrentNodeHandle, targetStateNode);
                stateMachineInstance.CurrentStateNode = targetStateNode;
            }
        }

        static unsafe public void Init(StateMachineAspect smAspect, NodeHandle nodeHandle, ref TransitionDefinition transitionDefinition)
        {
            smAspect.ValidateNodeHandle(nodeHandle);

            if (smAspect.IsStateMachineInstance(nodeHandle))
            {
                ref var stateMachineInstance = ref smAspect.GetStateMachineInstance(nodeHandle);
                StateMachineInit(smAspect, ref stateMachineInstance, ref transitionDefinition);
            }
            else if (smAspect.IsBlendInstance(nodeHandle))
            {
            }
            else if (smAspect.IsGraphInstance(nodeHandle))
            {
            }
        }

        static unsafe public void PreUpdate(StateMachineAspect smAspect)
        {
            var orderedNode = smAspect.OrderedNode;

            // Set capacity to last frame evaluation length
            smAspect.NodeStack = new NativeStack<NodeContext>(orderedNode.Length, Allocator.Temp);

            orderedNode.Clear();

            smAspect.NodeStack.Push(new NodeContext
            {
                NodeHandle = smAspect.RootNodeHandle,
                TimeControl = smAspect.TimeControl,
            });

            while (!smAspect.NodeStack.IsEmpty)
            {
                var nodeContext = smAspect.NodeStack.Top();
                var nodeHandle = nodeContext.NodeHandle;

                smAspect.CurrentNodeHandle = nodeHandle;
                smAspect.TimeControl = nodeContext.TimeControl;

                smAspect.NodeStack.Pop();

                orderedNode.Add(nodeHandle);

                if (smAspect.IsStateMachineInstance(nodeHandle))
                {
                    ref var stateMachineInstance = ref smAspect.GetStateMachineInstance(nodeHandle);
                    StateMachinePreUpdate(smAspect, ref stateMachineInstance);
                }
                else if (smAspect.IsBlendInstance(nodeHandle))
                {
                    ref var blendInstance = ref smAspect.GetBlendInstance(nodeHandle);
                    BlendPreUpdate(smAspect, ref blendInstance, nodeHandle);
                }
                else if (smAspect.IsGraphInstance(nodeHandle))
                {
                    ref var graphInstance = ref smAspect.GetGraphInstance(nodeHandle);
                    GraphPreUpdate(smAspect, ref graphInstance);
                }
            }
            smAspect.NodeStack.Dispose();
        }

        static public float GraphPreUpdate(StateMachineAspect smAspect, ref GraphInstance graphInstance)
        {
            //@TODO sonny, if we're supposed to only pass part of the DT to a graph state, we shouldn't be sending the ScaledDeltaTime right ? we are adding the full dt to the graph accumulated time
            graphInstance.AccumulatedTime += smAspect.ScaledDeltaTime;
            graphInstance.CurrentFrameDeltaTime = smAspect.ScaledDeltaTime;
            return 0.0f;
        }

        static public float BlendPreUpdate(StateMachineAspect smAspect, ref BlendInstance blendInstance, NodeHandle blendNodeHandle)
        {
            float newTime = blendInstance.AccumulatedTime + smAspect.ScaledDeltaTime;
            float durationMinusEpsilon = blendInstance.TransitionDefinition.Duration - float.Epsilon;
            if (newTime >= durationMinusEpsilon)
            {
                smAspect.NodeStack.Push(new NodeContext { NodeHandle = blendInstance.TargetStateNode, TimeControl = smAspect.TimeControl });

                smAspect.SetCurrentStateNode(blendInstance.ParentStateNode, blendInstance.TargetStateNode);

                smAspect.RemoveNodeRecursive(blendInstance.SourceStateNode);
                smAspect.RemoveNode(blendNodeHandle);
            }
            else
            {
                var sourceTimeControl = smAspect.TimeControl;
                var targetTimeControl = smAspect.TimeControl;

                // tick source up to the start of the transition
                sourceTimeControl.DeltaRatio += blendInstance.StartTimeDT;

                // Even if the source doesn't advance time during a transition we still need to tick up to the start of the transition
                if (blendInstance.TransitionDefinition.AdvanceSourceDuringTransition == 0)
                {
                    sourceTimeControl.DeltaRatio = math.select(0, blendInstance.StartTimeDT, blendInstance.StartTimeDT > 0);
                }

                targetTimeControl.DeltaRatio = smAspect.ScaledDeltaTime;
                blendInstance.AccumulatedTime += smAspect.ScaledDeltaTime;
                blendInstance.StartTimeDT = 0;
                smAspect.NodeStack.Push(new NodeContext { NodeHandle = blendInstance.SourceStateNode, TimeControl = sourceTimeControl });
                smAspect.NodeStack.Push(new NodeContext { NodeHandle = blendInstance.TargetStateNode, TimeControl = targetTimeControl });

                if (blendInstance.TransitionDefinition.Duration > float.Epsilon)
                    blendInstance.Weight = Math.Min(blendInstance.AccumulatedTime, blendInstance.TransitionDefinition.Duration) / blendInstance.TransitionDefinition.Duration;
            }

            return 0.0f;
        }

        static readonly int k_MaxNumberOfTransitionTakenSafeguard = 10;
        static unsafe public float StateMachinePreUpdate(StateMachineAspect smAspect, ref StateMachineInstance stateMachineInstance)
        {
            float dt = smAspect.ScaledDeltaTime;
            float remainingDt = dt;

            bool foundValidTransition = false;
            int indexFirstValidTransition = -1;
            float timeFirstValidTransition = float.MaxValue;
            for (int i = 0; i < stateMachineInstance.Definition.Value.GlobalTransitions.Length; i++)
            {
                var globalTransition = stateMachineInstance.Definition.Value.GlobalTransitions[i];

                if (globalTransition.TargetStateIndex == stateMachineInstance.CurrentState)
                    continue;

                float startTime = NaiveStackBustingEvaluateConditionFragment(globalTransition.RootConditionIndex, ref stateMachineInstance, new ConditionFragmentEvaluationContext() {DeltaTimeAbsolute = remainingDt, SMAspect = smAspect});
                if (startTime < 0.0f)
                    continue;
                if (startTime < timeFirstValidTransition)
                {
                    foundValidTransition = true;
                    indexFirstValidTransition = i;
                    timeFirstValidTransition = startTime;
                    if (startTime == 0.0f)
                        break;
                }
            }
            if (foundValidTransition)
            {
                var globalTransition = stateMachineInstance.Definition.Value.GlobalTransitions[indexFirstValidTransition];
                TransitionToState(smAspect, ref stateMachineInstance, globalTransition.TargetStateIndex, ref globalTransition, timeFirstValidTransition);
                remainingDt -= timeFirstValidTransition;
            }

            int numberOfTransitionsTaken = 0;
            // this array needs to be outside of the while remainingDT > 0.0f loop to make sure we don't go over a transition multiple times through cycling with multiple transitions
            var outgoingTransitionTaken = new UnsafeBitArray(stateMachineInstance.Definition.Value.OutgoingTransitions.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
            do
            {
                foundValidTransition = false;
                indexFirstValidTransition = -1;
                timeFirstValidTransition = float.MaxValue;

                for (int i = stateMachineInstance.Definition.Value.OutgoingTransitionsStartIndices[stateMachineInstance.CurrentState]; i < stateMachineInstance.Definition.Value.OutgoingTransitions.Length; ++i)
                {
                    var transition = stateMachineInstance.Definition.Value.OutgoingTransitions[i];
                    if (transition.SourceStateIndex != stateMachineInstance.CurrentState)
                        break;

                    float startTime = NaiveStackBustingEvaluateConditionFragment(transition.RootConditionIndex, ref stateMachineInstance, new ConditionFragmentEvaluationContext() {DeltaTimeAbsolute = remainingDt, SMAspect = smAspect});
                    if (startTime < 0.0f)
                        continue;
                    if (startTime < timeFirstValidTransition)
                    {
                        foundValidTransition = true;
                        indexFirstValidTransition = i;
                        timeFirstValidTransition = startTime;
                        if (startTime == 0.0f)
                            break;
                    }
                }

                if (foundValidTransition)
                {
                    var transition = stateMachineInstance.Definition.Value.OutgoingTransitions[indexFirstValidTransition];
                    if (outgoingTransitionTaken.IsSet(indexFirstValidTransition))
                    {
                        // this check should be moved up to avoid reusing that transition a 2nd time if there's another one after that would want to be taken.
                        UnityEngine.Debug.LogError($"Transition {transition.ID} was taken more than once during frame");
                    }

                    outgoingTransitionTaken.Set(indexFirstValidTransition, true);
                    TransitionToState(smAspect, ref stateMachineInstance, transition.TargetStateIndex, ref transition, timeFirstValidTransition);
                    remainingDt -= timeFirstValidTransition;
                    ++numberOfTransitionsTaken;
                    if (numberOfTransitionsTaken > k_MaxNumberOfTransitionTakenSafeguard)
                    {
                        UnityEngine.Debug.LogError($"More than {k_MaxNumberOfTransitionTakenSafeguard} transitions were taken within this frame");
                        remainingDt = 0.0f;
                    }
                }
            }
            while (remainingDt > 0.0f && foundValidTransition);

            stateMachineInstance.AccumulatedTime += remainingDt;

            var timeControl = smAspect.TimeControl;
            timeControl.DeltaRatio = remainingDt;

            smAspect.NodeStack.Push(new NodeContext { NodeHandle = stateMachineInstance.CurrentStateNode, TimeControl = timeControl });

            return remainingDt;
        }

        [Flags]
        internal enum ConnectTo : byte
        {
            None = 0,
            Root = 1 << 1,
            Source = 1 << 2,
        }

        static unsafe public void UpdateRenderGraph(StateMachineAspect smAspect, Unity.Animation.IAnimationGraphSystem graphSystem, NodeSet set, Rig rig, NodeHandle<ComponentNode> rigEntityNode, ref NativeArray<InputReference> inputs, EntityManager mgr)
        {
            var orderedNode = smAspect.OrderedNode;

            var nodeToConnect = stackalloc ConnectTo[orderedNode.Length];

            // 1. Process the free list and dispose all graph node
            for (int i = 0; i < smAspect.DFGGraphToFree.Length; i++)
            {
                if (smAspect.DFGGraphToFree[i].GraphHandle != default)
                    graphSystem.Dispose(smAspect.DFGGraphToFree[i].GraphHandle);
            }
            smAspect.DFGGraphToFree.Clear();

            // 2. Create Node
            for (int i = 0; i < orderedNode.Length; i++)
            {
                var nodeHandle = orderedNode[i];
                if (smAspect.IsStateMachineInstance(nodeHandle))
                {
                    ref var stateMachineInstance = ref smAspect.GetStateMachineInstance(nodeHandle);

                    if (!set.Exists(stateMachineInstance.OutputNode))
                    {
                        var graphHandle = graphSystem.CreateGraph();
                        stateMachineInstance.GraphHandle = graphHandle;
                        stateMachineInstance.OutputNode = graphSystem.CreateNode<KernelPassThroughNodeBufferFloat>(graphHandle);
                        stateMachineInstance.OutputPortID = (OutputPortID)KernelPassThroughNodeBufferFloat.KernelPorts.Output;

                        set.SendMessage(stateMachineInstance.OutputNode, (InputPortID)KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize,  rig.Value.Value.Bindings.StreamSize);

                        nodeToConnect[i] |= ConnectTo.Source;
                        nodeToConnect[i] |= (i == 0 ? ConnectTo.Root : ConnectTo.None);
                    }
                    else if (!set.Exists(stateMachineInstance.ConnectToNode))
                    {
                        nodeToConnect[i] |= ConnectTo.Source;
                    }
                }
                else if (smAspect.IsBlendInstance(nodeHandle))
                {
                    ref var blendInstance = ref smAspect.GetBlendInstance(nodeHandle);
                    if (!set.Exists(blendInstance.OutputNode))
                    {
                        var graphHandle = graphSystem.CreateGraph();
                        blendInstance.GraphHandle = graphHandle;

                        // TODO@sonny depending on the blend type instanciate the right node
                        // linear, Inertia
                        blendInstance.OutputNode = graphSystem.CreateNode<MixerNode>(graphHandle);
                        blendInstance.OutputPortID = (OutputPortID)MixerNode.KernelPorts.Output;

                        set.SendMessage(blendInstance.OutputNode, (InputPortID)MixerNode.SimulationPorts.Rig, rig);
                        set.SetData(blendInstance.OutputNode, (InputPortID)MixerNode.KernelPorts.Weight, blendInstance.Weight);

                        nodeToConnect[i] |= ConnectTo.Source;
                        nodeToConnect[i] |= (i == 0 ? ConnectTo.Root : ConnectTo.None);

                        if (i > 0)
                        {
                            nodeToConnect[i - 1] |= ConnectTo.Source;
                        }
                    }
                    else
                    {
                        if (!set.Exists(blendInstance.ConnectToNode1) || !set.Exists(blendInstance.ConnectToNode2))
                        {
                            nodeToConnect[i] |= ConnectTo.Source;
                        }
                        set.SetData(blendInstance.OutputNode, (InputPortID)MixerNode.KernelPorts.Weight, blendInstance.Weight);
                    }
                }
                else if (smAspect.IsGraphInstance(nodeHandle))
                {
                    ref var graphInstance = ref smAspect.GetGraphInstance(nodeHandle);
                    if (!set.Exists(graphInstance.OutputNode))
                    {
                        var graphHandle = graphSystem.CreateGraph();
                        graphInstance.GraphHandle = graphHandle;

                        graphInstance.OutputNode = graphSystem.CreateNode<PreAnimationGraphSlot>(graphHandle);
                        graphInstance.OutputPortID = (OutputPortID)PreAnimationGraphSlot.KernelPorts.Output;

                        set.SendMessage(graphInstance.OutputNode, (InputPortID)PreAnimationGraphSlot.SimulationPorts.Context, rig);
                        set.SendMessage(graphInstance.OutputNode, (InputPortID)PreAnimationGraphSlot.SimulationPorts.TimeControl, new Animation.TimeControl() {AbsoluteTime = graphInstance.CurrentFrameDeltaTime, Timescale = 1.0f});
                        set.SendMessage(graphInstance.OutputNode, (InputPortID)PreAnimationGraphSlot.SimulationPorts.GraphID, new GraphMessage { GraphId = graphInstance.GraphReference, GraphManager = smAspect.GraphManager });
                        set.SendMessage(graphInstance.OutputNode, (InputPortID)PreAnimationGraphSlot.SimulationPorts.EntityManager, mgr);
                        set.SendMessage(graphInstance.OutputNode, (InputPortID)PreAnimationGraphSlot.SimulationPorts.InputReferences, inputs);

                        nodeToConnect[i] |= ConnectTo.Source;
                        nodeToConnect[i] |= (i == 0 ? ConnectTo.Root : ConnectTo.None);
                    }
                    else
                    {
                        set.SendMessage(graphInstance.OutputNode, (InputPortID)PreAnimationGraphSlot.SimulationPorts.TimeControl, new Unity.Animation.TimeControl() {AbsoluteTime = graphInstance.CurrentFrameDeltaTime, Timescale = 1.0f});
                    }
                }
            }

            // 2. Connect node
            if (nodeToConnect[0].HasFlag(ConnectTo.Root))
                ConnectNode(smAspect, set, rigEntityNode, ComponentNode.Input(new ComponentType(typeof(AnimatedData))), orderedNode[0]);

            for (int i = 0; i < orderedNode.Length; i++)
            {
                var nodeHandle = orderedNode[i];
                if (nodeToConnect[i].HasFlag(ConnectTo.Source) && smAspect.IsStateMachineInstance(nodeHandle))
                {
                    ref var animationSource = ref smAspect.GetStateMachineInstance(nodeHandle);

                    if (set.Exists(animationSource.ConnectToNode))
                    {
                        set.Disconnect(animationSource.ConnectToNode, animationSource.ConnectToPortID, animationSource.OutputNode, (InputPortID)KernelPassThroughNodeBufferFloat.KernelPorts.Input);
                    }

                    var connectTo = ConnectNode(smAspect, set, animationSource.OutputNode, (InputPortID)KernelPassThroughNodeBufferFloat.KernelPorts.Input, animationSource.CurrentStateNode);
                    animationSource.ConnectToNode = connectTo.Item1;
                    animationSource.ConnectToPortID = connectTo.Item2;
                }
                else if (nodeToConnect[i].HasFlag(ConnectTo.Source) && smAspect.IsBlendInstance(nodeHandle))
                {
                    ref var animationSource = ref smAspect.GetBlendInstance(nodeHandle);

                    if (set.Exists(animationSource.ConnectToNode1))
                    {
                        set.Disconnect(animationSource.ConnectToNode1, animationSource.ConnectToPortID1, animationSource.OutputNode, (InputPortID)MixerNode.KernelPorts.Input0);
                    }
                    if (set.Exists(animationSource.ConnectToNode2))
                    {
                        set.Disconnect(animationSource.ConnectToNode2, animationSource.ConnectToPortID2, animationSource.OutputNode, (InputPortID)MixerNode.KernelPorts.Input1);
                    }

                    var connectTo1 = ConnectNode(smAspect, set, animationSource.OutputNode, (InputPortID)MixerNode.KernelPorts.Input0, animationSource.SourceStateNode);
                    var connectTo2 = ConnectNode(smAspect, set, animationSource.OutputNode, (InputPortID)MixerNode.KernelPorts.Input1, animationSource.TargetStateNode);

                    animationSource.ConnectToNode1 = connectTo1.Item1;
                    animationSource.ConnectToPortID1 = connectTo1.Item2;
                    animationSource.ConnectToNode2 = connectTo2.Item1;
                    animationSource.ConnectToPortID2 = connectTo2.Item2;
                }
                else if (nodeToConnect[i].HasFlag(ConnectTo.Source) && smAspect.IsGraphInstance(nodeHandle))
                {
                }
            }
        }

        // @DEVNOTE: This function will need to be modified to fetch the clip duration from the AssetManager for a specific clip
        // Right now, it expects the graph to store the duration of its only clip and access it from the definition.
        // The commented code at the end is what we'd be calling to get the real clip, but it's not burstable because of the managed object.
        static float GetAnimationDurationFromGraphID(GraphMessage graphDefinitionHandle)
        {
            var graphID = new GraphID
            {
                Value = graphDefinitionHandle.GraphId,
            };
            if (!graphDefinitionHandle.GraphManager.TryGetGraph(graphID, out var graph))
            {
                return -1.0f;
            }

            if (graph.Graph.Value.ConnectAssetCommands.Length != 1)
            {
                return -1.0f;
            }

            if (graph.Graph.Value.ConnectAssetCommands[0].AssetType != AnimationGraphAssetHashes.ClipHash)
            {
                return -1.0f;
            }

            return graph.Graph.Value.ConnectAssetCommands[0].ClipDuration;

//            BlobAssetReference<Clip> asset = GenericAssetManager<Clip, ClipRegister>.Instance.GetAsset(graph.Graph.Value.ConnectAssetCommands[0].AssetID);
//            if (!asset.IsCreated)
//            {
//                return -1.0f;
//            }
//
//            return asset.Value.Duration;
        }

        static unsafe public (DataFlowGraph.NodeHandle, OutputPortID) ConnectNode(StateMachineAspect smAspect, NodeSet set, DataFlowGraph.NodeHandle targetNodeHandle, InputPortID targetInputPortID, NodeHandle nodeHandle)
        {
            smAspect.ValidateNodeHandle(nodeHandle);

            DataFlowGraph.NodeHandle connectTo = default;
            OutputPortID connectToPortID = default;
            if (smAspect.IsStateMachineInstance(nodeHandle))
            {
                var animationSource = smAspect.GetStateMachineInstance(nodeHandle);
                connectTo = animationSource.OutputNode;
                connectToPortID = animationSource.OutputPortID;
                set.Connect(animationSource.OutputNode, animationSource.OutputPortID, targetNodeHandle, targetInputPortID, NodeSetAPI.ConnectionType.Normal);
            }
            else if (smAspect.IsBlendInstance(nodeHandle))
            {
                var animationSource = smAspect.GetBlendInstance(nodeHandle);
                connectTo = animationSource.OutputNode;
                connectToPortID = animationSource.OutputPortID;
                set.Connect(animationSource.OutputNode, animationSource.OutputPortID, targetNodeHandle, targetInputPortID, NodeSetAPI.ConnectionType.Normal);
            }
            else if (smAspect.IsGraphInstance(nodeHandle))
            {
                var animationSource = smAspect.GetGraphInstance(nodeHandle);
                connectTo = animationSource.OutputNode;
                connectToPortID = animationSource.OutputPortID;
                set.Connect(animationSource.OutputNode, animationSource.OutputPortID, targetNodeHandle, targetInputPortID, NodeSetAPI.ConnectionType.Normal);
            }

            return (connectTo, connectToPortID);
        }

        internal struct ConditionFragmentEvaluationContext
        {
            public float DeltaTimeAbsolute;
            public StateMachineAspect SMAspect;
        }

        public static unsafe float NaiveStackBustingEvaluateConditionFragment(int rootConditionIndex, ref StateMachineInstance stateMachineInstance, ConditionFragmentEvaluationContext evalContext)
        {
            if (rootConditionIndex == -1)
                return 0.0f;
            var rootCondition = stateMachineInstance.Definition.Value.ConditionFragments[rootConditionIndex];
            if (rootCondition.Type == ConditionFragmentType.GroupAnd)
            {
                if (rootCondition.FirstChildConditionIndex == -1)
                    return 0.0f;
                float resultingStartTime = -1.0f;
                int conditionIndex = rootCondition.FirstChildConditionIndex;
                while (conditionIndex != -1)
                {
                    float startTime = NaiveStackBustingEvaluateConditionFragment(conditionIndex, ref stateMachineInstance, evalContext);
                    if (startTime < 0.0f)
                        return -1.0f;
                    resultingStartTime = math.max(resultingStartTime, startTime);
                    TransitionConditionFragment conditionToEval = stateMachineInstance.Definition.Value.ConditionFragments[conditionIndex];
                    conditionIndex = conditionToEval.NextSiblingConditionIndex;
                }

                return resultingStartTime;
            }

            if (rootCondition.Type == ConditionFragmentType.GroupOr)
            {
                if (rootCondition.FirstChildConditionIndex == -1)
                    return 0.0f;
                float resultingStartTime = -1.0f;
                int conditionIndex = rootCondition.FirstChildConditionIndex;
                while (conditionIndex != -1)
                {
                    float startTime = NaiveStackBustingEvaluateConditionFragment(conditionIndex, ref stateMachineInstance, evalContext);
                    if (startTime == 0.0f)
                        return 0.0f;
                    if (startTime > 0.0f)
                    {
                        if (resultingStartTime < 0.0f || startTime < resultingStartTime)
                        {
                            resultingStartTime = startTime;
                            if (resultingStartTime == 0.0f)
                                return 0.0f;
                        }
                    }
                    TransitionConditionFragment conditionToEval = stateMachineInstance.Definition.Value.ConditionFragments[conditionIndex];
                    conditionIndex = conditionToEval.NextSiblingConditionIndex;
                }

                return resultingStartTime;
            }

            if (rootCondition.Type == ConditionFragmentType.ElapsedTime)
            {
                return stateMachineInstance.AccumulatedTime + evalContext.DeltaTimeAbsolute >= rootCondition.FloatValue ? rootCondition.FloatValue - stateMachineInstance.AccumulatedTime : -1.0f;
            }

            if (rootCondition.Type == ConditionFragmentType.EndOfDominantAnimation)
            {
                float accumulatedTime = 0.0f;
                float singleAnimationDuration = -1.0f;
                NodeHandle currentStateNode = stateMachineInstance.CurrentStateNode;
                while (!evalContext.SMAspect.IsGraphInstance(currentStateNode))
                {
                    if (evalContext.SMAspect.IsBlendInstance(currentStateNode))
                    {
                        currentStateNode = evalContext.SMAspect.GetBlendInstance(currentStateNode).TargetStateNode;
                    }
                    else if (evalContext.SMAspect.IsStateMachineInstance(currentStateNode))
                    {
                        currentStateNode = evalContext.SMAspect.GetStateMachineInstance(currentStateNode).CurrentStateNode;
                    }
                }
                ref var graphInstance = ref evalContext.SMAspect.GetGraphInstance(currentStateNode);
                accumulatedTime = graphInstance.AccumulatedTime;
                singleAnimationDuration = graphInstance.SingleAnimationDuration;

                if (singleAnimationDuration >= 0.0f)
                {
                    if (singleAnimationDuration >= 0.0f && accumulatedTime + evalContext.DeltaTimeAbsolute >= singleAnimationDuration - rootCondition.FloatValue)
                    {
                        var timeToValidFragment = singleAnimationDuration - rootCondition.FloatValue - accumulatedTime;
                        return timeToValidFragment > 0.0f ? timeToValidFragment : 0.0f;
                    }
                }
                return -1.0f;
            }

            if (rootCondition.Type == ConditionFragmentType.BlackboardValue)
            {
                var typeIndex = rootCondition.BlackboardValueComponentDataTypeIndex;

//                var componentDataPtr = evalContext.SMAspect.EntityManager.GetComponentDataRawRO(evalContext.SMAspect.Entity, typeIndex);
//                var fieldDataPtr = (byte*)componentDataPtr + rootCondition.GameplayPropertyOffset;
                var componentDataPtr = evalContext.SMAspect.GameplayPropertiesCopy.GameplayProperties[typeIndex].Ptr;
                var fieldDataPtr = componentDataPtr + rootCondition.BlackboardValueOffset;

                if (componentDataPtr == null || fieldDataPtr == null)
                    return -1.0f;

                if (rootCondition.CompareValue.Type == GraphVariant.ValueType.Bool)
                {
                    UnsafeUtility.CopyPtrToStructure<bool>(fieldDataPtr, out var fieldValue);
                    if (CompareCondition(rootCondition, fieldValue))
                        return 0.0f;
                }
                else if (rootCondition.CompareValue.Type == GraphVariant.ValueType.Float)
                {
                    UnsafeUtility.CopyPtrToStructure<float>(fieldDataPtr, out var fieldValue);
                    if (CompareCondition(rootCondition, fieldValue))
                        return 0.0f;
                }
                else if (rootCondition.CompareValue.Type == GraphVariant.ValueType.Int)
                {
                    UnsafeUtility.CopyPtrToStructure<int>(fieldDataPtr, out var fieldValue);
                    if (CompareCondition(rootCondition, fieldValue))
                        return 0.0f;
                }
            }

            return -1.0f;
        }

        static bool CompareCondition(TransitionConditionFragment rootCondition, bool fieldValue)
        {
            switch (rootCondition.Operation)
            {
                case ComparisonOperation.Equal when fieldValue == rootCondition.CompareValue.Bool:
                case ComparisonOperation.NotEqual when fieldValue != rootCondition.CompareValue.Bool:
                    return true;
                default:
                    return false;
            }
        }

        static bool CompareCondition(TransitionConditionFragment rootCondition, float fieldValue)
        {
            switch (rootCondition.Operation)
            {
                case ComparisonOperation.Equal when fieldValue == rootCondition.CompareValue.Float:
                case ComparisonOperation.NotEqual when fieldValue != rootCondition.CompareValue.Float:
                case ComparisonOperation.LessThan when fieldValue<rootCondition.CompareValue.Float:
                                                                  case ComparisonOperation.LessOrEqual when fieldValue <= rootCondition.CompareValue.Float:
                                                                  case ComparisonOperation.GreaterThan when fieldValue> rootCondition.CompareValue.Float:
                case ComparisonOperation.GreaterOrEqual when fieldValue >= rootCondition.CompareValue.Float:
                    return true;
                default:
                    return false;
            }
        }

        static bool CompareCondition(TransitionConditionFragment rootCondition, int fieldValue)
        {
            switch (rootCondition.Operation)
            {
                case ComparisonOperation.Equal when fieldValue == rootCondition.CompareValue.Int:
                case ComparisonOperation.NotEqual when fieldValue != rootCondition.CompareValue.Int:
                case ComparisonOperation.LessThan when fieldValue<rootCondition.CompareValue.Int:
                                                                  case ComparisonOperation.LessOrEqual when fieldValue <= rootCondition.CompareValue.Int:
                                                                  case ComparisonOperation.GreaterThan when fieldValue> rootCondition.CompareValue.Int:
                case ComparisonOperation.GreaterOrEqual when fieldValue >= rootCondition.CompareValue.Int:
                    return true;
                default:
                    return false;
            }
        }
    }
}
