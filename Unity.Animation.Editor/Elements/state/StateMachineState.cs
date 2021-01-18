namespace Unity.Animation.Editor
{
    internal class StateMachineState : BaseState
    {
        public new static readonly string ussClassName = "sm-statemachinestate";

        protected override void BuildElementUI()
        {
            base.BuildElementUI();
            AddToClassList(ussClassName);
        }
    }
}
