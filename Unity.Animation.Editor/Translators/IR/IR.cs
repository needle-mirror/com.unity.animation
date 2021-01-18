using System;
using System.Collections.Generic;
using Unity.DataFlowGraph.Attributes;
using Unity.Entities;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    class IRPortTarget
    {
        internal IRPortTarget(IRNodeDefinition node, string portName, int portGroupInstance = -1)
        {
            Node = node;
            PortName = portName;
            PortGroupInstance = portGroupInstance;
        }

        internal IRNodeDefinition Node;
        internal string PortName;
        internal int PortGroupInstance = -1;
    }

    internal class IRPortDefaultValue
    {
        public static GlobalObjectId k_DefaultGlobalObjectId = new GlobalObjectId();

        internal IRPortDefaultValue(IRPortTarget target, object value, bool isMessagePort, GlobalObjectId id = default)
        {
            Destination = target;
            Value = value;
            ObjectReference = id;
            MessagePort = isMessagePort;
        }

        internal IRPortTarget Destination { get; }
        internal object Value { get; }
        internal bool MessagePort { get; }
        internal GlobalObjectId ObjectReference { get; }
    }

    internal class PortDefinitionAttributes
    {
        internal string Tooltip { get; set; }
        internal object DefaultValue { get; set; }
        internal DefaultValueType DefValueType { get; set; }
    }

    internal class IRNodePortDefinition
    {
        internal IRNodePortDefinition(string portName, Type dataType, int portIndex, PortDefinitionAttributes attributes)
        {
            PortName = portName;
            DataType = dataType;
            PortIndex = portIndex;
            Attributes = attributes;
        }

        internal string PortName { get; }
        internal Type DataType { get; }
        internal int PortIndex { get; }
        internal PortDefinitionAttributes Attributes { get; }
    }

    internal class IRPortGroupInfo
    {
        internal IRPortGroupInfo(int portGroupIndex, int portGroupSize)
        {
            PortGroupIndex = portGroupIndex;
            PortGroupSize = portGroupSize;
        }

        internal int PortGroupIndex { get; }
        internal int PortGroupSize { get; }
        internal List<string> MessagePortNameInGroup { get; } = new List<string>();
        internal List<string> DataPortNameInGroup { get; } = new List<string>();
        internal string GroupSizeTarget;
    }

    internal class IRPortGroupInfos
    {
        internal IRPortGroupInfos(IRNodeDefinition node)
        {
            Node = node;
        }

        internal IRNodeDefinition Node { get; }
        internal Dictionary<int, IRPortGroupInfo> PortGroupInfos { get; } = new Dictionary<int, IRPortGroupInfo>();
    }

    internal class IRNodeDefinition
    {
        internal IRNodeDefinition(string name, string typeName)
        {
            Name = name;
            m_TypeName = typeName;
        }

        internal IRNodeDefinition(string name, Type nodeType)
        {
            Name = name;
            IsGenericType = true;
            NodeType = nodeType;
        }

        private readonly string m_TypeName;

        internal Type NodeType { get; }
        internal bool IsGenericType { get; }

        internal string GetUnresolvedTypeName()
        {
            return m_TypeName;
        }

        internal string Name { get; }

        internal string GetTypeName()
        {
            if (string.IsNullOrEmpty(m_TypeName))
                return default;
            var type = Type.GetType(m_TypeName);
            return type == null ? m_TypeName : type.FullName;
        }

        internal IRNodePortDeclarations PortDeclarations { get; set; } = new IRNodePortDeclarations();
    }

    internal class IRNodePortDeclarations
    {
        internal enum PortContainerType
        {
            InputData,
            OutputData,
            InputMessage,
            OutputMessage
        }

        private IRNodePortDefinition AddPort(List<IRNodePortDefinition> list, string portName, Type dataType, PortDefinitionAttributes attributes)
        {
            int index = list.Count;
            var newPort = new IRNodePortDefinition(portName, dataType, index, attributes);
            list.Add(newPort);
            return newPort;
        }

        private List<IRNodePortDefinition> GetContainerFromType(PortContainerType containerType)
        {
            switch (containerType)
            {
                case PortContainerType.InputData:
                    return InputDataPorts;
                case PortContainerType.OutputData:
                    return OutputDataPorts;
                case PortContainerType.InputMessage:
                    return InputMessagePorts;
                default:
                case PortContainerType.OutputMessage:
                    return OutputMessagePorts;
            }
        }

        private Dictionary<string, IRNodePortDefinition> GetCacheContainerFromType(PortContainerType containerType)
        {
            switch (containerType)
            {
                case PortContainerType.InputData:
                    return _inputDataPortsCache;
                case PortContainerType.OutputData:
                    return _outputDataPortsCache;
                case PortContainerType.InputMessage:
                    return _inputMessagePortsCache;
                default:
                case PortContainerType.OutputMessage:
                    return _outputMessagePortsCache;
            }
        }

        internal IRNodePortDefinition AddPort(PortContainerType containerType, string portName, Type dataType, PortDefinitionAttributes attributes)
        {
            var newPort = AddPort(GetContainerFromType(containerType), portName, dataType, attributes);
            GetCacheContainerFromType(containerType).Add(portName, newPort);
            return newPort;
        }

        internal IRNodePortDefinition FindPortByName(PortContainerType containerType, string portName)
        {
            var portCache = GetCacheContainerFromType(containerType);
            portCache.TryGetValue(portName, out IRNodePortDefinition value);
            return value;
        }

        internal List<IRNodePortDefinition> InputDataPorts { get; } = new List<IRNodePortDefinition>();
        internal List<IRNodePortDefinition> OutputDataPorts { get; } = new List<IRNodePortDefinition>();
        internal List<IRNodePortDefinition> InputMessagePorts { get; } = new List<IRNodePortDefinition>();
        internal List<IRNodePortDefinition> OutputMessagePorts { get; } = new List<IRNodePortDefinition>();

        private readonly Dictionary<string, IRNodePortDefinition> _inputDataPortsCache = new Dictionary<string, IRNodePortDefinition>();
        private readonly Dictionary<string, IRNodePortDefinition> _outputDataPortsCache = new Dictionary<string, IRNodePortDefinition>();
        private readonly Dictionary<string, IRNodePortDefinition> _inputMessagePortsCache = new Dictionary<string, IRNodePortDefinition>();
        private readonly Dictionary<string, IRNodePortDefinition> _outputMessagePortsCache = new Dictionary<string, IRNodePortDefinition>();
    }

    internal struct IRAssetReference
    {
        public IRAssetReference(GlobalObjectId id, Type referenceType, Type destinationType, string name, IRNodeDefinition passThroughForAssetReference, bool isPropagatedReference)
        {
            Id = id;
            ReferenceType = referenceType;
            DestinationType = destinationType;
            Name = name;
            PassThroughForAssetReference = passThroughForAssetReference;
            IsPropagatedReference = isPropagatedReference;
        }

        public static IRAssetReference Clone(IRAssetReference other, IRNodeDefinition passThroughForAssetReference, bool isPropagatedReference)
        {
            return new IRAssetReference(other.Id, other.ReferenceType, other.DestinationType, other.Name, passThroughForAssetReference, isPropagatedReference);
        }

        internal GlobalObjectId Id { get; }
        internal Type ReferenceType { get; }
        internal Type DestinationType { get; }
        internal string Name { get; }
        internal IRNodeDefinition PassThroughForAssetReference { get; }
        internal bool IsPropagatedReference { get; }

        internal string AssetReferenceName => $"Asset_{StringExtensions.CodifyString(Name)}";
    }

    internal struct IRObjectReference
    {
        public IRObjectReference(GUID nodeID, string portUniqueName, Type referenceType, Type destinationType, string name, IRNodeDefinition converterNodeReference, bool isPropagatedReference, Type primitiveType)
        {
            NodeID = nodeID;
            PortUniqueName = portUniqueName;
            ReferenceType = referenceType;
            DestinationType = destinationType;
            Name = name;
            ConverterNodeReference = converterNodeReference;
            IsPropagatedReference = isPropagatedReference;
            PrimitiveType = primitiveType;
        }

        public static IRObjectReference Clone(IRObjectReference other, IRNodeDefinition converterNodeReference, bool isPropagatedReference)
        {
            return new IRObjectReference(other.NodeID, other.PortUniqueName, other.ReferenceType, other.DestinationType, other.Name, converterNodeReference, isPropagatedReference, other.PrimitiveType);
        }

        internal SerializableGUID NodeID { get; }
        internal string PortUniqueName { get; }
        internal Type ReferenceType { get; }
        internal Type DestinationType { get; }
        internal string Name { get; }
        internal IRNodeDefinition ConverterNodeReference { get; }
        internal bool IsPropagatedReference { get; }
        internal Type PrimitiveType { get; }
    }

    internal class IRExternalAssetReference
    {
        public IRExternalAssetReference(IRPortTarget subGraphAssetReferencePort, GlobalObjectId id)
        {
            SubGraphAssetReferencePort = subGraphAssetReferencePort;
            Id = id;
        }

        internal IRPortTarget SubGraphAssetReferencePort { get; }
        internal GlobalObjectId Id { get; }
    }

    internal class IRConnection
    {
        public IRConnection(IRPortTarget src, IRPortTarget dst)
        {
            Source = src;
            Destination = dst;
        }

        internal IRPortTarget Source { get; }
        internal IRPortTarget Destination { get; }
    }

    class IR
    {
        public string Name { get; }

        internal IR(string name, bool isStateMachine)
        {
            Name = name;
            CompilationResult = new CompilationResult();
            IsStateMachine = isStateMachine; //Can we remove that? It does not seem used
        }

        public class NodeDependency
        {
            public Entities.Hash128 Hash;
            public GraphModel Model;
        }

        public void AddDependency(NodeDependency dep)
        {
            Dependencies.Add(dep);
        }

        void DebugLog(string log)
        {
            //UnityEngine.Debug.Log(log);
        }

        internal List<NodeDependency> Dependencies { get; } = new List<NodeDependency>();

        internal IRNodeDefinition CreateNode(string nodeName, Type type)
        {
            DebugLog($"Creating {nodeName} with type {type.Name}");
            var uniqueName = GenerateUniqueName(nodeName);
            var node = new IRNodeDefinition(uniqueName, type);
            NodeNames.Add(nodeName);
            m_Nodes.Add(node);
            return node;
        }

        internal IRNodeDefinition CreateNode(string nodeName, string typeName)
        {
            DebugLog($"Creating {nodeName} with type {typeName}");
            var uniqueName = GenerateUniqueName(nodeName);
            var node = new IRNodeDefinition(uniqueName, typeName);
            NodeNames.Add(nodeName);
            m_Nodes.Add(node);
            return node;
        }

        internal IRNodeDefinition CreateNodeFromModel(GUID guid, string nodeName, Type type)
        {
            var node = CreateNode(nodeName, type);
            ModelToNode.Add(guid, node);
            return node;
        }

        internal IRNodeDefinition CreateNodeFromModel(GUID guid, string nodeName, string typeName)
        {
            var node = CreateNode(nodeName, typeName);
            ModelToNode.Add(guid, node);
            return node;
        }

        //Todo NAM : Have only one Connect(), resolve if it’s Sim/Data/Hybrid later on (probably in translator)
        internal IRConnection ConnectSimulation(IRPortTarget src, IRPortTarget dst)
        {
            DebugLog($"Connecting Simulation : Source {src.Node.Name}.{src.PortName}  Destination {dst.Node.Name}.{dst.PortName}");
            var connection = new IRConnection(src, dst);
            m_SimulationConnections.Add(connection);
            return connection;
        }

        internal IRConnection ConnectRendering(IRPortTarget src, IRPortTarget dst)
        {
            DebugLog($"Connecting Rendering : Source {src.Node.Name}.{src.PortName}  Destination {dst.Node.Name}.{dst.PortName}");
            var connection = new IRConnection(src, dst);
            m_DataConnections.Add(connection);
            return connection;
        }

        internal IRConnection ConnectHybrid(IRPortTarget src, IRPortTarget dst)
        {
            DebugLog($"Connecting Hybrid : Source {src.Node.Name}.{src.PortName}  Destination {dst.Node.Name}.{dst.PortName}");
            var connection = new IRConnection(src, dst);
            m_SimulationToDataConnections.Add(connection);
            return connection;
        }

        internal IRPortDefaultValue AddDefaultValue(IRPortTarget portTarget, object value, bool isMessagePort, GlobalObjectId id = default)
        {
            DebugLog($"AddDefaultValue : {portTarget.Node.Name}.{portTarget.PortName}  Value {value}");
            var defaultValue = new IRPortDefaultValue(portTarget, value, isMessagePort, id);
            m_PortDefaultValues.Add(defaultValue);
            return defaultValue;
        }

        internal IRNodeDefinition GetNodeFromName(string name)
        {
            return m_Nodes.Find(n => n.Name == name);
        }

        internal IRNodeDefinition GetNodeFromModel(GUID guid)
        {
            return ModelToNode[guid];
        }

        internal void AddInput(IRPortTarget src, IRPortTarget dst)
        {
            DebugLog($"AddInput : Source {src.Node.Name}.{src.PortName}  Destination {dst.Node.Name}.{dst.PortName}");
            m_Inputs.Add(new IRConnection(src, dst));
        }

        internal void AddOutput(IRPortTarget src, IRPortTarget dst)
        {
            DebugLog($"AddOutput : Source {src.Node.Name}.{src.PortName}  Destination {dst.Node.Name}.{dst.PortName}");
            m_Outputs.Add(new IRConnection(src, dst));
        }

        internal Dictionary<GUID, IRNodeDefinition> ModelToNode { get; } = new Dictionary<GUID, IRNodeDefinition>();
        HashSet<string> NodeNames = new HashSet<string>();
        List<IRPortDefaultValue> m_PortDefaultValues = new List<IRPortDefaultValue>();
        List<IRNodeDefinition> m_Nodes = new List<IRNodeDefinition>();
        List<IRConnection> m_SimulationConnections = new List<IRConnection>();
        List<IRConnection> m_DataConnections = new List<IRConnection>();
        List<IRConnection> m_SimulationToDataConnections = new List<IRConnection>();
        List<IRConnection> m_Inputs = new List<IRConnection>();
        List<IRConnection> m_Outputs = new List<IRConnection>();

        internal IReadOnlyList<IRPortDefaultValue> PortDefaultValues => m_PortDefaultValues;
        internal IReadOnlyList<IRNodeDefinition> Nodes => m_Nodes;
        internal IReadOnlyList<IRConnection> SimulationConnections => m_SimulationConnections;
        internal IReadOnlyList<IRConnection> SimulationToDataConnections => m_SimulationToDataConnections;
        internal IReadOnlyList<IRConnection> DataConnections => m_DataConnections;
        internal IReadOnlyList<IRConnection> Inputs => m_Inputs;
        internal IReadOnlyList<IRConnection> Outputs => m_Outputs;

        internal Dictionary<GlobalObjectId, IRAssetReference> AssetReferences { get; } = new Dictionary<GlobalObjectId, IRAssetReference>();
        internal List<IRObjectReference> BoundObjectReferences { get; } = new List<IRObjectReference>();
        internal Dictionary<ComponentBindingIdentifier, IRPortTarget> InputReferencesTargets { get; } = new Dictionary<ComponentBindingIdentifier, IRPortTarget>();
        internal List<IRExternalAssetReference> ExternalAssetReferenceMappings { get; } = new List<IRExternalAssetReference>();

        internal Dictionary<IRNodeDefinition, IRPortGroupInfos> PortGroupInfos { get; } = new Dictionary<IRNodeDefinition, IRPortGroupInfos>();

        internal CompilationResult CompilationResult { get; set; }
        internal bool HasBuildFailed { get { return CompilationResult.status == CompilationStatus.Failed; } }

        internal Dictionary<string, IR> ReferencedIRs = new Dictionary<string, IR>();

        private string GenerateUniqueName(string baseName) { return NameCache.GenerateUniqueName(baseName); }
        private NameCache NameCache = new NameCache();


        internal bool IsStateMachine { get; set; }
        internal List<StateMachineIRStateDefinition> States { get; } = new List<StateMachineIRStateDefinition>();
        internal List<StateMachineIRTransition> Transitions { get; } = new List<StateMachineIRTransition>();
    }

    internal class NameCache
    {
        private HashSet<string> m_registeredNames = new HashSet<string>();
        public string GenerateUniqueName(string baseName)
        {
            var generatedName = baseName;
            var index = 1;

            // Check for collisions
            while (m_registeredNames.Contains(generatedName))
            {
                generatedName = baseName + index;
                index++;
            }
            m_registeredNames.Add(generatedName);
            return generatedName;
        }
    }

    internal class StateMachineIRStateDefinition
    {
        internal StateMachineIRStateDefinition(string name, Hash128 definitionHash, int nodeIndex)
        {
            Name = name;
            DefinitionHash = definitionHash;
            Index = nodeIndex;
        }

        internal string Name { get; }
        internal Hash128 DefinitionHash { get; }
        internal int Index { get; }
    }

    internal class StateMachineIRTransition
    {
        internal enum TransitionType
        {
            StateToState,
            Global,
            OnEnterSelector,
            Self
        }

        internal StateMachineIRTransition(TransitionType type, int sourceState, int targetState, BaseTransitionSelectorProperties selectorProp, StateTransitionProperties transitionProp)
        {
            Type = type;
            SourceState = sourceState;
            TargetState = targetState;
            SelectorProperties = selectorProp;
            TransitionProperties = transitionProp;
        }

        internal TransitionType Type { get; }
        internal int SourceState { get; }
        internal int TargetState { get; }
        internal BaseTransitionSelectorProperties SelectorProperties { get; }
        internal StateTransitionProperties TransitionProperties { get; }
    }
}
