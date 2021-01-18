using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.DataFlowGraph;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.Overdrive;
using TypeHash = Unity.Entities.TypeHash;
using Unity.Animation.Hybrid;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class Translator : ITranslator
    {
        Stencil Stencil;
        private NodeCache m_NodeCache = new NodeCache();
        private IR m_RootIR = null;
        private Dictionary<string, IR> m_CachedIRs = new Dictionary<string, IR>();
        private GraphDefinition m_Graph;
        private Dictionary<Type, int> m_TypesToTypeHash = new Dictionary<Type, int>();
        private NodeSet m_DummyNodeSet;

        public Translator(Stencil stencil)
        {
            Stencil = stencil;
        }

        public bool SupportsCompilation() => true;


        //We add all the subdependencies in a list to be able to register them all at once to the graphmanager
        public static void AddSubdependenciesRecursive(List<IR.NodeDependency> source, List<OtherGraphDependency> targetList)
        {
            foreach (IR.NodeDependency irDependency in source)
            {
                //This is bad because we're passing the same stencil as the current one but it should be a stencil dependent on the context (StateMachineStencil or CompositorGraphStencil)
                //Because of that it's not gonna create the correct PassThrough for Buffer<AnimatedData>
                var graphModel = irDependency.Model;
                var t = graphModel.Stencil.CreateTranslator();

                var translator = (Translator)t;
                var results = translator.TranslateAndCompile(irDependency.Model);
                var depIR = translator.m_RootIR;

                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(irDependency.Model.AssetModel as UnityEngine.Object, out var guid, out long _))
                    return;

                if (targetList.FirstOrDefault(x => x.Guid == guid) == null) //No need to add it twice
                {
                    targetList.Add(new OtherGraphDependency()
                    {
                        Guid = guid,
                    });
                }
                AddSubdependenciesRecursive(depIR.Dependencies , targetList);
            }
        }

        public CompilationResult TranslateAndCompile(IGraphModel gm)
        {
            if (gm is StateMachineModel stateMachineModel)
            {
                return TranslateAndCompileStateMachine(stateMachineModel);
            }
            else if (gm is BaseGraphModel baseGraphModel)
            {
                return TranslateAndCompileBlendTree(baseGraphModel);
            }


            return null;
        }

        public CompilationResult TranslateAndCompileStateMachine(StateMachineModel model)
        {
            var compilationResults = new CompilationResult();
            m_RootIR = IRBuilder.BuildStateMachineIR(model);

            m_Graph = new GraphDefinition();
            BuildGraphFromStateMachineIR();

            var compiledGraphProvider = model.AssetModel as ICompiledGraphProvider;
            compiledGraphProvider.CompiledGraph.DisplayName = m_RootIR.Name;

            compiledGraphProvider.CompiledGraph.CompiledDependencies.Clear(); //Need to clear() because the list is kept between calls
            AddSubdependenciesRecursive(m_RootIR.Dependencies, compiledGraphProvider.CompiledGraph.CompiledDependencies);
            compiledGraphProvider.CompiledGraph.Definition = m_Graph;
            return compilationResults;
        }

        public CompilationResult TranslateAndCompileBlendTree(BaseGraphModel model)
        {
            var compilationResults = new CompilationResult();

            var stencil = model.Stencil as BaseGraphStencil;
            m_RootIR = IRBuilder.BuildBlendTreeIR(model);
            m_CachedIRs = new Dictionary<string, IR>(m_RootIR.ReferencedIRs);

            var compiledGraphProvider = model.AssetModel as ICompiledGraphProvider;
            compiledGraphProvider.CompiledGraph.DisplayName = m_RootIR.Name;

            m_Graph = new GraphDefinition();

            m_DummyNodeSet = new DataFlowGraph.NodeSet(); //Nodeset used to obtain the PortIDs
            {
                Log($"Building flat graph definition for {m_RootIR.Name}");
                Log($"{m_RootIR.DataConnections.Count} Data connections {m_RootIR.SimulationConnections.Count} Sim connections.");
                Log($"{m_RootIR.Inputs.Count} Inputs {m_RootIR.Outputs.Count} Outputs");
                Log($"{m_RootIR.ExternalAssetReferenceMappings.Count} AssetReferenceMappings");
                Log($"{m_RootIR.PortDefaultValues.Count} Defaultvalues");
                Log($"{m_RootIR.PortGroupInfos.Count} PortGroupInfos");

                GraphPath currentPath = new GraphPath();
                currentPath.Push(m_RootIR.Name);

                RegisterNodes(m_RootIR, currentPath);
                ProcessConnectionsRecursive(m_RootIR, currentPath, true);
            }
            m_DummyNodeSet.Dispose();

            compiledGraphProvider.CompiledGraph.Definition = m_Graph;

            compilationResults = m_RootIR.CompilationResult;

            return compilationResults;
        }

        public CompilationResult Compile(IGraphModel graphModel)
        {
            Stencil.PreProcessGraph(graphModel);
            CompilationResult result;

            try
            {
                result = TranslateAndCompile(graphModel);

                if (result.status == CompilationStatus.Failed)
                {
                    Stencil.OnCompilationFailed(graphModel, result);
                }
                else
                {
                    Stencil.OnCompilationSucceeded(graphModel, result);
                }
            }
            catch (Exception e)
            {
                result = null;
                UnityEngine.Debug.LogException(e);
            }

            return result;
        }

        void BuildGraphFromStateMachineIR()
        {
            m_Graph.TopologyDefinition.IsStateMachine = true;
            foreach (var state in m_RootIR.States)
            {
                m_Graph.TopologyDefinition.States.Add(new CreateStateCommand(){StateIndex = state.Index, /*DebugName = state.Name,*/ DefinitionHash = state.DefinitionHash});
            }

            int transitionFragmentIndex = 0;

            foreach (var transition in m_RootIR.Transitions)
            {
                int parentTransitionFragmentIndex = -1;

                GroupConditionModel rootGroupModel =
                    transition.Type == StateMachineIRTransition.TransitionType.OnEnterSelector ?
                    transition.SelectorProperties?.Condition : transition.TransitionProperties?.Condition;
                bool hasConditionFragments = rootGroupModel != null && rootGroupModel.ListSubConditions.Count != 0;
                if (hasConditionFragments)
                {
                    parentTransitionFragmentIndex = transitionFragmentIndex++;
                    m_Graph.TopologyDefinition.ConditionFragments.Add(
                        new CreateConditionFragmentCommand()
                        {
                            ParentIndex = -1,
                            Type = rootGroupModel.GroupOperation == GroupConditionModel.Operation.And ? TransitionFragmentType.GroupAnd : TransitionFragmentType.GroupOr
                        });
                    BuildConditionFragmentsForChildren(rootGroupModel, parentTransitionFragmentIndex, ref transitionFragmentIndex);
                }

                var newTransitionCommand = new CreateTransitionCommand()
                {
                    Type = TranslateTransitionType(transition.Type),
                    TransitionFragmentIndex = parentTransitionFragmentIndex,
                    SourceState = transition.SourceState,
                    TargetState = transition.TargetState
                };

                if (transition.Type == StateMachineIRTransition.TransitionType.OnEnterSelector)
                {
                    if (transition.SelectorProperties == null)
                        throw new InvalidExpressionException();
                    newTransitionCommand.OverrideDuration = transition.SelectorProperties.OverrideTransitionDuration;
                    newTransitionCommand.OverrideSyncType = transition.SelectorProperties.OverrideTransitionSynchronization;
                    newTransitionCommand.OverrideSyncTargetRatio = transition.SelectorProperties.OverrideSyncTargetRatio;
                    newTransitionCommand.OverrideSyncTagType = transition.SelectorProperties.OverrideSyncTagType;
                    newTransitionCommand.OverrideSyncEntryPoint = transition.SelectorProperties.OverrideSyncEntryPoint;
                    newTransitionCommand.OverrideAdvanceSourceDuringTransition = transition.SelectorProperties.OverrideAdvanceSourceDuringTransition;

                    newTransitionCommand.Duration = transition.SelectorProperties.OverriddenTransitionDuration;
                    newTransitionCommand.SyncType = ConvertSynchronizationType(transition.SelectorProperties.OverriddenTransitionSynchronization);
                    newTransitionCommand.SyncTargetRatio = transition.SelectorProperties.OverriddenSyncTargetRatio;
                    //@TODO hash? reference to a guid ?
                    newTransitionCommand.SyncTagType = transition.SelectorProperties.OverriddenSyncTagType == "Foot" ? 1 : 0;
                    newTransitionCommand.SyncEntryPoint = transition.SelectorProperties.OverriddenSyncEntryPoint;
                    newTransitionCommand.AdvanceSourceDuringTransition = transition.SelectorProperties.OverriddenAdvanceSourceDuringTransition;
                }
                else
                {
                    if (transition.TransitionProperties == null)
                        throw new InvalidExpressionException();
                    newTransitionCommand.Duration = transition.TransitionProperties.TransitionDuration;
                    newTransitionCommand.SyncType = ConvertSynchronizationType(transition.TransitionProperties.SynchronizationMode);
                    newTransitionCommand.SyncTargetRatio = transition.TransitionProperties.SyncTargetRatio;
                    //@TODO hash? reference to a guid ?
                    newTransitionCommand.SyncTagType = transition.TransitionProperties.SyncTagType == "Foot" ? 1 : 0;
                    newTransitionCommand.SyncEntryPoint = transition.TransitionProperties.SyncEntryPoint;
                    newTransitionCommand.AdvanceSourceDuringTransition = transition.TransitionProperties.AdvanceSourceDuringTransition;
                }

                m_Graph.TopologyDefinition.Transitions.Add(newTransitionCommand);
            }
        }

        TransitionSynchronizationType ConvertSynchronizationType(StateTransitionProperties.TransitionSynchronization transitionPropertiesSynchronizationMode)
        {
            switch (transitionPropertiesSynchronizationMode)
            {
                case StateTransitionProperties.TransitionSynchronization.None:
                    return TransitionSynchronizationType.None;
                case StateTransitionProperties.TransitionSynchronization.Proportional:
                    return TransitionSynchronizationType.Proportional;
                case StateTransitionProperties.TransitionSynchronization.Ratio:
                    return TransitionSynchronizationType.Ratio;
                case StateTransitionProperties.TransitionSynchronization.Tag:
                    return TransitionSynchronizationType.Tag;
                case StateTransitionProperties.TransitionSynchronization.EntryPoint:
                    return TransitionSynchronizationType.EntryPoint;
                case StateTransitionProperties.TransitionSynchronization.InverseProportional:
                    return TransitionSynchronizationType.InverseProportional;
            }

            return TransitionSynchronizationType.None;
        }

        void BuildConditionFragmentsForChildren(GroupConditionModel transitionPropertiesCondition, int parentIndex, ref int transitionFragmentIndex)
        {
            foreach (var condition in transitionPropertiesCondition.ListSubConditions)
            {
                int parentTransitionFragmentIndex = transitionFragmentIndex++;
                m_Graph.TopologyDefinition.ConditionFragments.Add(CreateTransitionFragmentCommandFromCondition(condition, parentIndex));
                if (condition is GroupConditionModel group)
                {
                    BuildConditionFragmentsForChildren(group, parentTransitionFragmentIndex, ref transitionFragmentIndex);
                }
            }
        }

        CreateConditionFragmentCommand CreateTransitionFragmentCommandFromCondition(BaseConditionModel condition, int parentIndex)
        {
            switch (condition)
            {
                case GroupConditionModel group:
                {
                    return new CreateConditionFragmentCommand()
                    {
                        Type = group.GroupOperation == GroupConditionModel.Operation.And ? TransitionFragmentType.GroupAnd : TransitionFragmentType.GroupOr,
                        ParentIndex = parentIndex
                    };
                }
                case ElapsedTimeConditionModel elapsed:
                {
                    return new CreateConditionFragmentCommand()
                    {
                        Type = TransitionFragmentType.ElapsedTime,
                        CompareVariant = elapsed.TimeElapsed,
                        ParentIndex = parentIndex
                    };
                }
                case EvaluationRatioConditionModel evalRatio:
                {
                    return new CreateConditionFragmentCommand()
                    {
                        Type = TransitionFragmentType.EvaluationRatio,
                        CompareVariant = evalRatio.Ratio,
                        ParentIndex = parentIndex
                    };
                }
                case EndOfDominantAnimationConditionModel endOfAnim:
                {
                    return new CreateConditionFragmentCommand()
                    {
                        Type = TransitionFragmentType.EndOfDominantAnimation,
                        CompareVariant = endOfAnim.TimeBeforeEnd,
                        ParentIndex = parentIndex
                    };
                }
                case BlackboardValueConditionModel value:
                {
                    var blackboardValueReference = value.BlackboardValueReference;
                    var componentDataType = blackboardValueReference.ComponentBindingId.Type.Resolve();
                    if (!Hybrid.AuthoringComponentService.TryGetComponentByRuntimeType(componentDataType, out var componentInfo))
                    {
                        m_RootIR.CompilationResult.AddError($"Invalid Authoring Component {componentDataType}");
                        return new CreateConditionFragmentCommand();
                    }

                    if (!componentInfo.RuntimeFields.TryGetValue(blackboardValueReference.FieldId.Resolve(), out var fieldReference))
                    {
                        m_RootIR.CompilationResult.AddError($"Invalid Field {blackboardValueReference.FieldId.Resolve()} in Authoring Component {componentDataType}");
                        return new CreateConditionFragmentCommand();
                    }

                    return new CreateConditionFragmentCommand()
                    {
                        Type = TransitionFragmentType.BlackboardValue,
                        BlackboardValueId = new BlackboardValueRuntimeId(){ ComponentDataTypeHash = TypeHash.CalculateStableTypeHash(componentDataType), Offset = fieldReference.Offset},
                        CompareOp = TranslateCompareOp(value.Comparison),
                        CompareVariant = value.CompareValue,
                        ParentIndex = parentIndex
                    };
                }
                case MarkupConditionModel markup:
                {
                    return new CreateConditionFragmentCommand()
                    {
                        Type = TransitionFragmentType.Markup,
                        MarkupHash = new Entities.Hash128(0) /*markup.MarkupReference.ToHash()*/,
                        IsSet = markup.IsSet,
                        ParentIndex = parentIndex
                    };
                }
                case StateTagConditionModel stateTag:
                {
                    return new CreateConditionFragmentCommand()
                    {
                        Type = TransitionFragmentType.StateTag,
                        StateTagHash = new Entities.Hash128(0) /*markup.MarkupReference.ToHash()*/,
                        ParentIndex = parentIndex
                    };
                }
            }

            return new CreateConditionFragmentCommand();
        }

        static CompareOp TranslateCompareOp(BlackboardValueConditionModel.ComparisonType gameplayComparison)
        {
            if (gameplayComparison == BlackboardValueConditionModel.ComparisonType.Equal)
                return CompareOp.Equal;
            if (gameplayComparison == BlackboardValueConditionModel.ComparisonType.NotEqual)
                return CompareOp.NotEqual;
            if (gameplayComparison == BlackboardValueConditionModel.ComparisonType.Greater)
                return CompareOp.Greater;
            if (gameplayComparison == BlackboardValueConditionModel.ComparisonType.Smaller)
                return CompareOp.Less;
            if (gameplayComparison == BlackboardValueConditionModel.ComparisonType.GreaterOrEqual)
                return CompareOp.GreaterOrEqual;
            return CompareOp.LessOrEqual;
        }

        static TransitionType TranslateTransitionType(StateMachineIRTransition.TransitionType transitionType)
        {
            if (transitionType == StateMachineIRTransition.TransitionType.Global)
                return TransitionType.Global;
            if (transitionType == StateMachineIRTransition.TransitionType.Self)
                return TransitionType.Self;
            if (transitionType == StateMachineIRTransition.TransitionType.OnEnterSelector)
                return TransitionType.OnEnterSelector;
            return TransitionType.StateToState;
        }

        public IR GetIRByName(string name)
        {
            if (m_RootIR.Name == name) //maybe useless
                return m_RootIR;
            return m_CachedIRs[name];
        }

        static bool LogCompilation = false;
        void Log(object message)
        {
            if (LogCompilation)
                UnityEngine.Debug.Log(message);
        }

        void LogWarning(object message)
        {
            if (LogCompilation)
                UnityEngine.Debug.LogWarning(message);
        }

        void LogError(object message)
        {
            if (LogCompilation)
                UnityEngine.Debug.LogError(message);
        }

        public void ResolveDataInputPorts(GraphPath path, IRPortTarget target, List<PortReference> targetList)
        {
            ResolvePorts(path, target, targetList, true, true);
        }

        public void ResolveDataOutputPorts(GraphPath path, IRPortTarget target, List<PortReference> targetList)
        {
            ResolvePorts(path, target, targetList, false, true);
        }

        public void ResolveMessageInputPorts(GraphPath path, IRPortTarget target, List<PortReference> targetList)
        {
            ResolvePorts(path, target, targetList, true, false);
        }

        public void ResolveMessageOutputPorts(GraphPath path, IRPortTarget target, List<PortReference> targetList)
        {
            ResolvePorts(path, target, targetList, false, false);
        }

        public void ResolvePorts(GraphPath path, IRPortTarget target, List<PortReference> targetList, bool isInput, bool isData)
        {
            var targetType = Helpers.GetDefinedType(target.Node);
            if (targetType == null)
            {
                LogError($"Invalid Target Type for {target.Node.Name}");
            }
            else
            {
                PortID portID;
                if (isInput && isData)
                {
                    portID = DFGTranslationHelpers.GetDataInputPortIDValue(m_DummyNodeSet, targetType, target.PortName, target.PortGroupInstance);
                }
                else if (isInput && !isData)
                {
                    portID = DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, targetType, target.PortName, target.PortGroupInstance);
                }
                else if (!isInput && isData)
                {
                    portID = DFGTranslationHelpers.GetDataOutputPortIDValue(m_DummyNodeSet, targetType, target.PortName);
                }
                else
                {
                    portID = DFGTranslationHelpers.GetMessageOutputPortIDValue(m_DummyNodeSet, targetType, target.PortName);
                }
                targetList.Add(new PortReference()
                {
                    ID = m_NodeCache.GetNodeID(path, target.Node.Name),
                    Port = portID
                });
            }
        }

        private NodeID AddNewNode(IRNodeDefinition node, GraphPath path, Type t)
        {
            NodeID id = m_NodeCache.RegisterNode(node, path);
            if (!m_TypesToTypeHash.TryGetValue(t, out var typeHash))
            {
                var fullName = t.AssemblyQualifiedName;
                typeHash = fullName.GetHashCode();
                m_Graph.TypesUsed.Add(fullName);
                m_TypesToTypeHash.Add(t, typeHash);
            }
            m_Graph.TopologyDefinition.NodeCreations.Add(new CreateNodeCommand()
            {
                NodeID = id,
                TypeHash = typeHash
            });
            return id;
        }

        void RegisterNodes(IR ir, GraphPath currentPath)
        {
            Log($"Registering nodes for current path : {currentPath}");
            foreach (var n in ir.Nodes)
            {
                Type type = Helpers.GetDefinedType(n);
                if (type != null)
                {
                    var id = AddNewNode(n, currentPath, type);
                }
                else //it is a compositor uber node, we do not register it and we flatten it instead
                {
                    var subIR = GetIRByName(n.GetTypeName());
                    currentPath.Push(n.Name);
                    {
                        RegisterNodes(subIR, currentPath);
                    }
                    currentPath.Pop();
                }
            }
        }

        private void ProcessConnectionsRecursive(IR ir, GraphPath currentPath, bool isRoot)
        {
            Log($"Processing connections for current path : {currentPath}");
            ProcessConnections(ir, currentPath, isRoot);
            foreach (var n in ir.Nodes)
            {
                Type type = Helpers.GetDefinedType(n);
                if (type == null)
                {
                    var subIR = GetIRByName(n.GetTypeName());
                    currentPath.Push(n.Name);
                    {
                        ProcessConnectionsRecursive(subIR, currentPath, false);
                    }
                    currentPath.Pop();
                }
            }
        }

        private void ProcessConnections(IR ir, GraphPath currentPath, bool isRoot)
        {
            SerializeAssetReferences(ir, currentPath);
            SerializeBoundObjectReferences(ir, currentPath);
            SerializeInputReferenceTargets(ir, currentPath);
            SerializeDefaultValues(ir, currentPath);
            SerializeExternalReferences(ir, currentPath);
            SerializeDataConnections(ir, currentPath);
            SerializeMessageConnections(ir, currentPath);
            SerializeSimulationToDataConnections(ir, currentPath);

            if (isRoot)
            {
                SerializeGraphInputs(ir, currentPath);
                SerializeGraphOutputs(ir, currentPath);
            }

            SerializePortInfos(ir, currentPath);
        }

        void SerializeAssetReferences(IR ir, GraphPath currentPath)
        {
            foreach (var a in ir.AssetReferences)
            {
                var assetReference = new GraphAssetReference()
                {
                    TypeHash = TypeHash.CalculateStableTypeHash(a.Value.DestinationType),
                    NodeID = m_NodeCache.GetNodeID(currentPath, a.Value.PassThroughForAssetReference.Name),
                    PortID = DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, a.Value.PassThroughForAssetReference.NodeType, "Input"),
                    Asset = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(a.Key)
                };

                m_Graph.Assets.Add(assetReference);
            }
        }

        void SerializeBoundObjectReferences(IR ir, GraphPath currentPath)
        {
            foreach (var a in ir.BoundObjectReferences)
            {
                var converter = DFGTranslationHelpers.CreateGraphVariantConverterNodeOfType(m_RootIR, a.PrimitiveType);
                var converterType = Helpers.GetDefinedType(converter);
                var converterID = AddNewNode(converter, currentPath, converterType);
                var variantConverterInPortID = DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, converterType, "In");
                var variantConverterOutPortID = DFGTranslationHelpers.GetMessageOutputPortIDValue(m_DummyNodeSet, converterType, "Out");

                m_Graph.TopologyDefinition.Connections.Add(new ConnectNodesCommand()
                {
                    SourceNodeID = converterID,
                    SourcePortID = variantConverterOutPortID,
                    DestinationNodeID =  m_NodeCache.GetNodeID(currentPath, a.ConverterNodeReference.Name),
                    DestinationPortID = DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, a.ConverterNodeReference.NodeType, "Input"),
                });
                //insert graph variant converter here as well

                var objectReference = new ExposedObjectReference()
                {
                    NodeID = converterID,
                    PortID = variantConverterInPortID,
                    TypeHash = TypeHash.CalculateStableTypeHash(a.DestinationType),
                };

                a.NodeID.ToParts(out objectReference.TargetGUID.NodeIDPart1, out objectReference.TargetGUID.NodeIDPart2);
                objectReference.TargetGUID.PortUniqueName = a.PortUniqueName;

                m_Graph.ExposedObjects.Add(objectReference);
            }
        }

        void SerializeInputReferenceTargets(IR ir, GraphPath currentPath)
        {
            foreach (var i in ir.InputReferencesTargets)
            {
                m_Graph.InputTargets.Add(
                    new InputTarget()
                    {
                        TypeHash = TypeHash.CalculateStableTypeHash(i.Key.Type.Resolve()),
                        NodeID = m_NodeCache.GetNodeID(currentPath, i.Value.Node.Name),
                        PortID = DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, i.Value.Node.NodeType, i.Value.PortName),
                    });
            }
        }

        void SerializeDefaultValues(IR ir, GraphPath currentPath)
        {
            foreach (var c in ir.PortDefaultValues)
            {
                if (!c.ObjectReference.Equals(IRPortDefaultValue.k_DefaultGlobalObjectId) && ir.AssetReferences.TryGetValue(c.ObjectReference, out IRAssetReference assetRef))
                {
                    Type sourceType = Helpers.GetDefinedType(assetRef.PassThroughForAssetReference);
                    if (sourceType == null || sourceType == typeof(Unknown))
                    {
                        //compilationResults.AddWarning($"Undefined destination type for port default values on node {assetRef.PassThroughForAssetReference.Name}");
                        LogError($"Undefined destination type for port default values on node {assetRef.PassThroughForAssetReference.Name}");
                    }

                    Type nodeDestinationType = Helpers.GetDefinedType(c.Destination.Node);
                    if (nodeDestinationType == null || nodeDestinationType == typeof(Unknown))
                    {
                        //compilationResults.AddWarning($"Undefined destination type for port default values on node {c.Destination.Node.Name}");
                        LogError($"Undefined destination type for port default values on node {c.Destination.Node.Name}");
                    }

                    m_Graph.TopologyDefinition.Connections.Add(new ConnectNodesCommand()
                    {
                        SourceNodeID = m_NodeCache.GetNodeID(currentPath, assetRef.PassThroughForAssetReference.Name),
                        SourcePortID = c.MessagePort ? DFGTranslationHelpers.GetMessageOutputPortIDValue(m_DummyNodeSet, sourceType, "Output") :
                            DFGTranslationHelpers.GetDataOutputPortIDValue(m_DummyNodeSet, sourceType, "Output"),
                        DestinationNodeID = m_NodeCache.GetNodeID(currentPath, c.Destination.Node.Name),
                        DestinationPortID = c.MessagePort ? DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, nodeDestinationType, c.Destination.PortName, c.Destination.PortGroupInstance) :
                            DFGTranslationHelpers.GetDataInputPortIDValue(m_DummyNodeSet, nodeDestinationType, c.Destination.PortName, c.Destination.PortGroupInstance),
                    });
                }
                else
                {
                    Type destinationType = Helpers.GetDefinedType(c.Destination.Node);
                    if (destinationType != null && destinationType != typeof(Unknown)) //Actually has an already defined NodeDefinition
                    {
                        if (c.Value == null || GraphVariant.ValueTypeFromType(c.Value.GetType()) == GraphVariant.ValueType.Unknown)
                            continue;
                        //throw new InvalidOperationException($"Type {c.Value.GetType()} not supported for default values");
                        var portIDValue = c.MessagePort ? DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, destinationType, c.Destination.PortName, c.Destination.PortGroupInstance) :
                            DFGTranslationHelpers.GetDataInputPortIDValue(m_DummyNodeSet, destinationType, c.Destination.PortName, c.Destination.PortGroupInstance);
                        var nodeID = m_NodeCache.GetNodeID(currentPath, c.Destination.Node.Name);
                        GraphVariant variant = GraphVariant.FromObject(c.Value);
                        var variantType = variant.Type;
                        variant.Type = GraphVariant.ValueType.Int4;

                        var converter = DFGTranslationHelpers.CreateGraphVariantConverterNodeOfType(m_RootIR, GraphVariant.TypeFromValueType(variantType));
                        var converterType = Helpers.GetDefinedType(converter);
                        var converterID = AddNewNode(converter, currentPath, converterType);
                        var converterInPortID = DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, converterType, "In");
                        var converterOutPortID = DFGTranslationHelpers.GetMessageOutputPortIDValue(m_DummyNodeSet, converterType, "Out");
                        m_Graph.TopologyDefinition.Connections.Add(new ConnectNodesCommand()
                        {
                            SourceNodeID = converterID,
                            SourcePortID = converterOutPortID,
                            DestinationNodeID = nodeID,
                            DestinationPortID = portIDValue
                        });
                        m_Graph.TopologyDefinition.Values.Add(new SetValueCommand()
                        {
                            Type = variantType,
                            Value = variant.Int4,
                            Port = converterInPortID,
                            Node = converterID
                        });
                    }
                    else
                    {
                        LogError($"Undefined destination type for port default values on node {c.Destination.Node.Name}");
                    }
                }
            }
        }

        void SerializeExternalReferences(IR ir, GraphPath currentPath)
        {
            foreach (var externalReference in ir.ExternalAssetReferenceMappings)
            {
                if (ir.AssetReferences.TryGetValue(externalReference.Id, out IRAssetReference assetRef))
                {
                    Log($"Serializing connection from {assetRef.PassThroughForAssetReference.Name}.Output to {externalReference.SubGraphAssetReferencePort.Node.Name}.{ externalReference.SubGraphAssetReferencePort.PortName}");
                    Type sourceType = Helpers.GetDefinedType(assetRef.PassThroughForAssetReference);
                    if (sourceType == null || sourceType == typeof(Unknown))
                    {
                        //compilationResults.AddWarning($"Undefined source type for external asset reference on node {assetRef.PassThroughForAssetReference.Name}");
                        Log($"Undefined source type for external asset reference on node {assetRef.PassThroughForAssetReference.Name}");
                        continue;
                    }

                    Type destinationType = Helpers.GetDefinedType(externalReference.SubGraphAssetReferencePort.Node);
                    if (destinationType == null || destinationType == typeof(Unknown))
                    {
                        //compilationResults.AddWarning($"Undefined destination type for external asset reference on node {externalReference.SubGraphAssetReferencePort.Node.Name}");
                        Log($"Undefined destination type for external asset reference on node {externalReference.SubGraphAssetReferencePort.Node.Name}");
                        continue;
                    }

                    m_Graph.TopologyDefinition.Connections.Add(new ConnectNodesCommand()
                    {
                        SourceNodeID = m_NodeCache.GetNodeID(currentPath, assetRef.PassThroughForAssetReference.Name),
                        SourcePortID = DFGTranslationHelpers.GetMessageOutputPortIDValue(m_DummyNodeSet, sourceType, "Output"),
                        DestinationNodeID = m_NodeCache.GetNodeID(currentPath, externalReference.SubGraphAssetReferencePort.Node.Name),
                        DestinationPortID = DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, destinationType, externalReference.SubGraphAssetReferencePort.PortName, externalReference.SubGraphAssetReferencePort.PortGroupInstance),
                    });
                }
            }
        }

        void SerializeDataConnections(IR ir, GraphPath currentPath)
        {
            foreach (var c in ir.DataConnections)
            {
                Log($"Serializing data connection from {c.Source.Node.Name}.{c.Source.PortName} to {c.Destination.Node.Name}.{c.Destination.PortName}");

                var foundSources = new List<PortReference>();
                ResolveDataOutputPorts(currentPath, c.Source, foundSources);
                if (foundSources.Count > 1)
                {
                    LogError($"{m_RootIR.Name} : {c.Destination.Node.Name}.{c.Destination.PortName} has more than one incoming connections {foundSources.Count}");
                }
                else if (foundSources.Count == 0)
                {
                    Log($"no incoming connection found for {c.Destination.Node.Name}.{c.Destination.PortName} port ");
                    continue;
                }

                var foundDestinations = new List<PortReference>();
                ResolveDataInputPorts(currentPath, c.Destination, foundDestinations);

                var sourcePortRef = foundSources[0];
                foreach (var destinationPortRef in foundDestinations)
                {
                    m_Graph.TopologyDefinition.Connections.Add(new ConnectNodesCommand()
                    {
                        SourceNodeID = sourcePortRef.ID,
                        SourcePortID = sourcePortRef.Port,
                        DestinationNodeID = destinationPortRef.ID,
                        DestinationPortID = destinationPortRef.Port,
                    });
                }
            }
        }

        void SerializeMessageConnections(IR ir, GraphPath currentPath)
        {
            foreach (var c in ir.SimulationConnections)
            {
                Log($"Serializing message connection from {c.Source.Node.Name}.{c.Source.PortName} to {c.Destination.Node.Name}.{c.Destination.PortName}");

                var foundSources = new List<PortReference>();
                ResolveMessageOutputPorts(currentPath, c.Source, foundSources);
                if (foundSources.Count > 1)
                {
                    LogError($"{m_RootIR.Name} : {c.Destination.Node.Name}.{c.Destination.PortName} has more than one incoming connections {foundSources.Count}");
                }
                else if (foundSources.Count == 0)
                {
                    Log($"no incoming connection found for {c.Destination.Node.Name}.{c.Destination.PortName} port ");
                    continue;
                }

                var foundDestinations = new List<PortReference>();
                ResolveMessageInputPorts(currentPath, c.Destination, foundDestinations);

                var sourcePortRef = foundSources[0];
                foreach (var destinationPortRef in foundDestinations)
                {
                    m_Graph.TopologyDefinition.Connections.Add(new ConnectNodesCommand()
                    {
                        SourceNodeID = sourcePortRef.ID,
                        SourcePortID = sourcePortRef.Port,
                        DestinationNodeID = destinationPortRef.ID,
                        DestinationPortID = destinationPortRef.Port,
                    });
                }
            }
        }

        void SerializeSimulationToDataConnections(IR ir, GraphPath currentPath)
        {
            foreach (var c in ir.SimulationToDataConnections)
            {
                Log($"Serializing message to data connection from {c.Source.Node.Name}.{c.Source.PortName} to {c.Destination.Node.Name}.{c.Destination.PortName}");

                var foundSources = new List<PortReference>();
                ResolveMessageOutputPorts(currentPath, c.Source, foundSources);
                if (foundSources.Count > 1)
                {
                    LogError($"{m_RootIR.Name} : {c.Destination.Node.Name}.{c.Destination.PortName} has more than one incoming connections {foundSources.Count}");
                }
                else if (foundSources.Count == 0)
                {
                    Log($"no incoming connection found for {c.Destination.Node.Name}.{c.Destination.PortName} port ");
                    continue;
                }

                var foundDestinations = new List<PortReference>();
                ResolveDataInputPorts(currentPath, c.Destination, foundDestinations);

                var sourcePortRef = foundSources[0];
                foreach (var destinationPortRef in foundDestinations)
                {
                    m_Graph.TopologyDefinition.Connections.Add(new ConnectNodesCommand()
                    {
                        SourceNodeID = sourcePortRef.ID,
                        SourcePortID = sourcePortRef.Port,
                        DestinationNodeID = destinationPortRef.ID,
                        DestinationPortID = destinationPortRef.Port,
                    });
                }
            }
        }

        void SerializeGraphInputs(IR ir, GraphPath currentPath)
        {
            HashSet<string> alreadyTreatedInputs = new HashSet<string>();

            foreach (var input in ir.Inputs)
            {
                string propertyName = input.Source.Node.Name;

                if (alreadyTreatedInputs.Contains(propertyName))
                    continue;

                Type dataType = Helpers.GetDefinedType(input.Source.Node);

                Log($"Serializing message forward input statement from {propertyName} of type {dataType.Name}");

                var foundTargets = new List<PortReference>();
                var forwardInputs = ir.Inputs.Where(x => x.Source.Node.Name == propertyName);
                foreach (var forwardInput in forwardInputs)
                {
                    ResolveMessageInputPorts(currentPath, forwardInput.Destination, foundTargets);
                }
                alreadyTreatedInputs.Add(propertyName);

                Log($"Input {propertyName} was expanded to {foundTargets.Count} targets");

                InputOutputType handlerType = InputOutputType.Unknown;

                if (propertyName == "ContextInput")
                {
                    handlerType = InputOutputType.ContextHandler;
                }
                else if (propertyName == "EntityManagerInput")
                {
                    handlerType = InputOutputType.EntityManager;
                }
                else if (propertyName == "TimeControl")
                {
                    handlerType = InputOutputType.TimeControlHandler;
                }
                else if (propertyName == "InputReferences")
                {
                    handlerType = InputOutputType.InputReferenceHandler;
                }

                if (handlerType != InputOutputType.Unknown)
                {
                    //get target nodedefinition
                    if (foundTargets.Count > 1)
                    {
                        Debug.Log("There should not be more than one target for outputs");
                    }

                    var portRef = foundTargets.First();

                    m_Graph.TopologyDefinition.Inputs.Add(new GraphInput()
                    {
                        TargetNodeID = portRef.ID,
                        TargetPortID = portRef.Port,
                        TypeHash = 0,
                        Type = handlerType
                    });

                    continue;
                }
            }
        }

        void SerializeGraphOutputs(IR ir, GraphPath currentPath)
        {
            var alreadyForwardedTypes = new HashSet<Type>();
            foreach (var output in ir.Outputs)
            {
                Type dataType = Helpers.GetDefinedType(output.Source.Node);
                var foundOutputs = new List<PortReference>();
                ResolveDataOutputPorts(currentPath, output.Destination, foundOutputs);

                if (foundOutputs.Count == 0 || !Helpers.IsComponentNodeCompatible(dataType))
                    continue;

                if (foundOutputs.Count > 1 || alreadyForwardedTypes.Contains(dataType))
                {
                    LogWarning($"Graph {m_RootIR.Name} : Cannot have multiple connections to the same output, only one will be selected");
                }

                Type componentNodeDataType = Helpers.IsBufferElementDataType(dataType) ? Helpers.GetUnderlyingBufferElementType(dataType) : dataType;
                m_Graph.TopologyDefinition.Outputs.Add(new GraphOutput
                {
                    TargetNodeID = foundOutputs.First().ID,
                    TargetPortID = foundOutputs.First().Port,
                    TypeHash = TypeHash.CalculateStableTypeHash(componentNodeDataType),
                    Type = InputOutputType.ComponentData
                });

                alreadyForwardedTypes.Add(dataType);
            }
        }

        void SerializePortInfos(IR ir, GraphPath currentPath)
        {
            foreach (var p in ir.PortGroupInfos)
            {
                var typeName = p.Value.Node.GetUnresolvedTypeName();
                if (string.IsNullOrEmpty(typeName))
                    continue;
                Type destinationType = Type.GetType(typeName);
                if (destinationType == null)
                    continue;
                var nodeId = m_NodeCache.GetNodeID(currentPath, p.Value.Node.Name);
                foreach (var g in p.Value.PortGroupInfos)
                {
                    var variant = (GraphVariant)g.Value.PortGroupSize;
                    variant.Type = GraphVariant.ValueType.Int4;

                    if (!String.IsNullOrEmpty(g.Value.GroupSizeTarget))
                    {
                        var converter = DFGTranslationHelpers.CreateGraphVariantConverterNodeOfType(m_RootIR, GraphVariant.TypeFromValueType(GraphVariant.ValueType.UShort));
                        var converterType = Helpers.GetDefinedType(converter);
                        var converterID = AddNewNode(converter, currentPath, converterType);
                        var converterInPortID = DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, converterType, "In");
                        var converterOutPortID = DFGTranslationHelpers.GetMessageOutputPortIDValue(m_DummyNodeSet, converterType, "Out");

                        m_Graph.TopologyDefinition.Connections.Add(new ConnectNodesCommand()
                        {
                            SourceNodeID = converterID,
                            SourcePortID = converterOutPortID,
                            DestinationNodeID = nodeId,
                            DestinationPortID = DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, destinationType, g.Value.GroupSizeTarget)
                        });

                        m_Graph.TopologyDefinition.Values.Add(new SetValueCommand()
                        {
                            Type = GraphVariant.ValueType.UShort,
                            Value = variant.Int4,
                            Port = converterInPortID,
                            Node = converterID
                        });
                    }

                    foreach (var m in g.Value.MessagePortNameInGroup)
                    {
                        Log($"Message Port Creation Translation : Node : {p.Value.Node.Name} with ID {nodeId}   Port : {m}    Size : {g.Value.PortGroupSize}");
                        m_Graph.TopologyDefinition.PortArrays.Add(
                            new ResizeArrayCommand()
                            {
                                Node = nodeId,
                                Port = DFGTranslationHelpers.GetMessageInputPortIDValue(m_DummyNodeSet, destinationType, m),
                                ArraySize = g.Value.PortGroupSize
                            });
                    }
                    foreach (var d in g.Value.DataPortNameInGroup)
                    {
                        Log($"Data Port Creation Translation : Node : {p.Value.Node.Name} with ID {nodeId}   Port : {d}    Size : {g.Value.PortGroupSize}");
                        m_Graph.TopologyDefinition.PortArrays.Add(
                            new ResizeArrayCommand()
                            {
                                Node = nodeId,
                                Port = DFGTranslationHelpers.GetDataInputPortIDValue(m_DummyNodeSet, destinationType, d),
                                ArraySize = g.Value.PortGroupSize
                            });
                    }
                }
            }
        }
    }
}
