using Unity.Animation.Model;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    internal class EvaluationRatioConditionViewModel : BaseConditionViewModel
    {
        public EvaluationRatioConditionViewModel(EvaluationRatioConditionModel model, IGraphAssetModel graphAssetModel)
            : base(model, graphAssetModel)
        {
        }

        internal EvaluationRatioConditionModel EvaluationRatioConditionModel => (EvaluationRatioConditionModel)Model;
    }
}
