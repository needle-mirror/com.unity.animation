using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    static class ConditionViewModelFactory
    {
        internal static BaseConditionViewModel GetViewModelFromModel(BaseConditionModel model, IGraphAssetModel graphAssetModel)
        {
            switch (model)
            {
                case BlackboardValueConditionModel blackboardValueModel:
                    return new BlackboardValueConditionViewModel(blackboardValueModel, graphAssetModel);
                case ElapsedTimeConditionModel elapsedTimeModel:
                    return new ElapsedTimeConditionViewModel(elapsedTimeModel, graphAssetModel);
                case GroupConditionModel groupModel:
                    return new GroupConditionViewModel(groupModel, graphAssetModel);
                case MarkupConditionModel markupModel:
                    return new MarkupConditionViewModel(markupModel, graphAssetModel);
                case StateTagConditionModel stateTagModel:
                    return new StateTagConditionViewModel(stateTagModel, graphAssetModel);
                case EvaluationRatioConditionModel evalRatioModel:
                    return new EvaluationRatioConditionViewModel(evalRatioModel, graphAssetModel);
                case EndOfDominantAnimationConditionModel endOfAnimModel:
                    return new EndOfDominantAnimationConditionViewModel(endOfAnimModel, graphAssetModel);
            }

            return null;
        }
    }
}
