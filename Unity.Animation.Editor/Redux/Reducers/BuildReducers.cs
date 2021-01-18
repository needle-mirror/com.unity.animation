using System.Linq;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    static class BuildReducers
    {
        public static void Register(Store store)
        {
            store.RegisterReducer<BuildAllEditorAction>(BuildAllEditor);
        }

        static void BuildAllEditor(UnityEditor.GraphToolsFoundation.Overdrive.State previousState, BuildAllEditorAction action)
        {
            BuildAll();
            previousState.RequestUIRebuild();
        }

        public static void BuildAll()
        {
            var assetGUIDs = AssetDatabase.FindAssets($"t:{typeof(BaseAssetModel).FullName}");

            var assetsByBuilder = assetGUIDs.Select(
                assetGuid => AssetDatabase.LoadAssetAtPath<BaseAssetModel>(AssetDatabase.GUIDToAssetPath(assetGuid)))
                .Where(asset => asset != null && asset.GraphModel?.Stencil is IBuilderProvider)
                .GroupBy(asset => (asset.GraphModel.Stencil as IBuilderProvider)?.Builder);
            foreach (IGrouping<IBuilder, BaseAssetModel> grouping in assetsByBuilder)
            {
                if (grouping.Key == null)
                    continue;
                var builder = grouping.Key;
                builder.Build(grouping.ToList());
            }
        }
    }
}
