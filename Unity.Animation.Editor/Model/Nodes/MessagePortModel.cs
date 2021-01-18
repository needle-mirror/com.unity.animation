using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Model
{
    internal class MessagePortModel : BasePortModel
    {
        public MessagePortModel(string name = null, string uniqueId = null, PortModelOptions options = PortModelOptions.Default, string displayName = "")
            : base(name, uniqueId, options)
        {
        }
    }
}
