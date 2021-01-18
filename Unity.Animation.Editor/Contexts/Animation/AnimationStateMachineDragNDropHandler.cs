using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class AnimationStateMachineDragNDropHandler : IExternalDragNDropHandler
    {
        public void HandleDragUpdated(DragUpdatedEvent e, DragNDropContext ctx)
        {
            var animClips = DragAndDrop.objectReferences.OfType<AnimationClip>();
            DragAndDrop.visualMode = animClips.Any() ? DragAndDropVisualMode.Link : DragAndDropVisualMode.None;
        }

        public void HandleDragPerform(DragPerformEvent e, Store store, DragNDropContext ctx, VisualElement element)
        {
            var state = store.State;
            var smModel = state?.GraphModel as StateMachineModel;
            if (smModel == null)
                return;
            Vector2 graphSpacePosition = element.WorldToLocal(e.mousePosition);

            var animClips = DragAndDrop.objectReferences.OfType<AnimationClip>();
            var animationClips = animClips.ToList();
            if (animationClips.Count > 0)
            {
                store.Dispatch(new CreateStatesFromAnimationsAction(graphSpacePosition, animationClips, new List<GUID>()));
            }
        }
    }
}
