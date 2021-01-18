using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.DataFlowGraph.Attributes;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unknown = UnityEditor.GraphToolsFoundation.Overdrive.Unknown;
using Unity.Animation.Editor;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class DFGNodeModel : BaseNodeModel, IPostDefineNode, IMigratePorts, IPortGroup
    {
        internal BaseGraphModel BaseGraphModel => GraphModel as BaseGraphModel;
        public override string Title => m_NodeIdentifier.Name;

        [Serializable]
        internal class PortIdentifier
        {
            internal bool Processed;

            [SerializeField]
            internal SerializableGUID Guid;
            [SerializeField]
            internal string ModelUniqueId;
            [SerializeField]
            internal NodePortCreation Port;
        }

        [Serializable]
        internal class NodeIdentifier
        {
            [SerializeField]
            internal SerializableGUID Guid;
            [SerializeField]
            internal int Version;
            [SerializeField]
            internal TypeHandle Type;
            [SerializeField]
            internal string Name;
            [SerializeField]
            internal List<PortIdentifier> InputIdentifiers = new List<PortIdentifier>();
            [SerializeField]
            internal List<PortIdentifier> OutputIdentifiers = new List<PortIdentifier>();

            internal void FromType(System.Type type, Stencil stencil)
            {
                var node = DFGService.GetNode(type);
                if (node != null)
                {
                    Guid = node.Guid;
                    Version = node.Definition.Version;
                }

                Type = type.GenerateTypeHandle();
                Name = DFGService.FormatNodeName(type.Name);
            }
        }

        [SerializeField, HideInInspector]
        internal NodeIdentifier m_NodeIdentifier = new NodeIdentifier();

        public override PortGroups PortGroupDefinitions => DFGService.GetNodePortDescription(NodeType);

        class PortValidationData : IDisposable
        {
            internal Dictionary<string, NodePortCreation> InputsToProcessed = new Dictionary<string, NodePortCreation>();
            internal Dictionary<string, NodePortCreation> OutputsToProcessed = new Dictionary<string, NodePortCreation>();
            internal Dictionary<string, string> InputPortNameChanges = new Dictionary<string, string>(); // Old Name, New Name
            internal Dictionary<string, string> OutputPortNameChanges = new Dictionary<string, string>(); // Old Name, New Name
            internal List<Tuple<SerializableGUID, string>> InputPortsRemoved = new List<Tuple<SerializableGUID, string>>();
            internal List<Tuple<SerializableGUID, string>> OutputPortsRemoved = new List<Tuple<SerializableGUID, string>>();

            public void Dispose()
            {
                InputsToProcessed.Clear();
                OutputsToProcessed.Clear();
                InputPortNameChanges.Clear();
                OutputPortNameChanges.Clear();
                InputPortsRemoved.Clear();
                OutputPortsRemoved.Clear();
            }
        }

        PortValidationData m_ValidationData;

        public bool IsValid { get; set; } = true;

        public Type NodeType
        {
            get
            {
                if (m_NodeIdentifier.Type.IsValid && Stencil != null)
                    return m_NodeIdentifier.Type.Resolve();
                return null;
            }
            set
            {
                m_NodeIdentifier.FromType(value, Stencil);
            }
        }

        public override string Tooltip
        {
            get
            {
                if (NodeType.GetCustomAttributes(typeof(NodeDefinitionAttribute), false).FirstOrDefault() is NodeDefinitionAttribute description
                    && !string.IsNullOrEmpty(description.NodeDescription))
                {
                    return description.NodeDescription;
                }
                return base.Tooltip;
            }
        }

        public override string NodeName
        {
            get { return m_NodeIdentifier.Name; }
        }

        DFGNodeIRBuilder m_Builder;
        public override INodeIRBuilder Builder
        { get { if (m_Builder == null) m_Builder = new DFGNodeIRBuilder(this); return m_Builder; } }

        public override PortCapacity GetPortCapacity(IPortModel portModel)
        {
            return portModel.Direction == Direction.Input ? PortCapacity.Single : PortCapacity.Multi;
        }

        protected override void OnPortGroupResized()
        {
            m_ValidationData = new PortValidationData();
        }

        bool ValidateNodeType()
        {
            if (NodeType == null)
                Debug.LogWarning($"Invalid Type {m_NodeIdentifier.Type.Identification}");

            if (!IsNodeTypeValid(NodeType))
            {
                ResolveNode();
            }

            return IsNodeTypeValid(NodeType);
        }

        void ResolveNode()
        {
            foreach (var n in DFGService.GetAvailableNodes())
            {
                if (n.Guid == m_NodeIdentifier.Guid.GUID)
                {
                    m_NodeIdentifier.Version = n.Definition.Version;
                    m_NodeIdentifier.Type = n.Type.GenerateTypeHandle();
                    break;
                }
            }
        }

        void ValidateVersion()
        {
            var type = NodeType;

            var node = DFGService.GetNode(type);
            if (node != null)
            {
                // Check Version
                if (m_NodeIdentifier.Version != node.Definition.Version)
                {
                    // We might want to trigger a forced build of graphs referencing this node
                }
            }
            else
            {
                IsValid = false;
            }
        }

        bool ValidatePorts()
        {
            bool valid = ValidatePorts(true);
            valid &= ValidatePorts(false);
            return valid;
        }

        bool ValidatePorts(bool isInput)
        {
            var identifiers = isInput ? m_NodeIdentifier.InputIdentifiers : m_NodeIdentifier.OutputIdentifiers;

            InitPortIdentifiers(identifiers);

            bool valid = true;

            var ports = isInput ? m_ValidationData.InputsToProcessed : m_ValidationData.OutputsToProcessed;
            var nameChanges = isInput ? m_ValidationData.InputPortNameChanges : m_ValidationData.OutputPortNameChanges;

            foreach (var p in ports)
            {
                if (p.Value.Guid.GUID.Empty()) continue;

                bool processed = false;

                foreach (var id in identifiers)
                {
                    if (!id.Guid.Equals(p.Value.Guid))
                        continue;

                    if (id.Processed)
                    {
                        // Port Array
                        processed = true;
                        continue;
                    }
                    id.Processed = true;
                    processed = true;

                    if (id.ModelUniqueId != p.Value.Name)
                    {
                        nameChanges.Add(id.ModelUniqueId, p.Value.Name);
                        valid = false;
                    }

                    if (id.Port.DataType == TypeHandle.Unknown && p.Value.DataType != TypeHandle.Unknown)
                    {
                        Debug.LogError($"Port with Guid {id.Guid} of unknown type changed type to {p.Value.DataType.Resolve()}");
                        valid = false;
                    }
                    else if (id.Port.DataType != TypeHandle.Unknown && p.Value.DataType == TypeHandle.Unknown)
                    {
                        Debug.LogError($"Port with old type {id.Port.DataType.Resolve()} with Guid {id.Guid} has been changed to an unknown type");
                        valid = false;
                    }
                    else if (id.Port.DataType.Resolve() != p.Value.DataType.Resolve())
                    {
                        Debug.LogError($"Port with old type {id.Port.DataType.Resolve()} with Guid {id.Guid} changed type to {p.Value.DataType.Resolve()}");
                        valid = false;
                    }
                }

                if (!processed)
                    identifiers.Add(CreatePortIdentifier(p.Key, p.Value));
            }

            valid &= CleanPortIdentifiers(identifiers, isInput);
            return valid;
        }

        static void InitPortIdentifiers(List<PortIdentifier> identifiers)
        {
            foreach (var id in identifiers)
                id.Processed = false;
        }

        bool CleanPortIdentifiers(List<PortIdentifier> identifiers, bool isInput)
        {
            bool portRemoved = false;
            foreach (var id in identifiers)
            {
                if (!id.Processed)
                {
                    if (isInput)
                        m_ValidationData.InputPortsRemoved.Add(Tuple.Create(id.Guid, id.ModelUniqueId));
                    else
                        m_ValidationData.OutputPortsRemoved.Add(Tuple.Create(id.Guid, id.ModelUniqueId));
                    portRemoved = true;
                }
            }
            return !portRemoved;
        }

        PortIdentifier CreatePortIdentifier(string uniqueId, NodePortCreation p)
        {
            var newPortId =
                new PortIdentifier()
            {
                Processed = true,
                ModelUniqueId = uniqueId,
                Port = p
            };
            newPortId.Guid = p.Guid;
            return newPortId;
        }

        protected override void OnDefineNode()
        {
            if (!ValidateNodeType() && HasValidNodeIdentifier())
            {
                Debug.LogError($"Could not resolve Node {m_NodeIdentifier.Name} with type {m_NodeIdentifier.Type}");
                DefineNodeSafe();
                return;
            }

            if (!HasValidNodeIdentifier())
                ResolveIdentifier();

            IsValid = true;
            m_ValidationData = new PortValidationData();

            ValidateVersion();

            DFGService.DefineNode(this, Stencil);

            if (!ValidatePorts())
            {
                ResolvePortNameChanges();
            }
        }

        public void PostDefineNode()
        {
            if (m_ValidationData != null)
            {
                var portDictionary = m_NodeIdentifier.InputIdentifiers.Concat(m_NodeIdentifier.OutputIdentifiers).ToDictionary(k => k.Guid);
                ResolvePortsRemoved(m_ValidationData, portDictionary);
            }
        }

        void ResolveIdentifier()
        {
            var type = NodeType;

            var node = DFGService.GetNode(type);
            if (node != null)
            {
                m_NodeIdentifier.Guid = node.Guid;
                m_NodeIdentifier.Version = node.Definition.Version;
            }
            else
            {
                Debug.LogError($"Could not resolve Node ID of type {type.FullName}");
                IsValid = false;
            }
        }

        public bool MigratePort(ref string portReferenceUniqueId, Direction direction)
        {
            var currentUniqueID = portReferenceUniqueId;
            if (m_ValidationData != null)
            {
                if (direction == Direction.Input)
                {
                    var portId = m_NodeIdentifier.InputIdentifiers.Find(p => currentUniqueID == p.ModelUniqueId);
                    if (portId != null && m_ValidationData.InputPortsRemoved.Find(p => p.Item2 == currentUniqueID) == null)
                    {
                        if (m_ValidationData.InputPortNameChanges.TryGetValue(portReferenceUniqueId, out portReferenceUniqueId))
                        {
                            portId.Port.Name = portReferenceUniqueId;
                            portId.ModelUniqueId = portReferenceUniqueId;
                            return true;
                        }
                    }
                }
                else if (direction == Direction.Output)
                {
                    var portId = m_NodeIdentifier.OutputIdentifiers.Find(p => currentUniqueID == p.ModelUniqueId);
                    if (portId != null && m_ValidationData.OutputPortsRemoved.Find(p => p.Item2 == currentUniqueID) == null)
                    {
                        if (m_ValidationData.OutputPortNameChanges.TryGetValue(portReferenceUniqueId, out portReferenceUniqueId))
                        {
                            portId.Port.Name = portReferenceUniqueId;
                            portId.ModelUniqueId = portReferenceUniqueId;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        void ResolvePortsRemoved(PortValidationData data, Dictionary<SerializableGUID, PortIdentifier> portDictionary)
        {
            foreach (var p in data.InputPortsRemoved)
            {
                if (portDictionary.TryGetValue(p.Item1, out var portId))
                {
                    var uniqueId = p.Item2;
                    if (portId.Port.PortGroupInstance != -1)
                        uniqueId += portId.Port.PortGroupInstance;

                    m_NodeIdentifier.InputIdentifiers.Remove(portId);
                }
            }

            foreach (var p in data.OutputPortsRemoved)
            {
                if (portDictionary.TryGetValue(p.Item1, out var portId))
                {
                    var uniqueId = p.Item2;
                    if (portId.Port.PortGroupInstance != -1)
                        uniqueId += portId.Port.PortGroupInstance;

                    m_NodeIdentifier.OutputIdentifiers.Remove(portId);
                }
            }
        }

        void ResolvePortNameChanges()
        {
            var portDictionary = m_NodeIdentifier.InputIdentifiers.ToDictionary(k => k.ModelUniqueId);
            foreach (var c in m_ValidationData.InputPortNameChanges)
            {
                if (portDictionary.TryGetValue(c.Key, out var portId))
                {
                    var uniqueId = c.Value;
                    var oldUniqueId = portId.ModelUniqueId;
                    if (portId.Port.PortGroupInstance != -1)
                    {
                        uniqueId += portId.Port.PortGroupInstance;
                    }

                    // Update Constant Editor
                    if (InputConstantsById.TryGetValue(oldUniqueId, out var oldConstant) &&
                        InputConstantsById.TryGetValue(uniqueId, out var newConstant))
                    {
                        newConstant.ObjectValue = oldConstant.ObjectValue;
                    }
                }
                else
                    Debug.Log($"Failed to change name on port {c.Key} with new name {c.Value}");
            }
        }

        void DefineNodeSafe()
        {
            foreach (var p in m_NodeIdentifier.InputIdentifiers)
                if (!p.Port.IsHidden)
                    base.AddInputPort(p.Port, p.Port.PortGroupIndex != PortGroupDefinition.DefaultGroupIndex);

            foreach (var p in m_NodeIdentifier.OutputIdentifiers)
                if (!p.Port.IsHidden)
                    base.AddOutputPort(p.Port, p.Port.PortGroupIndex != PortGroupDefinition.DefaultGroupIndex);
            IsValid = false;
        }

        public void Rename(string newName)
        {
            Title = newName;
        }

        internal override BasePortModel AddInputPort(NodePortCreation nodePortCreation, bool appendGroupIndex = false)
        {
            var portModel = base.AddInputPort(nodePortCreation, appendGroupIndex);
            if (m_ValidationData != null)
                m_ValidationData.InputsToProcessed.Add(portModel.UniqueName, nodePortCreation);
            return portModel;
        }

        internal override BasePortModel AddOutputPort(NodePortCreation nodePortCreation, bool appendGroupIndex = false)
        {
            var portModel = base.AddOutputPort(nodePortCreation, appendGroupIndex);
            if (m_ValidationData != null)
                m_ValidationData.OutputsToProcessed.Add(portModel.UniqueName, nodePortCreation);
            return portModel;
        }

        static bool IsNodeTypeValid(Type type)
        {
            return type != null && type != typeof(Unknown);
        }

        bool HasValidNodeIdentifier()
        {
            return !m_NodeIdentifier.Guid.GUID.Empty();
        }
    }
}
