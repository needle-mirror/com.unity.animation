using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class CreateStateToStateTransitionAction : BaseAction
    {
        public readonly BaseStateModel FromStateModel;
        public readonly BaseTransitionModel.AnchorSide FromStateAnchorSide;
        public readonly float FromStateAnchorOffset;
        public readonly BaseStateModel ToStateModel;
        public readonly BaseTransitionModel.AnchorSide ToStateAnchorSide;
        public readonly float ToStateAnchorOffset;

        public CreateStateToStateTransitionAction(BaseStateModel fromStateModel, BaseTransitionModel.AnchorSide fromStateAnchorSide, float fromStateAnchorOffset, BaseStateModel toStateModel, StateToStateTransitionModel.AnchorSide toStateAnchorSide, float toStateAnchorOffset)
        {
            FromStateModel = fromStateModel;
            FromStateAnchorSide = fromStateAnchorSide;
            FromStateAnchorOffset = fromStateAnchorOffset;
            ToStateModel = toStateModel;
            ToStateAnchorSide = toStateAnchorSide;
            ToStateAnchorOffset = toStateAnchorOffset;
            UndoString = "Create State to State Transition";
        }

        public static void DefaultReducer(UnityEditor.GraphToolsFoundation.Overdrive.State previousState, CreateStateToStateTransitionAction action)
        {
            previousState.PushUndo(action);

            var smModel = (StateMachineModel)previousState.GraphModel;
            var newTransition =  smModel.AddTransition(
                action.FromStateModel, action.FromStateAnchorSide, action.FromStateAnchorOffset,
                action.ToStateModel, action.ToStateAnchorSide, action.ToStateAnchorOffset);

            previousState.MarkNew(newTransition);
            previousState.MarkChanged(action.FromStateModel);
            previousState.MarkChanged(action.ToStateModel);
        }
    }

    internal class MoveTransitionAnchorAction : BaseAction
    {
        public readonly BaseTransitionModel TransitionModel;
        public readonly float AnchorOffset;
        public readonly BaseTransitionModel.AnchorSide AnchorSide;
        public readonly StateSide Side;

        public enum StateSide
        {
            FromState,
            ToState
        }

        public MoveTransitionAnchorAction(BaseTransitionModel transitionModel, BaseTransitionModel.AnchorSide anchorSide, float anchorOffset, StateSide stateSide)
        {
            TransitionModel = transitionModel;
            AnchorSide = anchorSide;
            AnchorOffset = anchorOffset;
            Side = stateSide;
            UndoString = "Move Transition Anchor";
        }

        public static void DefaultReducer(UnityEditor.GraphToolsFoundation.Overdrive.State previousState, MoveTransitionAnchorAction action)
        {
            previousState.PushUndo(action);

            if (action.Side == StateSide.FromState)
                action.TransitionModel.UpdateFromStateAnchor(action.AnchorSide, action.AnchorOffset);
            else
                action.TransitionModel.UpdateToStateAnchor(action.AnchorSide, action.AnchorOffset);

            previousState.MarkChanged(action.TransitionModel);
        }
    }


    internal class CreateTargetStateTransitionAction : BaseAction
    {
        public readonly StateMachineModel StateMachine;
        public readonly BaseStateModel State;
        public readonly Model.TransitionType Type;

        public CreateTargetStateTransitionAction(StateMachineModel sm, BaseStateModel state, Model.TransitionType type)
        {
            StateMachine = sm;
            State = state;
            Type = type;
            switch (type)
            {
                case Model.TransitionType.GlobalTransition:
                    UndoString = "Create Global Transition";
                    break;
                case Model.TransitionType.OnEnterSelector:
                    UndoString = "Create On Enter Selector";
                    break;
                case Model.TransitionType.SelfTransition:
                    UndoString = "Create Self Transition";
                    break;
            }
        }

        public static void DefaultReducer(UnityEditor.GraphToolsFoundation.Overdrive.State previousState, CreateTargetStateTransitionAction action)
        {
            previousState.PushUndo(action);

            var graphModel = (StateMachineModel)previousState.GraphModel;
            BaseTransitionModel newTransition = null;
            if (action.Type == Model.TransitionType.GlobalTransition)
                newTransition = new GlobalTransitionModel(){AssetModel = graphModel.AssetModel};
            else if (action.Type == Model.TransitionType.SelfTransition)
                newTransition = new SelfTransitionModel(){AssetModel = graphModel.AssetModel};
            else if (action.Type == Model.TransitionType.OnEnterSelector)
                newTransition = new OnEnterStateSelectorModel(){AssetModel = graphModel.AssetModel};
            if (newTransition == null)
                return;

            newTransition.AssignNewGuid();
            float anchorPos = action.StateMachine.FindAnchorPositionForTargetStateTransition(action.State);
            newTransition.SetPorts(action.State.IncomingTransitionsPort, BaseTransitionModel.AnchorSide.Top, anchorPos, action.State.OutgoingTransitionsPort, BaseTransitionModel.AnchorSide.None, 0.0f);
            action.StateMachine.AddTargetStateTransition(newTransition);

            previousState.MarkChanged(action.State);
            previousState.MarkNew(newTransition);
        }
    }

    internal class CreateStateAction : BaseAction
    {
        public Vector2 Position;
        public StateType Type;
        public UnityEditor.GUID Guid;

        public enum StateType
        {
            GraphState,
            StateMachineState
        }

        public CreateStateAction(Vector2 position, StateType type, UnityEditor.GUID guid)
        {
            Position = position;
            Type = type;
            Guid = guid;
            UndoString = "Create State";
        }

        public static void DefaultReducer(UnityEditor.GraphToolsFoundation.Overdrive.State previousState, CreateStateAction action)
        {
            previousState.PushUndo(action);

            var smModel = (StateMachineModel)previousState.GraphModel;

            IGraphElementModel model;
            if (action.Type == StateType.GraphState)
                model = smModel.CreateNode<GraphStateModel>("Graph", action.Position, action.Guid);
            else //if(action.Type == StateType.StateMachineState)
                model = smModel.CreateNode<StateMachineStateModel>("StateMachine", action.Position, action.Guid);

            previousState.MarkNew(model);
        }
    }
}
