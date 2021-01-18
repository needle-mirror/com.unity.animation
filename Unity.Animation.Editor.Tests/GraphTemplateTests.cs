using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor.Tests
{
    class GraphTemplateTests : BaseGraphFixture
    {
        protected override bool CreateGraphOnStartup => false;

        [Test]
        public void CreatingGraphAsset_AppliesTemplate()
        {
            var name = "Test";
            var path = $"{k_GraphPath}{name}.asset";
            var graphAsset =
                GraphAssetCreationHelpers<BaseGraphAssetModel>.CreateInMemoryGraphAsset(
                    CreatedStencilType, name, path, new CreateGraphTemplate(CreatedStencilType));
            m_Store.Dispatch(new LoadGraphAssetAction(graphAsset));
            Assert.AreEqual(1, m_Store.State.GraphModel.NodeModels.Count);
            Assert.AreEqual(1, m_Store.State.GraphModel.NodeModels.OfType<OutputNodeModel>().Count());
            Assert.AreEqual(typeof(DummyOutput), m_Store.State.GraphModel.NodeModels[0].GetType());
        }
    }
}
