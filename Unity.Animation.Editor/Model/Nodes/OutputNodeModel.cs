using System;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Model
{
    [Serializable]
    internal abstract class OutputNodeModel : BaseNodeModel
    {
        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            m_Capabilities.Remove(UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Renamable);
            m_Capabilities.Remove(UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Deletable);
            m_Capabilities.Remove(UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Copiable);
            m_Capabilities.Remove(UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Droppable);
        }

        public override PortCapacity GetPortCapacity(IPortModel portModel) => PortCapacity.Single;
    }
}
