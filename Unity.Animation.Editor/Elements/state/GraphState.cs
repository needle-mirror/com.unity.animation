namespace Unity.Animation.Editor
{
    internal class GraphState : BaseState
    {
        public new static readonly string ussClassName = "sm-graphstate";

        protected override void BuildElementUI()
        {
            base.BuildElementUI();
            AddToClassList(ussClassName);
        }
    }
}
