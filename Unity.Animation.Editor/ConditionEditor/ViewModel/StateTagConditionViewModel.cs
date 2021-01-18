using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class StateTagConditionViewModel : BaseConditionViewModel
    {
        public StateTagConditionViewModel(StateTagConditionModel model, IGraphAssetModel graphAssetModel)
            : base(model, graphAssetModel)
        {
        }

        internal StateTagConditionModel StateTagConditionModel => (StateTagConditionModel)Model;
    }
}
