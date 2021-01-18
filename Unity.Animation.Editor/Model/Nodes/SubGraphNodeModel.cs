using System;
using Unity.Animation.Editor;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class SubGraphNodeModel : SubNodeModel<BaseGraphAssetModel>
    {
        public override INodeIRBuilder Builder => new SubGraphNodeIRBuilder(this);
    }
}
