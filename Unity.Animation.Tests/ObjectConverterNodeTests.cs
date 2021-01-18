using NUnit.Framework;
using Unity.DataFlowGraph;
using Unity.Mathematics;
using Hash128 = Unity.Entities.Hash128;

namespace Unity.Animation.Tests
{
    class ObjectConverterNodeTests
    {
        private class StorageNode<T> :
            SimulationKernelNodeDefinition<StorageNode<T>.SimPorts, StorageNode<T>.DataPorts>
            where T : struct
        {
            public struct SimPorts : ISimulationPortDefinition
            {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
                public MessageInput<StorageNode<T> , T> Input;
                public MessageOutput<StorageNode<T> , T> Output;
#pragma warning restore 649
            }

            internal struct Data : INodeData, IMsgHandler<T>
            {
                public T value;

                public void HandleMessage(MessageContext ctx, in T msg)
                {
                    value = msg;
                }
            }

            public struct KernelData : IKernelData
            {
            }
            public struct DataPorts : IKernelPortDefinition
            {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
                public DataInput<StorageNode<T> , T> Input;
                public DataOutput<StorageNode<T> , T> Output;
#pragma warning restore 649
            }

            //[BurstCompile]
            public struct Kernel : IGraphKernel<KernelData, DataPorts>
            {
                public void Execute(RenderContext ctx, in KernelData data, ref DataPorts ports)
                {
                    ctx.Resolve(ref ports.Output) = ctx.Resolve(ports.Input);
                }
            }
        }

        public struct BoolConvertible : IConvertibleObject<bool>
        {
            public bool Value { get; set; }
        }

        public struct IntConvertible : IConvertibleObject<int>
        {
            public int Value { get; set; }
        }

        public struct UIntConvertible : IConvertibleObject<uint>
        {
            public uint Value { get; set; }
        }

        public struct FloatConvertible : IConvertibleObject<float>
        {
            public float Value { get; set; }
        }

        public struct Float2Convertible : IConvertibleObject<float2>
        {
            public float2 Value { get; set; }
        }

        public struct Float3Convertible : IConvertibleObject<float3>
        {
            public float3 Value { get; set; }
        }

        public struct Float4Convertible : IConvertibleObject<float4>
        {
            public float4 Value { get; set; }
        }

        public struct ShortConvertible : IConvertibleObject<short>
        {
            public short Value { get; set; }
        }

        public struct UShortConvertible : IConvertibleObject<ushort>
        {
            public ushort Value { get; set; }
        }

        public struct LongConvertible : IConvertibleObject<long>
        {
            public long Value { get; set; }
        }

        public struct ULongConvertible : IConvertibleObject<ulong>
        {
            public ulong Value { get; set; }
        }

        public struct QuaternionConvertible : IConvertibleObject<quaternion>
        {
            public quaternion Value { get; set; }
        }

        public struct Hash128Convertible : IConvertibleObject<Hash128>
        {
            public Hash128 Value { get; set; }
        }

        public struct Int4Convertible : IConvertibleObject<int4>
        {
            public int4 Value { get; set; }
        }

        public abstract class BaseObjectConverterTestData
        {
            public delegate void CompareNodeValue(NodeSet set, NodeHandle h, object o);
            public delegate InputPortID GetInputPortID();
            public delegate OutputPortID GetOutputPortID();

            public abstract GetInputPortID StorageSimulationInputPort { get; }
            public abstract GetOutputPortID StorageSimulationOutputPort { get; }
            public abstract GetInputPortID StorageDataInputPort { get; }
            public abstract GetOutputPortID StorageDataOutputPort { get; }
            public GetInputPortID ObjectConverterSimulationInputPort { get; set; }
            public GetOutputPortID ObjectConverterSimulationOutputPort { get; set; }
            public abstract NodeHandle CreateStorageNode(NodeSet set);
            public abstract NodeHandle CreateObjectConverterNode(NodeSet set);
            public abstract CompareNodeValue CompareValue { get; }
            public object ExpectedValue { get; set; }
        }

        public class ObjectConverterTest<TInput, TOutput> : BaseObjectConverterTestData
            where TInput : struct
            where TOutput : struct, IConvertibleObject<TInput>
        {
            public override GetInputPortID StorageSimulationInputPort => () => (InputPortID)StorageNode<TOutput>.SimulationPorts.Input;
            public override GetOutputPortID StorageSimulationOutputPort => () => (OutputPortID)StorageNode<TOutput>.SimulationPorts.Output;
            public override GetInputPortID StorageDataInputPort => () => (InputPortID)StorageNode<TOutput>.KernelPorts.Input;
            public override GetOutputPortID StorageDataOutputPort => () => (OutputPortID)StorageNode<TOutput>.KernelPorts.Output;
            public override NodeHandle CreateStorageNode(NodeSet set) => set.Create<StorageNode<TOutput>>();
            public override NodeHandle CreateObjectConverterNode(NodeSet set) => set.Create<ObjectConverterNode<TInput, TOutput>>();
            public override CompareNodeValue CompareValue => (s, h, o) =>
            {
                s.SendTest<StorageNode<TOutput>.Data>(h, ctx =>
                {
                    Assert.That(ctx.NodeData.value.Value.Equals(o));
                });
            };
        }

