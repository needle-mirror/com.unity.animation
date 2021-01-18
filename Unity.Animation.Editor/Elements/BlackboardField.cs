namespace Unity.Animation.Editor
{
    class BlackboardField : UnityEditor.GraphToolsFoundation.Overdrive.BlackboardField
    {
        public bool CanInstantiateInGraph()
        {
            return Model?.GraphModel != null;
        }
    }
}
