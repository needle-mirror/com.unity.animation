using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor.Tests
{
    class DFGSearcherTests : BaseGraphFixture
    {
        protected override string[] TestAssemblies => new[] { "Unity.Animation.Editor.Nodes.Tests" };

        [Test]
        public void CreateNodeFromSearcherAction()
        {
            var db = new GraphElementSearcherDatabase(Stencil, GraphModel).AddDataFlowGraphNodes(new[] { typeof(DummyNode) }, Stencil).Build();
            var item = (GraphNodeModelSearcherItem)db.Search(DFGService.FormatNodeName(typeof(DummyNode).Name), out _)[0];

            Assert.That(GetNodeCount(), Is.EqualTo(0));
            m_Store.Dispatch(new CreateNodeFromSearcherAction(new Vector2(100, 200), item, new[] {GUID.Generate() }));

            Assert.That(GetNodeCount(), Is.EqualTo(1));
            Assert.That(GraphModel.NodeModels.First(), Is.TypeOf<DFGNodeModel>());
        }

        [Test]
        public void CreateSubGraphNodeFromSearcherAction()
        {
            var db = new GraphElementSearcherDatabase(Stencil, GraphModel)
                .AddCreateSubGraphNode<BaseGraphAssetModel, SubGraphNodeModel>("Dummy Name", typeof(BaseGraphStencil)).Build();
            var item = (GraphNodeModelSearcherItem)db.Search("Create", out _)[0];

            Assert.That(GetNodeCount(), Is.EqualTo(0));
            m_Store.Dispatch(new CreateNodeFromSearcherAction(new Vector2(100, 200), item, new[] {GUID.Generate() }));

            Assert.That(GetNodeCount(), Is.EqualTo(1));
            Assert.That(GraphModel.NodeModels.First(), Is.TypeOf<SubGraphNodeModel>());
        }

        [Test]
        public void ValidateNoDuplicatePortTypes()
        {
            List<Type> portTypes = Stencil.GetAllPortTypes();
            List<Type> portTypesDistinct = Stencil.GetAllPortTypes().Distinct().ToList();
            Assert.That(portTypes.Count, Is.EqualTo(portTypesDistinct.Count));
        }
    }
}
