using System;
using System.Collections.Generic;
using Unity.Assertions;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Editor;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class StatePortModel : IPortModel
    {
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
            m_Guid = GUID.Generate();
        }

        [SerializeField, HideInInspector]
        SerializableGUID m_Guid;

        [SerializeField, HideInInspector]
        internal StateMachineAsset m_StateMachineAssetModel;

        [SerializeReference]
        internal IPortNode m_NodeModel;

        [SerializeField, HideInInspector]
        public Direction m_Direction;

        public IReadOnlyList<Capabilities> Capabilities => new Capabilities[] {UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.NoCapabilities};
        public IPortNode NodeModel { get { return m_NodeModel; } set { m_NodeModel = value; } }
        public Direction Direction { get { return m_Direction; } set { m_Direction = value; } }
        public PortType PortType { get; set; }
        public Orientation Orientation { get; set; }
        public PortCapacity Capacity { get; set; }
        public Type PortDataType { get; set; }
        public PortModelOptions Options { get; set; }
        public TypeHandle DataTypeHandle { get; set; }
        public string ToolTip { get; set; }
        public bool CreateEmbeddedValueIfNeeded => false;
        public IEnumerable<IPortModel> GetConnectedPorts()
        {
            return PortModelDefaultImplementations.GetConnectedPorts(this);
        }

        public IEnumerable<IEdgeModel> GetConnectedEdges()
        {
            return PortModelDefaultImplementations.GetConnectedEdges(this);
        }

        public bool IsConnectedTo(IPortModel toPort)
        {
            return PortModelDefaultImplementations.IsConnectedTo(this, toPort);
        }

        public PortCapacity GetDefaultCapacity()
        {
            return PortCapacity.Multi;
        }

        public IConstant EmbeddedValue => null;
        public bool DisableEmbeddedValueEditor => true;
        public string UniqueName => string.Empty;

        public StatePortModel(Direction direction, IPortNode node)
        {
            Direction = direction;
            m_NodeModel = node;
            Orientation = Orientation.Horizontal;
            Capacity = PortCapacity.Multi;
            PortDataType = typeof(BaseStateModel);
            Options = PortModelOptions.NoEmbeddedConstant | PortModelOptions.Hidden;
        }
    }

    [Serializable]
    internal class BaseStateModel : IPortNode, IHasTitle, IRenamable
    {
        [SerializeField, HideInInspector]
        public BaseAssetModel StateDefinitionAsset;

        public bool HasStateDefinitionAssetBeenCreated => StateDefinitionAsset != null;

        public void CreateDefinitionAsset()
        {
            var smStencil = (StateMachineStencil)GraphModel.Stencil;
            smStencil.CreateAssetFromStateModel(this, AssetModel);
        }

        [SerializeField, HideInInspector]
        SerializableGUID m_Guid;

        [SerializeField, HideInInspector]
        internal StateMachineAsset m_StateMachineAssetModel;

        [SerializeField, HideInInspector]
        Vector2 m_Position;

        [SerializeField, HideInInspector]
        Color m_Color;

        [SerializeField, HideInInspector]
        bool m_HasUserColor;

        [SerializeField, HideInInspector]
        string m_Title;

        [SerializeField, HideInInspector]
        internal StatePortModel OutgoingTransitionsPort;

        [SerializeField, HideInInspector]
        internal StatePortModel IncomingTransitionsPort;

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
            m_Guid = GUID.Generate();
        }

        protected List<Capabilities> m_Capabilities;
        public virtual void InitCapabilities()
        {
            m_Capabilities = new List<Capabilities>()
            {
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Selectable,
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Deletable,
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Copiable,
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Renamable,
                UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Movable
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

        public Vector2 Position
        {
            get => m_Position;
            set
            {
                if (!this.IsMovable())
                    return;

                m_Position = value;
            }
        }

        public void Move(Vector2 position)
        {
            if (!this.IsMovable())
                return;

            Position = position;
        }

        public bool Destroyed { get; private set; }

        public void Destroy()
        {
            if (Destroyed == true)
                return;
            Destroyed = true;
            OnDestroyed();
        }

        protected virtual void OnDestroyed()
        {
        }

        public Color Color
        {
            get => m_HasUserColor ? m_Color : Color.clear;
            set
            {
                m_HasUserColor = true;
                m_Color = value;
            }
        }

        public virtual bool AllowSelfConnect => true;
        public bool HasUserColor
        {
            get => m_HasUserColor;
            set => m_HasUserColor = value;
        }
        public bool HasProgress { get; }
        public string IconTypeString => string.Empty;
        public ModelState State { get; set; }

        public virtual string Tooltip => string.Empty;

        public IEnumerable<IEdgeModel> GetConnectedEdges()
        {
            foreach (var edge in OutgoingTransitionsPort.GetConnectedEdges())
                yield return edge;
            foreach (var edge in IncomingTransitionsPort.GetConnectedEdges())
                yield return edge;
        }

        public void DefineNode()
        {
            InitCapabilities();
            if (OutgoingTransitionsPort == null)
            {
                OutgoingTransitionsPort = new StatePortModel(Direction.Output, this){ AssetModel = AssetModel};
                OutgoingTransitionsPort.AssignNewGuid();
            }

            if (IncomingTransitionsPort == null)
            {
                IncomingTransitionsPort = new StatePortModel(Direction.Input, this){ AssetModel = AssetModel};
                IncomingTransitionsPort.AssignNewGuid();
            }
            OnDefineNode();
        }

        protected virtual void OnDefineNode()
        {
        }

        public void OnDuplicateNode(INodeModel sourceNode)
        {
            Title = (sourceNode as IHasTitle)?.Title ?? "";
            if (StateDefinitionAsset != null)
            {
                var copiedAsset = StateDefinitionAsset;
                var smStencil = (StateMachineStencil)GraphModel.Stencil;
                smStencil.CreateAssetFromStateModel(this, AssetModel);
                StateDefinitionAsset.GraphModel.CloneGraph(copiedAsset.GraphModel);
            }

            UpdateDuplicatedPorts();
        }

        protected void UpdateDuplicatedPorts()
        {
            if (OutgoingTransitionsPort != null)
            {
                GUID existingPortGuid = OutgoingTransitionsPort.Guid;
                OutgoingTransitionsPort = new StatePortModel(Direction.Output, this){ AssetModel = AssetModel, Guid = existingPortGuid };
            }

            if (IncomingTransitionsPort != null)
            {
                GUID existingPortGuid = IncomingTransitionsPort.Guid;
                IncomingTransitionsPort = new StatePortModel(Direction.Output, this){ AssetModel = AssetModel, Guid = existingPortGuid };
            }
        }

        public virtual string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        public virtual string DisplayTitle => Title.Nicify();
        public IEnumerable<IPortModel> Ports
        {
            get
            {
                yield return OutgoingTransitionsPort;
                yield return IncomingTransitionsPort;
            }
        }
        public void OnConnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
        }

        public void OnDisconnection(IPortModel selfConnectedPortModel, IPortModel otherConnectedPortModel)
        {
        }

        public PortCapacity GetPortCapacity(IPortModel portModel)
        {
            return PortCapacity.Multi;
        }

        public IPortModel GetPortFitToConnectTo(IPortModel portModel)
        {
            return null;
        }

        public void Rename(string newName)
        {
            Title = newName;
        }

        public IPortModel CreatePort(Direction direction, string portName, PortType portType, TypeHandle dataType, string portId, PortModelOptions options)
        {
            throw new NotImplementedException();
        }

        public void DeletePort(IPortModel portModel, bool removeFromOrderedPorts = false)
        {
            throw new NotImplementedException();
        }
    }
}
