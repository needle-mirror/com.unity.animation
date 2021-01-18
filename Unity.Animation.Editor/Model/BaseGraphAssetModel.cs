using System;

namespace Unity.Animation.Model
{
    internal class BaseGraphAssetModel : BaseAssetModel
    {
        protected override Type GraphModelType => typeof(BaseGraphModel);
    }
}
