using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class AnimationGraphDragNDropHandler : DragNDropHandler
    {
        public override void HandleDragUpdated(DragUpdatedEvent e, DragNDropContext ctx)
        {
            var animClips = DragAndDrop.objectReferences.OfType<AnimationClip>();
            if (animClips.Any())
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                return;
            }
            base.HandleDragUpdated(e, ctx);
        }

        public override void HandleDragPerform(DragPerformEvent e, Store store, DragNDropContext ctx, VisualElement element)
        {
            var state = store.State;
            var graphModel = state?.GraphModel as BaseGraphModel;
            if (graphModel == null)
                return;
            Vector2 graphSpacePosition = element.WorldToLocal(e.mousePosition);

            var animClips = DragAndDrop.objectReferences.OfType<AnimationClip>();
            var animationClips = animClips.ToList();
            if (animationClips.Count > 0)
            {
                store.Dispatch(new CreateClipsFromAnimationsAction(graphSpacePosition, animationClips, new List<GUID>()));
                return;
            }
            base.HandleDragPerform(e, store, ctx, element);
        }
    }
}
