using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace Unity.Animation.Model
{
    internal class StateMachineModel : BaseModel
    {
        public StateToStateTransitionModel AddTransition(BaseStateModel fromStateModel, StateToStateTransitionModel.AnchorSide fromStateAnchorSide, float fromStateAnchorOffset, BaseStateModel toStateModel, StateToStateTransitionModel.AnchorSide toStateAnchorSide, float toStateAnchorOffset)
        {
            StateToStateTransitionModel newTransition = new StateToStateTransitionModel() {AssetModel = AssetModel};
            newTransition.AssignNewGuid();
            newTransition.SetPorts(toStateModel?.IncomingTransitionsPort, toStateAnchorSide, toStateAnchorOffset, fromStateModel?.OutgoingTransitionsPort, fromStateAnchorSide, fromStateAnchorOffset);
            m_GraphEdgeModels.Add(newTransition);

            return newTransition;
        }

        public override IEdgeModel CreateEdge(IPortModel inputPort, IPortModel outputPort)
        {
            return null;
        }

        public void AddTargetStateTransition(BaseTransitionModel transition)
        {
            m_GraphEdgeModels.Add(transition);
        }

        static readonly float kWidthForTargetStateTransition = 60.0f;
        public float FindAnchorPositionForTargetStateTransition(BaseStateModel actionState)
        {
            float maxOffset = 0.0f;
            foreach (var edge in actionState.IncomingTransitionsPort.GetConnectedEdges())
            {
                if (edge is BaseTransitionModel transitionModel && !(edge is StateToStateTransitionModel))
                {
                    if (transitionModel.ToStateAnchorSide == BaseTransitionModel.AnchorSide.Top && transitionModel.ToStateAnchorOffset > maxOffset)
                    {
                        maxOffset = transitionModel.ToStateAnchorOffset;
                    }
                }
            }

            maxOffset += kWidthForTargetStateTransition;
            return maxOffset;
        }

        public override IEdgeModel DuplicateEdge(IEdgeModel sourceEdge, INodeModel targetInputNode, INodeModel targetOutputNode)
        {
            if (sourceEdge is BaseTransitionModel sourceTransition &&
                targetInputNode is BaseStateModel targetInputState &&
                targetOutputNode is BaseStateModel targetOutputState)
            {
                BaseTransitionModel newTransition = null;
                if (sourceTransition is StateToStateTransitionModel)
                {
                    newTransition = AddTransition(targetOutputState, sourceTransition.FromStateAnchorSide, sourceTransition.FromStateAnchorOffset,
                        targetInputState, sourceTransition.ToStateAnchorSide, sourceTransition.ToStateAnchorOffset);

                    newTransition.DuplicateTransitionProperties(sourceTransition);
                    return newTransition;
                }

                if (sourceTransition is GlobalTransitionModel)
                    newTransition = new GlobalTransitionModel(){AssetModel = AssetModel};
                else if (sourceTransition is SelfTransitionModel)
                    newTransition = new SelfTransitionModel(){AssetModel = AssetModel};
                else if (sourceTransition is OnEnterStateSelectorModel)
                    newTransition = new OnEnterStateSelectorModel(){AssetModel = AssetModel};
                if (newTransition == null)
                    return null;

                newTransition.AssignNewGuid();
                float anchorPos = FindAnchorPositionForTargetStateTransition(targetInputState);
                newTransition.SetPorts(targetInputState.IncomingTransitionsPort, BaseTransitionModel.AnchorSide.Top,
                    anchorPos, targetInputState.OutgoingTransitionsPort, BaseTransitionModel.AnchorSide.None,
                    0.0f);
                AddTargetStateTransition(newTransition);
                newTransition.DuplicateTransitionProperties(sourceTransition);
                return newTransition;
            }

            return null;
        }
    }
}
