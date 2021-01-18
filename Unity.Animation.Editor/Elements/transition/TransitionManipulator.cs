using Unity.Mathematics;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class TransitionManipulator : MouseManipulator
    {
        bool m_Active;
        BaseTransition m_Transition;
        Vector2 m_MouseDownPosition;
        bool m_MovingAnchor;
        readonly int k_StartDragDistanceSquare = 5 * 5;

        BaseTransitionModel TransitionModel => (BaseTransitionModel)m_Transition.TransitionModel;
        BaseTransitionModel.AnchorSide m_OriginalAnchorSide;
        float m_OriginalAnchorOffset;
        bool m_MovingToAnchor;
        Store m_Store;

        public TransitionManipulator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            Reset();
        }

        public void SetStore(Store store)
        {
            m_Store = store;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void Reset()
        {
            m_Active = false;
            m_Transition = null;
            m_OriginalAnchorSide = BaseTransitionModel.AnchorSide.None;
            m_OriginalAnchorOffset = 0.0f;
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (m_Active)
            {
                evt.StopImmediatePropagation();
                return;
            }

            if (!CanStartManipulation(evt))
            {
                return;
            }

            m_Transition = (evt.target as VisualElement)?.GetFirstOfType<BaseTransition>();

            if (TransitionModel.TransitionType == Model.TransitionType.StateToStateTransition)
            {
                var distanceSquareFromPt = (m_Transition.From - evt.localMousePosition).sqrMagnitude;
                var distanceSquareToPt = (m_Transition.To - evt.localMousePosition).sqrMagnitude;
                if (distanceSquareFromPt < distanceSquareToPt)
                {
                    m_MovingToAnchor = false;
                    m_OriginalAnchorOffset = TransitionModel.FromStateAnchorOffset;
                    m_OriginalAnchorSide = TransitionModel.FromStateAnchorSide;
                }
                else
                {
                    m_MovingToAnchor = true;
                    m_OriginalAnchorOffset = TransitionModel.ToStateAnchorOffset;
                    m_OriginalAnchorSide = TransitionModel.ToStateAnchorSide;
                }
            }
            else
            {
                m_MovingToAnchor = true;
                m_OriginalAnchorOffset = TransitionModel.ToStateAnchorOffset;
                m_OriginalAnchorSide = TransitionModel.ToStateAnchorSide;
            }

            m_MouseDownPosition = evt.mousePosition;
            target.CaptureMouse();
            evt.StopPropagation();
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            // If the left mouse button is not down then return
            if (m_Transition == null)
            {
                return;
            }

            evt.StopPropagation();

            if (!m_MovingAnchor)
            {
                float deltaSquare = (evt.mousePosition - m_MouseDownPosition).sqrMagnitude;

                if (deltaSquare < k_StartDragDistanceSquare)
                {
                    return;
                }

                m_MovingAnchor = true;
            }

            BaseStateModel targetState = null;
            BaseTransitionModel.AnchorSide targetSide = BaseTransitionModel.AnchorSide.None;
            if (m_MovingToAnchor)
            {
                targetState = TransitionModel.ToPort.NodeModel as BaseStateModel;
                targetSide = TransitionModel.ToStateAnchorSide;
            }
            else
            {
                targetState = TransitionModel.FromPort.NodeModel as BaseStateModel;
                targetSide = TransitionModel.FromStateAnchorSide;
            }

            var targetStateUI = targetState.GetUI<BaseState>(m_Transition.GraphView);
            if (targetStateUI == null)
                return;

            float minDistance = float.MaxValue;
            var targetPointOnRightSide = new Vector2(targetStateUI.worldBound.xMax, Mathf.Clamp(evt.mousePosition.y, targetStateUI.worldBound.yMin, targetStateUI.worldBound.yMax));
            float distanceSquare = (targetPointOnRightSide - evt.mousePosition).sqrMagnitude;
            if (distanceSquare < minDistance)
            {
                minDistance = distanceSquare;
                targetSide = BaseTransitionModel.AnchorSide.Right;
            }
            var targetPointOnLeftSide = new Vector2(targetStateUI.worldBound.xMin, Mathf.Clamp(evt.mousePosition.y, targetStateUI.worldBound.yMin, targetStateUI.worldBound.yMax));
            distanceSquare = (targetPointOnLeftSide - evt.mousePosition).sqrMagnitude;
            if (distanceSquare < minDistance)
            {
                minDistance = distanceSquare;
                targetSide = BaseTransitionModel.AnchorSide.Left;
            }
            var targetPointOnTopSide = new Vector2(Mathf.Clamp(evt.mousePosition.x, targetStateUI.worldBound.xMin, targetStateUI.worldBound.xMax), targetStateUI.worldBound.yMin);
            distanceSquare = (targetPointOnTopSide - evt.mousePosition).sqrMagnitude;
            if (distanceSquare < minDistance)
            {
                minDistance = distanceSquare;
                targetSide = BaseTransitionModel.AnchorSide.Top;
            }
            var targetPointOnBottomSide = new Vector2(Mathf.Clamp(evt.mousePosition.x, targetStateUI.worldBound.xMin, targetStateUI.worldBound.xMax), targetStateUI.worldBound.yMax);
            distanceSquare = (targetPointOnBottomSide - evt.mousePosition).sqrMagnitude;
            if (distanceSquare < minDistance)
            {
                minDistance = distanceSquare;
                targetSide = BaseTransitionModel.AnchorSide.Bottom;
            }

            float targetOffset = 0.0f;
            if (targetSide == BaseTransitionModel.AnchorSide.Top || targetSide == BaseTransitionModel.AnchorSide.Bottom)
            {
                var newPosX = evt.mousePosition.x;
                if (newPosX < targetStateUI.worldBound.xMin)
                    newPosX = targetStateUI.worldBound.xMin;
                if (newPosX > targetStateUI.worldBound.xMax)
                    newPosX = targetStateUI.worldBound.xMax;
                targetOffset = (newPosX - targetStateUI.worldBound.xMin) / targetStateUI.worldBound.width * targetStateUI.localBound.width;
            }
            else if (targetSide == BaseTransitionModel.AnchorSide.Left || targetSide == BaseTransitionModel.AnchorSide.Right)
            {
                var newPosY = evt.mousePosition.y;
                if (newPosY < targetStateUI.worldBound.yMin)
                    newPosY = targetStateUI.worldBound.yMin;
                if (newPosY > targetStateUI.worldBound.yMax)
                    newPosY = targetStateUI.worldBound.yMax;
                targetOffset = (newPosY - targetStateUI.worldBound.yMin) / targetStateUI.worldBound.height * targetStateUI.localBound.height;
            }

            if (targetSide != BaseTransitionModel.AnchorSide.None)
            {
                if (m_MovingToAnchor)
                {
                    TransitionModel.UpdateToStateAnchor(targetSide, targetOffset);
                    m_Transition.UpdateTransitionVisual();
                }
                else
                {
                    TransitionModel.UpdateFromStateAnchor(targetSide, targetOffset);
                    m_Transition.UpdateTransitionVisual();
                }
            }
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (CanStopManipulation(evt))
            {
                BaseTransitionModel.AnchorSide currentSide = m_MovingToAnchor ? TransitionModel.ToStateAnchorSide : TransitionModel.FromStateAnchorSide;
                float currentOffset = m_MovingToAnchor ? TransitionModel.ToStateAnchorOffset : TransitionModel.FromStateAnchorOffset;

                if (currentSide != m_OriginalAnchorSide || math.abs(currentOffset - m_OriginalAnchorOffset) > float.Epsilon)
                {
                    ResetTransitionToDefaultValues();
                    m_Store.Dispatch(new MoveTransitionAnchorAction(TransitionModel, currentSide, currentOffset, m_MovingToAnchor ? MoveTransitionAnchorAction.StateSide.ToState : MoveTransitionAnchorAction.StateSide.FromState));
                }
                m_MovingAnchor = false;
                target.ReleaseMouse();
                Reset();
                evt.StopPropagation();
            }
        }

        private void ResetTransitionToDefaultValues()
        {
            if (m_MovingToAnchor)
            {
                TransitionModel.UpdateToStateAnchor(m_OriginalAnchorSide, m_OriginalAnchorOffset);
                m_Transition.UpdateTransitionVisual();
            }
            else
            {
                TransitionModel.UpdateFromStateAnchor(m_OriginalAnchorSide, m_OriginalAnchorOffset);
                m_Transition.UpdateTransitionVisual();
            }
        }

        protected void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Active)
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    ResetTransitionToDefaultValues();
                    Reset();
                    target.ReleaseMouse();
                    evt.StopPropagation();
                }
            }
        }
    }
}
