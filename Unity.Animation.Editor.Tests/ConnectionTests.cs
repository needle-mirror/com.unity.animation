using System;
using System.Linq;
using NUnit.Framework;

namespace Unity.Animation.Editor.Tests
{
    class ConnectionTests : BaseGraphFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override string[] TestAssemblies => new[] { "Unity.Animation.Editor.Nodes.Tests" };

        [TestCase(typeof(OutputFloatDataNode), typeof(InputFloatDataNode))]
        [TestCase(typeof(OutputFloatMessageNode), typeof(InputFloatMessageNode))]
        public void Validate_WithCompatibleNode_NodesAreConnected(Type outNode, Type inNode)
        {
            var outputDataNode = CreateNode(outNode);
            var inputDataNode = CreateNode(inNode);

            Assert.IsNotNull(GraphModel.GetCompatiblePorts(outputDataNode.Ports.FirstOrDefault()).Find(p => p.Equals(inputDataNode.Ports.FirstOrDefault())));

            CreateEdge(outputDataNode, inputDataNode);

            Assert.AreEqual(1, GraphModel.EdgeModels.Count);

            Assert.AreEqual(inputDataNode, GraphModel.EdgeModels[0].ToPort.NodeModel);
            Assert.AreEqual(outputDataNode, GraphModel.EdgeModels[0].FromPort.NodeModel);
        }

        [TestCase(typeof(OutputFloatDataNode), typeof(InputIntDataNode))]
        [TestCase(typeof(OutputIntDataNode), typeof(InputFloatDataNode))]
        [TestCase(typeof(OutputIntMessageNode), typeof(InputFloatMessageNode))]
        [TestCase(typeof(OutputFloatMessageNode), typeof(InputIntMessageNode))]
        [TestCase(typeof(OutputIntDataNode), typeof(InputIntMessageNode))]
        [TestCase(typeof(OutputIntMessageNode), typeof(InputIntDataNode))]
        public void Validate_WithIncompatibleNode_NodesAreNotConnected(Type outNode, Type inNode)
        {
            var outputDataNode = CreateNode(outNode);
            CreateNode(inNode);

            Assert.IsEmpty(GraphModel.GetCompatiblePorts(outputDataNode.Ports.FirstOrDefault()));
        }

        [TestCase(typeof(InputFloatMessageNode))]
        [TestCase(typeof(InputFloatDataNode))]
        public void CreateEdge_FromInputToPort_WithSameType_IsConnected(Type type)
        {
            var inputNode = CreateNode(type);

            var varNode = CreateInputFieldVariableModel("Test", typeof(DummyAuthoringComponent), "Field1");

            Assert.IsNotNull(
                GraphModel.GetCompatiblePorts(varNode.Ports.FirstOrDefault()).Find(p => p.Equals(inputNode.Ports.FirstOrDefault())));

            GraphModel.CreateEdge(
                inputNode.Ports.FirstOrDefault(),
                varNode.Ports.FirstOrDefault());

            Assert.That(GraphModel.EdgeModels.Count, Is.EqualTo(1));

            Assert.That(GraphModel.EdgeModels[0].ToPort.NodeModel, Is.EqualTo(inputNode));
            Assert.That(GraphModel.EdgeModels[0].FromPort.NodeModel, Is.EqualTo(varNode));
        }
    }
}
