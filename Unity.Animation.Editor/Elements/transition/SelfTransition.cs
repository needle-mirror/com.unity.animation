namespace Unity.Animation.Editor
{
    internal class SelfTransition : BaseTransition
    {
        public new static readonly string ussClassName = "sm-self-transition";

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }
    }
}
