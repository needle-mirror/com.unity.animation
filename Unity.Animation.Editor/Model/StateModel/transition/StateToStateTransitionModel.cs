using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace Unity.Animation.Model
{
    internal class GhostStateToStateTransitionModel : IEdgeModel
    {
        public IGraphAssetModel AssetModel
        {
            get => GraphModel.AssetModel;
            set => GraphModel.AssetModel = value;
        }

        public IGraphModel GraphModel { get; }

        public IPortModel FromPort { get; set; }

        public string FromPortId => FromPort?.UniqueName;

        public string ToPortId => ToPort?.UniqueName;

        public GUID FromNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        public GUID ToNodeGuid => FromPort?.NodeModel?.Guid ?? default;

        public IPortModel ToPort { get; set; }

        public string EdgeLabel { get; set; }

        public Vector2 FromPoint { get; set; } = Vector2.zero;
        public Vector2 ToPoint { get; set; } = Vector2.zero;

        public GUID Guid { get; set; } = GUID.Generate();

        public GhostStateToStateTransitionModel(IGraphModel graphModel)
        {
            GraphModel = graphModel;
        }

        public void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
        {
            FromPort = fromPortModel;
            ToPort = toPortModel;
        }

        public void ResetPorts()
        {
        }

        public void AssignNewGuid()
        {
            Guid = GUID.Generate();
        }

        // Ghost edges have no capabilities
        readonly List<Capabilities> m_Capabilities = new List<Capabilities> { UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.NoCapabilities};
        public virtual IReadOnlyList<Capabilities> Capabilities => m_Capabilities;
    }

    [Serializable]
    internal class StateToStateTransitionModel : BaseTransitionModel
    {
        [SerializeReference]
        protected StateTransitionProperties m_StoreTransitionProperties = new StateTransitionProperties();

        public override BaseTransitionProperties TransitionProperties => m_StoreTransitionProperties;

        public override TransitionType TransitionType => TransitionType.StateToStateTransition;

        public override void DuplicateTransitionProperties(BaseTransitionModel sourceTransition)
        {
            m_StoreTransitionProperties = (StateTransitionProperties)sourceTransition.TransitionProperties.Clone();
        }
    }
}
