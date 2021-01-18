/*
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Compositor.Editor.Redux;
using Unity.Compositor.GraphViewModel;
using Unity.Compositor.Editor.Services;
using Unity.Compositor.Editor.IR;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Compositor.Editor.Tests.Animation
{
    class DataFlowGraphAttributeTests : BaseGraphFixture
    {
        protected override string[] TestAssemblies => new[]
        { "Unity.Animation.Editor.Nodes.Tests",
          "Unity.Animation.Editor.Tests" };

        [Test]
        public void ValidateNodeAttribute_Description()
        {
            var nodeType = typeof(CategorizedNode);
            var nodeModel = CreateNode(nodeType);
            var nodeDefinition = nodeType.GetCustomAttribute<NodeDefinitionAttribute>();
            Assert.IsNotNull(nodeDefinition);

            Assert.That(nodeModel.Tooltip, Is.EqualTo(nodeDefinition.NodeDescription));
        }

        [Test]
        public void BuildResizableNode()
        {
            // Validate that node has correct attribute
            var portGroupDefinition = typeof(ResizablePortNode).GetCustomAttribute<PortGroupDefinitionAttribute>();
            Assert.That(portGroupDefinition, !Is.Null);

            // Test resizeable node initialization
            var node = CreateNode(typeof(ResizablePortNode));

            var ir = CompositorIRBuilder.BuildBlendTreeIR(GraphModel);
            var irNode = ir.Nodes.SingleOrDefault(n => n.GetTypeName() == typeof(ResizablePortNode).FullName);
            Assert.AreEqual(portGroupDefinition.MinInstance, irNode.PortDeclarations.InputDataPorts.Count);

            // Test resizing node within bounds
            m_Store.Dispatch(new SetNumberOfPortGroupInstanceAction(node, 1, portGroupDefinition.MinInstance, portGroupDefinition.MinInstance + 1));
            ir = CompositorIRBuilder.BuildBlendTreeIR(GraphModel);
            irNode = ir.Nodes.SingleOrDefault(n => n.GetTypeName() == typeof(ResizablePortNode).FullName);
            Assert.AreEqual(portGroupDefinition.MinInstance + 1, irNode.PortDeclarations.InputDataPorts.Count);

            // Test resizing node below bounds
            m_Store.Dispatch(new SetNumberOfPortGroupInstanceAction(node, 1, portGroupDefinition.MinInstance + 1, portGroupDefinition.MinInstance - 1));
            ir = CompositorIRBuilder.BuildBlendTreeIR(GraphModel);
            irNode = ir.Nodes.SingleOrDefault(n => n.GetTypeName() == typeof(ResizablePortNode).FullName);
            Assert.AreEqual(portGroupDefinition.MinInstance, irNode.PortDeclarations.InputDataPorts.Count);

            // Test resizing node above bounds
            m_Store.Dispatch(new SetNumberOfPortGroupInstanceAction(node, 1, portGroupDefinition.MinInstance, portGroupDefinition.MaxInstance + 1));
            ir = CompositorIRBuilder.BuildBlendTreeIR(GraphModel);
            irNode = ir.Nodes.SingleOrDefault(n => n.GetTypeName() == typeof(ResizablePortNode).FullName);
            Assert.AreEqual(portGroupDefinition.MaxInstance, irNode.PortDeclarations.InputDataPorts.Count);
        }

        [Test]
        public void ValidatePortAttribute_Hidden()
        {
            var node = CreateNode(typeof(HiddenPortNode));

            // Test hidden port
            Assert.IsTrue(DFGService.IsPortHidden(
                typeof(HiddenPortNode),
                node.InputsByDisplayOrder[0] is DataPortModel ? DFGService.PortUsage.Data : DFGService.PortUsage.Message,
                (node.InputsByDisplayOrder[0] as IHasTitle)?.Title ?? string.Empty));

            // Test non-hidden port
            Assert.IsFalse(DFGService.IsPortHidden(
                typeof(HiddenPortNode),
                node.InputsByDisplayOrder[1] is DataPortModel ? DFGService.PortUsage.Data : DFGService.PortUsage.Message,
                (node.InputsByDisplayOrder[1] as IHasTitle)?.Title ?? string.Empty));
        }

        private FieldInfo[] GetSimPortDefinitions(System.Type nodeType)
        {
            var fields = nodeType.GetFields(BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public);
            FieldInfo simulationPortDefinition = null;
            foreach (var fieldInfo in fields)
            {
                if (simulationPortDefinition != null)
                    break;

                if (typeof(ISimulationPortDefinition).IsAssignableFrom(fieldInfo.FieldType))
                    simulationPortDefinition = fieldInfo;
            }

            var definitionFields = simulationPortDefinition.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            return simulationPortDefinition.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public);
        }

        [Test]
        public void ValidatePortAttribute_Description()
        {
            var node = CreateNode(typeof(TooltipPortNode));

            var definitionFields = GetSimPortDefinitions(typeof(TooltipPortNode));

            // Test input port with description attribute
            PortDefinitionAttribute portDefinitionInfo = definitionFields[0].GetCustomAttribute<PortDefinitionAttribute>();
            Assert.That(node.InputsByDisplayOrder[0].ToolTip.Contains(portDefinitionInfo.Description));
        }

        [Test]
        public void ValidatePortAttribute_DefaultValue()
        {
            var node = CreateNode(typeof(DefaultValuePortNode));

            var definitionFields = GetSimPortDefinitions(typeof(DefaultValuePortNode));
            PortDefinitionAttribute portDefinitionInfo = definitionFields[0].GetCustomAttribute<PortDefinitionAttribute>();

            Assert.That((float)node.Ports.FirstOrDefault().EmbeddedValue.ObjectValue == (float)portDefinitionInfo.DefaultValue);

            var ir = CompositorIRBuilder.BuildBlendTreeIR(GraphModel);

            Assert.AreEqual(1, ir.PortDefaultValues.Count);
            Assert.AreEqual(node.Title, ir.PortDefaultValues[0].Destination.Node.Name);
            Assert.AreEqual(definitionFields[0].Name, ir.PortDefaultValues[0].Destination.PortName);
            Assert.AreEqual((float)portDefinitionInfo.DefaultValue, (float)ir.PortDefaultValues[0].Value);
        }

        [Test]
        public void ValidatePortAttribute_Static()
        {
            var staticNode = CreateNode(typeof(StaticPortNode));
            var src = CreateNode(typeof(OutputFloatMessageNode));

            CompositorBasePortModel port = staticNode.InputsByDisplayOrder[0] as CompositorBasePortModel;
            Assert.That(port.IsStatic);

            Assert.IsEmpty(GraphModel.GetCompatiblePorts(src.Ports.FirstOrDefault()));
        }

        [Test]
        public void ValidatePortAttribute_PortGroup()
        {
            var dummyNode = CreateNode(typeof(DummyNode));
            Assert.AreEqual(0, DFGService.GetPortGroupDefinitions(dummyNode).Definitions.Count);

            var node = CreateNode(typeof(ResizablePortNode));
            Assert.AreEqual(1, DFGService.GetPortGroupDefinitions(node).Definitions.Count);
            Assert.AreEqual(1, DFGService.GetPortGroupDefinitions(node).Definitions[1].GroupIndex);

            var ir = CompositorIRBuilder.BuildBlendTreeIR(GraphModel);

            Assert.AreEqual(1, ir.PortGroupInfos.Count);
            Assert.AreEqual(1, ir.PortGroupInfos.Values.ToList()[0].PortGroupInfos.Values.ToList()[0].PortGroupIndex);
        }

        [Test]
        public void ValidatePortAttribute_DisplayName()
        {
            var node = CreateNode(typeof(NamedPortNode));

            var definitionFields = GetSimPortDefinitions(typeof(NamedPortNode));

            // TODO: update this test once display names are being correctly used by the ports
            PortDefinitionAttribute portDefinitionInfo = definitionFields[0].GetCustomAttribute<PortDefinitionAttribute>();
            var compPort = node.InputsByDisplayOrder[0] as CompositorBasePortModel;
            Assert.AreEqual(portDefinitionInfo.DisplayName, compPort.DisplayName);
        }
    }
}
*/
