using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation.Editor.Tests
{
    [NodeDefinition(guid: "70ccd07f482f4165b15bf30592a82fcf", version: 1)]
    public class SortedDummyNode : SimulationNodeDefinition<SortedDummyNode.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }

    [NodeDefinition(guid: "24832f04d5d94fe0ab63221fc6a51993", version: 1)]
    public class DummyNode : SimulationNodeDefinition<DummyNode.MyPorts>
    {
        private struct MyData : INodeData {}
        public struct MyPorts : ISimulationPortDefinition {}
    }

    [NodeDefinition(guid: "abc7c38ff9d24d32922fd8a0a722b984", version: 1)]
    public class DummyFloatNode : SimulationKernelNodeDefinition<DummyFloatNode.SimPorts, DummyFloatNode.KernelDefs>
    {
        private struct MyData : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float weight)
            {
            }
        }

        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<DummyFloatNode, float> MessageInput1;
        }

        public struct KernelData : IKernelData
        {
            public float DataInput1;
        }

        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
            }
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<DummyFloatNode, Buffer<float>> DataInput1;
        }
    }

    [NodeDefinition(guid: "1c43c229a651493bb859c86ead0d6af7", version: 1)]
    public class OutputFloatMessageNode : SimulationNodeDefinition<OutputFloatMessageNode.MySimPorts>
    {
        private struct MyData : INodeData {}

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageOutput<OutputFloatMessageNode, float> Output;
        }
    }

    [NodeDefinition(guid: "46c2862599cb4dbbb88f86535217d32b", version: 1)]
    public class InputFloatMessageNode : SimulationNodeDefinition<InputFloatMessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<InputFloatMessageNode, float> Input;
        }
    }

    [NodeDefinition(guid: "eb74cda6a5b64cb4a3e32b03c68abab9", version: 1)]
    public class BoolMessageNode : SimulationNodeDefinition<BoolMessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<bool>
        {
            public void HandleMessage(MessageContext ctx, in bool msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<BoolMessageNode, bool> Input;
            public MessageOutput<BoolMessageNode, bool> Output;
        }
    }

    [NodeDefinition(guid: "cea864f654644c3cb0b1c7d2e7e39179", version: 1)]
    public class Float2MessageNode : SimulationNodeDefinition<Float2MessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<float2>
        {
            public void HandleMessage(MessageContext ctx, in float2 msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<Float2MessageNode, float2> Input;
            public MessageOutput<Float2MessageNode, float2> Output;
        }
    }

    [NodeDefinition(guid: "23c9cb7396ad471683eaa89a63d79e4b", version: 1)]
    public class Float3MessageNode : SimulationNodeDefinition<Float3MessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<float3>
        {
            public void HandleMessage(MessageContext ctx, in float3 msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<Float3MessageNode, float3> Input;
            public MessageOutput<Float3MessageNode, float3> Output;
        }
    }

    [NodeDefinition(guid: "d1f3a94356da401c9196be9154a7cfbf", version: 1)]
    public class Float4MessageNode : SimulationNodeDefinition<Float4MessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<float4>
        {
            public void HandleMessage(MessageContext ctx, in float4 msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<Float4MessageNode, float4> Input;
            public MessageOutput<Float4MessageNode, float4> Output;
        }
    }

    [NodeDefinition(guid: "9876c7c8233649fc9d1193e77e3cad01", version: 1)]
    public class QuaternionMessageNode : SimulationNodeDefinition<QuaternionMessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<quaternion>
        {
            public void HandleMessage(MessageContext ctx, in quaternion msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<QuaternionMessageNode, quaternion> Input;
            public MessageOutput<QuaternionMessageNode, quaternion> Output;
        }
    }

    [NodeDefinition(guid: "ab9d88715a90450b8710add5869ab116", version: 1)]
    public class OutputIntMessageNode : SimulationNodeDefinition<OutputIntMessageNode.MySimPorts>
    {
        private struct MyData : INodeData {}

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageOutput<OutputIntMessageNode, int> Output;
        }
    }

    [NodeDefinition(guid: "eb3a5c0299634b5e80ff1865e96f485f", version: 1)]
    public class InputIntMessageNode : SimulationNodeDefinition<InputIntMessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<int>
        {
            public void HandleMessage(MessageContext ctx, in int msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<InputIntMessageNode, int> Input;
        }
    }

    [NodeDefinition(guid: "aa3e0ab7ad1146fb992f35d4d2c77347", version: 1)]
    public class UIntMessageNode : SimulationNodeDefinition<UIntMessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<uint>
        {
            public void HandleMessage(MessageContext ctx, in uint msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<UIntMessageNode, uint> Input;
            public MessageOutput<UIntMessageNode, uint> Output;
        }
    }

    [NodeDefinition(guid: "2668567193db401a8018c6952168439c", version: 1)]
    public class ShortMessageNode : SimulationNodeDefinition<ShortMessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<short>
        {
            public void HandleMessage(MessageContext ctx, in short msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<ShortMessageNode, short> Input;
            public MessageOutput<ShortMessageNode, short> Output;
        }
    }

    [NodeDefinition(guid: "68c76578f5fb417891e61aa6414c6aa3", version: 1)]
    public class UShortMessageNode : SimulationNodeDefinition<UShortMessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<ushort>
        {
            public void HandleMessage(MessageContext ctx, in ushort msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<UShortMessageNode, ushort> Input;
            public MessageOutput<UShortMessageNode, ushort> Output;
        }
    }

    [NodeDefinition(guid: "fb4d3abf70014ee68bec08d1d8d62032", version: 1)]
    public class LongMessageNode : SimulationNodeDefinition<LongMessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<long>
        {
            public void HandleMessage(MessageContext ctx, in long msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<LongMessageNode, long> Input;
            public MessageOutput<LongMessageNode, long> Output;
        }
    }

    [NodeDefinition(guid: "1d54144432d14cdebf4a247599797be7", version: 1)]
    public class ULongMessageNode : SimulationNodeDefinition<ULongMessageNode.MySimPorts>
    {
        private struct MyData : INodeData, IMsgHandler<ulong>
        {
            public void HandleMessage(MessageContext ctx, in ulong msg) {}
        }

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<ULongMessageNode, ulong> Input;
            public MessageOutput<ULongMessageNode, ulong> Output;
        }
    }

    [NodeDefinition(guid: "0fb8a9ec6f7f4ef8b1df5965a7618231", version: 1)]
    public class OutputFloatDataNode : KernelNodeDefinition<OutputFloatDataNode.MyKernelPorts>
    {
        private struct MyData : INodeData {}
        public struct MyKernelData : IKernelData {}
        public struct MyKernelPorts : IKernelPortDefinition
        {
            public DataOutput<OutputFloatDataNode, float> Output;
        }
        public struct MyKernel : IGraphKernel<MyKernelData, MyKernelPorts>
        {
            public void Execute(RenderContext ctx, in MyKernelData data, ref MyKernelPorts ports) {}
        }
    }

    [NodeDefinition(guid: "2fe2ee0bfcc6456ebe7614f0e1722e3a", version: 1)]
    public class InputFloatDataNode : KernelNodeDefinition<InputFloatDataNode.MyKernelPorts>
    {
        private struct MyData : INodeData {}
        public struct MyKernelData : IKernelData {}
        public struct MyKernelPorts : IKernelPortDefinition
        {
            public DataInput<InputFloatDataNode, float> Input;
        }
        public struct MyKernel : IGraphKernel<MyKernelData, MyKernelPorts>
        {
            public void Execute(RenderContext ctx, in MyKernelData data, ref MyKernelPorts ports) {}
        }
    }

    [NodeDefinition(guid: "ef4d4ed172e6411eb15768f88d823756", version: 1)]
    public class OutputIntDataNode : KernelNodeDefinition<OutputIntDataNode.MyKernelPorts>
    {
        private struct MyData : INodeData {}
        public struct MyKernelData : IKernelData {}
        public struct MyKernelPorts : IKernelPortDefinition
        {
            public DataOutput<OutputIntDataNode, int> Output;
        }
        public struct MyKernel : IGraphKernel<MyKernelData, MyKernelPorts>
        {
            public void Execute(RenderContext ctx, in MyKernelData data, ref MyKernelPorts ports) {}
        }
    }

    [NodeDefinition(guid: "15fe53dc13ea4984b06531817fac65e2", version: 1)]
    public class InputIntDataNode : KernelNodeDefinition<InputIntDataNode.MyKernelPorts>
    {
        private struct MyData : INodeData {}
        public struct MyKernelData : IKernelData {}
        public struct MyKernelPorts : IKernelPortDefinition
        {
            public DataInput<InputIntDataNode, int> Input;
        }
        public struct MyKernel : IGraphKernel<MyKernelData, MyKernelPorts>
        {
            public void Execute(RenderContext ctx, in MyKernelData data, ref MyKernelPorts ports) {}
        }
    }

    [NodeDefinition(guid: "5cc4de0921e34737b26491987326daea", version: 1)]
    public class InputBufferFloatDataNode : KernelNodeDefinition<InputBufferFloatDataNode.MyKernelPorts>
    {
        private struct MyData : INodeData {}
        public struct MyKernelData : IKernelData {}
        public struct MyKernelPorts : IKernelPortDefinition
        {
            public DataInput<InputBufferFloatDataNode, Buffer<float>> Input;
        }
        public struct MyKernel : IGraphKernel<MyKernelData, MyKernelPorts>
        {
            public void Execute(RenderContext ctx, in MyKernelData data, ref MyKernelPorts ports) {}
        }
    }

    [NodeDefinition(guid: "98b1b68aaee7404f9bef7fb22288cfc5", version: 1)]
    public class DummyDoubleInputNode : SimulationNodeDefinition<DummyDoubleInputNode.SimPorts>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<DummyDoubleInputNode, float> Input0;
            public MessageInput<DummyDoubleInputNode, float> Input1;
            public MessageOutput<DummyDoubleInputNode, float> Output0;
        }
        private struct NodeData : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float msg)
            {
                throw new System.NotImplementedException();
            }
        }
    }

    [NodeDefinition(guid: "37826622acc04e069638b6d41c7fe5aa", version: 1)]
    public class MultipleInputsOutputsNodes :
        SimulationKernelNodeDefinition<MultipleInputsOutputsNodes.MySimPorts, MultipleInputsOutputsNodes.MyKernelPorts>
    {
        private struct MyData : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float msg)
            {
                throw new System.NotImplementedException();
            }
        }

        public struct MyKernelData : IKernelData {}

        public struct MySimPorts : ISimulationPortDefinition
        {
            public MessageInput<MultipleInputsOutputsNodes, float> MessageInput1;
            public MessageOutput<MultipleInputsOutputsNodes, float> MessageOutput1;
            public MessageInput<MultipleInputsOutputsNodes, float> MessageInput2;
            public PortArray<MessageInput<MultipleInputsOutputsNodes, float>> MessageArray;
            public MessageOutput<MultipleInputsOutputsNodes, float> MessageOutput2;
            public MessageOutput<MultipleInputsOutputsNodes, float> ConflictName;
        }

        public struct MyKernelPorts : IKernelPortDefinition
        {
            public PortArray<DataInput<MultipleInputsOutputsNodes, float>> DataArray1;

            public DataInput<MultipleInputsOutputsNodes, float> This;
            public DataInput<MultipleInputsOutputsNodes, float> Is;
            public DataInput<MultipleInputsOutputsNodes, float> AnInput;

            public DataOutput<MultipleInputsOutputsNodes, float> Those;
            public DataOutput<MultipleInputsOutputsNodes, float> Are;
            public DataOutput<MultipleInputsOutputsNodes, float> Outputs;

            public DataInput<MultipleInputsOutputsNodes, float> Another;
            public DataInput<MultipleInputsOutputsNodes, float> InputValue;

            public PortArray<DataInput<MultipleInputsOutputsNodes, float>> DataArray2;

            public DataOutput<MultipleInputsOutputsNodes, float> Other;
            public DataOutput<MultipleInputsOutputsNodes, float> OutputValue;
            public DataOutput<MultipleInputsOutputsNodes, float> ConflictName;
        }

        public struct MyKernel : IGraphKernel<MyKernelData, MyKernelPorts>
        {
            public void Execute(RenderContext ctx, in MyKernelData data, ref MyKernelPorts ports) {}
        }
    }
    public class EntityManagerHandlerNode : SimulationNodeDefinition<EntityManagerHandlerNode.SimPorts>
        , IEntityManagerHandler<EntityManagerHandlerNode.NodeData>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            public MessageInput<EntityManagerHandlerNode, EntityManager> Context;
