namespace Unity.Animation.Editor
{
    static class ConditionViewFactory
    {
        internal static BaseConditionView CreateViewFromViewModel(BaseConditionViewModel viewModel)
        {
            switch (viewModel)
            {
                case BlackboardValueConditionViewModel blackboardValueVM:
                    return new BlackboardValueConditionView(blackboardValueVM);
                case ElapsedTimeConditionViewModel elapsedTimeVM:
                    return new ElapsedTimeConditionView(elapsedTimeVM);
                case EvaluationRatioConditionViewModel evalRatioVM:
                    return new EvaluationRatioConditionView(evalRatioVM);
                case EndOfDominantAnimationConditionViewModel endOfAnimVM:
                    return new EndOfDominantAnimationConditionView(endOfAnimVM);
                case GroupConditionViewModel groupVM:
                    return new GroupConditionView(groupVM);
                case MarkupConditionViewModel markupVM:
                    return new MarkupConditionView(markupVM);
                case StateTagConditionViewModel stateTagVM:
                    return new StateTagConditionView(stateTagVM);
            }

            return null;
        }
    }
}
