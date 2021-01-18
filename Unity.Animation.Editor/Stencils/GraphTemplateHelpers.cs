using System.IO;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Store = UnityEditor.GraphToolsFoundation.Overdrive.Store;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal static class GraphTemplateHelpers
    {
        public static void PromptToCreateGraph(this CreateGraphTemplate template, Store store, bool useSelection)
        {
            if (useSelection)
            {
                template.CurrentGameObject = Selection.activeGameObject;
            }

            var graphAsset = GraphAssetCreationHelpers<BaseGraphAssetModel>.PromptToCreate(
                template,
                "Create Graph",
                "Create a new graph for " + template.GraphTypeName,
                "asset");

            if (store != null && graphAsset != null)
            {
                store.Dispatch(new LoadGraphAssetAction(graphAsset));
            }
        }

        public static void PromptToCreateStateMachine(this CreateStateMachineTemplate template, Store store, bool useSelection)
        {
            if (useSelection)
            {
                template.CurrentGameObject = Selection.activeGameObject;
            }

            var graphAsset = GraphAssetCreationHelpers<StateMachineAsset>.PromptToCreate(
                template,
                "Create State Machine",
                "Create a new state machine for " + template.GraphTypeName,
                "asset");

            if (store != null && graphAsset != null)
            {
                store.Dispatch(new LoadGraphAssetAction(graphAsset));
            }
        }

        public static void CreateGraphAsset<TAssetType>(ICreatableGraphTemplate template)
            where TAssetType : GraphAssetModel
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == string.Empty)
                path = "Assets";
            else if (Path.GetExtension(path) != string.Empty)
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), string.Empty);

            Store store = null;
            if (EditorWindow.HasOpenInstances<AnimationGraphWindow>())
            {
                store = EditorWindow.GetWindow<AnimationGraphWindow>()?.Store;
            }

            GraphAssetCreationHelpers<TAssetType>.CreateInProjectWindow(template, store, path);
        }
    }
}
