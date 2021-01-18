using NUnit.Framework;
using System.Linq;

namespace Unity.Animation.Editor.Tests
{
    class IRBuilderTests : BaseGraphFixture
    {
        protected override string[] TestAssemblies => new[] { "Unity.Animation.Editor.Nodes.Tests", "Unity.Animation.Editor.Tests" };

        [Test]
        public void BuildNode()
        {
            var nodeType = typeof(DummyNode);
            CreateNode(nodeType);
            var ir = IRBuilder.BuildBlendTreeIR(GraphModel);

            Assert.AreEqual(4, ir.Nodes.Count);
            Assert.AreEqual(nodeType.FullName, ir.Nodes[3].GetTypeName());
        }

        [Test]
        public void DetectNameClashing_WithNodes()
        {
            var nodeType = typeof(DummyNode);

            var node1 = CreateNode(nodeType);
            CreateNode(nodeType);

            var ir = IRBuilder.BuildBlendTreeIR(GraphModel);

            // 3x Passthrough
            // 2x Dummy Nodes
            Assert.AreEqual(5, ir.Nodes.Count);
            Assert.AreEqual(node1.Title, ir.Nodes[3].Name);
            Assert.AreNotEqual(ir.Nodes[4].Name, ir.Nodes[3].Name);
        }

        [Test]
        public void BuildSimulationConnection()
        {
            var outNodeType = typeof(OutputFloatMessageNode);
            var src = CreateNode(outNodeType);
            var inNodeType = typeof(InputFloatMessageNode);
            var dst = CreateNode(inNodeType);

            GraphModel.CreateEdge(
                dst.Ports.FirstOrDefault(),
                src.Ports.FirstOrDefault());

            var ir = IRBuilder.BuildBlendTreeIR(GraphModel);

            Assert.AreEqual(1, ir.SimulationConnections.Count);
            Assert.AreEqual("Output", ir.SimulationConnections[0].Source.PortName);
            Assert.AreEqual("Input", ir.SimulationConnections[0].Destination.PortName);

            Assert.AreEqual(src.Title, ir.SimulationConnections[0].Source.Node.Name);
            Assert.AreEqual(dst.Title, ir.SimulationConnections[0].Destination.Node.Name);
        }

        [Test]
        public void BuildDataConnection()
        {
            var outNodeType = typeof(OutputFloatDataNode);
            var src = CreateNode(outNodeType);
            var inNodeType = typeof(InputFloatDataNode);
            var dst = CreateNode(inNodeType);

            GraphModel.CreateEdge(
                dst.Ports.FirstOrDefault(),
                src.Ports.FirstOrDefault());

            var ir = IRBuilder.BuildBlendTreeIR(GraphModel);

            Assert.AreEqual(1, ir.DataConnections.Count);
            Assert.AreEqual("Output", ir.DataConnections[0].Source.PortName);
            Assert.AreEqual("Input", ir.DataConnections[0].Destination.PortName);

            Assert.AreEqual(src.Title, ir.DataConnections[0].Source.Node.Name);
            Assert.AreEqual(dst.Title, ir.DataConnections[0].Destination.Node.Name);
        }

        [Test]
        public void BuildPortDefaultValue_WithEmbeddedValue()
        {
            const float k_EmbeddedValue = 2.0f;
            var inNodeType = typeof(InputFloatMessageNode);
            var dfgNode = CreateNode(inNodeType);

            dfgNode.Ports.FirstOrDefault().EmbeddedValue.ObjectValue = k_EmbeddedValue;

            var ir = IRBuilder.BuildBlendTreeIR(GraphModel);

            Assert.AreEqual(1, ir.PortDefaultValues.Count);
            Assert.AreEqual(dfgNode.Title, ir.PortDefaultValues[0].Destination.Node.Name);
            Assert.AreEqual("Input", ir.PortDefaultValues[0].Destination.PortName);
            Assert.AreEqual(k_EmbeddedValue, ir.PortDefaultValues[0].Value);
        }

        [TestCase(typeof(InputFloatMessageNode), 2, 0)]
        [TestCase(typeof(InputFloatDataNode), 1, 1)]
        public void BuildConnection_WithInput_ToDataPort(System.Type nodeType, int simulationConnectionCount, int simulationToDataConnectionCount)
        {
            var dfgNode = CreateNode(nodeType);
            var varNode = CreateInputFieldVariableModel("Test", typeof(DummyAuthoringComponent), "Field1");

            GraphModel.CreateEdge(
                dfgNode.Ports.FirstOrDefault(),
                varNode.Ports.FirstOrDefault());

            var ir = IRBuilder.BuildBlendTreeIR(GraphModel);

            Assert.AreEqual(1, ir.InputReferencesTargets.Count);
            Assert.AreEqual(simulationConnectionCount, ir.SimulationConnections.Count);
            Assert.AreEqual(simulationToDataConnectionCount, ir.SimulationToDataConnections.Count);
        }
    }
}
