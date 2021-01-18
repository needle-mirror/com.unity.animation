using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class MarkupConditionViewModel : BaseConditionViewModel
    {
        public MarkupConditionViewModel(MarkupConditionModel model, IGraphAssetModel graphAssetModel)
            : base(model, graphAssetModel)
        {
        }

        internal MarkupConditionModel MarkupConditionModel => (MarkupConditionModel)Model;
    }
}
