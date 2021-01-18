using NUnit.Framework;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Editor.Tests
{
    class TranslationTests : BaseGraphFixture
    {
        protected override string[] TestAssemblies => new[] { "Unity.Animation.Editor.Nodes.Tests" };

        [Test]
        public void TranslateSimpleGraph()
        {
            var translator = Stencil.CreateTranslator();
            var node0 = CreateNode(typeof(OutputFloatMessageNode));
            var node1 = CreateNode(typeof(OutputFloatMessageNode));
            var node2 = CreateNode(typeof(DummyDoubleInputNode));

            GraphModel.CreateEdge(
                node2.Ports.FirstOrDefault(p => p is IHasTitle portTitle && portTitle.Title == "Input0"),
                node0.Ports.FirstOrDefault(p => p is IHasTitle portTitle && portTitle.Title == "Output"));

            GraphModel.CreateEdge(
                node2.Ports.FirstOrDefault(p => p is IHasTitle portTitle && portTitle.Title == "Input1"),
                node1.Ports.FirstOrDefault(p => p is IHasTitle portTitle && portTitle.Title == "Output"));

            var result = translator.Compile(GraphModel);
            GraphDefinition def  = GraphAsset.CompiledGraph.Definition;

            // 1x Passthrough<Context>
            // 1x Passthrough<EntityManager>
            // 1x Passthrough<InputReferences>
            // 2x OutputFloatMessageNode
            // 1x DummyDoubleInputNode
            // 2x ToFloatVariantConverterNode
            Assert.AreEqual(8, def.TopologyDefinition.NodeCreations.Count);

            // node0 to node2
            // node1 to node2
            // ToFloatVariantConverterNode to node0
            // ToFloatVariantConverterNode to node1
            Assert.AreEqual(4, def.TopologyDefinition.Connections.Count);
        }

        //[Test]
        //public void TranslateNestedGraph()
        //{
        //    var translator = Stencil.CreateTranslator();

        //    //Subnode
        //    {
        //        var clip0 = CreateNode(typeof(OutputFloatMessageNode));
        //        var clip1 = CreateNode(typeof(OutputFloatMessageNode));
        //        var doubleInput = CreateNode(typeof(DummyDoubleInputNode));

        //        GraphModel.CreateEdge(
        //            doubleInput.Ports.Where(p => p is IHasTitle portTitle && portTitle.Title == "Input0").FirstOrDefault(),
        //            clip0.Ports.Where(p => p is IHasTitle portTitle && portTitle.Title == "Output").FirstOrDefault());

        //        GraphModel.CreateEdge(
        //            doubleInput.Ports.Where(p => p is IHasTitle portTitle && portTitle.Title == "Input1").FirstOrDefault(),
        //            clip1.Ports.Where(p => p is IHasTitle portTitle && portTitle.Title == "Output").FirstOrDefault());
        //    }

        //    var asset = this.GraphModel.AssetModel;

        //    CreateNewCompositorGraphModel("Test2");

        //    var internalNode = GraphModel.CreateNode<CompositorSubGraphNodeModel>(
        //        "Test1", default, default,
        //        model => { model.GraphAsset = (CompositorGraphAsset)asset; }
        //    );
        //    var result = translator.Compile(GraphModel);
        //    GraphDefinition def = GraphModel.CompiledGraph.Definition;

        //    // 1 Passthrough<Context>, 1 Passthrough<Entity>, 2 OutputFloatMessageNode, 1 DummyDoubleInputNode
        //    Assert.AreEqual(7, def.TopologyDefinition.NodeCreations.Count);

        //    // clip0 to doubleInput
        //    // clip1 to doubleInput
        //    Assert.AreEqual(2, def.TopologyDefinition.Connections.Count);
        //}
    }
}
