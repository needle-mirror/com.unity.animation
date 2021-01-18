using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using Unity.Animation.Editor;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class BaseVariableModel : VariableNodeModel
    {
        private BasePortModel.PortEvaluationType m_PortEvaluationType;

        protected void DefineNode(bool isInput, TypeHandle dataType, BasePortModel.PortEvaluationType portEvalType)
        {
            m_PortEvaluationType = portEvalType;

            if (isInput)
                m_MainPortModel = (PortModel)AddOutputPort("",
                    dataType.Resolve() == ((BaseGraphStencil)Stencil).Context.DefaultDataType ? PortType.Execution : PortType.Data, dataType);
            else
                m_MainPortModel = (PortModel)AddInputPort("",
                    dataType.Resolve() == ((BaseGraphStencil)Stencil).Context.DefaultDataType ? PortType.Execution : PortType.Data, dataType);
        }

        public override IPortModel CreatePort(Direction direction, string portName, PortType portType,
            TypeHandle dataType, string portId, PortModelOptions options)
        {
            if (m_PortEvaluationType == BasePortModel.PortEvaluationType.Rendering)
            {
                return new DataPortModel(portName ?? "", portId, options)
                {
                    Direction = direction,
                    PortType = portType,
                    DataTypeHandle = dataType,
                    NodeModel = this,
                    EvaluationType = BasePortModel.PortEvaluationType.Rendering
                };
            }

            if (m_PortEvaluationType == BasePortModel.PortEvaluationType.Properties)
            {
                return new PropertiesPortModel(portName ?? "", portId, options)
                {
                    Direction = direction,
                    PortType = portType,
                    DataTypeHandle = dataType,
                    NodeModel = this,
                    EvaluationType = BasePortModel.PortEvaluationType.Properties
                };
            }

            return new MessagePortModel(portName ?? "", portId, options)
            {
                Direction = direction,
                PortType = portType,
                DataTypeHandle = dataType,
                NodeModel = this,
                EvaluationType = BasePortModel.PortEvaluationType.Simulation
            };
        }
    }
}
