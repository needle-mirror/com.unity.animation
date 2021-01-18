using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    internal class MainToolbar : UnityEditor.GraphToolsFoundation.Overdrive.MainToolbar
    {
        public MainToolbar(Store store, GraphView graphView) : base(store, graphView)
        {
            Remove(m_EnableTracingButton);
            Remove(m_OptionsButton);
        }
    }
}
