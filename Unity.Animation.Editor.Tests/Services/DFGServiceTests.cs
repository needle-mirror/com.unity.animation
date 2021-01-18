using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.DataFlowGraph;
using Unity.Mathematics;

namespace Unity.Animation.Editor.Tests
{
    class DFGServiceTests : BaseGraphFixture
    {
        protected override string[] TestAssemblies => new[] {"Unity.Animation.Editor.Nodes.Tests", "Unity.Animation.Editor.Nodes.Error.Tests"};

        void LogExpectNodeID()
        {
            UnityEngine.TestTools.LogAssert.Expect(
                UnityEngine.LogType.Error,
                new Regex("^Node ID"));
        }

        [Test]
        public void DetectAvailableTypes()
        {
            // See NodeDefinitions in DFGTestNodes.cs
            // Adjust value based on newly created NodeDefinition

            const int numNodes = 24;
            Assert.AreEqual(numNodes, DFGService.GetAvailableTypes().Count);
            LogExpectNodeID();
        }

        [Test]
        public void DetectAvailablePortDataTypes_WithMessageUsage()
        {
            var dataTypes = DFGService.GetAvailablePortDataTypes(DFGService.PortUsage.Message);
            Assert.That(dataTypes.Count, Is.EqualTo(12));
            LogExpectNodeID();
        }

        [Test]
        public void DetectAvailablePortDataTypes_WithDataUsage()
        {
            var dataTypes = DFGService.GetAvailablePortDataTypes(DFGService.PortUsage.Data);
            Assert.That(dataTypes.Count, Is.EqualTo(3));
            LogExpectNodeID();
        }

        [Test]
        public void ValidateGetAvailableTestsWithSorting()
        {
            var availableTypes = DFGService.GetAvailableTypes(sorted: true);
            for (int i = 0; i < availableTypes.Count - 1; ++i)
            {
                Assert.That(availableTypes[i].Name.CompareTo(availableTypes[i + 1].Name), Is.LessThanOrEqualTo(0));
            }

            LogExpectNodeID();
        }

