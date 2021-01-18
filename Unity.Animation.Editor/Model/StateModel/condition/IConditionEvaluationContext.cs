namespace Unity.Animation.Model
{
    interface IBlackboardValueEvaluator
    {
        string GetValue(string blackboardValueId);
        bool IsBlackboardValueDirty(string blackboardValueId);
    }

    interface IStateElapsedTimeEvaluator
    {
        float GetElapsedTime(string stateId);
    }

    interface IMarkupEvaluator
    {
        bool IsMarkupPresent(float dt);
    }

    interface IStateTagEvaluator
    {
        bool IsStateTagActive(string stateTagId);
    }

    interface IConditionEvaluationContext
    {
        IBlackboardValueEvaluator GetBlackboardValueEvaluator(string entityId = "");
        IStateElapsedTimeEvaluator ElapsedTimeEvaluator { get; }
        IMarkupEvaluator GetMarkupEvaluator(string entityId = "");
        IStateTagEvaluator StateTagEvaluator { get; }

        string CurrentStateId { get; }
        float CurrentDt { get; }
    }
}
