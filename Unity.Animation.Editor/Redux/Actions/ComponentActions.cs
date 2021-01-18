using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Hybrid;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    static class ComponentReducers
    {
        public static void Register(Store store)
        {
            CreateInputComponentAction.Register(store);
            UpdateComponentNameAction.Register(store);
            RemoveComponentAction.Register(store);
        }
    }

    internal class CreateInputComponentAction : BaseAction
    {
        public InputData[] InputsToCreate;

        internal class InputData
        {
            public string Name;
            public readonly Type Type;
            public readonly Component Value;

            public InputData(string name, Type type, Component value = null)
            {
                Name = name;
                Type = type;
                Value = value;
            }
        }

        internal static void Register(Store store)
        {
            store.RegisterReducer<UnityEditor.GraphToolsFoundation.Overdrive.State, CreateInputComponentAction>(DefaultReducer);
        }

        static void DefaultReducer(UnityEditor.GraphToolsFoundation.Overdrive.State previousState, CreateInputComponentAction action)
        {
            var graphModel = previousState.GraphModel as BaseModel;
            previousState.PushUndo(action);

            foreach (var b in action.InputsToCreate)
            {
                var input = graphModel.AddComponentBinding(b.Name, b.Type);

                if (b.Value != null && previousState.WindowState.CurrentGraph.BoundObject is GameObject go)
                {
                    var animGraph = go.GetComponent<AnimationGraph>();
                    if (animGraph != null)
                    {
                        animGraph.Inputs.Add(
                            new AnimationGraph.InputBindingEntry()
                            {
                                Identification = input.Identifier.Type.Identification,
                                Value = b.Value
                            });
                        animGraph.UpdateBindings();
                    }
                }
            }

            previousState.RequestUIRebuild();
        }

        public CreateInputComponentAction(InputData[] inputsToCreate)
        {
            InputsToCreate = inputsToCreate;
            UndoString = "Create Blackboard Input";
        }
    }

    internal class RemoveComponentAction : BaseAction
    {
        public readonly ComponentBindingIdentifier ComponentId;

        internal static void Register(Store store)
        {
            store.RegisterReducer<UnityEditor.GraphToolsFoundation.Overdrive.State, RemoveComponentAction>(DefaultReducer);
        }

        static void DefaultReducer(UnityEditor.GraphToolsFoundation.Overdrive.State previousState, RemoveComponentAction action)
        {
            var graphModel = previousState.GraphModel as BaseModel;

            previousState.PushUndo(action);

            graphModel.DeleteVariableDeclarations(
                graphModel.GetComponentVariableDeclarations().Where(p => p.Identifier == action.ComponentId).ToArray(), true);

            graphModel.RemoveComponentBinding(action.ComponentId);

            if (previousState.WindowState.CurrentGraph.BoundObject is GameObject go && go != null)
            {
                var animGraph = go.GetComponent<AnimationGraph>();
                if (animGraph != null)
                {
                    animGraph.UpdateBindings();
                }
            }

            previousState.RequestUIRebuild();
        }

        public RemoveComponentAction(ComponentBindingIdentifier componentId)
        {
            ComponentId = componentId;
            UndoString = "Remove Blackboard Input";
        }
    }

    internal class UpdateComponentNameAction : BaseAction
    {
        public readonly ComponentBindingIdentifier ComponentId;
        public string Name;

        public UpdateComponentNameAction(ComponentBindingIdentifier input, string name)
        {
            ComponentId = input;
            Name = name;
            UndoString = "Update Blackboard Input Name";
        }

        internal static void Register(Store store)
        {
            store.RegisterReducer<UnityEditor.GraphToolsFoundation.Overdrive.State, UpdateComponentNameAction>(DefaultReducer);
        }

        static void DefaultReducer(UnityEditor.GraphToolsFoundation.Overdrive.State previousState, UpdateComponentNameAction action)
        {
            var graphModel = previousState.GraphModel as BaseModel;
            previousState.PushUndo(action);

            if (graphModel.GetComponentBinding(action.Name) == null)
            {
                if (graphModel.TryGetComponentBinding(action.ComponentId, out var component))
                {
                    component.Name = action.Name;

                    var componentVariables = graphModel.NodeModels.OfType<InputComponentFieldVariableModel>()
                        .Where(v => v.DeclarationModel != null &&
                            v.DeclarationModel is InputComponentFieldVariableDeclarationModel inputDecl &&
                            inputDecl.Identifier == action.ComponentId);

                    foreach (var p in componentVariables)
                        p.UpdateNameFromDeclaration();
                }
            }
            else
                Debug.LogWarning($"Component Binding with name {action.Name} already exists.");

            previousState.RequestUIRebuild();
        }
    }
}