#pragma warning restore 649
        }

        public InputPortID GetPort(NodeHandle handle)
        {
            return (InputPortID)SimulationPorts.Context;
        }

        public struct NodeData : INodeData, IMsgHandler<EntityManager>
        {
            public void HandleMessage(MessageContext ctx, in EntityManager msg)
            {
                throw new System.NotImplementedException();
            }
        }
    }

    public class InternalEntityContextHandlerNode : SimulationNodeDefinition<InternalEntityContextHandlerNode.SimPorts>
        , IEntityManagerHandler<InternalEntityContextHandlerNode.NodeData>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            internal MessageInput<InternalEntityContextHandlerNode, EntityManager> Context;
#pragma warning restore 649
        }

        public InputPortID GetPort(NodeHandle handle)
        {
            return (InputPortID)SimulationPorts.Context;
        }

        public struct NodeData : INodeData, IMsgHandler<EntityManager>
        {
            public void HandleMessage(MessageContext ctx, in EntityManager msg)
            {
                throw new System.NotImplementedException();
            }
        }
    }

    public class NotEntityManagerHandlerNode : SimulationNodeDefinition<NotEntityManagerHandlerNode.SimPorts>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            internal MessageInput<NotEntityManagerHandlerNode, EntityManager> Context;
#pragma warning restore 649
        }

        public InputPortID GetPort(NodeHandle handle)
        {
            return (InputPortID)SimulationPorts.Context;
        }

        public struct NodeData : INodeData, IMsgHandler<EntityManager>
        {
            public void HandleMessage(MessageContext ctx, in EntityManager msg)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
