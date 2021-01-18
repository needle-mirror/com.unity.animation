using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    class SubNode<TGraphAsset> : BaseNode
        where TGraphAsset : GraphAssetModel
    {
        protected SubNodeModel<TGraphAsset> SubGraphModel => (NodeModel as SubNodeModel<TGraphAsset>);

        public static readonly string clickablePartName = "clickable";

        public SubNode()
        {
            AddToClassList("macro");
        }

        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(ClickableNodePart.Create(clickablePartName, Model, this, ussClassName, OpenSubGraph));
        }

        void OpenSubGraph()
        {
            if (SubGraphModel.GraphAsset != null)
            {
                Store.Dispatch(new LoadGraphAssetAction(SubGraphModel.GraphAsset.GetPath(), loadType: LoadGraphAssetAction.Type.PushOnStack));
            }
            else
            {
                var graphAssetName = $"{(NodeModel as NodeModel).Title}.asset";
                var path = EditorUtility.SaveFilePanelInProject(
                    "Create Animation Graph",
                    graphAssetName,
                    "asset", "Create a new Animation Graph");

                if (path.Length != 0)
                {
                    string modelName = System.IO.Path.GetFileName(path);

                    GraphAssetCreationHelpers<TGraphAsset>.CreateGraphAsset(SubGraphModel.StencilType, modelName, path);
                    SubGraphModel.GraphAsset = (TGraphAsset)Store.State.AssetModel;
                }
            }
        }
    }
}
