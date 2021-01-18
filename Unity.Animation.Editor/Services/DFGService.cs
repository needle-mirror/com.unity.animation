using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.DataFlowGraph.Attributes;
using Unity.DataFlowGraph;
using UnityEngine.UIElements;
using static Unity.DataFlowGraph.PortDescription;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal static class DFGService
    {
        internal enum PortUsage
        {
            Message,
            Data
        }

        internal static readonly string[] k_BlackListedAssemblies =
        {
            "Unity.DataFlowGraph",
            "Unity.DataFlowGraph.Tests",
            "boo.lang",
            "castle.core",
            "excss.unity",
            "jetbrains",
            "lucene",
            "microsoft",
            "mono",
            "moq",
            "nunit",
            "system.web",
            "unityscript",
            "visualscriptingassembly-csharp"
        };

        static IEnumerable<Assembly> s_Assemblies;
        private static Dictionary<Type, PortGroups> s_PortDescriptions = new Dictionary<Type, PortGroups>();
        private static Dictionary<Type, PortGroupDefinition> s_FlattenPortDescriptions = new Dictionary<Type, PortGroupDefinition>();

        internal static IEnumerable<Assembly> CachedAssemblies
        {
            get
            {
                return s_Assemblies ?? (s_Assemblies = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic
                            && !k_BlackListedAssemblies.Any(b => a.GetName().Name.ToLower().Contains(b))
                            && !a.GetName().Name.EndsWith(".Tests"))
                        .ToList());
            }
            set
            {
                s_Assemblies = value;
                m_AvailableNodes = null;
            }
        }

        internal static DFGNode GetNode(Type type)
        {
            PopulateAvailableNodes();
            if (m_AvailableNodes.TryGetValue(type, out var value))
                return value;
            return null;
        }

        internal class DFGNode
        {
            internal Type Type;
            internal GUID Guid;
            internal NodeDefinitionAttribute Definition;
        }

        static void PopulateAvailableNodes()
        {
            if (m_AvailableNodes == null)
            {
                m_AvailableNodes = new Dictionary<Type, DFGNode>();

                Dictionary<GUID, Type> nodeIds = new Dictionary<GUID, Type>();

                foreach (var a in CachedAssemblies)
                {
                    foreach (var t in AssemblyExtensions.GetTypesSafe(a))
                    {
                        if (!t.IsAbstract && t.IsClass && t.IsVisible && !t.IsGenericType && typeof(NodeDefinition).IsAssignableFrom(t))
                        {
                            var idatt = t.GetCustomAttribute<NodeDefinitionAttribute>();
                            if (idatt != null)
                            {
                                if (!GUID.TryParse(idatt.Guid, out var guid))
                                {
                                    Debug.LogWarning($"Invalid Node ID for type {t}");
                                    continue;
                                }

                                if (nodeIds.TryGetValue(guid, out var type))
                                {
                                    Debug.LogError($"Node ID {idatt.Guid} on type {t.FullName} already exists on type {type.FullName}");
                                    continue;
                                }

                                nodeIds.Add(guid, t);
                                m_AvailableNodes.Add(t, new DFGNode() { Type = t, Guid = guid, Definition = idatt });
                            }
                        }
                    }
                }
            }
        }

        static Dictionary<Type, DFGNode> m_AvailableNodes;
        internal static IReadOnlyList<Type> GetAvailableTypes(bool sorted = false)
        {
            PopulateAvailableNodes();
            var nodeList = m_AvailableNodes.Select(k => k.Value.Type).ToList();
            if (sorted)
                nodeList.Sort((x, y) => string.Compare(x.Name, y.Name, true));
            return nodeList;
        }

        internal static IReadOnlyList<DFGNode> GetAvailableNodes()
        {
            PopulateAvailableNodes();
            return m_AvailableNodes.Select(v => v.Value).ToList();
        }

        private static Category CategoryFromPortUsage(PortUsage usage)
        {
            return usage == PortUsage.Message ? Category.Message : Category.Data;
        }

        private static PortUsage PortUsageFromCategory(Category usage)
        {
            return usage == Category.Message ? PortUsage.Message : PortUsage.Data;
        }

        internal static List<Type> GetAvailablePortDataTypes(PortUsage usage)
        {
            PopulateAvailableNodes();

            var portDataTypes = new List<Type>();

            foreach (var n in m_AvailableNodes)
            {
                try
                {
                    var portDesc = GetFlattenedNodePortDescription(n.Value.Type);
                    if (usage == PortUsage.Message)
                    {
                        portDataTypes.AddRange(from i in portDesc.MessageInputs
                            select i.Type);
                        portDataTypes.AddRange(from o in portDesc.MessageOutputs
                            select o.Type);
                    }
                    else
                    {
                        portDataTypes.AddRange(from i in portDesc.DataInputs
                            select i.Type);
                        portDataTypes.AddRange(from o in portDesc.DataOutputs
                            select o.Type);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"Exception was raised during port description generation for node {n.Value.Type.Name}.\nException Message : {e.Message}");
                }
            }

            return portDataTypes.Distinct().ToList();
        }

        internal static List<Tuple<Type, string>> GetInputTypesFromNodeType(Type nodeType, PortUsage usage)
        {
            List<Tuple<Type, string>> types = new List<Tuple<Type, string>>();
            var portDesc = GetFlattenedNodePortDescription(nodeType);
            if (usage == PortUsage.Data)
                types.AddRange(portDesc.DataInputs.Select(t => new Tuple<Type, string>(t.Type, t.FieldName)));
            else
                types.AddRange(portDesc.MessageInputs.Select(t => new Tuple<Type, string>(t.Type, t.FieldName)));
            return types;
        }

        internal static List<Tuple<Type, string>> GetOutputTypesFromNodeType(Type nodeType, PortUsage usage)
        {
            List<Tuple<Type, string>> types = new List<Tuple<Type, string>>();
            var portDesc = GetFlattenedNodePortDescription(nodeType);
            if (usage == PortUsage.Data)
                types.AddRange(portDesc.DataOutputs.Select(t => new Tuple<Type, string>(t.Type, t.FieldName)));
            else
                types.AddRange(portDesc.MessageOutputs.Select(t => new Tuple<Type, string>(t.Type, t.FieldName)));
            return types;
        }

        internal static bool IsPortHidden(Type nodeType, PortUsage usage, string name)
        {
            var nestedTypes = nodeType.GetNestedTypes();
            return IsPortHidden(
                nestedTypes.FirstOrDefault(a => typeof(ISimulationPortDefinition).IsAssignableFrom(a)),
                nestedTypes.FirstOrDefault(a => typeof(IKernelPortDefinition).IsAssignableFrom(a)),
                CategoryFromPortUsage(usage), name);
        }

        static bool IsPortHidden(Type simType, Type kernelType, Category usage, string name)
        {
            var isData = usage != Category.Message;
            if (isData && kernelType != null)
            {
                var portDefAttrib = kernelType.GetField(name)?.GetCustomAttribute<PortDefinitionAttribute>();
                if (portDefAttrib != null && portDefAttrib.IsHidden)
                    return true;
            }
            else if (!isData && simType != null)
            {
                var portDefAttrib = simType.GetField(name)?.GetCustomAttribute<PortDefinitionAttribute>();
                if (portDefAttrib != null && portDefAttrib.IsHidden)
                    return true;
            }

            return false;
        }

        internal static PortGroups GetPortGroupDefinitions(DFGNodeModel model)
        {
            var portDesc = GetNodePortDescription(model.NodeType);
            return portDesc;
        }

        internal static void DefineNode(DFGNodeModel model, Stencil stencil)
        {
            var portDesc = GetNodePortDescription(model.NodeType);
            foreach (var portGroup in portDesc.Definitions)
            {
                CreatePortsForPortGroup(portGroup.Value, model, stencil);
            }
        }

        static void CreatePortsForPortGroup(PortGroupDefinition portGroup, DFGNodeModel model, Stencil stencil)
        {
            var portGroupDefinitions = GetPortGroupDefinitions(model);
            portGroupDefinitions.Definitions.TryGetValue(portGroup.GroupIndex, out PortGroupDefinition portGroupDefinition);
            int nbrInstanceToCreate = 1;
            int nbrPortGroupInstance = model.GetPortGroupSize(portGroup.GroupIndex);
            if (nbrPortGroupInstance != -1)
            {
                if (!portGroup.IsDefaultGroup)
                    nbrInstanceToCreate = nbrPortGroupInstance;
            }
            else
            {
                if (!portGroup.IsDefaultGroup)
                    nbrInstanceToCreate = portGroupDefinition.MinInstance;
                model.SetPortGroupSize(portGroup.GroupIndex, nbrInstanceToCreate);
            }

            if (portGroup.GroupIndex != 0 && (portGroupDefinition.MaxInstance == -1 || (portGroupDefinition.MaxInstance > 1 && portGroupDefinition.MinInstance < portGroupDefinition.MaxInstance)))
            {
                var newPort = model.AddInputPort(
                    new NodePortCreation()
                    {
                        PortType = PortType.Data,
                        EvalType = BasePortModel.PortEvaluationType.Simulation,
                        DataType = typeof(PortGroup).GenerateTypeHandle(),
                        Name = portGroupDefinition.PortGroupSizeDescription,
                        DisplayName = portGroupDefinition.PortGroupSizeDescription,
                        PortDescription = null,
                        PortGroupIndex = portGroup.GroupIndex,
                        IsStatic = true
                    });
                newPort.EmbeddedValue.ObjectValue = new PortGroup(){ Index = portGroup.GroupIndex, Size = nbrInstanceToCreate };
                newPort.IsPortGroupSize = true;
            }

            model.SetPortGroupInstanceSize(portGroup.GroupIndex, nbrInstanceToCreate, forceCreation: true);
        }

        static void OnPortGroupSizeChange(IChangeEvent valueChangeEvent, Store store, IPortModel portModel)
        {
            BasePortModel basePortModel = portModel as BasePortModel;
            var changeEvent = valueChangeEvent as ChangeEvent<int>;
            if (basePortModel == null || changeEvent == null || !basePortModel.IsPortGroupSize || changeEvent.previousValue == changeEvent.newValue)
            {
                store.MarkStateDirty();
                return;
            }

            var setNbrPortAction = new SetNumberOfPortGroupInstanceAction(portModel.NodeModel, basePortModel.PortGroupIndex, changeEvent.previousValue, changeEvent.newValue);
            store.Dispatch(setNbrPortAction);
        }

        internal static PortGroupDefinition GetFlattenedNodePortDescription(Type nodeType)
        {
            PortGroupDefinition allPorts;

            if (s_FlattenPortDescriptions.TryGetValue(nodeType, out allPorts))
                return allPorts;

            allPorts = new PortGroupDefinition();
            var nodePortDescription = GetNodePortDescription(nodeType);
            foreach (var entry in nodePortDescription.Definitions)
            {
                allPorts.DataInputs.AddRange(entry.Value.DataInputs);
                allPorts.DataOutputs.AddRange(entry.Value.DataOutputs);
                allPorts.MessageInputs.AddRange(entry.Value.MessageInputs);
                allPorts.MessageOutputs.AddRange(entry.Value.MessageOutputs);
            }

            s_FlattenPortDescriptions[nodeType] = allPorts;
            return allPorts;
        }

        internal static PortGroups GetNodePortDescription(Type nodeType)
        {
            PortGroups desc;

            if (s_PortDescriptions.TryGetValue(nodeType, out desc))
                return desc;

            desc = new PortGroups()
            {
                Definitions = new Dictionary<int, PortGroupDefinition>()
            };

            try
            {
                var fields = nodeType.GetFields(BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public);

                FieldInfo simulationPortDefinition = null, kernelPortDefinition = null;

                foreach (var fieldInfo in fields)
                {
                    if (simulationPortDefinition != null && kernelPortDefinition != null)
                        break;

                    if (typeof(ISimulationPortDefinition).IsAssignableFrom(fieldInfo.FieldType))
                        simulationPortDefinition = fieldInfo;
                    else if (typeof(IKernelPortDefinition).IsAssignableFrom(fieldInfo.FieldType))
                        kernelPortDefinition = fieldInfo;
                }

                if (simulationPortDefinition != null)
                    ParsePortDefinition(simulationPortDefinition, desc, nodeType, true);

                if (kernelPortDefinition != null)
                    ParsePortDefinition(kernelPortDefinition, desc, nodeType, false);

                s_PortDescriptions[nodeType] = desc;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning(
                    $"Exception was raised during port description generation for node {nodeType.Name}.\nException Message : {e.Message}");
            }

            return desc;
        }

        static void ParsePortDefinition(FieldInfo staticTopLevelField, PortGroups portGroups, Type nodeType, bool isSimulation)
        {
            var qualifiedFieldType = staticTopLevelField.FieldType;

            var definitionFields = qualifiedFieldType.GetFields(BindingFlags.Instance | BindingFlags.Public);

            foreach (var fieldInfo in definitionFields)
            {
                // standalone port message/dsl declarations
                var qualifiedSimFieldType = fieldInfo.FieldType;

                if (!qualifiedSimFieldType.IsConstructedGenericType)
                    throw new InvalidNodeDefinitionException($"Simulation port definition contains disallowed field {qualifiedSimFieldType}.");

                var genericField = qualifiedSimFieldType.GetGenericTypeDefinition();

                var generics = qualifiedSimFieldType.GetGenericArguments();

                bool isPortArray = genericField == typeof(PortArray<>);
                if (isPortArray)
                {
                    // Extract the specifics of the port type inside the port array.
                    genericField = generics[0];
                    if (!genericField.IsConstructedGenericType)
                        throw new InvalidNodeDefinitionException($"Simulation port definition contains disallowed field {qualifiedSimFieldType}.");
                    generics = genericField.GetGenericArguments();
                    genericField = genericField.GetGenericTypeDefinition();
                }

                if (generics.Length < 2)
                    throw new InvalidNodeDefinitionException($"Simulation port definition contains disallowed type {qualifiedSimFieldType}.");

                var genericType = generics[1];

                int portGroup = PortGroupDefinition.DefaultGroupIndex;
                PortDefinitionAttribute portDefinitionInfo = fieldInfo.GetCustomAttribute<PortDefinitionAttribute>();

                var newPort = new PortDefinition() { Type = genericType, FieldName = fieldInfo.Name };
                if (portDefinitionInfo != null)
                {
                    newPort.IsStatic = portDefinitionInfo.IsStatic;
                    newPort.IsHidden = portDefinitionInfo.IsHidden;

                    if (portDefinitionInfo.PortGroupIndex != PortGroupDefinition.InvalidGroupIndex)
                        portGroup = portDefinitionInfo.PortGroupIndex;
                    if (portDefinitionInfo.DisplayName != null)
                        newPort.DisplayName = portDefinitionInfo.DisplayName;
                    if (portDefinitionInfo.Description != null)
                        newPort.Description = portDefinitionInfo.Description;
                    if (portDefinitionInfo.DefaultValue != null)
                    {
                        newPort.DefaultValue = portDefinitionInfo.DefaultValue;
                        newPort.DefValueType = portDefinitionInfo.DefaultValueType;
                    }
                }

                var portDefAtt = fieldInfo.GetCustomAttribute<PortDefinitionAttribute>();

                if (portDefAtt != null)
                {
                    if (!GUID.TryParse(portDefAtt.Guid, out var guid))
                        throw new ArgumentException($"Invalid Guid {portDefAtt.Guid} for Port {fieldInfo} in Node {nodeType}");
                    newPort.Guid = guid;
                }

                if (isSimulation)
                {
                    if (genericField == typeof(MessageInput<,>))
                        portGroups.GetOrCreateGroupInstance(portGroup).MessageInputs.Add(newPort);
                    else if (genericField == typeof(MessageOutput<,>))
                        portGroups.GetOrCreateGroupInstance(portGroup).MessageOutputs.Add(newPort);
                    else
                        throw new InvalidNodeDefinitionException($"Simulation port definition contains disallowed type {genericField}.");
                }
                else
                {
                    if (genericField == typeof(DataInput<,>))
                        portGroups.GetOrCreateGroupInstance(portGroup).DataInputs.Add(newPort);
                    else if (genericField == typeof(DataOutput<,>))
                        portGroups.GetOrCreateGroupInstance(portGroup).DataOutputs.Add(newPort);
                    else
                        throw new InvalidNodeDefinitionException($"Kernel port definition contains disallowed type {genericField}.");
                }

                if (generics[0] != nodeType)
                    throw new InvalidNodeDefinitionException($"Port definition references incorrect NodeDefinition class {generics[0]}");
            }

            foreach (var attribute in nodeType.GetCustomAttributes<PortGroupDefinitionAttribute>())
            {
                var group = portGroups.GetOrCreateGroupInstance(attribute.PortGroupIndex);
                group.PortGroupSizeDescription = attribute.PortGroupSizeDescription;
                group.SimulationPortToDrive = attribute.SimulationPortToDrive;
                group.MinInstance = attribute.MinInstance;
                group.MaxInstance = attribute.MaxInstance;
                group.IsDefaultGroup = false;
            }
        }

        internal static bool IsExecutionPort(TypeHandle portType, Stencil stencil)
        {
            return portType == typeof(DataFlowGraph.DataInput<,>).GenerateTypeHandle() ||
                portType == typeof(DataFlowGraph.DataOutput<,>).GenerateTypeHandle();
        }

        internal static bool IsInputPort(TypeHandle portType, Stencil stencil)
        {
            return portType == typeof(DataFlowGraph.MessageInput<,>).GenerateTypeHandle() ||
                portType == typeof(DataFlowGraph.DataInput<,>).GenerateTypeHandle();
        }

        internal static bool IsPortType(System.Type type)
        {
            return type == typeof(DataFlowGraph.MessageInput<,>) || type == typeof(DataFlowGraph.MessageOutput<,>) ||
                type == typeof(DataFlowGraph.DataInput<,>) || type == typeof(DataFlowGraph.DataOutput<,>) ||
                type == typeof(DataFlowGraph.DSLInput<, ,>) || type == typeof(DataFlowGraph.DSLOutput<, ,>);
        }

        internal static string FormatNodeName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                name, "(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=[a-z])(?=[0-9])", " ",
                System.Text.RegularExpressions.RegexOptions.Compiled).Trim().Replace(" Node", "");
        }
    }
}
