using System;
using System.Collections.Generic;
using Unity.Assertions;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;

using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    internal abstract class BaseTransitionModel : IEdgeModel, ITransitionPropertiesModel
    {
        public abstract BaseTransitionProperties TransitionProperties { get; }
        public abstract TransitionType TransitionType { get; }

        [SerializeField, HideInInspector]
        internal StateMachineAsset m_StateMachineAssetModel;

        [SerializeField, HideInInspector]
        SerializableGUID m_Guid;

        [SerializeReference]
        internal BaseStateModel m_FromNodeModel;

        [SerializeReference]
        internal BaseStateModel m_ToNodeModel;

        public enum AnchorSide
        {
            None,
            Top,
            Right,
            Bottom,
            Left
        }

        [SerializeField, HideInInspector]
        internal AnchorSide m_FromStateAnchorSide;
        [SerializeField, HideInInspector]
        internal float m_FromStateAnchorOffset;
        [SerializeField, HideInInspector]
        internal AnchorSide m_ToStateAnchorSide;
        [SerializeField, HideInInspector]
        internal float m_ToStateAnchorOffset;

        public AnchorSide FromStateAnchorSide => m_FromStateAnchorSide;
        public float FromStateAnchorOffset => m_FromStateAnchorOffset;
        public AnchorSide ToStateAnchorSide => m_ToStateAnchorSide;
        public float ToStateAnchorOffset => m_ToStateAnchorOffset;

        public void UpdateToStateAnchor(AnchorSide side, float offset)
        {
            m_ToStateAnchorOffset = offset;
            m_ToStateAnchorSide = side;
        }

        public void UpdateFromStateAnchor(AnchorSide side, float offset)
        {
            m_FromStateAnchorOffset = offset;
            m_FromStateAnchorSide = side;
        }

        public IGraphModel GraphModel => AssetModel?.GraphModel;

        public GUID Guid
        {
            get
            {
                if (m_Guid.GUID.Empty())
                    AssignNewGuid();
                return m_Guid;
            }
            // Setter for tests only.
            set => m_Guid = value;
        }

        public virtual IGraphAssetModel AssetModel
        {
            get => m_StateMachineAssetModel;
            set
            {
                Assert.IsNotNull(value as StateMachineAsset);
                m_StateMachineAssetModel = (StateMachineAsset)value;
            }
        }
        public void AssignNewGuid()
        {
            Guid = GUID.Generate();
        }

        protected List<Capabilities> m_Capabilities;
        public virtual void InitCapabilities()
        {
            m_Capabilities = new List<Capabilities>()
            {
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Deletable,
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Copiable,
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Selectable
            };
        }

        public IReadOnlyList<Capabilities> Capabilities
        {
            get
            {
                if (m_Capabilities == null)
                    InitCapabilities();
                return m_Capabilities;
            }
        }
        public IPortModel FromPort
        {
            get => m_FromNodeModel?.OutgoingTransitionsPort;
            set => m_FromNodeModel = value?.NodeModel as BaseStateModel;
        }

        public IPortModel ToPort
        {
            get => m_ToNodeModel?.IncomingTransitionsPort;
            set => m_ToNodeModel = value?.NodeModel as BaseStateModel;
        }

        public void SetPorts(IPortModel toPortModel, IPortModel fromPortModel)
        {
            SetPorts(toPortModel, AnchorSide.None, 0.0f, fromPortModel, AnchorSide.None, 0.0f);
        }

        public void SetPorts(IPortModel toPortModel, AnchorSide toStateSide, float toStateOffset, IPortModel fromPortModel, AnchorSide fromStateSide, float fromStateOffset)
        {
            m_FromNodeModel = fromPortModel?.NodeModel as BaseStateModel;
            m_ToNodeModel = toPortModel?.NodeModel as BaseStateModel;
            m_FromStateAnchorSide = fromStateSide;
            m_ToStateAnchorSide = toStateSide;
            m_FromStateAnchorOffset = fromStateOffset;
            m_ToStateAnchorOffset = toStateOffset;
        }

        public void ResetPorts()
        {
        }

        public string FromPortId { get; }
        public string ToPortId { get; }
        public GUID ToNodeGuid => ToPort.NodeModel.Guid;
        public GUID FromNodeGuid => FromPort != null ? FromPort.NodeModel.Guid : new GUID();
        public string EdgeLabel { get; set; }
        public abstract void DuplicateTransitionProperties(BaseTransitionModel sourceTransition);
    }
}
