namespace Unity.Animation.Editor
{
    internal class OnEnterSelector : BaseTransition
    {
        public new static readonly string ussClassName = "sm-on-enter-selector";

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }
    }
}