        private static readonly BaseObjectConverterTestData[] ObjectConverterTestSources =
        {
            new ObjectConverterTest<bool, BoolConvertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<bool, BoolConvertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<bool, BoolConvertible>.SimulationPorts.Output,
                ExpectedValue = true
            },
            new ObjectConverterTest<int, IntConvertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<int, IntConvertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<int, IntConvertible>.SimulationPorts.Output,
                ExpectedValue = 4
            },
            new ObjectConverterTest<uint, UIntConvertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<uint, UIntConvertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<uint, UIntConvertible>.SimulationPorts.Output,
                ExpectedValue = 4U
            },
            new ObjectConverterTest<float, FloatConvertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<float, FloatConvertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<float, FloatConvertible>.SimulationPorts.Output,
                ExpectedValue = 4.0f
            },
            new ObjectConverterTest<float2, Float2Convertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<float2, Float2Convertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<float2, Float2Convertible>.SimulationPorts.Output,
                ExpectedValue = new float2(2, 4)
            },
            new ObjectConverterTest<float3, Float3Convertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<float3, Float3Convertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<float3, Float3Convertible>.SimulationPorts.Output,
                ExpectedValue = new float3(2, 4, 8)
            },
            new ObjectConverterTest<float4, Float4Convertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<float4, Float4Convertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<float4, Float4Convertible>.SimulationPorts.Output,
                ExpectedValue = new float4(2, 4, 8, 16)
            },
            new ObjectConverterTest<short, ShortConvertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<short, ShortConvertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<short, ShortConvertible>.SimulationPorts.Output,
                ExpectedValue = (short)4
            },
            new ObjectConverterTest<ushort, UShortConvertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<ushort, UShortConvertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<ushort, UShortConvertible>.SimulationPorts.Output,
                ExpectedValue = (ushort)4
            },
            new ObjectConverterTest<long, LongConvertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<long, LongConvertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<long, LongConvertible>.SimulationPorts.Output,
                ExpectedValue = 4L
            },
            new ObjectConverterTest<ulong, ULongConvertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<ulong, ULongConvertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<ulong, ULongConvertible>.SimulationPorts.Output,
                ExpectedValue = 4UL
            },
            new ObjectConverterTest<quaternion, QuaternionConvertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<quaternion, QuaternionConvertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<quaternion, QuaternionConvertible>.SimulationPorts.Output,
                ExpectedValue = new quaternion(2, 4, 8, 16)
            },
            new ObjectConverterTest<Hash128, Hash128Convertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<Hash128, Hash128Convertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<Hash128, Hash128Convertible>.SimulationPorts.Output,
                ExpectedValue = new Hash128("4fb16d384de56ba44abed9ffe2fc0370")
            },
            new ObjectConverterTest<int4, Int4Convertible>()
            {
                ObjectConverterSimulationInputPort = () => (InputPortID)ObjectConverterNode<int4, Int4Convertible>.SimulationPorts.Input,
                ObjectConverterSimulationOutputPort = () => (OutputPortID)ObjectConverterNode<int4, Int4Convertible>.SimulationPorts.Output,
                ExpectedValue = new int4(-1, 1, -2, 8)
            },
        };

        [Test]
        public void ObjectConverterNode_ConvertsValue([ValueSource("ObjectConverterTestSources")] BaseObjectConverterTestData test)
        {
            using (var set = new NodeSet())
            {
                var objectConvertNode = test.CreateObjectConverterNode(set);
                var storageNode = test.CreateStorageNode(set);

                set.Connect(
                    objectConvertNode,
                    test.ObjectConverterSimulationOutputPort(),
                    storageNode,
                    test.StorageSimulationInputPort());

                var type = test.ExpectedValue.GetType();
                if (type == typeof(bool))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (bool)test.ExpectedValue);
                if (type == typeof(long))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (long)test.ExpectedValue);
                if (type == typeof(ulong))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (ulong)test.ExpectedValue);
                if (type == typeof(uint))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (uint)test.ExpectedValue);
                if (type == typeof(ushort))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (ushort)test.ExpectedValue);
                if (type == typeof(int))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (int)test.ExpectedValue);
                if (type == typeof(short))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (short)test.ExpectedValue);
                if (type == typeof(float))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (float)test.ExpectedValue);
                if (type == typeof(float2))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (float2)test.ExpectedValue);
                if (type == typeof(float3))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (float3)test.ExpectedValue);
                if (type == typeof(float4))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (float4)test.ExpectedValue);
                if (type == typeof(quaternion))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (quaternion)test.ExpectedValue);
                if (type == typeof(Unity.Entities.Hash128))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (Unity.Entities.Hash128)test.ExpectedValue);
                if (type == typeof(int4))
                    set.SendMessage(objectConvertNode, test.ObjectConverterSimulationInputPort(), (int4)test.ExpectedValue);

                set.Update();

                test.CompareValue(set, storageNode, test.ExpectedValue);

                set.Destroy(storageNode, objectConvertNode);
            }
        }
    }
}
