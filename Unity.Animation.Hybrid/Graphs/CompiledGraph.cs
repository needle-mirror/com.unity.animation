using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    internal interface ICompiledGraphProvider
    {
        CompiledGraph CompiledGraph { get; }
    }

    [Serializable]
    internal class CompiledGraph
    {
        public string DisplayName;
        public GraphDefinition Definition = new GraphDefinition();
        public List<OtherGraphDependency> CompiledDependencies = new List<OtherGraphDependency>();
    }

    [Serializable]
    internal class OtherGraphDependency
    {
        [SerializeField]
        public string Guid;
    }

    [Serializable]
    internal class GraphDefinition
    {
        public List<GraphAssetReference> Assets = new List<GraphAssetReference>(); //Added to the conversion entity in a buffer
        public List<ExposedObjectReference> ExposedObjects = new List<ExposedObjectReference>(); // Scene object references
        public List<InputTarget> InputTargets = new List<InputTarget>();
        public TopologyDefinition TopologyDefinition = new TopologyDefinition(); //See below
        public List<string> TypesUsed = new List<string>();
    }

    [Serializable]
    internal class TopologyDefinition
    {
        public List<CreateNodeCommand> NodeCreations = new List<CreateNodeCommand>(); //Defines the nodes that will be instantiated
        public List<ConnectNodesCommand> Connections = new List<ConnectNodesCommand>(); //Describes the connections between the different created nodes
        public List<SetValueCommand> Values = new List<SetValueCommand>(); //Default values for ports that are not connected
        public List<GraphInput> Inputs = new List<GraphInput>(); // Read connections that are made to ComponentData through ComponentNode
        public List<GraphOutput> Outputs = new List<GraphOutput>(); // Write connections that are made to ComponentData through ComponentNode
        public List<ResizeArrayCommand> PortArrays = new List<ResizeArrayCommand>(); //Describes the sizes of PortArrays
        public List<CreateStateCommand> States = new List<CreateStateCommand>();
        public List<CreateTransitionCommand> Transitions = new List<CreateTransitionCommand>();
        public List<CreateConditionFragmentCommand> ConditionFragments = new List<CreateConditionFragmentCommand>();
        public bool IsStateMachine;
    }

    [Serializable]
    internal class ExposedObjectReference
    {
        public NodeID NodeID = NodeID.Invalid;
        public PortID PortID = PortID.Invalid;
        public PortTargetGUID TargetGUID;
        public ulong TypeHash;
    }

    [Serializable]
    internal class GraphAssetReference
    {
        public ulong TypeHash;
        public NodeID NodeID = NodeID.Invalid;
        public PortID PortID = PortID.Invalid;
        public UnityEngine.Object Asset;
    }
}