        [TestCase(typeof(InputBufferFloatDataNode), "Input", (ushort)0)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "This", (ushort)4)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "Is", (ushort)5)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "AnInput", (ushort)6)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "Another", (ushort)7)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "InputValue", (ushort)8)]
        public void GetDataInputPortID_NoArray_ReturnValidValues(System.Type nodeDefinition, string portName, ushort expectedPortValue)
        {
            using (NodeSet dummyNodeSet = new NodeSet())
            {
                PortID portID = DFGTranslationHelpers.GetDataInputPortIDValue(dummyNodeSet, nodeDefinition, portName);
                Assert.AreEqual(expectedPortValue, portID.ID);
                Assert.False(portID.IsPortArray());
            }
        }

        [TestCase(typeof(MultipleInputsOutputsNodes), "MessageInput1", (ushort)0)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "MessageInput2", (ushort)1)]
        public void GetMessageInputPortID_NoArray_ReturnValidValues(System.Type nodeDefinition, string portName, ushort expectedPortValue)
        {
            using (NodeSet dummyNodeSet = new NodeSet())
            {
                PortID portID = DFGTranslationHelpers.GetMessageInputPortIDValue(dummyNodeSet, nodeDefinition, portName);
                Assert.AreEqual(expectedPortValue, portID.ID);
                Assert.False(portID.IsPortArray());
            }
        }

        [TestCase(typeof(OutputIntDataNode), "Output", (ushort)0)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "Those", (ushort)3)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "Are", (ushort)4)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "Outputs", (ushort)5)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "Other", (ushort)6)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "OutputValue", (ushort)7)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "ConflictName", (ushort)8)]
        public void GetDataOutputPortID_NoArray_ReturnValidValues(System.Type nodeDefinition, string portName, ushort expectedPortValue)
        {
            using (NodeSet dummyNodeSet = new NodeSet())
            {
                PortID portID = DFGTranslationHelpers.GetDataOutputPortIDValue(dummyNodeSet, nodeDefinition, portName);
                Assert.AreEqual(expectedPortValue, portID.ID);
                Assert.False(portID.IsPortArray());
            }
        }

        [TestCase(typeof(MultipleInputsOutputsNodes), "MessageOutput1", (ushort)0)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "MessageOutput2", (ushort)1)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "ConflictName", (ushort)2)]
        public void GetMessageOutputPortID_NoArray_ReturnValidValues(System.Type nodeDefinition, string portName, ushort expectedPortValue)
        {
            using (NodeSet dummyNodeSet = new NodeSet())
            {
                PortID portID = DFGTranslationHelpers.GetMessageOutputPortIDValue(dummyNodeSet, nodeDefinition, portName);
                Assert.AreEqual(expectedPortValue, portID.ID);
                Assert.False(portID.IsPortArray());
            }
        }

        [TestCase(typeof(MultipleInputsOutputsNodes), "DataArray1", (ushort)3, 0)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "DataArray1", (ushort)3, 1)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "DataArray2", (ushort)9, 1)]
        public void GetDataInputPortID_PortArray_ReturnValidValues(System.Type nodeDefinition, string portName, ushort expectedPortIDValue, int portIndexValue)
        {
            using (NodeSet dummyNodeSet = new NodeSet())
            {
                PortID portID = DFGTranslationHelpers.GetDataInputPortIDValue(dummyNodeSet, nodeDefinition, portName, portIndexValue);
                Assert.AreEqual(expectedPortIDValue, portID.ID);
                Assert.AreEqual((ushort)portIndexValue, portID.Index);
                Assert.True(portID.IsPortArray());
            }
        }

        [TestCase(typeof(MultipleInputsOutputsNodes), "MessageArray", (ushort)2, 0)]
        [TestCase(typeof(MultipleInputsOutputsNodes), "MessageArray", (ushort)2, 1)]
        public void GetMessageInputPortID_PortArray_ReturnValidValues(System.Type nodeDefinition, string portName, ushort expectedPortIDValue, int portIndexValue)
        {
            using (NodeSet dummyNodeSet = new NodeSet())
            {
                PortID portID = DFGTranslationHelpers.GetMessageInputPortIDValue(dummyNodeSet, nodeDefinition, portName, portIndexValue);
                Assert.AreEqual(expectedPortIDValue, portID.ID);
                Assert.AreEqual((ushort)portIndexValue, portID.Index);
                Assert.True(portID.IsPortArray());
            }
        }

        [Test]
        public void GetPortID_AbsentPort_Throw()
        {
            using (NodeSet dummyNodeSet = new NodeSet())
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    PortID _ = DFGTranslationHelpers.GetMessageInputPortIDValue(dummyNodeSet, typeof(MultipleInputsOutputsNodes), "IDoNotExist");
                });
                Assert.Throws<InvalidOperationException>(() =>
                {
                    PortID _ = DFGTranslationHelpers.GetMessageOutputPortIDValue(dummyNodeSet, typeof(MultipleInputsOutputsNodes), "IDoNotExist");
                });
                Assert.Throws<InvalidOperationException>(() =>
                {
                    PortID _ = DFGTranslationHelpers.GetDataInputPortIDValue(dummyNodeSet, typeof(MultipleInputsOutputsNodes), "IDoNotExist");
                });
                Assert.Throws<InvalidOperationException>(() =>
                {
                    PortID _ = DFGTranslationHelpers.GetDataOutputPortIDValue(dummyNodeSet, typeof(MultipleInputsOutputsNodes), "IDoNotExist");
                });
            }
        }

        [TestCase(typeof(bool), typeof(ToBoolVariantConverterNode))]
        [TestCase(typeof(int), typeof(ToIntVariantConverterNode))]
        [TestCase(typeof(uint), typeof(ToUIntVariantConverterNode))]
        [TestCase(typeof(short), typeof(ToShortVariantConverterNode))]
        [TestCase(typeof(ushort), typeof(ToUShortVariantConverterNode))]
        [TestCase(typeof(ulong), typeof(ToULongVariantConverterNode))]
        [TestCase(typeof(float), typeof(ToFloatVariantConverterNode))]
        [TestCase(typeof(float2), typeof(ToFloat2VariantConverterNode))]
        [TestCase(typeof(float3), typeof(ToFloat3VariantConverterNode))]
        [TestCase(typeof(float4), typeof(ToFloat4VariantConverterNode))]
        [TestCase(typeof(quaternion), typeof(ToQuaternionVariantConverterNode))]
        [TestCase(typeof(Entities.Hash128), typeof(ToHash128VariantConverterNode))]
        [TestCase(typeof(int4), typeof(ToInt4VariantConverterNode))]
        public void CreateGraphVariantConverterNodeOfType_CreateCorrectNode(Type primitiveType, Type nodeBuiltType)
        {
            var ir = new IR("TestIR", false);
            var irNode = DFGTranslationHelpers.CreateGraphVariantConverterNodeOfType(ir, primitiveType);
            Assert.That(irNode.NodeType, Is.EqualTo(nodeBuiltType));
        }

        [TestCase(typeof(int), typeof(SimulationPhasePassThroughNode<int>))]
        [TestCase(typeof(float), typeof(SimulationPhasePassThroughNode<float>))]
        public void CreateMessagePassThroughNodeOfType_CreateCorrectNode(Type primitiveType, Type nodeBuiltType)
        {
            var ir = new IR("TestIR", false);
            var irNode = DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, primitiveType);
            Assert.That(irNode.NodeType, Is.EqualTo(nodeBuiltType));
        }

        [Test]
        public void CreateMessagePassThroughNodeOfType_InvalidType_Throw()
        {
            Type primitiveType = typeof(string);
            var ir = new IR("TestIR", false);
            Assert.Throws<ArgumentException>(() =>
            {
                var _ = DFGTranslationHelpers.CreateMessagePassThroughNodeOfType(ir, primitiveType);
            });
        }

        [TestCase(typeof(int), typeof(DataPhasePassThroughNode<int>))]
        [TestCase(typeof(bool), typeof(DataPhasePassThroughNode<bool>))]
        [TestCase(typeof(float), typeof(DummyPassThroughNode))]
        public void CreateDataPassThroughNodeOfType_CreateCorrectNode(Type primitiveType, Type nodeBuiltType)
        {
            var ct = new DummyContext();
            var ir = new IR("TestIR", false);
            var irNode = DFGTranslationHelpers.CreateDataPassThroughNodeOfType(ir, primitiveType, ct);
            Assert.That(irNode.NodeType, Is.EqualTo(nodeBuiltType));
        }

        [Test]
        public void CreateDataPassThroughNodeOfType_InvalidType_Throw()
        {
            Type primitiveType = typeof(string);
            var ct = new DummyContext();
            var ir = new IR("TestIR", false);
            Assert.Throws<ArgumentException>(() =>
            {
                var _ = DFGTranslationHelpers.CreateDataPassThroughNodeOfType(ir, primitiveType, ct);
            });
        }

        [Test]
        public void CreateGraphVariantConverterNodeOfType_InvalidType_Throw()
        {
            var primitiveType = typeof(string);
            var ir = new IR("TestIR", false);
            Assert.Throws<InvalidDataException>(() =>
            {
                var _ = DFGTranslationHelpers.CreateGraphVariantConverterNodeOfType(ir, primitiveType);
            });
        }
    }
}
