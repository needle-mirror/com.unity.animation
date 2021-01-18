using System.Collections.Generic;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class CreateClipsFromAnimationsAction : BaseAction
    {
        public Vector2 Position;
        public List<GUID> ClipGuids;
        public List<AnimationClip> Clips;

        public CreateClipsFromAnimationsAction(Vector2 position, IEnumerable<AnimationClip> animClips, List<GUID> guids)
        {
            Position = position;
            ClipGuids = new List<GUID>(guids);
            Clips = new List<AnimationClip>(animClips);

            int nbrGuids = ClipGuids.Count;
            for (int i = nbrGuids; i < Clips.Count; ++i)
            {
                ClipGuids.Add(GUID.Generate());
            }
            UndoString = "Create Clips from Animations";
        }

        public static void DefaultReducer(UnityEditor.GraphToolsFoundation.Overdrive.State previousState, CreateClipsFromAnimationsAction action)
        {
            previousState.PushUndo(action);

            var graphModel = (BaseGraphModel)previousState.GraphModel;
            Vector2 currentPos = action.Position;
            var listAnimations = new List<AnimationClipNodeModel>();
            for (int i = 0; i < action.Clips.Count; ++i)
            {
                var animationNode = graphModel.CreateNode<AnimationClipNodeModel>(position: currentPos, guid: action.ClipGuids[i]);
                animationNode.SetClip(action.Clips[i]);
                listAnimations.Add(animationNode);

                currentPos.y += 100;
            }

            previousState.SelectionStateComponent?.SelectElementsUponCreation(listAnimations, true);
            previousState.MarkNew(listAnimations);
        }
    }
}
