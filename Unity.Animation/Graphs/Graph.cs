using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    public struct Graph
    {
        public BlobArray<CreateNodeCommand> CreateCommands; //Defines the nodes that will be instantiated
        public BlobArray<GraphInput> GraphInputs; // Read connections that are made to ComponentData through ComponentNode
        public BlobArray<GraphOutput> GraphOutputs; // Write connections that are made to ComponentData through ComponentNode
        public BlobArray<ConnectNodesCommand> ConnectCommands; //Describes the connections between the different created nodes
        public BlobArray<ConnectAssetCommand> ConnectAssetCommands; //Describes the connections between the different created nodes
        public BlobArray<ResizeArrayCommand> ResizeCommands; //Describes the sizes of PortArrays
        public BlobArray<SetValueCommand> SetValueCommands; //Default values for ports that are not connected
        public BlobArray<InputTarget> InputTargets;  //Describes the destination for gameplay inputs to target nodes.

        public BlobArray<CreateStateCommand> CreateStateCommands;
        public BlobArray<CreateTransitionCommand> CreateTransitionCommands;
        public BlobArray<CreateConditionFragmentCommand> CreateConditionFragmentCommands;
        public BlobArray<BlobString> TypesUsed;

        internal int m_HashCode;
        public ulong GraphDefinitionHash;
        public bool IsStateMachine;

        public override int GetHashCode() => m_HashCode;
    }

    public struct GraphInstanceParameters
    {
        public BlobArray<SetValueCommand> Values;
        internal int m_HashCode;
        public override int GetHashCode() => m_HashCode;
    }

    /// <summary>
    /// Command to set a value on a port of a node
    /// </summary>
    [Serializable]
    public struct SetValueCommand
    {
        public NodeID Node;
        public PortID Port;
        public GraphVariant.ValueType Type;
        public int4 Value;
        // Question : why don't we directly serialize the graphvariant here?
    }

    /// <summary>
    /// Command to instantiate a node
    /// </summary>
    [Serializable]
    public struct CreateStateCommand
    {
        public int StateIndex;
        public Unity.Entities.Hash128 DefinitionHash;
        public int DebugId;
    }

    public enum TransitionType
    {
        StateToState,
        Global,
        OnEnterSelector,
        Self
    }

    public enum TransitionSynchronizationType
    {
        None,
        Ratio, // ratio jumps at a specific ratio
        Proportional, // proportional jumps to the source's ratio
        InverseProportional, // jumps to 1-sourceRatio
        EntryPoint,
        Tag
    }

    [Serializable]
    public struct CreateTransitionCommand
    {
        public int SourceState;
        public int TargetState;
        public int TransitionFragmentIndex;
        public TransitionType Type;

        public float Duration;
        public TransitionSynchronizationType SyncType;
        public float SyncTargetRatio;
        public int SyncTagType;
        public int SyncEntryPoint;
        public bool AdvanceSourceDuringTransition;

        public bool OverrideDuration;
        public bool OverrideSyncType;
        public bool OverrideSyncTargetRatio;
        public bool OverrideSyncTagType;
        public bool OverrideSyncEntryPoint;
        public bool OverrideAdvanceSourceDuringTransition;
    }

    public enum TransitionFragmentType
    {
        GroupAnd,
        GroupOr,
        BlackboardValue,
        Markup,
        ElapsedTime,
        StateTag,
        EndOfDominantAnimation,
        EvaluationRatio
    }

    public enum CompareOp
    {
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual
    }

    [Serializable]
    public struct BlackboardValueRuntimeId
    {
        public ulong ComponentDataTypeHash;
        public int Offset;
    }

    [Serializable]
    public struct CreateConditionFragmentCommand
    {
        public TransitionFragmentType Type;
        public int ParentIndex;

        public Hash128 StateTagHash;

        public Hash128 MarkupHash;
        public bool IsSet;

        public BlackboardValueRuntimeId BlackboardValueId;

        public CompareOp CompareOp;

        public BlackboardValueRuntimeId CompareBlackboardValueId;
        public GraphVariant CompareVariant;
    }

    [Serializable]
    public struct CreateNodeCommand
    {
        public int TypeHash;
        public NodeID NodeID;
    }

    /// <summary>
    /// Command describing a PortArray size
    /// </summary>
    [Serializable]
    public struct ResizeArrayCommand
    {
        public NodeID Node;
        public PortID Port;
        public int ArraySize;
    }

    /// <summary>
    /// Command to connects two nodes together
    /// </summary>
    [Serializable]
    public struct ConnectNodesCommand
    {
        public NodeID SourceNodeID;
        public PortID SourcePortID;
        public NodeID DestinationNodeID;
        public PortID DestinationPortID;
    }

    /// <summary>
    /// Command to connect an asset to a node input port
    /// </summary>
    [Serializable]
    public struct ConnectAssetCommand
    {
        public NodeID DestinationNodeID;
        public PortID DestinationPortID;
        public Hash128 AssetID;
        public ulong AssetType; //Way to identify the asset type... maybe there’s a better way?
        public float ClipDuration; //clip specific property to avoid having to query the clip manager managed object in the middle of burst compiled code
    }

    /// <summary>
    /// Port target for a specific input
    /// </summary>
    [Serializable]
    public struct InputTarget
    {
        public ulong TypeHash;
        public NodeID NodeID;
        public PortID PortID;
    }

    /// <summary>
    /// Command to describe an input or an output of the graph
    /// </summary>
    [Serializable]
    public struct GraphInput
    {
        public NodeID TargetNodeID;
        public PortID TargetPortID;
        public ulong TypeHash;
        public InputOutputType Type;
    }

    [Serializable]
    public struct GraphOutput
    {
        public NodeID TargetNodeID;
        public PortID TargetPortID;
        public ulong TypeHash;
        public InputOutputType Type;
    }

    public enum InputOutputType
    {
        ComponentData,
        ContextHandler,
        InputReferenceHandler,
        EntityManager,
        TimeControlHandler,
        Unknown
    }
}
