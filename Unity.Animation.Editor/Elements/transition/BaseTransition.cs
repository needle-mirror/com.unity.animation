using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class BaseTransition : GraphElement
    {
        public new static readonly string ussClassName = "sm-transition";
        public static readonly string ghostModifierUssClassName = ussClassName.WithUssModifier("ghost");

        public static readonly string transitionControlPartName = "transition-control";

        TransitionControl m_TransitionControl;
        protected TransitionManipulator m_TransitionManipulator;

        public IEdgeModel TransitionModel => Model as IEdgeModel;

        public bool IsGhostTransition => TransitionModel is GhostStateToStateTransitionModel;

        public Vector2 From
        {
            get
            {
                var p = Vector2.zero;

                var port = TransitionModel.FromPort;
                if (port == null)
                {
                    if (TransitionModel is GhostStateToStateTransitionModel ghostTransitionModel)
                    {
                        p = ghostTransitionModel.FromPoint;
                    }
                }
                else
                {
                    var ui = port.NodeModel.GetUI<BaseState>(GraphView);
                    if (ui == null)
                        return Vector2.zero;

                    p = ui.GetPositionForTransition(TransitionModel);
                }

                return this.WorldToLocal(p);
            }
        }

        public Vector2 To
        {
            get
            {
                var p = Vector2.zero;

                var port = TransitionModel.ToPort;
                if (port == null)
                {
                    if (TransitionModel is GhostStateToStateTransitionModel ghostTransitionModel)
                    {
                        p = ghostTransitionModel.ToPoint;
                    }
                }
                else
                {
                    var ui = port.NodeModel.GetUI<BaseState>(GraphView);
                    if (ui == null)
                        return Vector2.zero;

                    p = ui.GetPositionForTransition(TransitionModel);
                }

                return this.WorldToLocal(p);
            }
        }

        public TransitionControl TransitionControl
        {
            get
            {
                if (m_TransitionControl == null)
                {
                    var transitionControlPart = PartList.GetPart(transitionControlPartName);
                    m_TransitionControl = transitionControlPart?.Root as TransitionControl;
                }

                return m_TransitionControl;
            }
        }

        public override bool ShowInMiniMap => false;

        public BaseTransition()
        {
            Layer = -1;
            m_TransitionManipulator = new TransitionManipulator();
            this.AddManipulator(m_TransitionManipulator);
        }

        protected override void BuildPartList()
        {
            PartList.AppendPart(TransitionControlPart.Create(transitionControlPartName, Model, this, ussClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            m_TransitionManipulator.SetStore(Store);

            TransitionControl?.RegisterCallback<GeometryChangedEvent>(OnTransitionGeometryChanged);

            AddToClassList(ussClassName);
            EnableInClassList(ghostModifierUssClassName, IsGhostTransition);

            var clickable = new Clickable(OnDoubleClickState);
            clickable.activators.Clear();
            clickable.activators.Add(
                new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            this.AddManipulator(clickable);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.TemplatePath + "BaseTransition.uss"));
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();
        }

        public override bool Overlaps(Rect rectangle)
        {
            return TransitionControl.Overlaps(this.ChangeCoordinatesTo(TransitionControl, rectangle));
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return TransitionControl.ContainsPoint(localPoint);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            UpdateTransitionVisual();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            UpdateTransitionVisual();
        }

        void OnTransitionGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateFromModel();
        }

        public void UpdateTransitionVisual()
        {
            var transitionControlPart = PartList.GetPart(transitionControlPartName);
            transitionControlPart?.UpdateFromModel();
        }

        void OnDoubleClickState()
        {
            ConditionEditor.ShowConditionEditor();
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
        }

        public override void AddBackwardDependencies()
        {
            base.AddBackwardDependencies();

            AddBackwardDependencies(TransitionModel.FromPort);
            AddBackwardDependencies(TransitionModel.ToPort);

            void AddBackwardDependencies(IPortModel portModel)
            {
                if (portModel == null)
                    return;

                var ui = portModel.NodeModel.GetUI<BaseState>(GraphView);
                if (ui != null)
                {
                    // Edge position changes with node position.
                    Dependencies.AddBackwardDependency(ui, DependencyType.Geometry);
                }
            }
        }
    }
}
