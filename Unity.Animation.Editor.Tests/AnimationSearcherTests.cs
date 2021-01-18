using NUnit.Framework;
//using Unity.DataFlowGraph.Attributes;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor.Tests
{
    class AnimationSearcherTests : BaseGraphFixture
    {
        protected override string[] TestAssemblies => new[] { "Unity.Animation.Editor.Tests", "Unity.Animation.Editor.Nodes.Tests" };

        [Test]
        public void ValidateNodeAttribute_Hidden()
        {
            var db = new GraphElementSearcherDatabase(Stencil, GraphModel).AddDataFlowGraphNodes(new[] { typeof(HiddenNode) }, Stencil).Build();

            Assert.That(db.ItemList.Count, Is.EqualTo(0));
        }

        /*
        [Test]
        public void ValidateNodeAttribute_Category()
        {
            var nodeType1 = typeof(CategorizedNode);
            var nodeType2 = typeof(DummyNode);
            var db = new GraphElementSearcherDatabase(Stencil, GraphModel).AddDataFlowGraphNodes(new[] { nodeType1, nodeType2 }, Stencil).Build();

            var item = (GraphNodeModelSearcherItem)db.Search(DFGService.FormatNodeName(nodeType1.Name), out _)[0];
            var nodeDefinition = nodeType1.GetCustomAttributes(
                typeof(NodeDefinitionAttribute), false).FirstOrDefault() as NodeDefinitionAttribute;
            Assert.That(nodeDefinition, !Is.Null);

            string expectedPath = $"{(nodeDefinition.Category.Replace('/', ' '))} {DFGService.FormatNodeName(nodeType1.Name)}";
            Assert.AreEqual(expectedPath, item.Path);

            item = (GraphNodeModelSearcherItem)db.Search(DFGService.FormatNodeName(nodeType2.Name), out _)[0];
            expectedPath = $"{nodeType2.Namespace.Replace('.', ' ').Nicify()} {DFGService.FormatNodeName(nodeType2.Name)}";
            Assert.AreEqual(expectedPath, item.Path);
        }
        */

        [Test]
        public void ValidateNoDuplicateCategoryPaths()
        {
            var db = new GraphElementSearcherDatabase(Stencil, GraphModel).AddDataFlowGraphNodes(new[] { typeof(CategorizedNode), typeof(DuplicateCategorizedNode) }, Stencil).Build();
            var item1 = (GraphNodeModelSearcherItem)db.Search(DFGService.FormatNodeName(typeof(CategorizedNode).Name), out _)[0];
            var item2 = (GraphNodeModelSearcherItem)db.Search(DFGService.FormatNodeName(typeof(DuplicateCategorizedNode).Name), out _)[0];

            Assert.AreEqual(item2.Parent, item1.Parent);
            Assert.AreEqual(item2.Parent.Parent, item1.Parent.Parent);
        }
    }
}
