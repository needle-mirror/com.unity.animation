using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class IRBuilder
    {
        static void DebugLog(string log)
        {
            //UnityEngine.Debug.Log(log);
        }

        internal static void BuildPortGroupSizeValue(BaseNodeModel model, BasePortModel port, IRNodeDefinition node, IR ir)
        {
            if (!ir.PortGroupInfos.TryGetValue(node, out IRPortGroupInfos portGroupInfos))
            {
                portGroupInfos = new IRPortGroupInfos(node);
                ir.PortGroupInfos.Add(node, portGroupInfos);
            }

            var pgi = new IRPortGroupInfo(port.PortGroupIndex, ((PortGroup)port.EmbeddedValue.ObjectValue).Size);
            var portGroupDefs = model.PortGroupDefinitions;
            if (portGroupDefs.Definitions.TryGetValue(port.PortGroupIndex, out PortGroupDefinition portGroupDefinition))
            {
                foreach (var messageInput in portGroupDefinition.MessageInputs)
                {
                    pgi.MessagePortNameInGroup.Add(messageInput.FieldName);
                }
                foreach (var messageOutput in portGroupDefinition.MessageOutputs)
                {
                    pgi.MessagePortNameInGroup.Add(messageOutput.FieldName);
                }
                foreach (var dataInput in portGroupDefinition.DataInputs)
                {
                    pgi.DataPortNameInGroup.Add(dataInput.FieldName);
                }
                foreach (var dataOutput in portGroupDefinition.DataOutputs)
                {
                    pgi.DataPortNameInGroup.Add(dataOutput.FieldName);
                }
            }

            pgi.GroupSizeTarget = portGroupDefinition.SimulationPortToDrive;
            portGroupInfos.PortGroupInfos.Add(port.PortGroupIndex, pgi);
        }

        internal static void BuildEnumDefaultValue(EnumValueReference enumValue, BasePortModel port, IRPortTarget destinationTarget, IR ir)
        {
            //For enum values, we have to inject a intermediate node to convert from the untyped int to the actual enum value
            var isMessageConnection =
                (port.EvaluationType == BasePortModel.PortEvaluationType.Simulation);
            IRNodeDefinition nodeConverter;
            if (isMessageConnection)
            {
                var enumNodeType = typeof(EnumConverter<>).MakeGenericType(enumValue.EnumType.Resolve());
                nodeConverter = ir.CreateNode("EnumConverter", enumNodeType);
                ir.ConnectSimulation(new IRPortTarget(nodeConverter, "EnumValue"), destinationTarget);
            }
            else
            {
                var enumNodeType = typeof(DataPhaseEnumConverter<>).MakeGenericType(enumValue.EnumType.Resolve());
                nodeConverter = ir.CreateNode("EnumConverter", enumNodeType);
                ir.ConnectRendering(new IRPortTarget(nodeConverter, "EnumValue"), destinationTarget);
            }

            ir.AddDefaultValue(
                new IRPortTarget(nodeConverter, "IntValue"),
                enumValue.Value,
                isMessageConnection);
        }

        internal static void BuildPortDefaultValues(BaseNodeModel n, IRNodeDefinition node, IR ir, IBuildContext context)
        {
            var nodeWithPorts = n as IInOutPortsNode;
            if (nodeWithPorts == null)
                return;
            foreach (var pm in nodeWithPorts.InputsByDisplayOrder)
            {
                if (!(pm is BasePortModel port) || pm.EmbeddedValue == null)
                    continue;

                if (port.IsPortGroupSize)
                {
                    BuildPortGroupSizeValue(n, port, node, ir);
                }
                else
                {
                    var destinationTarget =
                        n.Builder.GetDestinationPortTarget(port, ir, context);
                    if (destinationTarget == null)
                        continue;
                    if (pm.EmbeddedValue is IValueIRBuilder valueBuilder)
                    {
                        valueBuilder.Build(port, destinationTarget, ir, context);
                    }
                    else
                    {
                        var value = pm.EmbeddedValue.ObjectValue;

                        if (value is EnumValueReference enumValue)
                        {
                            BuildEnumDefaultValue(enumValue, port, destinationTarget, ir);
                        }
                        else
                        {
                            ir.AddDefaultValue(
                                destinationTarget,
                                value,
                                port.EvaluationType == BasePortModel.PortEvaluationType.Simulation);
                        }
                    }
                }
            }
        }

        public static void BuildNodePorts(IR ir, BaseNodeModel nodeModel, IRNodeDefinition node)
        {
            foreach (var p in nodeModel.GetInputMessagePorts().OfType<BasePortModel>())
                node.PortDeclarations.AddPort(IRNodePortDeclarations.PortContainerType.InputMessage, p.Title, p.DataTypeHandle.Resolve(), new PortDefinitionAttributes() { Tooltip = p.ToolTip, DefaultValue = p.EmbeddedValue?.ObjectValue});
            foreach (var p in nodeModel.GetOutputMessagePorts().OfType<BasePortModel>())
                node.PortDeclarations.AddPort(IRNodePortDeclarations.PortContainerType.OutputMessage, p.Title, p.DataTypeHandle.Resolve(), new PortDefinitionAttributes() { Tooltip = p.ToolTip, DefaultValue = p.EmbeddedValue?.ObjectValue });
            foreach (var p in nodeModel.GetInputDataPorts().OfType<BasePortModel>())
                node.PortDeclarations.AddPort(IRNodePortDeclarations.PortContainerType.InputData, p.Title, p.DataTypeHandle.Resolve(), new PortDefinitionAttributes() { Tooltip = p.ToolTip, DefaultValue = p.EmbeddedValue?.ObjectValue });
            foreach (var p in nodeModel.GetOutputDataPorts().OfType<BasePortModel>())
                node.PortDeclarations.AddPort(IRNodePortDeclarations.PortContainerType.OutputData, p.Title, p.DataTypeHandle.Resolve(), new PortDefinitionAttributes() { Tooltip = p.ToolTip, DefaultValue = p.EmbeddedValue?.ObjectValue });
        }

        internal static void BuildSubGraphDependenciesList(GraphModel graphModel, ref HashSet<SubGraphNodeModel> dependencies)
        {
            foreach (var sub in graphModel.NodeModels.OfType<SubGraphNodeModel>())
            {
                if (sub.GraphAsset == null)
                    continue;

                if (!dependencies.Contains(sub))
                {
                    dependencies.Add(sub);
                    var nestedGraph = (GraphModel)sub.GraphAsset.GraphModel;
                    if (nestedGraph != null)
                    {
                        BuildSubGraphDependenciesList(nestedGraph, ref dependencies);
                    }
                }
            }
        }

        internal static void VerifyNoCircularDependencies(GraphModel graphModel, IR ir)
        {
            var dependencies = new HashSet<SubGraphNodeModel>();

            UnityEngine.Profiling.Profiler.BeginSample(nameof(BuildSubGraphDependenciesList));
            {
                BuildSubGraphDependenciesList(graphModel, ref dependencies);
            }
            UnityEngine.Profiling.Profiler.EndSample();

            foreach (var subdependency in dependencies)
            {
                if ((BaseGraphModel)subdependency.GraphAsset.GraphModel == graphModel)
                {
                    ir.CompilationResult.AddError($"{graphModel.Name} is referencing itself through {subdependency.AssetModel.Name}", subdependency);
                    return;
                }
            }
        }

        internal static void PreBuildNodes(GraphModel graphModel, IR ir, IBuildContext context)
        {
            foreach (var n in graphModel.NodeModels)
            {
                if (n is BaseNodeModel nodeModel)
                {
                    nodeModel.Builder.PreBuild(ir, context);
                }
            }
        }

        internal static void BuildNodes(GraphModel graphModel, IR ir, IBuildContext context)
        {
            foreach (var n in graphModel.NodeModels)
            {
                if (n is BaseNodeModel nodeModel)
                {
                    nodeModel.Builder.Build(ir, context);
                }
            }
        }

        internal static void BuildInputReferenceIR(string portName, int portGroup, BaseNodeModel baseNodeModel, InputComponentFieldVariableModel fieldModel, bool isMessageConnection, IR ir)
        {
            var propDeclVarModel = fieldModel.DeclarationModel as InputComponentFieldVariableDeclarationModel;

            if (!Hybrid.AuthoringComponentService.TryGetComponentByRuntimeType(propDeclVarModel.Identifier.Type.Resolve(), out var componentInfo))
            {
                ir.CompilationResult.AddError($"Invalid Authoring Component {propDeclVarModel.Identifier.Type.Resolve()}", fieldModel);
                return;
            }

            var fieldReaderNode =
                ir.CreateNode("ComponentDataFieldReaderNode",
                    typeof(ComponentDataFieldReaderNode<,>)
                        .MakeGenericType(componentInfo.RuntimeType, fieldModel.GetDataType().Resolve()));

            if (isMessageConnection)
                ir.ConnectSimulation(
                    new IRPortTarget(fieldReaderNode, "Value"),
                    new IRPortTarget(
                        ir.ModelToNode[baseNodeModel.Guid],
                        portName,
                        portGroup));
            else
                ir.ConnectHybrid(
                    new IRPortTarget(fieldReaderNode, "Value"),
                    new IRPortTarget(
                        ir.ModelToNode[baseNodeModel.Guid],
                        portName,
                        portGroup));

            if (!componentInfo.RuntimeFields.TryGetValue(propDeclVarModel.FieldHandle.Resolve(), out var fieldReference))
            {
                ir.CompilationResult.AddError(
                    $"Invalid Field {propDeclVarModel.VariableName} in Authoring Component {propDeclVarModel.Identifier.Type.Identification}", fieldModel);
                return;
            }

            ir.AddDefaultValue(
                new IRPortTarget(fieldReaderNode, "FieldOffset"), fieldReference.Offset, true);

            IRPortTarget inputTarget;
            if (!ir.InputReferencesTargets.TryGetValue(propDeclVarModel.Identifier, out inputTarget))
            {
                var inputContextNode = ir.CreateNode($"{fieldModel.DisplayTitle}ContextNode",
                    typeof(SimulationPhasePassThroughNode<>).MakeGenericType(typeof(EntityContext)));
                inputTarget = new IRPortTarget(inputContextNode, "Input");
                ir.InputReferencesTargets[propDeclVarModel.Identifier] = inputTarget;
            }

            ir.ConnectSimulation(
                new IRPortTarget(inputTarget.Node, "Output"),
                new IRPortTarget(fieldReaderNode, "EntityContext"));
        }

        internal static void BuildConnections(GraphModel graphModel, IR ir, IBuildContext context, Type OutputType)
        {
            var messageForwardConnections = new Dictionary<GUID, Tuple<IDeclarationModel, List<IRConnection>>>();
            var dataForwardConnections = new Dictionary<GUID, Tuple<IDeclarationModel, List<IRConnection>>>();

            foreach (var e in graphModel.EdgeModels)
            {
                var inNodeModel = e.ToPort.NodeModel;
                var outNodeModel = e.FromPort.NodeModel;

                string outputPortModelName = string.Empty;
                string inputPortModelName = string.Empty;
                if (e.FromPort is IHasTitle fromPortTitle)
                {
                    outputPortModelName = fromPortTitle.Title;
                }
                if (e.ToPort is IHasTitle toPortTitle)
                {
                    inputPortModelName = toPortTitle.Title;
                }

                int outputPortGroupInstance = -1;
                int inputPortGroupInstance = -1;
                if (e.FromPort is BasePortModel outputBasePortModel)
                {
                    outputPortGroupInstance = outputBasePortModel.PortGroupInstance;
                    outputPortModelName = outputBasePortModel.OriginalScriptName;
                }

                if (e.ToPort is BasePortModel inputBasePortModel)
                {
                    inputPortGroupInstance = inputBasePortModel.PortGroupInstance;
                    inputPortModelName = inputBasePortModel.OriginalScriptName;
                }

                if (inNodeModel is BaseNodeModel inBaseNodeModel)
                {
                    var destinationTarget =
                        inBaseNodeModel.Builder.GetDestinationPortTarget(
                            e.ToPort as BasePortModel, ir, context);
                    if (destinationTarget == null)
                        continue;

                    if (outNodeModel is BaseNodeModel outBaseNodeModel)
                    {
                        var sourceTarget =
                            outBaseNodeModel.Builder.GetSourcePortTarget(
                                e.FromPort as BasePortModel, ir, context);

                        // Graph Output
                        if (inBaseNodeModel.GetType() == OutputType)
                        {
                            ir.ConnectRendering(sourceTarget, destinationTarget);
                        }
                        else
                        {
                            // Generic Connections
                            if (e.FromPort is MessagePortModel)
                            {
                                ir.ConnectSimulation(sourceTarget, destinationTarget);
                            }
                            else
                            {
                                ir.ConnectRendering(sourceTarget, destinationTarget);
                            }
                        }
                    }
                    else if (outNodeModel is InputComponentFieldVariableModel fieldVarModel)
                    {
                        BuildInputReferenceIR(
                            inputPortModelName, inputPortGroupInstance,
                            inBaseNodeModel, fieldVarModel, e.ToPort is MessagePortModel, ir);
                    }
                }
            }
        }

        internal static IR BuildBlendTreeIR(BaseGraphModel graphModel)
        {
            IAuthoringContext authoringContext = ((BaseStencil)graphModel.Stencil).Context;
            var ir = new IR(graphModel.AssetModel.FriendlyScriptName, false);

            IBuildContext buildContext;
            if (graphModel.IsStandAloneGraph)
            {
                DebugLog($"Build {graphModel.Name} as standalone");
                buildContext = new StandAloneGraphBuildContext();
            }
            else
            {
                DebugLog($"Build {graphModel.Name} as SM graph");
                buildContext = new StateMachineGraphBuildContext();
            }

            VerifyNoCircularDependencies(graphModel, ir);
            if (ir.HasBuildFailed)
            {
                return ir;
            }

            buildContext.PreBuild(ir, authoringContext);
            if (ir.HasBuildFailed)
            {
                return ir;
            }

            PreBuildNodes(graphModel, ir, buildContext);
            if (ir.HasBuildFailed)
            {
                return ir;
            }

            BuildNodes(graphModel, ir, buildContext);
            if (ir.HasBuildFailed)
            {
                return ir;
            }

            BuildConnections(graphModel, ir, buildContext, authoringContext.OutputNodeType);
            if (ir.HasBuildFailed)
            {
                return ir;
            }

            buildContext.PostBuild(ir, authoringContext);
            if (ir.HasBuildFailed)
            {
                return ir;
            }

            return ir;
        }

        internal static IR BuildStateMachineIR(StateMachineModel stateMachineModel)
        {
            var ir = new IR(stateMachineModel.AssetModel.FriendlyScriptName, true);
            Dictionary<BaseStateModel, int> mappingStateToIndex = new Dictionary<BaseStateModel, int>();
            foreach (var state in stateMachineModel.NodeModels.OfType<BaseStateModel>())
            {
                if (state.StateDefinitionAsset == null)
                {
                    ir.CompilationResult.AddError($"State {state.Title} doesn't have a definition");
                    return ir;
                }
                int newStateIndex = ir.States.Count;
                //Add this state to dependencies?
                var hashState = new Entities.Hash128(state.StateDefinitionAsset.AssetId);
                ir.AddDependency(new IR.NodeDependency()
                {
                    Hash = hashState,
                    Model = (GraphModel)state.StateDefinitionAsset.GraphModel,
                });
                ir.States.Add(new StateMachineIRStateDefinition(state.Title, hashState, newStateIndex));
                mappingStateToIndex.Add(state, newStateIndex);
            }

            foreach (var transition in stateMachineModel.EdgeModels.OfType<BaseTransitionModel>())
            {
                if (!transition.TransitionProperties.Enable)
                    continue;
                int sourceIndex = -1;
                int targetIndex = -1;
                var sourceState = transition.FromPort.NodeModel as BaseStateModel;
                var targetState = transition.ToPort.NodeModel as BaseStateModel;
                if (targetState == null || !mappingStateToIndex.TryGetValue(targetState, out targetIndex))
                {
                    ir.CompilationResult.AddError("Cannot find target state for transition");
                    return ir;
                }

                switch (transition)
                {
                    case StateToStateTransitionModel stateToStateModel:
                    {
                        if (sourceState == null || !mappingStateToIndex.TryGetValue(sourceState, out sourceIndex))
                        {
                            ir.CompilationResult.AddError("Cannot find source state for transition");
                            return ir;
                        }
                        ir.Transitions.Add(new StateMachineIRTransition(StateMachineIRTransition.TransitionType.StateToState, sourceIndex, targetIndex, null, (StateTransitionProperties)transition.TransitionProperties));
                        break;
                    }
                    case OnEnterStateSelectorModel enterStateModel:
                    {
                        ir.Transitions.Add(new StateMachineIRTransition(StateMachineIRTransition.TransitionType.OnEnterSelector, -1, targetIndex, (BaseTransitionSelectorProperties)enterStateModel.TransitionProperties, null));
                        break;
                    }
                    case GlobalTransitionModel globalModel:
                    {
                        ir.Transitions.Add(new StateMachineIRTransition(StateMachineIRTransition.TransitionType.Global, -1, targetIndex, null, (StateTransitionProperties)globalModel.TransitionProperties));
                        break;
                    }
                    case SelfTransitionModel selfModel:
                    {
                        ir.Transitions.Add(new StateMachineIRTransition(StateMachineIRTransition.TransitionType.Self, targetIndex, targetIndex, null, (StateTransitionProperties)selfModel.TransitionProperties));
                        break;
                    }
                }
            }
            return ir;
        }
    }
}
