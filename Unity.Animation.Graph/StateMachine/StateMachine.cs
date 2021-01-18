using System;
using System.Diagnostics;

using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.DataFlowGraph;

namespace Unity.Animation.StateMachine
{
    internal enum NodeType
    {
        Invalid,
        Graph,
        Blend,
        StateMachine
    }

    [InternalBufferCapacity(0)]
    internal struct Node : IBufferElementData
    {
        public NodeType Type;
        public int      Version;
        public int      Index;
    }

    [InternalBufferCapacity(0)]
    internal struct NodeHandle : IBufferElementData, IEquatable<NodeHandle>
    {
        /// <summary>
        /// The generational version of the node.
        /// </summary>
        /// <remarks>The Version number can, theoretically, overflow and wrap around within the lifetime of an
        /// application. For this reason, you cannot assume that a Node instance with a larger Version is a more
        /// recent incarnation of the node than one with a smaller Version (and the same Index).</remarks>
        /// <value>Used to determine whether this Node object still identifies an existing node.</value>
        public int      Version;

        /// <summary>
        /// The ID of a Node.
        /// </summary>
        /// <value>The index into the internal list of nodes.</value>
        /// <remarks>
        /// Node indexes are recycled when a node is destroyed. When a node is destroyed, the
        /// StateMachine increments the version identifier. To represent the same node, both the Index and the
        /// Version fields of the Node object must match. If the Index is the same, but the Version is different,
        /// then the node has been recycled.
        /// </remarks>
        public int      Index;

        /// <summary>
        /// Two NodeHandle are equal when they reference the same data.
        /// </summary>
        /// <param name="lhs">The NodeHandle on the left side of the operator.</param>
        /// <param name="rhs">The NodeHandle on the right side of the operator.</param>
        /// <returns>True, if both handle are equal.</returns>
        public static bool operator==(NodeHandle lhs, NodeHandle rhs)
        {
            return lhs.Version == rhs.Version &&
                lhs.Index == rhs.Index;
        }

        /// <summary>
        /// Two NodeHandle are equal when they reference the same data.
        /// </summary>
        /// <param name="lhs">The NodeHandle on the left side of the operator.</param>
        /// <param name="rhs">The NodeHandle on the right side of the operator.</param>
        /// <returns>True, if both handle are different.</returns>
        public static bool operator!=(NodeHandle lhs, NodeHandle rhs)
        {
            return lhs.Version != rhs.Version ||
                lhs.Index != rhs.Index;
        }

        /// <summary>
        /// Two NodeHandle are equal when they reference the same data.
        /// </summary>
        /// <param name="other">The reference to compare to this one.</param>
        /// <returns>True, if both references point to the same data or if both are <see cref="Null"/>.</returns>
        public bool Equals(NodeHandle other)
        {
            return Version == other.Version &&
                Index == other.Index;
        }

        /// <summary>
        /// Two NodeHandle are equal when they reference the same data.
        /// </summary>
        /// <param name="obj">The object to compare to this reference</param>
        /// <returns>True, if the object is a NodeHandle that references to the same data as this one</returns>
        public override bool Equals(object obj)
        {
            return this == (NodeHandle)obj;
        }

        /// <summary>
        /// Generates the hash code for this object.
        /// </summary>
        /// <returns>A standard C# value-type hash code.</returns>
        public override int GetHashCode()
        {
            return Index.GetHashCode() ^ Version.GetHashCode();
        }
    }

    [InternalBufferCapacity(0)]
    internal struct GraphInstance : IBufferElementData, IAnimationSource
    {
        public int   ID;
        public float AccumulatedTime;
        public float CurrentFrameDeltaTime;
        public Hash128 GraphReference;

        public float SingleAnimationDuration;

        public GraphHandle GraphHandle { get; set; }
        public DataFlowGraph.NodeHandle OutputNode { get; set; }
        public OutputPortID OutputPortID { get; set; }

        public Unity.DataFlowGraph.NodeHandle  ConnectToNode { get; set; }

        public OutputPortID                     ConnectToPortID   { get; set; }
    }

    [InternalBufferCapacity(0)]
    internal struct BlendInstance : IBufferElementData, IAnimationSource
    {
        public int          ID;
        public float        AccumulatedTime;
        public int          SourceState;
        public int          TargetState;
        public NodeHandle   ParentStateNode;
        public NodeHandle   SourceStateNode;
        public NodeHandle   TargetStateNode;
        public float        Weight;
        public float        StartTimeDT;

        public TransitionDefinition TransitionDefinition;

        public GraphHandle GraphHandle { get; set; }
        public DataFlowGraph.NodeHandle OutputNode { get; set; }
        public OutputPortID OutputPortID { get; set; }

        public Unity.DataFlowGraph.NodeHandle  ConnectToNode1 { get; set; }

        public OutputPortID                     ConnectToPortID1   { get; set; }

        public Unity.DataFlowGraph.NodeHandle  ConnectToNode2 { get; set; }

        public OutputPortID                     ConnectToPortID2   { get; set; }
    }

    internal struct StateMachineVersion : IComponentData
    {
        public int Value;
    }

    internal struct TimeControl : IComponentData
    {
        public float DeltaRatio;
        public float Timescale;
    }

    internal struct StateMachineSystemState : ISystemStateComponentData
    {
    }

    internal struct CharacterGameplayPropertiesCopy : IComponentData
    {
        public UnsafeHashMap<int, UnsafeList<byte>> GameplayProperties;
    }

    [InternalBufferCapacity(0)]
    [DebuggerTypeProxy(typeof(StateMachineInstanceDebugView))]
    internal struct StateMachineInstance : IBufferElementData, IAnimationSource
    {
        public BlobAssetReference<StateMachineDefinition> Definition;

        public int          ID;
        public float        AccumulatedTime;
        public int          CurrentState;
        public NodeHandle   CurrentStateNode;
        public GraphHandle GraphHandle { get; set; }
        public DataFlowGraph.NodeHandle OutputNode { get; set; }
        public OutputPortID OutputPortID { get; set; }
        public Unity.DataFlowGraph.NodeHandle  ConnectToNode { get; set; }
        public OutputPortID                     ConnectToPortID   { get; set; }
    }

    internal struct DFGGraphToFree : ISystemStateBufferElementData
    {
        public GraphHandle GraphHandle { get; set; }
    }
}
