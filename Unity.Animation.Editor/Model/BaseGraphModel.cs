using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using EdgeModel = UnityEditor.GraphToolsFoundation.Overdrive.BasicModel.EdgeModel;
using Unity.Animation.Editor;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class BaseGraphModel : BaseModel
    {
        [SerializeField, HideInInspector]
        internal bool IsStandAloneGraph = true;

        internal IEnumerable<EdgeModel> GetEdgesInputConnections(GUID inputNodeGuid, string inputPortId)
        {
            return EdgeModels.OfType<EdgeModel>().Where(e => e.ToNodeGuid == inputNodeGuid && e.ToPortId == inputPortId);
        }

        internal IEnumerable<EdgeModel> GetEdgesOutputConnections(GUID outputNodeGuid, string outputPortId)
        {
            return EdgeModels.OfType<EdgeModel>().Where(e => e.FromNodeGuid == outputNodeGuid && e.FromPortId == outputPortId);
        }

        internal void PatchEdgeWithNewOutput(GUID outputNodeGuid, string outputPortId, IPortModel newOutputPortModel)
        {
            foreach (var e in GetEdgesOutputConnections(outputNodeGuid, outputPortId))
                e.FromPort = newOutputPortModel;
        }

        internal void PatchEdgeWithNewInput(GUID inputNodeGuid, string inputPortId, IPortModel newInputPortModel)
        {
            foreach (var e in GetEdgesInputConnections(inputNodeGuid, inputPortId))
                e.ToPort = newInputPortModel;
        }

        public override IVariableNodeModel CreateVariableNode(IVariableDeclarationModel declarationModel,
            Vector2 position, GUID? guid = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return (Stencil as BaseGraphStencil).CreateVariableModelForDeclaration(this, declarationModel, position, spawnFlags, guid);
        }

        protected override bool IsCompatiblePort(IPortModel startPortModel, IPortModel compatiblePortModel)
        {
            if (!base.IsCompatiblePort(startPortModel, compatiblePortModel))
                return false;
            if (startPortModel is BasePortModel && compatiblePortModel is BasePortModel)
            {
                if (!IsValidEdge(startPortModel, compatiblePortModel))
                    return false;
            }

            return true;
        }

        public bool IsValidEdge(IPortModel startPortModel, IPortModel evaluatedPort)
        {
            BasePortModel inputPort = null;
            BasePortModel outputPort = null;

            if (startPortModel.Direction == Direction.Input)
            {
                inputPort = startPortModel as BasePortModel;
                outputPort = evaluatedPort as BasePortModel;
            }
            else
            {
                outputPort = startPortModel as BasePortModel;
                inputPort = evaluatedPort as BasePortModel;
            }

            if (inputPort == null || outputPort == null)
                throw new ArgumentException("Invalid IPortModel");

            if (!inputPort.DataTypeHandle.IsValid)
                return false;

            return
                EdgeValidation_ArePortsEqualEvaluationType(ref inputPort, ref outputPort) &&
                !EdgeValidation_IsPropertyToProperty(ref inputPort, ref outputPort) &&
                !EdgeValidation_IsPropertyWriterDataConnection(ref inputPort, ref outputPort) &&
                inputPort.DataTypeHandle == outputPort.DataTypeHandle &&
                !inputPort.IsStatic &&
                !outputPort.IsStatic;
        }

        private bool EdgeValidation_ArePortsEqualEvaluationType(ref BasePortModel inputPort, ref BasePortModel outputPort)
        {
            return outputPort.EvaluationType == inputPort.EvaluationType ||
                outputPort.EvaluationType == BasePortModel.PortEvaluationType.Properties ||
                inputPort.EvaluationType == BasePortModel.PortEvaluationType.Properties;
        }

        private bool EdgeValidation_IsPropertyToProperty(ref BasePortModel inputPort, ref BasePortModel outputPort)
        {
            return outputPort.EvaluationType == BasePortModel.PortEvaluationType.Properties &&
                inputPort.EvaluationType == BasePortModel.PortEvaluationType.Properties;
        }

        // TODO: remove this validation check once property writers for data connections are supported
        private bool EdgeValidation_IsPropertyWriterDataConnection(ref BasePortModel inputPort, ref BasePortModel outputPort)
        {
            return inputPort.EvaluationType == BasePortModel.PortEvaluationType.Properties &&
                outputPort.EvaluationType == BasePortModel.PortEvaluationType.Rendering;
        }

        public override IEdgeModel DuplicateEdge(IEdgeModel sourceEdge, INodeModel targetInputNode, INodeModel targetOutputNode)
        {
            if (targetInputNode == null || targetOutputNode == null)
                return null;

            return base.DuplicateEdge(sourceEdge, targetInputNode, targetOutputNode);
        }
    }
}
