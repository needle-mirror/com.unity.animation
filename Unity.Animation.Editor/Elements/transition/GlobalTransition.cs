namespace Unity.Animation.Editor
{
    internal class GlobalTransition : BaseTransition
    {
        public new static readonly string ussClassName = "sm-global-transition";

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }
    }
}
