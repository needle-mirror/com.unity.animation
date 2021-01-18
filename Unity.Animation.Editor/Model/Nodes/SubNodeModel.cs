using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    internal abstract class SubNodeModel<TGraphAsset> : BaseNodeModel
        where TGraphAsset : IGraphAssetModel
    {
        [SerializeField, HideInInspector]
        public TGraphAsset GraphAsset;

        [SerializeField, HideInInspector]
        public Type StencilType;

        public override string NodeName => nameof(TGraphAsset);

        public override void OnVisit()
        {
            DefineNode();
        }

        protected override void OnDefineNode()
        {
            //if (GraphAsset != null && GraphAsset.GraphModel != null)
            //{
            //     var portDeclarations = CompositorIRBuilder.BuildPortDeclarations(GraphAsset.GraphModel.VariableDeclarations);

            //     foreach (var p in portDeclarations.InputMessagePorts)
            //         AddCompositorInputPort(new CompositorNodePortCreation() { EvalType = CompositorBasePortModel.PortEvaluationType.Simulation, DefaultValue = p.Attributes.DefaultValue, DefValueType = p.Attributes.DefValueType, PortDescription = p.Attributes.Tooltip, Name = p.PortName, PortType = PortType.Data, DataType = p.DataType.GenerateTypeHandle()});
            //     foreach (var p in portDeclarations.OutputMessagePorts)
            //         AddCompositorOutputPort(new CompositorNodePortCreation() { EvalType = CompositorBasePortModel.PortEvaluationType.Simulation, DefaultValue = p.Attributes.DefaultValue, DefValueType = p.Attributes.DefValueType, PortDescription = p.Attributes.Tooltip, Name = p.PortName, PortType = PortType.Data, DataType = p.DataType.GenerateTypeHandle()});
            //     foreach (var p in portDeclarations.InputDataPorts)
            //         AddCompositorInputPort(new CompositorNodePortCreation() { EvalType = CompositorBasePortModel.PortEvaluationType.Rendering, DefaultValue = p.Attributes.DefaultValue, DefValueType = p.Attributes.DefValueType, PortDescription = p.Attributes.Tooltip, Name = p.PortName, PortType = PortType.Data, DataType = p.DataType.GenerateTypeHandle()});
            //     foreach (var p in portDeclarations.OutputDataPorts)
            //         AddCompositorOutputPort(new CompositorNodePortCreation() { EvalType = CompositorBasePortModel.PortEvaluationType.Rendering, DefaultValue = p.Attributes.DefaultValue, DefValueType = p.Attributes.DefValueType, PortDescription = p.Attributes.Tooltip, Name = p.PortName, PortType = PortType.Data, DataType = p.DataType.GenerateTypeHandle()});
            //}
        }
    }
}
