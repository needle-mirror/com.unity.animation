using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    [GraphElementsExtensionMethodsCache]
    static class GraphElementFactoryExtensions
    {
        public static IGraphElement CreateSubNode(this ElementBuilder builder, Store store, SubGraphNodeModel model)
        {
            var ui = new SubGraphNode();
            ui.SetupBuildAndUpdate(model, store, builder.GraphView, builder.Context);
            return ui;
        }

        public static IGraphElement CreateBaseNode(this ElementBuilder builder, Store store, BaseNodeModel model)
        {
            var ui = new Node();
            ui.SetupBuildAndUpdate(model, store, builder.GraphView, builder.Context);
            return ui;
        }

        public static IGraphElement CreateBasePort(this ElementBuilder elementBuilder, Store store, BasePortModel model)
        {
            var ui = new BasePort();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);

            return ui;
        }

        public static IGraphElement CreateVariableNode(this ElementBuilder builder, Store store, BaseVariableModel model)
        {
            var tokenElement = new TokenNode();
            tokenElement.SetupBuildAndUpdate(model, store, builder.GraphView, builder.Context);

            return tokenElement;
        }

        public static IGraphElement CreateBlackboard(this ElementBuilder elementBuilder, Store store, BlackboardGraphModel model)
        {
            var ui = new Blackboard();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IGraphElement CreateGraphState(this ElementBuilder builder, Store store, GraphStateModel model)
        {
            var ui = new GraphState();
            ui.SetupBuildAndUpdate(model, store, builder.GraphView, builder.Context);
            return ui;
        }

        public static IGraphElement CreateStateMachineState(this ElementBuilder builder, Store store, StateMachineStateModel model)
        {
            var ui = new StateMachineState();
            ui.SetupBuildAndUpdate(model, store, builder.GraphView, builder.Context);
            return ui;
        }

        public static IGraphElement CreateStateToStateTransition(this ElementBuilder builder, Store store, GhostStateToStateTransitionModel model)
        {
            var ui = new StateToStateTransition();
            ui.SetupBuildAndUpdate(model, store, builder.GraphView, builder.Context);
            return ui;
        }

        public static IGraphElement CreateStateToStateTransition(this ElementBuilder builder, Store store, StateToStateTransitionModel model)
        {
            var ui = new StateToStateTransition();
            ui.SetupBuildAndUpdate(model, store, builder.GraphView, builder.Context);
            return ui;
        }

        public static IGraphElement CreateGlobalTransition(this ElementBuilder builder, Store store, GlobalTransitionModel model)
        {
            var ui = new GlobalTransition();
            ui.SetupBuildAndUpdate(model, store, builder.GraphView, builder.Context);
            return ui;
        }

        public static IGraphElement CreateSelfTransition(this ElementBuilder builder, Store store, SelfTransitionModel model)
        {
            var ui = new SelfTransition();
            ui.SetupBuildAndUpdate(model, store, builder.GraphView, builder.Context);
            return ui;
        }

        public static IGraphElement CreateOnEnterSelector(this ElementBuilder builder, Store store, OnEnterStateSelectorModel model)
        {
            var ui = new OnEnterSelector();
            ;
            ui.SetupBuildAndUpdate(model, store, builder.GraphView, builder.Context);
            return ui;
        }
    }
}
