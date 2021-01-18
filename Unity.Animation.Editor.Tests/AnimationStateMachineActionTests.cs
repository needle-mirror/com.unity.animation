using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;
using Unity.Animation.Model;

namespace Unity.Animation.Editor.Tests
{
    class AnimationStateMachineActionTests : BaseStateMachineFixture
    {
        protected override bool CreateGraphOnStartup => true;

        static bool KeepAssetFiles => false;

        protected override string[] TestAssemblies => new[] { "Unity.Animation.Editor.Nodes.Tests", "Unity.Animation.Editor.Tests" };

        [Test]
        public void CreateStatesAndVerifyAssetCreation()
        {
            var newStateMachineStateModel = GraphModel.CreateNode<StateMachineStateModel>("StateMachine");
            Assert.IsNotNull(newStateMachineStateModel);
            Stencil.CreateAssetFromStateModel(newStateMachineStateModel, GraphModel.AssetModel);
            Assert.IsNotNull(newStateMachineStateModel.StateDefinitionAsset);
            var stateMachineStateAssetPath = AssetDatabase.GetAssetPath(newStateMachineStateModel.StateDefinitionAsset);
            Assert.IsFalse(string.IsNullOrEmpty(stateMachineStateAssetPath));
            if (!KeepAssetFiles)
                CreatedAssetsPath.Add(stateMachineStateAssetPath);

            var newGraphStateModel = GraphModel.CreateNode<GraphStateModel>("Graph");
            Assert.IsNotNull(newGraphStateModel);
            Stencil.CreateAssetFromStateModel(newGraphStateModel, GraphModel.AssetModel);
            Assert.IsNotNull(newGraphStateModel.StateDefinitionAsset);
            var graphStateAssetPath = AssetDatabase.GetAssetPath(newGraphStateModel.StateDefinitionAsset);
            Assert.IsFalse(string.IsNullOrEmpty(graphStateAssetPath));
            if (!KeepAssetFiles)
                CreatedAssetsPath.Add(graphStateAssetPath);
        }

        public enum StateType
        {
            StateMachineState,
            GraphState
        }

        [Test]
        public void DuplicateGraphState_WithoutSubGraphDefinition()
        {
            var newGraphStateModel = GraphModel.CreateNode<GraphStateModel>("Graph");
            Assert.IsNotNull(newGraphStateModel);
            var duplicatedNode = GraphModel.DuplicateNode(newGraphStateModel, Vector2.zero);
            Assert.IsNotNull(duplicatedNode);
            Assert.AreEqual(newGraphStateModel.GetType(), duplicatedNode.GetType());
            Assert.AreNotEqual(newGraphStateModel.Guid, duplicatedNode.Guid);
            Assert.IsNull((duplicatedNode as GraphStateModel).StateDefinitionAsset);
        }

        [Test]
        public void DuplicateGraphState_WithSubGraph()
        {
            var newGraphStateModel = GraphModel.CreateNode<GraphStateModel>("Graph");
            Stencil.CreateAssetFromStateModel(newGraphStateModel, GraphModel.AssetModel);
            var graphStateAssetPath = AssetDatabase.GetAssetPath(newGraphStateModel.StateDefinitionAsset);
            if (!KeepAssetFiles)
                CreatedAssetsPath.Add(graphStateAssetPath);

            var newBaseModel = newGraphStateModel.StateDefinitionAsset.GraphModel as BaseModel;
            var outNodeType = typeof(OutputFloatMessageNode);
            var src = CreateNode(newBaseModel, outNodeType);
            var inNodeType = typeof(InputFloatMessageNode);
            var dst = CreateNode(newBaseModel, inNodeType);
            newGraphStateModel.StateDefinitionAsset.GraphModel.CreateEdge(
                dst.Ports.FirstOrDefault(),
                src.Ports.FirstOrDefault());

            newBaseModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));

