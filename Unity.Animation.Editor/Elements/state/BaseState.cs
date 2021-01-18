using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class StateBorder : VisualElement
    {
        public static readonly string ussClassName = "sm-state-border";
        public static readonly string contentContainerElementName = "content-container";
        public static readonly string selectionBorderElementName = "selection-border";
        public static readonly string connectionModeUssClassName = ussClassName.WithUssModifier("connection-mode");

        public VisualElement ContentContainer { get; }
        public bool ConnectionMode
        {
            get => ClassListContains(connectionModeUssClassName);
            set => EnableInClassList(connectionModeUssClassName, value);
        }

        public StateBorder()
        {
            AddToClassList(ussClassName);

            var selectionBorder = new SelectionBorder { name = selectionBorderElementName };
            selectionBorder.AddToClassList(ussClassName.WithUssElement(selectionBorderElementName));
            Add(selectionBorder);

            ContentContainer = selectionBorder.ContentContainer;

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.TemplatePath + "StateBorder.uss"));
        }
    }


    internal class BaseState : Node
    {
        public new static readonly string ussClassName = "sm-basestate";
        public static readonly string stateBorderElementName = "state-border";

        VisualElement m_ContentContainer;

        StateBorder m_StateBorder;
        public override VisualElement contentContainer => m_ContentContainer ?? this;
        public TransitionConnector TransitionConnector { get; protected set; }

        public StateBorder StateBorder => m_StateBorder;

        protected override void BuildElementUI()
        {
            AddToClassList(GraphElement.ussClassName);
            m_StateBorder = new StateBorder { name = stateBorderElementName };
            m_StateBorder.AddToClassList(ussClassName.WithUssElement(stateBorderElementName));
            Add(m_StateBorder);
            m_ContentContainer = m_StateBorder.ContentContainer;

            AddToClassList(ussClassName);
        }

        protected override void BuildPartList()
        {
            PartList.AppendPart(EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            m_StateBorder.RegisterCallback<MouseOverEvent>(OnMouseOver);
            m_StateBorder.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            TransitionConnector = new TransitionConnector(Store, GraphView, this, m_StateBorder);
            this.AddManipulator(TransitionConnector);

            var clickable = new Clickable(OnDoubleClickState);
            clickable.activators.Clear();
            clickable.activators.Add(
                new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            this.AddManipulator(clickable);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.TemplatePath + "CompositorBaseState.uss"));
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            m_StateBorder.ConnectionMode = false;
        }

        void OnMouseOver(MouseOverEvent evt)
        {
            m_StateBorder.ConnectionMode = (evt.target == m_StateBorder);
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();
        }

        void OnDoubleClickState()
        {
            var baseStateModel = (BaseStateModel)Model;
            string definitionPath;

            if (!baseStateModel.HasStateDefinitionAssetBeenCreated)
            {
                baseStateModel.CreateDefinitionAsset();
            }
            definitionPath = AssetDatabase.GetAssetPath(baseStateModel.StateDefinitionAsset);
            Store.Dispatch(new LoadGraphAssetAction(definitionPath, loadType: LoadGraphAssetAction.Type.PushOnStack));
            var graphView = GraphView as GraphView;
            graphView.RequestFrameAll();
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var baseStateModel = (BaseStateModel)Model;

            if (evt.menu.MenuItems().Count != 0)
                evt.menu.AppendSeparator();

            var smModel = (StateMachineModel)Store.State.GraphModel;

            evt.menu.AppendAction("Add Global Transition", menuAction =>
            {
                Store.Dispatch(new CreateTargetStateTransitionAction(smModel, baseStateModel, Animation.Model.TransitionType.GlobalTransition));
            });

            evt.menu.AppendAction("Add OnEnter Selector", menuAction =>
            {
                Store.Dispatch(new CreateTargetStateTransitionAction(smModel, baseStateModel, Animation.Model.TransitionType.OnEnterSelector));
            });

            evt.menu.AppendAction("Add Self Transition", menuAction =>
            {
                Store.Dispatch(new CreateTargetStateTransitionAction(smModel, baseStateModel, Animation.Model.TransitionType.SelfTransition));
            });

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Find associated file", menuAction =>
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GetAssetPath(baseStateModel.StateDefinitionAsset));
                EditorGUIUtility.PingObject(obj);
            });

            if (evt.target is BaseState)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Rename", menuAction =>
                {
                    EditTitle();
                });
            }
        }

        public static Vector2 GetPositionFromAnchorSide(BaseTransitionModel.AnchorSide side, float offset, VisualElement element)
        {
            switch (side)
            {
                case BaseTransitionModel.AnchorSide.Left:
                    return new Vector2(element.worldBound.xMin, element.worldBound.yMin + (offset / element.localBound.height) * element.worldBound.height);
                case BaseTransitionModel.AnchorSide.Right:
                    return new Vector2(element.worldBound.xMax, element.worldBound.yMin + (offset / element.localBound.height) * element.worldBound.height);
                case BaseTransitionModel.AnchorSide.Top:
                    return new Vector2(element.worldBound.xMin + (offset / element.localBound.width) * element.worldBound.width, element.worldBound.yMin);
                case BaseTransitionModel.AnchorSide.Bottom:
                    return new Vector2(element.worldBound.xMin + (offset / element.localBound.width) * element.worldBound.width, element.worldBound.yMax);
            }

            return element.worldBound.center;
        }

        public Vector2 GetPositionForTransition(IEdgeModel transitionModel)
        {
            BaseTransitionModel baseTransitionModel = transitionModel as BaseTransitionModel;
            if (baseTransitionModel == null)
                return worldBound.center;
            BaseTransitionModel.AnchorSide side = BaseTransitionModel.AnchorSide.None;
            float offset = 0.0f;
            if (baseTransitionModel.TransitionType == Animation.Model.TransitionType.StateToStateTransition && transitionModel.FromPort.NodeModel == NodeModel)
            {
                side = baseTransitionModel.FromStateAnchorSide;
                offset = baseTransitionModel.FromStateAnchorOffset;
            }
            else if (transitionModel.ToPort.NodeModel == NodeModel)
            {
                side = baseTransitionModel.ToStateAnchorSide;
                offset = baseTransitionModel.ToStateAnchorOffset;
            }

            return GetPositionFromAnchorSide(side, offset, this);
        }
    }
}
