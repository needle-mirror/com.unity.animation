using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Model
{
    internal class DataPortModel : BasePortModel
    {
        public DataPortModel(string name = null, string uniqueId = null, PortModelOptions options = PortModelOptions.Default, string displayName = "")
            : base(name, uniqueId, options)
        {
        }
    }
}