            var duplicatedNode = GraphModel.DuplicateNode(newGraphStateModel, Vector2.zero) as GraphStateModel;
            Assert.IsNotNull(duplicatedNode);
            Assert.AreEqual(newGraphStateModel.GetType(), duplicatedNode.GetType());
            Assert.AreNotEqual(newGraphStateModel.Guid, duplicatedNode.Guid);
            Assert.IsNotNull(duplicatedNode.StateDefinitionAsset);
            Assert.AreNotEqual(newGraphStateModel.StateDefinitionAsset, duplicatedNode.StateDefinitionAsset);
            var duplicatedNodeAssetPath = AssetDatabase.GetAssetPath(duplicatedNode.StateDefinitionAsset);
            Assert.IsFalse(string.IsNullOrEmpty(duplicatedNodeAssetPath));
            if (!KeepAssetFiles)
                CreatedAssetsPath.Add(duplicatedNodeAssetPath);

            Assert.AreEqual(newGraphStateModel.StateDefinitionAsset.GraphModel.NodeModels.Count, duplicatedNode.StateDefinitionAsset.GraphModel.NodeModels.Count);
            Assert.AreEqual(newGraphStateModel.StateDefinitionAsset.GraphModel.EdgeModels.Count, duplicatedNode.StateDefinitionAsset.GraphModel.EdgeModels.Count);
            Assert.AreEqual(newGraphStateModel.StateDefinitionAsset.GraphModel.VariableDeclarations.Count, duplicatedNode.StateDefinitionAsset.GraphModel.VariableDeclarations.Count);
            Assert.AreEqual(1, (newGraphStateModel.StateDefinitionAsset.GraphModel as BaseModel).InputComponentBindings.Count);
            Assert.AreEqual((newGraphStateModel.StateDefinitionAsset.GraphModel as BaseModel).InputComponentBindings[0].Name,
                (duplicatedNode.StateDefinitionAsset.GraphModel as BaseModel).InputComponentBindings[0].Name);
            Assert.AreEqual((newGraphStateModel.StateDefinitionAsset.GraphModel as BaseModel).InputComponentBindings[0].Identifier,
                (duplicatedNode.StateDefinitionAsset.GraphModel as BaseModel).InputComponentBindings[0].Identifier);
        }

        [Test]
        public void StateToStateTransition_CreateAction()
        {
            var graphState1 = GraphModel.CreateNode<GraphStateModel>("Graph 1");
            Assert.IsNotNull(graphState1);
            var graphState2 = GraphModel.CreateNode<GraphStateModel>("Graph 2");
            Assert.IsNotNull(graphState2);
            m_Store.Dispatch(new CreateStateToStateTransitionAction(graphState1, BaseTransitionModel.AnchorSide.Right,
                1f,
                graphState2, BaseTransitionModel.AnchorSide.Left, 2f));
            Assert.AreEqual(1, graphState1.OutgoingTransitionsPort.GetConnectedEdges().ToList().Count);

            var transition = graphState1.GetConnectedEdges().First() as StateToStateTransitionModel;
            Assert.IsNotNull(transition);
            Assert.AreEqual(graphState1.OutgoingTransitionsPort, transition.FromPort);
            Assert.AreEqual(graphState2.IncomingTransitionsPort, transition.ToPort);
            Assert.AreEqual(BaseTransitionModel.AnchorSide.Right, transition.FromStateAnchorSide);
            Assert.AreEqual(1f, transition.FromStateAnchorOffset);
            Assert.AreEqual(BaseTransitionModel.AnchorSide.Left, transition.ToStateAnchorSide);
            Assert.AreEqual(2f, transition.ToStateAnchorOffset);
        }

        [Test]
        public void StateToStateTransition_Duplication()
        {
            var graphState1 = GraphModel.CreateNode<GraphStateModel>("Graph 1");
            Assert.IsNotNull(graphState1);
            var graphState2 = GraphModel.CreateNode<GraphStateModel>("Graph 2");
            Assert.IsNotNull(graphState2);
            var transition = GraphModel.AddTransition(
                graphState1, BaseTransitionModel.AnchorSide.Right, 1f,
                graphState2, BaseTransitionModel.AnchorSide.Left, 2f);
            Assert.IsNotNull(transition);

            var duplicatedTransition = GraphModel.DuplicateEdge(transition, graphState1, graphState2);
            Assert.IsNotNull(duplicatedTransition);
            Assert.AreEqual(graphState2.OutgoingTransitionsPort, duplicatedTransition.FromPort);
            Assert.AreEqual(graphState1.IncomingTransitionsPort, duplicatedTransition.ToPort);
            Assert.AreEqual(BaseTransitionModel.AnchorSide.Right, transition.FromStateAnchorSide);
            Assert.AreEqual(1f, transition.FromStateAnchorOffset);
            Assert.AreEqual(BaseTransitionModel.AnchorSide.Left, transition.ToStateAnchorSide);
            Assert.AreEqual(2f, transition.ToStateAnchorOffset);
        }

        [Test]
        public void StateToStateTransition_Duplication_WithConditions()
        {
            var graphState1 = GraphModel.CreateNode<GraphStateModel>("Graph 1");
            Assert.IsNotNull(graphState1);
            var graphState2 = GraphModel.CreateNode<GraphStateModel>("Graph 2");
            Assert.IsNotNull(graphState2);
            var transition = GraphModel.AddTransition(
                graphState1, BaseTransitionModel.AnchorSide.Right, 1f,
                graphState2, BaseTransitionModel.AnchorSide.Left, 2f);
            Assert.IsNotNull(transition);

            transition.TransitionProperties.Condition.InsertCondition(
                new GroupConditionModel()
                {
                    GroupOperation = GroupConditionModel.Operation.And,
                    width = GroupConditionModel.DefaultWidth,
                    height = GroupConditionModel.DefaultHeight
                }
            );
            (transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).InsertCondition(
                new GroupConditionModel()
                {
                    GroupOperation = GroupConditionModel.Operation.Or,
                    width = GroupConditionModel.DefaultWidth,
                    height = GroupConditionModel.DefaultHeight
                }
            );
            ((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] as GroupConditionModel).InsertCondition(
                new ElapsedTimeConditionModel()
                {
                    width = ElapsedTimeConditionModel.DefaultWidth,
                    height = ElapsedTimeConditionModel.DefaultHeight
                }
            );
            var sourceElapsedTime = new ElapsedTimeConditionModel()
            {
                width = ElapsedTimeConditionModel.DefaultWidth,
                height = ElapsedTimeConditionModel.DefaultHeight
            };
            transition.TransitionProperties.Condition.InsertCondition(sourceElapsedTime);

            var duplicatedTransition = GraphModel.DuplicateEdge(transition, graphState1, graphState2) as StateToStateTransitionModel;
            Assert.IsNotNull(duplicatedTransition);

            Assert.AreEqual(2, duplicatedTransition.TransitionProperties.Condition.ListSubConditions.Count);
            Assert.IsTrue(duplicatedTransition.TransitionProperties.Condition.ListSubConditions[0] is GroupConditionModel);
            Assert.AreEqual(GroupConditionModel.Operation.And, (duplicatedTransition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).GroupOperation);
            Assert.IsTrue(duplicatedTransition.TransitionProperties.Condition.ListSubConditions[1] is ElapsedTimeConditionModel);

            Assert.AreEqual(1, (transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions.Count);
            Assert.IsTrue((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] is GroupConditionModel);
            Assert.AreEqual(GroupConditionModel.Operation.Or, ((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] as GroupConditionModel).GroupOperation);

            Assert.AreEqual(1, ((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] as GroupConditionModel).ListSubConditions.Count);
            Assert.IsTrue(((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] is ElapsedTimeConditionModel);
            var duplicatedElaspedTime =
                ((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel)
                    .ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] as ElapsedTimeConditionModel;
            duplicatedElaspedTime.TimeElapsed = 2;
            Assert.AreNotEqual(sourceElapsedTime.TimeElapsed, duplicatedElaspedTime.TimeElapsed);
        }

        [Test]
        public void TargetStateTransition_GlobalTransition_Create()
        {
            var graphState = GraphModel.CreateNode<GraphStateModel>("Graph");
            Assert.IsNotNull(graphState);
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, graphState, Model.TransitionType.GlobalTransition));
            Assert.AreEqual(1, graphState.OutgoingTransitionsPort.GetConnectedEdges().ToList().Count);

            Assert.AreEqual(1, GraphModel.EdgeModels.Count);
            Assert.IsTrue(GraphModel.EdgeModels[0] is GlobalTransitionModel);
            Assert.AreEqual(graphState, GraphModel.EdgeModels[0].FromPort.NodeModel);
            Assert.AreEqual(graphState, GraphModel.EdgeModels[0].ToPort.NodeModel);
        }

        [Test]
        public void TargetStateTransition_SelfTransition_Create()
        {
            var graphState = GraphModel.CreateNode<GraphStateModel>("Graph");
            Assert.IsNotNull(graphState);
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, graphState, Model.TransitionType.SelfTransition));
            Assert.AreEqual(1, graphState.OutgoingTransitionsPort.GetConnectedEdges().ToList().Count);

            Assert.AreEqual(1, GraphModel.EdgeModels.Count);
            Assert.IsTrue(GraphModel.EdgeModels[0] is SelfTransitionModel);
            Assert.AreEqual(graphState, GraphModel.EdgeModels[0].FromPort.NodeModel);
            Assert.AreEqual(graphState, GraphModel.EdgeModels[0].ToPort.NodeModel);
        }

        [Test]
        public void TargetStateTransition_EnterTransition_Create()
        {
            var graphState = GraphModel.CreateNode<GraphStateModel>("Graph");
            Assert.IsNotNull(graphState);
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, graphState, Model.TransitionType.OnEnterSelector));
            Assert.AreEqual(1, graphState.OutgoingTransitionsPort.GetConnectedEdges().ToList().Count);

            Assert.AreEqual(1, GraphModel.EdgeModels.Count);
            Assert.IsTrue(GraphModel.EdgeModels[0] is OnEnterStateSelectorModel);
            Assert.AreEqual(graphState, GraphModel.EdgeModels[0].FromPort.NodeModel);
            Assert.AreEqual(graphState, GraphModel.EdgeModels[0].ToPort.NodeModel);
        }

        [Test]
        public void ConditionEditor_AddConditions_Breadth()
        {
            var graphState1 = GraphModel.CreateNode<GraphStateModel>("Graph 1");
            Assert.IsNotNull(graphState1);
            var graphState2 = GraphModel.CreateNode<GraphStateModel>("Graph 2");
            Assert.IsNotNull(graphState2);
            var transition = GraphModel.AddTransition(
                graphState1, BaseTransitionModel.AnchorSide.Right, 0f,
                graphState2, BaseTransitionModel.AnchorSide.Left, 0f);
            Assert.IsNotNull(transition);

            Assert.AreEqual(0, transition.TransitionProperties.Condition.ListSubConditions.Count);
            transition.TransitionProperties.Condition.InsertCondition(
                new GroupConditionModel()
                {
                    GroupOperation = GroupConditionModel.Operation.And,
                    width = GroupConditionModel.DefaultWidth,
                    height = GroupConditionModel.DefaultHeight
                }
            );
            Assert.AreEqual(1, transition.TransitionProperties.Condition.ListSubConditions.Count);
            Assert.IsTrue(transition.TransitionProperties.Condition.ListSubConditions[0] is GroupConditionModel);
            Assert.AreEqual(GroupConditionModel.Operation.And, (transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).GroupOperation);
            transition.TransitionProperties.Condition.InsertCondition(
                new GroupConditionModel()
                {
                    GroupOperation = GroupConditionModel.Operation.Or,
                    width = GroupConditionModel.DefaultWidth,
                    height = GroupConditionModel.DefaultHeight
                }
            );
            Assert.AreEqual(2, transition.TransitionProperties.Condition.ListSubConditions.Count);
            Assert.IsTrue(transition.TransitionProperties.Condition.ListSubConditions[1] is GroupConditionModel);
            Assert.AreEqual(GroupConditionModel.Operation.Or, (transition.TransitionProperties.Condition.ListSubConditions[1] as GroupConditionModel).GroupOperation);
            transition.TransitionProperties.Condition.InsertCondition(
                new ElapsedTimeConditionModel()
                {
                    width = ElapsedTimeConditionModel.DefaultWidth,
                    height = ElapsedTimeConditionModel.DefaultHeight
                }
            );
            Assert.AreEqual(3, transition.TransitionProperties.Condition.ListSubConditions.Count);
            Assert.IsTrue(transition.TransitionProperties.Condition.ListSubConditions[2] is ElapsedTimeConditionModel);
        }

        [Test]
        public void ConditionEditor_AddConditions_Depth()
        {
            var graphState1 = GraphModel.CreateNode<GraphStateModel>("Graph 1");
            Assert.IsNotNull(graphState1);
            var graphState2 = GraphModel.CreateNode<GraphStateModel>("Graph 2");
            Assert.IsNotNull(graphState2);
            var transition = GraphModel.AddTransition(
                graphState1, BaseTransitionModel.AnchorSide.Right, 0f,
                graphState2, BaseTransitionModel.AnchorSide.Left, 0f);
            Assert.IsNotNull(transition);
            Assert.AreEqual(0, transition.TransitionProperties.Condition.ListSubConditions.Count);

            transition.TransitionProperties.Condition.InsertCondition(
                new GroupConditionModel()
                {
                    GroupOperation = GroupConditionModel.Operation.And,
                    width = GroupConditionModel.DefaultWidth,
                    height = GroupConditionModel.DefaultHeight
                }
            );
            Assert.AreEqual(1, transition.TransitionProperties.Condition.ListSubConditions.Count);
            Assert.IsTrue(transition.TransitionProperties.Condition.ListSubConditions[0] is GroupConditionModel);
            Assert.AreEqual(0, (transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions.Count);
            Assert.AreEqual(GroupConditionModel.Operation.And, (transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).GroupOperation);

            (transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).InsertCondition(
                new GroupConditionModel()
                {
                    GroupOperation = GroupConditionModel.Operation.Or,
                    width = GroupConditionModel.DefaultWidth,
                    height = GroupConditionModel.DefaultHeight
                }
            );
            Assert.AreEqual(1, transition.TransitionProperties.Condition.ListSubConditions.Count);
            Assert.AreEqual(1, (transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions.Count);
            Assert.IsTrue((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] is GroupConditionModel);
            Assert.AreEqual(GroupConditionModel.Operation.Or, ((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] as GroupConditionModel).GroupOperation);

            ((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] as GroupConditionModel).InsertCondition(
                new ElapsedTimeConditionModel()
                {
                    width = ElapsedTimeConditionModel.DefaultWidth,
                    height = ElapsedTimeConditionModel.DefaultHeight
                }
            );
            Assert.AreEqual(1, ((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] as GroupConditionModel).ListSubConditions.Count);
            Assert.IsTrue(((transition.TransitionProperties.Condition.ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] as GroupConditionModel).ListSubConditions[0] is ElapsedTimeConditionModel);
        }

        [Test]
        public void ConditionEditor_ReorderConditions()
        {
            var graphState1 = GraphModel.CreateNode<GraphStateModel>("Graph 1");
            Assert.IsNotNull(graphState1);
            var graphState2 = GraphModel.CreateNode<GraphStateModel>("Graph 2");
            Assert.IsNotNull(graphState2);
            var transition = GraphModel.AddTransition(
                graphState1, BaseTransitionModel.AnchorSide.Right, 0f,
                graphState2, BaseTransitionModel.AnchorSide.Left, 0f);
            Assert.IsNotNull(transition);
            Assert.AreEqual(0, transition.TransitionProperties.Condition.ListSubConditions.Count);

            var timeCondition1 = new ElapsedTimeConditionModel()
            {
                width = ElapsedTimeConditionModel.DefaultWidth,
                height = ElapsedTimeConditionModel.DefaultHeight
            };
            var timeCondition2 = new ElapsedTimeConditionModel()
            {
                width = ElapsedTimeConditionModel.DefaultWidth,
                height = ElapsedTimeConditionModel.DefaultHeight
            };
            transition.TransitionProperties.Condition.InsertCondition(timeCondition1);
            transition.TransitionProperties.Condition.InsertCondition(timeCondition2);
            Assert.AreEqual(2, transition.TransitionProperties.Condition.ListSubConditions.Count);
            Assert.AreEqual(timeCondition1, transition.TransitionProperties.Condition.ListSubConditions[0]);
            Assert.AreEqual(timeCondition2, transition.TransitionProperties.Condition.ListSubConditions[1]);

            var movedConditions = new List<BaseConditionModel>() { timeCondition2 };
            transition.TransitionProperties.Condition.MoveConditions(movedConditions, 0);

            Assert.AreEqual(timeCondition2, transition.TransitionProperties.Condition.ListSubConditions[0]);
            Assert.AreEqual(timeCondition1, transition.TransitionProperties.Condition.ListSubConditions[1]);
        }

        [Test]
        public void CreateEachIndividualTargetStateTransition_OnEachStateType([Values] StateType stateType, [Values] Model.TransitionType type)
        {
            BaseStateModel newStateModel = null;
            if (stateType == StateType.StateMachineState)
                newStateModel = GraphModel.CreateNode<StateMachineStateModel>("StateMachine");
            else if (stateType == StateType.GraphState)
                newStateModel = GraphModel.CreateNode<GraphStateModel>("Graph");
            Assert.IsNotNull(newStateModel);

            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateModel, type));
//            Assert.AreEqual(1, newStateModel.SelfTransitions.Count);
//            Assert.IsTrue((type == Model.TransitionType.GlobalTransition && newStateModel.SelfTransitions[0] is GlobalTransitionModel)
//                || (type == Model.TransitionType.LoopingTransition && newStateModel.SelfTransitions[0] is LoopTransitionModel)
//                || (type == Model.TransitionType.OnEnterSelector && newStateModel.SelfTransitions[0] is OnEnterStateSelectorModel));
//            Undo.IncrementCurrentGroup();
//
//            Undo.PerformUndo();
//            newStateModel = GraphModel.NodeModels[0] as BaseStateModel;
//            Assert.IsNotNull(newStateModel);
//            Assert.AreEqual(0, newStateModel.SelfTransitions.Count);
//            Undo.PerformRedo();
//            newStateModel = GraphModel.NodeModels[0] as BaseStateModel;
//            Assert.IsNotNull(newStateModel);
//            Assert.AreEqual(1, newStateModel.SelfTransitions.Count);
        }

        [Test]
        public void CreateMultipleTargetStateTransition()
        {
            var newStateMachineStateModel = GraphModel.CreateNode<StateMachineStateModel>("StateMachine");

            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.GlobalTransition));
            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.SelfTransition));
            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.OnEnterSelector));
            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.OnEnterSelector));
            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.SelfTransition));
            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.GlobalTransition));
            Undo.IncrementCurrentGroup();
