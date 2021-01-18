using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class BaseConditionViewModel
    {
        internal BaseConditionModel Model { get; }
        internal IGraphAssetModel AssetModel { get; }
        public BaseConditionViewModel(BaseConditionModel model, IGraphAssetModel graphAssetModel)
        {
            Model = model;
            AssetModel = graphAssetModel;
        }

        public BaseConditionViewModel Parent { get; set; }

        public void SetItemBoundary(float posX, float posY, float width, float height)
        {
            Model.posX = posX;
            Model.posY = posY;
            Model.width = width;
            Model.height = height;
        }

        public float posX => Model.posX;
        public float posY => Model.posY;
        public float width => Model.width;
        public float height => Model.height;
    }
}
