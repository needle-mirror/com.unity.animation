using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Animation.Editor;
using Unity.DataFlowGraph.Attributes;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace Unity.Animation.Model
{
    internal class PortDefinition
    {
        internal Type Type { get; set; }
        internal string FieldName { get; set; }
        internal string DisplayName { get; set; }
        internal string Description { get; set; }
        internal object DefaultValue { get; set; }
        internal DefaultValueType DefValueType { get; set; }
        internal bool IsStatic { get; set; }
        internal bool IsHidden { get; set; }
        internal SerializableGUID Guid { get; set; }
    }

    internal class PortGroupDefinition
    {
        internal static int DefaultGroupIndex = 0;
        internal static int InvalidGroupIndex = -1;

        internal int GroupIndex { get; set; } = DefaultGroupIndex;
        internal List<PortDefinition> MessageInputs { get; set; } = new List<PortDefinition>();
        internal List<PortDefinition> MessageOutputs { get; set; } = new List<PortDefinition>();
        internal List<PortDefinition> DataInputs { get; set; } = new List<PortDefinition>();
        internal List<PortDefinition> DataOutputs { get; set; } = new List<PortDefinition>();
        internal int MinInstance { get; set; } = -1;
        internal int MaxInstance { get; set; } = -1;
        internal string SimulationPortToDrive { get; set; }
        internal string PortGroupSizeDescription { get; set; }
        internal bool IsDefaultGroup = true;
    }

    internal class PortGroups
    {
        internal Dictionary<int, PortGroupDefinition> Definitions;

        internal PortGroupDefinition GetOrCreateGroupInstance(int portGroupIndex)
        {
            if (Definitions == null)
            {
                Definitions = new Dictionary<int, PortGroupDefinition>();
            }

            PortGroupDefinition portGroup;
            if (!Definitions.TryGetValue(portGroupIndex, out portGroup))
            {
                portGroup = new PortGroupDefinition() { GroupIndex = portGroupIndex };
                Definitions.Add(portGroupIndex, portGroup);
            }

            return portGroup;
        }
    }

    [Serializable]
    internal class NodePortCreation
    {
        public PortType PortType;
        public TypeHandle DataType;
        public BasePortModel.PortEvaluationType EvalType;
        public string Name;
        public string DisplayName;
        public string PortDescription;
        public int PortGroupIndex = PortGroupDefinition.InvalidGroupIndex;
        public int PortGroupInstance = -1;
        public object DefaultValue;
        public DefaultValueType DefValueType;
        public bool IsStatic = false;
        public bool IsHidden = false;
        public SerializableGUID Guid;
    }

    [Serializable]
    internal abstract class BaseNodeModel : NodeModel, IPortGroup
    {
        [SerializeField, HideInInspector]
        private List<int> m_PortGroupSizes = new List<int>();

        public abstract string NodeName { get; }

        public abstract INodeIRBuilder Builder { get; }

        public virtual PortGroups PortGroupDefinitions => m_PortGroupDefinitions ?? (m_PortGroupDefinitions = new PortGroups());
        internal PortGroups m_PortGroupDefinitions = null;

        public virtual void OnVisit() {}

        protected override void OnDefineNode()
        {
            m_PortGroupDefinitions?.Definitions?.Clear();
        }

        public IEnumerable<IPortModel> GetInputMessagePorts()
        {
            return InputsById.Values.OfType<MessagePortModel>();
        }

        public IEnumerable<IPortModel> GetOutputMessagePorts()
        {
            return OutputsById.Values.OfType<MessagePortModel>();
        }

        public IEnumerable<IPortModel> GetInputDataPorts()
        {
            return InputsById.Values.OfType<DataPortModel>();
        }

        public IEnumerable<IPortModel> GetOutputDataPorts()
        {
            return OutputsById.Values.OfType<DataPortModel>();
        }

        private NodePortCreation m_PortCreationData;

        public override IPortModel CreatePort(Direction direction, string portName, PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            BasePortModel newPort;
            if (m_PortCreationData != null && m_PortCreationData.EvalType == BasePortModel.PortEvaluationType.Rendering)
            {
                newPort = new DataPortModel(portName ?? "", portId, options, m_PortCreationData.DisplayName)
                {
                    Direction = direction,
                    PortType = portType,
                    DataTypeHandle = dataType,
                    NodeModel = this,
                    IsStatic = m_PortCreationData?.IsStatic ?? false,
                    IsHidden = m_PortCreationData?.IsHidden ?? false
                };
            }
            else
            {
                newPort = new MessagePortModel(portName ?? "", portId, options, m_PortCreationData.DisplayName)
                {
                    Direction = direction,
                    PortType = portType,
                    DataTypeHandle = dataType,
                    NodeModel = this,
                    IsStatic = m_PortCreationData?.IsStatic ?? false,
                    IsHidden = m_PortCreationData?.IsHidden ?? false
                };
            }

            SetCreationParametersOnPort(newPort, m_PortCreationData);
            return newPort;
        }

        internal void SetCreationParametersOnPort(BasePortModel portModel, NodePortCreation nodePortCreation)
        {
            if (portModel == null)
                return;
            if (!string.IsNullOrEmpty(nodePortCreation.DisplayName))
            {
                portModel.OriginalScriptName = nodePortCreation.Name;
                portModel.DisplayName = nodePortCreation.DisplayName;
            }

            portModel.PortDescription = nodePortCreation.PortDescription;
            portModel.PortGroupInstance = nodePortCreation.PortGroupInstance;
            portModel.PortGroupIndex = nodePortCreation.PortGroupIndex;
            portModel.EvaluationType = nodePortCreation.EvalType;
        }

        protected PortGroupDefinition AddPortGroup(int portGroupIndex)
        {
            var group = PortGroupDefinitions.GetOrCreateGroupInstance(portGroupIndex);

            var countPort = AddInputPort(
                new NodePortCreation()
                {
                    PortType = PortType.Data,
                    EvalType = BasePortModel.PortEvaluationType.Simulation,
                    DataType = typeof(PortGroup).GenerateTypeHandle(),
                    Name = group.SimulationPortToDrive,
                    DisplayName = group.SimulationPortToDrive,
                    PortDescription = group.PortGroupSizeDescription,
                    PortGroupIndex = group.GroupIndex,
                    IsStatic = true
                }, false);

            var size = Math.Max(group.MinInstance, GetPortGroupSize(group.GroupIndex));
            countPort.EmbeddedValue.ObjectValue =
                new PortGroup()
            {
                Index = group.GroupIndex,
                Size = size
            };
            countPort.IsPortGroupSize = true;
            SetPortGroupInstanceSize(group.GroupIndex, size, forceCreation: true);
            return group;
        }

        public IEnumerable<BasePortModel> GetPortsByGroup(int groupIndex)
        {
            foreach (var input in InputsById.Values)
            {
                if (input is BasePortModel baseInput && baseInput.PortGroupIndex == groupIndex)
                    yield return baseInput;
            }
            foreach (var output in OutputsById.Values)
            {
                if (output is BasePortModel baseOutput && baseOutput.PortGroupIndex == groupIndex)
                    yield return baseOutput;
            }
        }

        public int GetPortGroupSize(int portGroupIndex)
        {
            if (portGroupIndex == PortGroupDefinition.InvalidGroupIndex || m_PortGroupSizes.Count <= portGroupIndex)
                return -1;
            return m_PortGroupSizes[portGroupIndex];
        }

        public void SetPortGroupSize(int portGroupIndex, int portGroupSize)
        {
            if (portGroupIndex != PortGroupDefinition.InvalidGroupIndex && portGroupIndex < m_PortGroupSizes.Count)
                m_PortGroupSizes[portGroupIndex] = portGroupSize;
            else
            {
                int oldPortGroupSize = m_PortGroupSizes.Count;
                int numberOfInvalidEntries = portGroupIndex - oldPortGroupSize;
                if (numberOfInvalidEntries > 0)
                    m_PortGroupSizes.AddRange(Enumerable.Repeat<int>(-1, numberOfInvalidEntries));
                m_PortGroupSizes.Add(portGroupSize);
            }
        }

        protected virtual void OnPortGroupResized() {}

        public void SetPortGroupInstanceSize(int groupIndex, int nbrPortGroupInstances, bool forceCreation = false)
        {
            if (groupIndex == PortGroupDefinition.InvalidGroupIndex)
                return;
            int currentPortGroupInstances = GetPortGroupSize(groupIndex);
            if (currentPortGroupInstances != -1 && currentPortGroupInstances == nbrPortGroupInstances && !forceCreation)
            {
                return;
            }
            if (!PortGroupDefinitions.Definitions.TryGetValue(groupIndex, out PortGroupDefinition portGroupDefinition))
                return;

            if (!forceCreation)
            {
                OnPortGroupResized();
            }
            if (!portGroupDefinition.IsDefaultGroup)
            {
                if (portGroupDefinition.MaxInstance >= 0 && nbrPortGroupInstances > portGroupDefinition.MaxInstance)
                    nbrPortGroupInstances = portGroupDefinition.MaxInstance;
                else if (portGroupDefinition.MinInstance >= 0 && nbrPortGroupInstances < portGroupDefinition.MinInstance)
                    nbrPortGroupInstances = portGroupDefinition.MinInstance;
            }

            if (currentPortGroupInstances > nbrPortGroupInstances)
            {
                var listPortsToDelete = GetPortsByGroup(groupIndex).Where(x => x.PortGroupInstance >= nbrPortGroupInstances).ToList();
                DeletePorts(listPortsToDelete);
            }
            else
            {
                for (int i = forceCreation ? 0 : currentPortGroupInstances; i < nbrPortGroupInstances; ++i)
                {
                    foreach (var port in portGroupDefinition.MessageInputs)
                    {
                        AddInputPort(
                            new NodePortCreation()
                            {
                                PortType = port.Type == ((BaseGraphStencil)Stencil).Context.DefaultDataType ? PortType.Execution : PortType.Data,
                                EvalType = BasePortModel.PortEvaluationType.Simulation,
                                DataType = port.Type.GenerateTypeHandle(),
                                Name = port.FieldName,
                                DisplayName = port.DisplayName,
                                PortDescription = port.Description,
                                PortGroupIndex = groupIndex,
                                PortGroupInstance = groupIndex != PortGroupDefinition.DefaultGroupIndex ? i : -1,
                                DefaultValue = port.DefaultValue,
                                DefValueType = port.DefValueType,
                                IsStatic = port.IsStatic,
                                Guid = port.Guid,
                                IsHidden = port.IsHidden
                            }, groupIndex != PortGroupDefinition.DefaultGroupIndex);
                    }

                    foreach (var port in portGroupDefinition.DataInputs)
                    {
                        AddInputPort(
                            new NodePortCreation()
                            {
                                PortType = port.Type == ((BaseGraphStencil)Stencil).Context.DefaultDataType ? PortType.Execution : PortType.Data,
                                EvalType = BasePortModel.PortEvaluationType.Rendering,
                                DataType = port.Type.GenerateTypeHandle(),
                                Name = port.FieldName,
                                DisplayName = port.DisplayName,
                                PortDescription = port.Description,
                                PortGroupIndex = groupIndex,
                                PortGroupInstance = groupIndex != PortGroupDefinition.DefaultGroupIndex ? i : -1,
                                DefaultValue = port.DefaultValue,
                                DefValueType = port.DefValueType,
                                IsStatic = port.IsStatic,
                                Guid = port.Guid,
                                IsHidden = port.IsHidden
                            }, groupIndex != PortGroupDefinition.DefaultGroupIndex);
                    }

                    foreach (var port in portGroupDefinition.MessageOutputs)
                    {
                        AddOutputPort(
                            new NodePortCreation()
                            {
                                PortType = port.Type == ((BaseGraphStencil)Stencil).Context.DefaultDataType ? PortType.Execution : PortType.Data,
                                EvalType = BasePortModel.PortEvaluationType.Simulation,
                                DataType = port.Type.GenerateTypeHandle(),
                                Name = port.FieldName,
                                DisplayName = port.DisplayName,
                                PortDescription = port.Description,
                                PortGroupIndex = groupIndex,
                                PortGroupInstance = groupIndex != PortGroupDefinition.DefaultGroupIndex ? i : -1,
                                Guid = port.Guid,
                                IsHidden = port.IsHidden
                            }, groupIndex != PortGroupDefinition.DefaultGroupIndex);
                    }

                    foreach (var port in portGroupDefinition.DataOutputs)
                    {
                        AddOutputPort(
                            new NodePortCreation()
                            {
                                PortType = port.Type == ((BaseGraphStencil)Stencil).Context.DefaultDataType ? PortType.Execution : PortType.Data,
                                EvalType = BasePortModel.PortEvaluationType.Rendering,
                                DataType = port.Type.GenerateTypeHandle(),
                                Name = port.FieldName,
                                DisplayName = port.DisplayName,
                                PortDescription = port.Description,
                                PortGroupIndex = groupIndex,
                                PortGroupInstance = groupIndex != PortGroupDefinition.DefaultGroupIndex ? i : -1,
                                Guid = port.Guid,
                                IsHidden = port.IsHidden
                            }, groupIndex != PortGroupDefinition.DefaultGroupIndex);
                    }
                }
            }

            SetPortGroupSize(groupIndex, nbrPortGroupInstances);
        }

        protected void DeletePorts(List<BasePortModel> listPortsToDelete)
        {
            foreach (var port in listPortsToDelete)
            {
                DeletePort(port, removeFromOrderedPorts: true);
            }
        }

        static object TryGetObjectReferenceValueInType(string objectRefString, Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (string.Compare(field.Name, objectRefString, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    object constantValue = field.GetValue(null);
                    if (constantValue != null)
                    {
                        return constantValue;
                    }
                }
            }

            return null;
        }

        public static object GetObjectReferenceValue(string objectRefString, Type dataType, Type nodeType)
        {
            var obj = TryGetObjectReferenceValueInType(objectRefString, dataType);
            if (obj != null)
                return obj;
            obj = TryGetObjectReferenceValueInType(objectRefString, nodeType);
            if (obj != null)
                return obj;
            int lastSeparator = objectRefString.LastIndexOf('.');
            if (lastSeparator != -1)
            {
                var objectTypeString = objectRefString.Substring(0, lastSeparator);
                var variableName = objectRefString.Substring(lastSeparator + 1);
                Type objectType = nodeType.Assembly.GetType(objectTypeString);
                if (objectType != null)
                {
                    obj = TryGetObjectReferenceValueInType(objectRefString, objectType);
                    if (obj != null)
                        return obj;
                }
            }

            return null;
        }

        static bool HandleComplexDefaultValue(object defaultValue, DefaultValueType valueType, IConstant constant, BaseNodeModel nodeModel)
        {
            object val = null;
            if (defaultValue is string defaultValueString)
            {
                if (valueType == DefaultValueType.Reference)
                {
                    var dfgNodeModel = (DFGNodeModel)nodeModel;
                    val = GetObjectReferenceValue(defaultValueString, constant.Type, dfgNodeModel.NodeType);
                }
                else if (valueType == DefaultValueType.ComplexValue)
                {
                    val = TypeConstantHandlerDictionary.Instance.GetValueFromDefaultString(constant.Type, defaultValueString);
                }
            }
            if (val != null)
            {
                constant.ObjectValue = val;
                return true;
            }

            return false;
        }

        internal virtual BasePortModel AddInputPort(NodePortCreation nodePortCreation, bool appendGroupIndex = false)
        {
            var dataType = nodePortCreation.DataType;

            var node = this;
            Action<IConstant> preDefine = null;
            if (nodePortCreation.DefaultValue != null)
            {
                preDefine = (constant =>
                {
                    if (HandleComplexDefaultValue(nodePortCreation.DefaultValue, nodePortCreation.DefValueType, constant, node))
                        return;
                    constant.ObjectValue = nodePortCreation.DefaultValue;
                });
            }
            m_PortCreationData = nodePortCreation;
            var newPort = AddInputPort(appendGroupIndex ? $"{nodePortCreation.Name}{nodePortCreation.PortGroupInstance}" : nodePortCreation.Name, nodePortCreation.PortType, dataType, preDefine: preDefine);
            BasePortModel newPortAsBasePortModel = newPort as BasePortModel;
            newPortAsBasePortModel.OriginalScriptName = nodePortCreation.Name;

            // TODO FB : feels a bit weird we have to do this in order for the constant editor to work.
            if (dataType.Resolve().IsEnum)
                newPort.EmbeddedValue.ObjectValue = new EnumValueReference(dataType);

            return newPortAsBasePortModel;
        }

        internal virtual BasePortModel AddOutputPort(NodePortCreation nodePortCreation, bool appendGroupIndex = false)
        {
            m_PortCreationData = nodePortCreation;
            var newPort = AddOutputPort(appendGroupIndex ? $"{nodePortCreation.Name}{nodePortCreation.PortGroupInstance}" : nodePortCreation.Name, nodePortCreation.PortType, nodePortCreation.DataType);
            BasePortModel newPortAsBasePortModel = newPort as BasePortModel;
            return newPortAsBasePortModel;
        }
    }
}