//            Assert.AreEqual(6, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is GlobalTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[4] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[5] is GlobalTransitionModel);
//            Undo.IncrementCurrentGroup();
//
//            Undo.PerformUndo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(5, newStateMachineStateModel.SelfTransitions.Count);
//            Undo.PerformUndo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(4, newStateMachineStateModel.SelfTransitions.Count);
//            Undo.PerformUndo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(3, newStateMachineStateModel.SelfTransitions.Count);
//            Undo.PerformUndo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(2, newStateMachineStateModel.SelfTransitions.Count);
//            Undo.PerformUndo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(1, newStateMachineStateModel.SelfTransitions.Count);
//            Undo.PerformUndo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(0, newStateMachineStateModel.SelfTransitions.Count);
//
//            Undo.PerformRedo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(1, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is GlobalTransitionModel);
//            Undo.PerformRedo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(2, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is LoopTransitionModel);
//            Undo.PerformRedo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(3, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Undo.PerformRedo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(4, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is OnEnterStateSelectorModel);
//            Undo.PerformRedo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(5, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[4] is LoopTransitionModel);
//            Undo.PerformRedo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(6, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[5] is GlobalTransitionModel);
        }

        [Test]
        public void RemoveTargetStateTransition()
        {
            var newStateMachineStateModel = GraphModel.CreateNode<StateMachineStateModel>("StateMachine");

            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.GlobalTransition));
            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.SelfTransition));
            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.OnEnterSelector));
            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.OnEnterSelector));
            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.SelfTransition));
            Undo.IncrementCurrentGroup();
            m_Store.Dispatch(new CreateTargetStateTransitionAction(GraphModel, newStateMachineStateModel, Model.TransitionType.GlobalTransition));
            Undo.IncrementCurrentGroup();
