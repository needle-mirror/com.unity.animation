using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Hybrid;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class DragNDropHandler : IExternalDragNDropHandler
    {
        public virtual void HandleDragUpdated(DragUpdatedEvent e, DragNDropContext ctx)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }

        public virtual void HandleDragPerform(DragPerformEvent e, Store store, DragNDropContext ctx, VisualElement element)
        {
            var state = store.State;
            if (state?.GraphModel == null)
                return;
            var graphModel = state.GraphModel as BaseModel;
            var stencil = (BaseGraphStencil)state.GraphModel.Stencil;

            if (state.WindowState.CurrentGraph.BoundObject == null)
            {
                Debug.LogError("Cannot create object references when a graph is opened in asset mode. Select a game object referencing this graph to do that.");
                return;
            }

            Vector2 graphSpacePosition = element.WorldToLocal(e.mousePosition);

            var gameObjects = DragAndDrop.objectReferences.OfType<GameObject>();
            var inputs = new List<CreateInputComponentAction.InputData>();

            foreach (var go in gameObjects)
            {
                foreach (var component in go.GetComponents(typeof(MonoBehaviour)))
                {
                    if (component == null)
                        continue;
                    if (AuthoringComponentService.TryGetComponentByAuthoringType(component.GetType(), out var componentInfo))
                    {
                        if (graphModel.GetComponentBinding(componentInfo.RuntimeType) == null)
                            inputs.Add(
                                new CreateInputComponentAction.InputData(
                                    componentInfo.RuntimeType.Name,
                                    componentInfo.RuntimeType,
                                    component));
                    }
                }
            }

            if (inputs.Any())
                store.Dispatch(new CreateInputComponentAction(inputs.ToArray()));

            var animGraph = (state.WindowState.CurrentGraph.BoundObject as GameObject).GetComponent<AnimationGraph>();
            if (animGraph != null)
                animGraph.UpdateBindings();
        }
    }
}
