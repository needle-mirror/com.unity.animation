using Unity.Animation.Model;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    internal class GroupConditionViewModel : BaseConditionViewModel
    {
        internal GroupConditionModel GroupConditionModel => (GroupConditionModel)Model;
        public bool IsAndGroup => GroupConditionModel.GroupOperation == GroupConditionModel.Operation.And;
        public GroupConditionViewModel(GroupConditionModel model, IGraphAssetModel graphAssetModel)
            : base(model, graphAssetModel)
        {
        }
    }
}