//            Assert.AreEqual(6, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is GlobalTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[4] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[5] is GlobalTransitionModel);
//
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            //remove first
//            m_Store.Dispatch(new RemoveSelfTransitionAction(GraphModel, newStateMachineStateModel, new List<int>(){0}));
//            Assert.AreEqual(5, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[4] is GlobalTransitionModel);
//            Undo.IncrementCurrentGroup();
//
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            // remove last
//            m_Store.Dispatch(new RemoveSelfTransitionAction(GraphModel, newStateMachineStateModel, new List<int>(){4}));
//            Assert.AreEqual(4, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is LoopTransitionModel);
//            Undo.IncrementCurrentGroup();
//
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            // remove invalid index does nothing
//            m_Store.Dispatch(new RemoveSelfTransitionAction(GraphModel, newStateMachineStateModel, new List<int>(){4}));
//            Assert.AreEqual(4, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is LoopTransitionModel);
//            Undo.IncrementCurrentGroup();
//
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            // remove multiple in invalid order
//            m_Store.Dispatch(new RemoveSelfTransitionAction(GraphModel, newStateMachineStateModel, new List<int>(){1, 0, 3}));
//            Assert.AreEqual(1, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is OnEnterStateSelectorModel);
//            Undo.IncrementCurrentGroup();
//
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            // remove multiple including last one and invalid index
//            m_Store.Dispatch(new RemoveSelfTransitionAction(GraphModel, newStateMachineStateModel, new List<int>(){0, 99}));
//            Assert.AreEqual(0, newStateMachineStateModel.SelfTransitions.Count);
//            Undo.IncrementCurrentGroup();
//
//            Undo.PerformUndo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(1, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is OnEnterStateSelectorModel);
//
//            Undo.PerformUndo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(4, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is LoopTransitionModel);
//
//            Undo.PerformUndo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(5, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[4] is GlobalTransitionModel);
//
//            Undo.PerformUndo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(6, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is GlobalTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[4] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[5] is GlobalTransitionModel);
//
//            Undo.PerformRedo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(5, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[4] is GlobalTransitionModel);
//
//            Undo.PerformRedo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(4, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is LoopTransitionModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[1] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[2] is OnEnterStateSelectorModel);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[3] is LoopTransitionModel);
//
//            Undo.PerformRedo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(1, newStateMachineStateModel.SelfTransitions.Count);
//            Assert.IsTrue(newStateMachineStateModel.SelfTransitions[0] is OnEnterStateSelectorModel);
//
//            Undo.PerformRedo();
//            newStateMachineStateModel = GraphModel.NodeModels[0] as StateMachineStateModel;
//            Assert.IsNotNull(newStateMachineStateModel);
//            Assert.AreEqual(0, newStateMachineStateModel.SelfTransitions.Count);
        }
    }
}
