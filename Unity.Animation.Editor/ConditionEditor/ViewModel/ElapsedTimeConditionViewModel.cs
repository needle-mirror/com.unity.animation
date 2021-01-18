using Unity.Animation.Model;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    internal class ElapsedTimeConditionViewModel : BaseConditionViewModel
    {
        public ElapsedTimeConditionViewModel(ElapsedTimeConditionModel model, IGraphAssetModel graphAssetModel)
            : base(model, graphAssetModel)
        {
        }

        internal ElapsedTimeConditionModel ElapsedTimeConditionModel => (ElapsedTimeConditionModel)Model;
    }
}
