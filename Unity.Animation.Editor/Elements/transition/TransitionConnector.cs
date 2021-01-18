using System;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class TransitionDragHelper
    {
        GhostStateToStateTransitionModel m_GhostTransitionModel;

        internal const float k_ThresholdBorderConnectionStart = 30.0f;

        public UnityEditor.GraphToolsFoundation.Overdrive.GraphView GraphView { get; }
        readonly Store m_Store;
        internal BaseTransitionModel.AnchorSide FromStateSide;
        internal float FromStateOffset;
        VisualElement TriggerElement { get; }
        public BaseState FromState { get; set; }
        public BaseStateModel FromStateModel => FromState.Model as BaseStateModel;
        StateToStateTransition m_GhostTransition;
        Vector2 m_WorldMouseDownPosition;

        public TransitionDragHelper(Store store, UnityEditor.GraphToolsFoundation.Overdrive.GraphView graphView, BaseState fromState, VisualElement triggerElement)
        {
            m_Store = store;
            FromState = fromState;
            GraphView = graphView;
            TriggerElement = triggerElement;
            Reset();
        }

        public StateToStateTransition CreateGhostTransition()
        {
            var ghostTransition = new GhostStateToStateTransitionModel(FromState.Model.GraphModel);
            var ui = GraphElementFactory.CreateUI<StateToStateTransition>(GraphView, m_Store, ghostTransition);
            return ui;
        }

        StateToStateTransition m_TransitionCandidate;
        public GhostStateToStateTransitionModel TransitionCandidateModel { get; private set; }
        public void CreateTransitionCandidate()
        {
            m_TransitionCandidate = CreateGhostTransition();
            TransitionCandidateModel = m_TransitionCandidate.TransitionModel as GhostStateToStateTransitionModel;
            TransitionCandidateModel.FromPoint = BaseState.GetPositionFromAnchorSide(FromStateSide, FromStateOffset, TriggerElement);
        }

        void ClearTransitionCandidate()
        {
            TransitionCandidateModel = null;
            m_TransitionCandidate = null;
        }

        public void Reset(bool didConnect = false)
        {
            if (m_GhostTransition != null)
            {
                GraphView.RemoveElement(m_GhostTransition);
            }

            if (m_TransitionCandidate != null)
            {
                GraphView.RemoveElement(m_TransitionCandidate);
            }


            m_GhostTransition = null;
            ClearTransitionCandidate();
        }

        public bool HandleMouseDown(MouseDownEvent evt)
        {
            if (evt.target != TriggerElement)
                return false;
            Vector2 mousePosition = evt.mousePosition;
            m_WorldMouseDownPosition = mousePosition;
            if (FromState == null || TransitionCandidateModel == null)
            {
                return false;
            }

            if (m_TransitionCandidate == null)
                return false;

            if (m_TransitionCandidate.parent == null)
            {
                GraphView.AddElement(m_TransitionCandidate);
            }

            ComputeAnchorPointsForSourceState(evt.localMousePosition);

            TransitionCandidateModel.FromPoint = mousePosition;
            TransitionCandidateModel.ToPoint = mousePosition;
            m_TransitionCandidate.SetEnabled(false);

            TransitionCandidateModel.ToPort = null;

            m_TransitionCandidate.UpdateFromModel();
            m_TransitionCandidate.Layer = Int32.MaxValue;

            if (FromStateSide == BaseTransitionModel.AnchorSide.None)
                return false;

            return true;
        }

        void ComputeAnchorPointsForSourceState(Vector2 mousePosition)
        {
            FromStateSide = BaseTransitionModel.AnchorSide.None;
            FromStateOffset = 0.0f;

            if (TriggerElement.localBound.xMax - k_ThresholdBorderConnectionStart < mousePosition.x)
            {
                FromStateSide = BaseTransitionModel.AnchorSide.Right;
                FromStateOffset = Mathf.Clamp(mousePosition.y - (TriggerElement.localBound.yMin + k_ThresholdBorderConnectionStart), 0, TriggerElement.localBound.height - 2 * k_ThresholdBorderConnectionStart);
            }
            else if (TriggerElement.localBound.xMin + k_ThresholdBorderConnectionStart > mousePosition.x)
            {
                FromStateSide = BaseTransitionModel.AnchorSide.Left;
                FromStateOffset = Mathf.Clamp(mousePosition.y - (TriggerElement.localBound.yMin + k_ThresholdBorderConnectionStart), 0, TriggerElement.localBound.height - 2 * k_ThresholdBorderConnectionStart);
            }
            else if (TriggerElement.localBound.yMin + k_ThresholdBorderConnectionStart > mousePosition.y)
            {
                FromStateSide = BaseTransitionModel.AnchorSide.Top;
                FromStateOffset = Mathf.Clamp(mousePosition.x - (TriggerElement.localBound.xMin + k_ThresholdBorderConnectionStart), 0, TriggerElement.localBound.width - 2 * k_ThresholdBorderConnectionStart);;
            }
            else if (TriggerElement.localBound.yMax - k_ThresholdBorderConnectionStart < mousePosition.y)
            {
                FromStateSide = BaseTransitionModel.AnchorSide.Bottom;
                FromStateOffset = Mathf.Clamp(mousePosition.x - (TriggerElement.localBound.xMin + k_ThresholdBorderConnectionStart), 0, TriggerElement.localBound.width - 2 * k_ThresholdBorderConnectionStart);;
            }
        }

        public void HandleMouseMove(MouseMoveEvent evt)
        {
            Vector2 mousePosition = evt.mousePosition;

            TransitionCandidateModel.ToPoint = mousePosition;

            m_TransitionCandidate.UpdateFromModel();

            var(toState, toStateModel) = GetToState(mousePosition);

            if (toStateModel != null)
            {
                if (m_GhostTransition == null)
                {
                    m_GhostTransition = CreateGhostTransition();
                    m_GhostTransitionModel = m_GhostTransition.TransitionModel as GhostStateToStateTransitionModel;

                    m_GhostTransitionModel.FromPoint = TransitionCandidateModel.FromPoint;

                    m_GhostTransition.pickingMode = PickingMode.Ignore;
                    GraphView.AddElement(m_GhostTransition);
                }

                var(toStateSide, toStateOffset) =
                    ComputeAnchorPointForTargetState(
                        m_WorldMouseDownPosition, mousePosition, toState);
                m_GhostTransitionModel.ToPoint =
                    BaseState.GetPositionFromAnchorSide(toStateSide, toStateOffset, toState);

                UnityEngine.Debug.Assert(m_GhostTransitionModel != null);

                m_GhostTransition.UpdateFromModel();
            }
            else if (m_GhostTransition != null && m_GhostTransitionModel != null)
            {
                GraphView.RemoveElement(m_GhostTransition);
                m_GhostTransitionModel.ToPort = null;
                m_GhostTransitionModel = null;
                m_GhostTransition = null;
            }
        }

        public void HandleMouseUp(MouseUpEvent evt)
        {
            bool didConnect = false;

            Vector2 mousePosition = evt.mousePosition;

            // Clean up ghost transitions.
            if (m_GhostTransitionModel != null)
            {
                GraphView.RemoveElement(m_GhostTransition);
                m_GhostTransitionModel.ToPort = null;
                m_GhostTransitionModel = null;
                m_GhostTransition = null;
            }

            var(toState, toStateModel) = GetToState(mousePosition);

            m_TransitionCandidate.SetEnabled(true);

            GraphView.RemoveElement(m_TransitionCandidate);

            if (toStateModel != null)
            {
                if (TransitionCandidateModel != null)
                {
                    TransitionCandidateModel.ToPort = toStateModel.IncomingTransitionsPort;
                }

                var(toStateSide, toStateOffset) = ComputeAnchorPointForTargetState(m_WorldMouseDownPosition, mousePosition, toState);
                m_Store.Dispatch(new CreateStateToStateTransitionAction(
                    FromStateModel, FromStateSide, FromStateOffset, toStateModel, toStateSide, toStateOffset));

                didConnect = true;
            }
            else if (TransitionCandidateModel != null)
            {
                TransitionCandidateModel.ToPort = null;
            }

            m_TransitionCandidate?.ResetLayer();

            ClearTransitionCandidate();

            Reset(didConnect);
        }

        (BaseState, BaseStateModel) GetToState(Vector2 mousePosition)
        {
            foreach (var stateModel in m_Store.State.GraphModel.NodeModels.OfType<BaseStateModel>().Where(x => x != FromStateModel))
            {
                var stateUI = stateModel.GetUI<BaseState>(GraphView);
                if (stateUI != null)
                {
                    Rect bounds = stateUI.worldBound;

                    var interactionBorder = stateUI.StateBorder;
                    if (interactionBorder != null && interactionBorder.ConnectionMode)
                        bounds = interactionBorder.worldBound;
                    if (bounds.Contains(mousePosition))
                    {
                        return (stateUI, stateModel);
                    }
                }
            }

            return default;
        }

        (BaseTransitionModel.AnchorSide, float) ComputeAnchorPointForTargetState(Vector2 sourceStateAnchorPosition, Vector2 targetStateInteractionPosition, BaseState state)
        {
            BaseTransitionModel.AnchorSide toStateSide = BaseTransitionModel.AnchorSide.None;
            float toStateOffset = 0F;

            float dx = targetStateInteractionPosition.x - sourceStateAnchorPosition.x;
            float dy = targetStateInteractionPosition.y - sourceStateAnchorPosition.y;

            // completely vertical
            if (math.abs(dx) < float.Epsilon)
            {
                if (sourceStateAnchorPosition.y < targetStateInteractionPosition.y)
                {
                    UnityEngine.Debug.Assert(FromStateSide == BaseTransitionModel.AnchorSide.Bottom);
                    toStateSide = BaseTransitionModel.AnchorSide.Top;
                }
                else
                {
                    UnityEngine.Debug.Assert(FromStateSide == BaseTransitionModel.AnchorSide.Top);
                    toStateSide = BaseTransitionModel.AnchorSide.Bottom;
                }

                toStateOffset = (targetStateInteractionPosition.x - state.worldBound.xMin) / state.worldBound.width * state.localBound.width;

                return (toStateSide, toStateOffset);
            }
            // completely horizontal
            if (math.abs(dy) < float.Epsilon)
            {
                if (sourceStateAnchorPosition.x < targetStateInteractionPosition.x)
                {
                    UnityEngine.Debug.Assert(FromStateSide == BaseTransitionModel.AnchorSide.Right);
                    toStateSide = BaseTransitionModel.AnchorSide.Left;
                }
                else
                {
                    UnityEngine.Debug.Assert(FromStateSide == BaseTransitionModel.AnchorSide.Left);
                    toStateSide = BaseTransitionModel.AnchorSide.Right;
                }

                toStateOffset = (targetStateInteractionPosition.y - state.worldBound.yMin) / state.worldBound.height * state.localBound.height;

                return (toStateSide, toStateOffset);
            }

            // compute the constant at x = 0 using the slope
            float c = sourceStateAnchorPosition.y - dy * sourceStateAnchorPosition.x / dx;

            //find intersection point at each axis to find the closest connection point
            float intersectWithTopAtX = (state.worldBound.yMin - c) * dx / dy;
            float shortestLengthSquare = float.MaxValue;
            if (intersectWithTopAtX >= state.worldBound.xMin && intersectWithTopAtX <= state.worldBound.xMax)
            {
                float localDiffX = sourceStateAnchorPosition.x - intersectWithTopAtX;
                float localDiffY = sourceStateAnchorPosition.y - state.worldBound.yMin;
                float localLengthSquare = localDiffX * localDiffX + localDiffY * localDiffY;
                if (localLengthSquare < shortestLengthSquare)
                {
                    shortestLengthSquare = localLengthSquare;
                    toStateSide = BaseTransitionModel.AnchorSide.Top;
                    toStateOffset = (intersectWithTopAtX - state.worldBound.xMin) / state.worldBound.width * state.localBound.width;
                }
            }

            float intersectWithBottomAtX = (state.worldBound.yMax - c) * dx / dy;
            if (intersectWithBottomAtX >= state.worldBound.xMin && intersectWithBottomAtX <= state.worldBound.xMax)
            {
                float localDiffX = sourceStateAnchorPosition.x - intersectWithBottomAtX;
                float localDiffY = sourceStateAnchorPosition.y - state.worldBound.yMax;
                float localLengthSquare = localDiffX * localDiffX + localDiffY * localDiffY;
                if (localLengthSquare < shortestLengthSquare)
                {
                    shortestLengthSquare = localLengthSquare;
                    toStateSide = BaseTransitionModel.AnchorSide.Bottom;
                    toStateOffset = (intersectWithBottomAtX - state.worldBound.xMin) / state.worldBound.width * state.localBound.width;
                }
            }

            float intersectWithLeftAtY = dy * state.worldBound.xMin / dx + c;
            if (intersectWithLeftAtY >= state.worldBound.yMin && intersectWithLeftAtY <= state.worldBound.yMax)
            {
                float localDiffX = sourceStateAnchorPosition.x - state.worldBound.xMin;
                float localDiffY = sourceStateAnchorPosition.y - intersectWithLeftAtY;
                float localLengthSquare = localDiffX * localDiffX + localDiffY * localDiffY;
                if (localLengthSquare < shortestLengthSquare)
                {
                    shortestLengthSquare = localLengthSquare;
                    toStateSide = BaseTransitionModel.AnchorSide.Left;
                    toStateOffset = (intersectWithLeftAtY - state.worldBound.yMin) / state.worldBound.height * state.localBound.height;
                }
            }

            float intersectWithRightAtY = dy * state.worldBound.xMax / dx + c;
            if (intersectWithRightAtY >= state.worldBound.yMin && intersectWithRightAtY <= state.worldBound.yMax)
            {
                float localDiffX = sourceStateAnchorPosition.x - state.worldBound.xMax;
                float localDiffY = sourceStateAnchorPosition.y - intersectWithRightAtY;
                float localLengthSquare = localDiffX * localDiffX + localDiffY * localDiffY;
                if (localLengthSquare < shortestLengthSquare)
                {
                    shortestLengthSquare = localLengthSquare;
                    toStateSide = BaseTransitionModel.AnchorSide.Right;
                    toStateOffset = (intersectWithRightAtY - state.worldBound.yMin) / state.worldBound.height * state.localBound.height;
                }
            }
            return (toStateSide, toStateOffset);
        }
    }

    internal class TransitionConnector : MouseManipulator
    {
        readonly TransitionDragHelper m_TransitionDragHelper;
        bool m_Active;
        Vector2 m_MouseDownPosition;

        internal const float k_ConnectionDistanceThreshold = 10f;

        public TransitionConnector(Store store, UnityEditor.GraphToolsFoundation.Overdrive.GraphView graphView, BaseState fromState, VisualElement triggerElement)
        {
            m_TransitionDragHelper = new TransitionDragHelper(store, graphView, fromState, triggerElement);
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        public virtual TransitionDragHelper DragHelper => m_TransitionDragHelper;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected virtual void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(e))
            {
                return;
            }

            m_MouseDownPosition = e.localMousePosition;

            m_TransitionDragHelper.CreateTransitionCandidate();

            if (m_TransitionDragHelper.HandleMouseDown(e))
            {
                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
            else
            {
                m_TransitionDragHelper.Reset();
            }
        }

        void OnCaptureOut(MouseCaptureOutEvent e)
        {
            m_Active = false;
            if (m_TransitionDragHelper.TransitionCandidateModel != null)
                Abort();
        }

        protected virtual void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active) return;
            m_TransitionDragHelper.HandleMouseMove(e);
            e.StopPropagation();
        }

        protected virtual void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            try
            {
                if (CanPerformConnection(e.localMousePosition))
                    m_TransitionDragHelper.HandleMouseUp(e);
                else
                    Abort();
            }
            finally
            {
                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || !m_Active)
                return;

            Abort();

            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }

        void Abort()
        {
            m_TransitionDragHelper.Reset();
        }

        bool CanPerformConnection(Vector2 mousePosition)
        {
            return Vector2.Distance(m_MouseDownPosition, mousePosition) > k_ConnectionDistanceThreshold;
        }
    }
}
