using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class BlackboardValueConditionViewModel : BaseConditionViewModel
    {
        public BlackboardValueConditionViewModel(BlackboardValueConditionModel model, IGraphAssetModel graphAssetModel)
            : base(model, graphAssetModel)
        {
        }

        internal BlackboardValueConditionModel BlackboardValueConditionModel => (BlackboardValueConditionModel)Model;
    }
}
