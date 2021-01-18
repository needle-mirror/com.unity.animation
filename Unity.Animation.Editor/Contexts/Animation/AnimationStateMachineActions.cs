using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class CreateStatesFromAnimationsAction : BaseAction
    {
        public Vector2 Position;
        public List<GUID> StateGuids;
        public List<AnimationClip> Clips;

        public CreateStatesFromAnimationsAction(Vector2 position, IEnumerable<AnimationClip> animClips, List<GUID> guids)
        {
            Position = position;
            StateGuids = new List<GUID>(guids);
            Clips = new List<AnimationClip>(animClips);

            int nbrGuids = StateGuids.Count;
            for (int i = nbrGuids; i < Clips.Count; ++i)
            {
                StateGuids.Add(GUID.Generate());
            }
            UndoString = "Create States from Animations";
        }

        public static void DefaultReducer(UnityEditor.GraphToolsFoundation.Overdrive.State previousState, CreateStatesFromAnimationsAction action)
        {
            previousState.PushUndo(action);

            var smModel = (StateMachineModel)previousState.GraphModel;
            var listStates = new List<GraphStateModel>();
            Vector2 currentPos = action.Position;
            for (int i = 0; i < action.Clips.Count; ++i)
            {
                var newState = smModel.CreateNode<GraphStateModel>("Graph", currentPos, action.StateGuids[i]);
                newState.Title = action.Clips[i].name;
                newState.CreateDefinitionAsset();
                var graphModel = newState.StateDefinitionAsset.GraphModel as BaseGraphModel;
                var outputNode = graphModel.NodeModels.OfType<AnimationOutputNodeModel>().FirstOrDefault();
                var outputNodeInputPort = outputNode.Ports.FirstOrDefault(x => x.UniqueName == AnimationOutputNodeModel.k_PoseResultPortName);

                var animationNode = graphModel.CreateNode<AnimationClipNodeModel>(position: outputNode.Position - new Vector2(400, 0));
                animationNode.SetClip(action.Clips[i]);
                var animationNodeOutputPort = animationNode.Ports.FirstOrDefault(x => x.UniqueName == AnimationClipNodeModel.k_PosePortName);
                graphModel.CreateEdge(outputNodeInputPort, animationNodeOutputPort);

                listStates.Add(newState);
                currentPos.x += 800;
            }

            previousState.SelectionStateComponent?.SelectElementsUponCreation(listStates, true);
            previousState.MarkNew(listStates);
        }
    }
}
