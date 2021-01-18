using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor.Tests
{
    class PortGroupTests : BaseGraphFixture
    {
        const int Group1Index = 1;
        const int Group2Index = 2;

        class DummyPortGroupNode : BaseNodeModel
        {
            public override string NodeName { get; }
            public override INodeIRBuilder Builder { get; }
            void InitGroups()
            {
                var group0 = PortGroupDefinitions.GetOrCreateGroupInstance(Group1Index);
                group0.SimulationPortToDrive = "Dummy Group 0";
                group0.MinInstance = 0;
                group0.MaxInstance = 4;
                group0.MessageInputs.Add(
                    new PortDefinition()
                    {
                        FieldName = "FieldGroup0",
                        Type = typeof(float)
                    });

                var group1 = PortGroupDefinitions.GetOrCreateGroupInstance(Group2Index);
                group1.SimulationPortToDrive = "Dummy Group 1";
                group1.MinInstance = 0;
                group1.MaxInstance = 8;
                group1.MessageInputs.Add(
                    new PortDefinition()
                    {
                        FieldName = "FieldGroup1",
                        Type = typeof(int)
                    });
            }

            protected override void OnDefineNode()
            {
                base.OnDefineNode();
                InitGroups();
                AddPortGroup(Group1Index);
                AddPortGroup(Group2Index);
            }
        }

        DummyPortGroupNode CreatePortGroupNode()
        {
            var node = GraphModel.CreateNode<DummyPortGroupNode>("Dummy");
            node.DefineNode();
            return node;
        }

        [Test]
        public void Adding_PortGroupPort_CreatesPortGroupConstant()
        {
            var node = CreatePortGroupNode();
            Assert.IsNotNull(node.Ports.First().EmbeddedValue is PortGroupConstant);
            Assert.IsNotNull(node.Ports.First().EmbeddedValue.ObjectValue is PortGroup);
        }

        [Test]
        public void ChangingPortGroupSize_OnNode_StoresNewSize_OnPort()
        {
            var node = CreatePortGroupNode();

            Assert.AreEqual(2, node.Ports.Count());

            var group1Port = node.Ports.First();
            Assert.IsNotNull(group1Port);

            m_Store.Dispatch(
                new SetNumberOfPortGroupInstanceAction(node, Group1Index, ((PortGroup)group1Port.EmbeddedValue.ObjectValue).Size, 3));

            Assert.AreEqual(3, node.GetPortGroupSize(Group1Index));
            node.DefineNode();
            Assert.AreEqual(3, (group1Port.EmbeddedValue.ObjectValue as PortGroup).Size);

            Assert.AreEqual(5, node.Ports.Count());
            var group2Port = node.Ports.ElementAt(4);
            Assert.IsNotNull(group2Port);

            m_Store.Dispatch(
                new SetNumberOfPortGroupInstanceAction(node, Group2Index, ((PortGroup)group2Port.EmbeddedValue.ObjectValue).Size, 8));

            Assert.AreEqual(8, node.GetPortGroupSize(Group2Index));
            node.DefineNode();
            Assert.AreEqual(8, (group2Port.EmbeddedValue.ObjectValue as PortGroup).Size);
        }
    }
}
