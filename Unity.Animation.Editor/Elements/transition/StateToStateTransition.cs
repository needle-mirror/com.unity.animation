namespace Unity.Animation.Editor
{
    internal class StateToStateTransition : BaseTransition
    {
        public new static readonly string ussClassName = "sm-state-to-state-transition";

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }
    }
}
