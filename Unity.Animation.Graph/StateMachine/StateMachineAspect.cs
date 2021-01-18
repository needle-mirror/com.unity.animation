using System.Diagnostics;
using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation.StateMachine
{
    internal enum Error
    {
        None = 0,
        NoEnterSelectorTransition,
        GraphIDNotFound
    };


    struct NodeContext
    {
        public NodeHandle  NodeHandle;
        public TimeControl TimeControl;
    }

    internal struct StateMachineAspect
    {
        public Entity                                           Entity;
        public ComponentDataFromEntity<StateMachineVersion>     StateMachineVersion;
        public ComponentDataFromEntity<StateMachineSystemState> StateMachineSystemState;
        public DynamicBuffer<Node>                              NodeRegistry;
        public DynamicBuffer<NodeHandle>                        OrderedNode;
        public DynamicBuffer<StateMachineInstance>              StateMachines;
        public DynamicBuffer<BlendInstance>                     Blenders;
        public DynamicBuffer<GraphInstance>                     Graphs;
        public DynamicBuffer<DFGGraphToFree>                    DFGGraphToFree;

        [ReadOnly] public CharacterGameplayPropertiesCopy       GameplayPropertiesCopy;
        [ReadOnly] public TimeControl                           TimeControl;

        public EntityCommandBuffer.ParallelWriter               ECB;
        public GraphManager                                     GraphManager;

        public float ScaledDeltaTime => TimeControl.DeltaRatio * TimeControl.Timescale;

        internal NativeStack<NodeContext>   NodeStack;
        internal NodeHandle                 CurrentNodeHandle;
        internal Error                      ErrorCode;

        public int Version
        {
            get { return StateMachineVersion[Entity].Value; }
        }

        public NodeHandle RootNodeHandle
        {
            get { return new NodeHandle { Index = 0, Version = NodeRegistry[0].Version }; }
        }

        public bool IsStateMachineInstance(NodeHandle nodeHandle)
        {
            ValidateNodeHandle(nodeHandle);

            return NodeRegistry[nodeHandle.Index].Type == NodeType.StateMachine;
        }

        public ref StateMachineInstance GetStateMachineInstance(NodeHandle nodeHandle)
        {
            ValidateNodeHandle(nodeHandle);
            ValidateNodeType(nodeHandle, NodeType.StateMachine);

            var index = NodeRegistry[nodeHandle.Index].Index;
            return ref StateMachines.ElementAt(index);
        }

        public bool IsGraphInstance(NodeHandle nodeHandle)
        {
            ValidateNodeHandle(nodeHandle);

            return NodeRegistry[nodeHandle.Index].Type == NodeType.Graph;
        }

        public ref GraphInstance GetGraphInstance(NodeHandle nodeHandle)
        {
            ValidateNodeHandle(nodeHandle);
            ValidateNodeType(nodeHandle, NodeType.Graph);

            var index = NodeRegistry[nodeHandle.Index].Index;
            return ref Graphs.ElementAt(index);
        }

        public bool IsBlendInstance(NodeHandle nodeHandle)
        {
            ValidateNodeHandle(nodeHandle);

            return NodeRegistry[nodeHandle.Index].Type == NodeType.Blend;
        }

        public ref BlendInstance GetBlendInstance(NodeHandle nodeHandle)
        {
            ValidateNodeHandle(nodeHandle);
            ValidateNodeType(nodeHandle, NodeType.Blend);

            var index = NodeRegistry[nodeHandle.Index].Index;
            return ref Blenders.ElementAt(index);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateNodeType(NodeHandle nodeHandle, NodeType expectedNodeType)
        {
            var nodeType = NodeRegistry[nodeHandle.Index].Type;
            if (nodeType != expectedNodeType)
                throw new System.InvalidCastException($"StateMachineAspect: Node type '{nodeType}' doesn't match the expected node type '{expectedNodeType}'.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void NotImplemented()
        {
            throw new System.NotImplementedException();
        }

        internal void SetCurrentStateNode(NodeHandle parentHandle, NodeHandle childHandle)
        {
            ValidateNodeHandle(parentHandle);
            ValidateNodeHandle(childHandle);

            if (IsStateMachineInstance(parentHandle))
            {
                ref var instance = ref GetStateMachineInstance(parentHandle);
                instance.CurrentStateNode = childHandle;
            }
            else if (IsBlendInstance(parentHandle))
            {
                ref var instance = ref GetBlendInstance(parentHandle);
                instance.SourceStateNode = childHandle;
            }
            else if (IsGraphInstance(parentHandle))
            {
                NotImplemented();
            }
        }

        internal void SetParentStateNode(NodeHandle parentHandle, NodeHandle childHandle)
        {
            ValidateNodeHandle(parentHandle);
            ValidateNodeHandle(childHandle);

            if (IsStateMachineInstance(childHandle))
            {
            }
            else if (IsBlendInstance(childHandle))
            {
                ref var instance = ref GetBlendInstance(childHandle);
                instance.ParentStateNode = parentHandle;
            }
            else if (IsGraphInstance(childHandle))
            {
            }
        }

        internal void IncrementVersion()
        {
            var smVersion = StateMachineVersion[Entity];
            smVersion.Value++;
            StateMachineVersion[Entity] = smVersion;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateNodeHandle(NodeHandle handle)
        {
            if ((uint)handle.Index >= NodeRegistry.Length)
                throw new System.IndexOutOfRangeException($"StateMachineAspect: Node index '{handle.Index}' is out of range of '{NodeRegistry.Length}'.");

            if ((uint)handle.Version != NodeRegistry[handle.Index].Version)
                throw new System.IndexOutOfRangeException($"StateMachineAspect: Invalid NodeHandle '{handle.Index}' version '{handle.Version}' doesn't match node version '{NodeRegistry[handle.Index].Version}'.");
        }

        internal int FindFirstFreeNode()
        {
            var index = 0;
            for (; index < NodeRegistry.Length; index++)
            {
                if (NodeRegistry[index].Type == NodeType.Invalid)
                    break;
            }

            return index;
        }

        internal unsafe void RemoveNode(NodeHandle nodeHandle)
        {
            IncrementVersion();

            ValidateNodeHandle(nodeHandle);

            for (int i = 0; i < OrderedNode.Length; i++)
            {
                if (OrderedNode[i] == nodeHandle)
                {
                    OrderedNode.RemoveAt(i);
                    break;
                }
            }

            var indexToRemove = NodeRegistry[nodeHandle.Index].Index;
            var typeToRemove = NodeRegistry[nodeHandle.Index].Type;
            if (IsStateMachineInstance(nodeHandle))
            {
                var sm = GetStateMachineInstance(nodeHandle);
                StateMachines.RemoveAt(indexToRemove);
                DFGGraphToFree.Add(new StateMachine.DFGGraphToFree
                {
                    GraphHandle = sm.GraphHandle
                });
            }
            else if (IsBlendInstance(nodeHandle))
            {
                var blender = GetBlendInstance(nodeHandle);
                Blenders.RemoveAt(indexToRemove);
                DFGGraphToFree.Add(new StateMachine.DFGGraphToFree
                {
                    GraphHandle = blender.GraphHandle
                });
            }
            else if (IsGraphInstance(nodeHandle))
            {
                var graph = GetGraphInstance(nodeHandle);
                Graphs.RemoveAt(indexToRemove);
                DFGGraphToFree.Add(new StateMachine.DFGGraphToFree
                {
                    GraphHandle = graph.GraphHandle
                });
            }

            NodeRegistry[nodeHandle.Index] = default;

            for (int i = 0; i < NodeRegistry.Length; i++)
            {
                var node = NodeRegistry[i];
                if (node.Type == typeToRemove && node.Index > indexToRemove)
                {
                    node.Index--;
                    NodeRegistry[i] = node;
                }
            }
        }

        internal unsafe void RemoveNodeRecursive(NodeHandle nodeHandle)
        {
            ValidateNodeHandle(nodeHandle);

            IncrementVersion();

            for (int i = 0; i < OrderedNode.Length; i++)
            {
                if (OrderedNode[i] == nodeHandle)
                {
                    OrderedNode.RemoveAt(i);
                    break;
                }
            }

            var indexToRemove = NodeRegistry[nodeHandle.Index].Index;
            var typeToRemove = NodeRegistry[nodeHandle.Index].Type;

            //
            // Since we need to access the AnimationGraphSystem this has to be done on the main thread
            // Add this instance to the freeList which will get process in UpdateRenderGraph Phase
            //
            var nodeToRemove = stackalloc NodeHandle[2];
            var nodeToRemoveCount = 0;

            if (IsStateMachineInstance(nodeHandle))
            {
                var sm = GetStateMachineInstance(nodeHandle);
                nodeToRemove[nodeToRemoveCount++] = sm.CurrentStateNode;
                StateMachines.RemoveAt(indexToRemove);
                DFGGraphToFree.Add(new StateMachine.DFGGraphToFree
                {
                    GraphHandle = sm.GraphHandle
                });
            }
            else if (IsBlendInstance(nodeHandle))
            {
                var blender = GetBlendInstance(nodeHandle);
                nodeToRemove[nodeToRemoveCount++] = blender.SourceStateNode;
                nodeToRemove[nodeToRemoveCount++] = blender.TargetStateNode;
                Blenders.RemoveAt(indexToRemove);
                DFGGraphToFree.Add(new StateMachine.DFGGraphToFree
                {
                    GraphHandle = blender.GraphHandle
                });
            }
            else if (IsGraphInstance(nodeHandle))
            {
                var graph = GetGraphInstance(nodeHandle);
                Graphs.RemoveAt(indexToRemove);
                DFGGraphToFree.Add(new StateMachine.DFGGraphToFree
                {
                    GraphHandle = graph.GraphHandle
                });
            }

            NodeRegistry[nodeHandle.Index] = default;

            for (int i = 0; i < NodeRegistry.Length; i++)
            {
                var node = NodeRegistry[i];
                if (node.Type == typeToRemove && node.Index > indexToRemove)
                {
                    node.Index--;
                    NodeRegistry[i] = node;
                }
            }

            for (int i = 0; i < nodeToRemoveCount; i++)
            {
                RemoveNodeRecursive(nodeToRemove[i]);
            }
        }

        /// <summary>
        /// Constructs a new StateMachineAspect given an entity from a SystemBase.
        /// </summary>
        internal static StateMachineAspect Create(Entity entity, SystemBase systemBase, EntityCommandBuffer.ParallelWriter ECB)
        {
            var graphManager = systemBase.GetSingleton<GraphManager>();

            var stateMachineVersion = systemBase.GetComponentDataFromEntity<StateMachineVersion>();
            var stateMachineSystemState = systemBase.GetComponentDataFromEntity<StateMachineSystemState>();
            var nodeRegistry = systemBase.GetBufferFromEntity<Node>();
            var orderedNode = systemBase.GetBufferFromEntity<NodeHandle>();
            var stateMachines = systemBase.GetBufferFromEntity<StateMachineInstance>();
            var blenders = systemBase.GetBufferFromEntity<BlendInstance>();
            var graphs = systemBase.GetBufferFromEntity<GraphInstance>();
            var dfgGraphToFree = systemBase.GetBufferFromEntity<DFGGraphToFree>();
            var timeControls = systemBase.GetComponentDataFromEntity<TimeControl>(true);

            return new StateMachineAspect
            {
                Entity = entity,
                StateMachineVersion = stateMachineVersion,
                StateMachineSystemState = stateMachineSystemState,
                NodeRegistry = nodeRegistry[entity],
                OrderedNode = orderedNode[entity],
                StateMachines = stateMachines[entity],
                Blenders = blenders[entity],
                Graphs = graphs[entity],
                DFGGraphToFree = dfgGraphToFree[entity],
                TimeControl = timeControls[entity],
                ECB = ECB,
                GraphManager = graphManager
            };
        }
    }
}
