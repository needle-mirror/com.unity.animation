using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Animation
{
    [NodeDefinition(guid: "7921f59316444412ae7120293a3cfe74", version: 1, category: "Animation Core/Utils", description: "Creates weight masks based on passed channel indices and weights")]
    [PortGroupDefinition(portGroupSizeDescription: "Number of channels", groupIndex: 1, minInstance: 1, maxInstance: -1)]
    public class WeightBuilderNode
        : SimulationKernelNodeDefinition<WeightBuilderNode.SimPorts, WeightBuilderNode.KernelDefs>
        , IRigContextHandler<WeightBuilderNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "197cf10d665a4f4981fad39738643998", isHidden: true)]
            public MessageInput<WeightBuilderNode, Rig> Rig;

            internal MessageOutput<WeightBuilderNode, Rig> m_OutRig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "2e023389c8bc4077b1a2e6b0107a0f7d", description: "Default weight that is applied to all channels", defaultValue: 1f)]
            public DataInput<WeightBuilderNode, float> DefaultWeight;
            [PortDefinition(guid: "4a113099c106421596a3e0354e0a3e39", displayName: "Channel Index", description: "Channel index in rig definition", portGroupIndex: 1)]
            public PortArray<DataInput<WeightBuilderNode, int>> ChannelIndices;
            [PortDefinition(guid: "4facdf4f067e42b290e8d7cbdf320ef8", displayName: "Channel Weight", description: "Weight value for this channel", portGroupIndex: 1)]
            public PortArray<DataInput<WeightBuilderNode, float>> ChannelWeights;

            [PortDefinition(guid: "0c1edff7de2a4184b203814dcc77ac1e", description: "Resulting weight data")]
            public DataOutput<WeightBuilderNode, Buffer<WeightData>> Output;
        }

        struct Data : INodeData, IInit, IDestroy, IMsgHandler<Rig>
        {
            internal NodeHandle<ConvertChannelIndicesNode> m_ConvertNode;
            internal NodeHandle<ComputeWeightDataNode> m_ComputeWeightDataNode;

            public void Init(InitContext ctx)
            {
                m_ConvertNode = ctx.Set.Create<ConvertChannelIndicesNode>();
                m_ComputeWeightDataNode = ctx.Set.Create<ComputeWeightDataNode>();

                ctx.Set.Connect(ctx.Handle, (OutputPortID)SimulationPorts.m_OutRig, m_ConvertNode, (InputPortID)ConvertChannelIndicesNode.SimulationPorts.Rig);
                ctx.Set.Connect(ctx.Handle, (OutputPortID)SimulationPorts.m_OutRig, m_ComputeWeightDataNode, (InputPortID)ComputeWeightDataNode.SimulationPorts.Rig);
                ctx.Set.Connect(m_ConvertNode, ConvertChannelIndicesNode.KernelPorts.WeightCount, m_ComputeWeightDataNode, ComputeWeightDataNode.KernelPorts.WeightCount);
                ctx.Set.Connect(m_ConvertNode, ConvertChannelIndicesNode.KernelPorts.WeightDataOffsets, m_ComputeWeightDataNode, ComputeWeightDataNode.KernelPorts.WeightDataOffsets);

                ctx.ForwardInput(KernelPorts.DefaultWeight, m_ComputeWeightDataNode, ComputeWeightDataNode.KernelPorts.DefaultWeight);
                ctx.ForwardInput(KernelPorts.ChannelIndices, m_ConvertNode, ConvertChannelIndicesNode.KernelPorts.ChannelIndices);
                ctx.ForwardInput(KernelPorts.ChannelWeights, m_ComputeWeightDataNode, ComputeWeightDataNode.KernelPorts.ChannelWeights);
                ctx.ForwardOutput(KernelPorts.Output, m_ComputeWeightDataNode, ComputeWeightDataNode.KernelPorts.Output);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(m_ConvertNode);
                ctx.Set.Destroy(m_ComputeWeightDataNode);
            }

            public void HandleMessage(MessageContext ctx, in Rig rig) =>
                ctx.EmitMessage(SimulationPorts.m_OutRig, rig);
        }

        struct KernelData : IKernelData {}

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }

    internal class ConvertChannelIndicesNode
        : SimulationKernelNodeDefinition<ConvertChannelIndicesNode.SimPorts, ConvertChannelIndicesNode.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<ConvertChannelIndicesNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public PortArray<DataInput<ConvertChannelIndicesNode, int>> ChannelIndices;

            public DataOutput<ConvertChannelIndicesNode, int>         WeightCount;
            public DataOutput<ConvertChannelIndicesNode, Buffer<int>> WeightDataOffsets;
        }

        struct Data : INodeData, IMsgHandler<Rig>
        {
            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                ctx.UpdateKernelData(new KernelData
                {
                    RigDefinition = rig
                });

                ctx.Set.SetBufferSize(
                    ctx.Handle,
                    (OutputPortID)KernelPorts.WeightDataOffsets,
                    Buffer<int>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.CurveCount : 0)
                );
            }
        }

        struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                var channelIndexPorts = context.Resolve(ports.ChannelIndices);
                var outOffsets = context.Resolve(ref ports.WeightDataOffsets);

                context.Resolve(ref ports.WeightCount) = channelIndexPorts.Length;
                for (int i = 0; i < channelIndexPorts.Length; ++i)
                {
                    outOffsets[i] = Core.ChannelIndexToWeightDataOffset(
                        data.RigDefinition,
                        channelIndexPorts[i]
                    );
                }
            }
        }
    }

    internal class ComputeWeightDataNode
        : SimulationKernelNodeDefinition<ComputeWeightDataNode.SimPorts, ComputeWeightDataNode.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<ComputeWeightDataNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<ComputeWeightDataNode, float>            DefaultWeight;
            public PortArray<DataInput<ComputeWeightDataNode, float>> ChannelWeights;
            public DataInput<ComputeWeightDataNode, int>              WeightCount;
            public DataInput<ComputeWeightDataNode, Buffer<int>>      WeightDataOffsets;

            public DataOutput<ComputeWeightDataNode, Buffer<WeightData>> Output;
        }

        struct Data : INodeData, IMsgHandler<Rig>
        {
            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                ctx.UpdateKernelData(new KernelData
                {
                    RigDefinition = rig
                });

                ctx.Set.SetBufferSize(
                    ctx.Handle,
                    (OutputPortID)KernelPorts.Output,
                    Buffer<WeightData>.SizeRequest(rig.Value.IsCreated ? Core.WeightDataSize(rig.Value) : 0)
                );
            }
        }

        struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                var channelWeightPorts = context.Resolve(ports.ChannelWeights);
                var maskOffsets = context.Resolve(ports.WeightDataOffsets);

                Core.ValidateBufferLengthsAreEqual(context.Resolve(ports.WeightCount), channelWeightPorts.Length);

                var channelWeights = new NativeArray<float>(channelWeightPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                channelWeightPorts.CopyTo(channelWeights);

                Core.ComputeWeightDataFromWeightOffsets(
                    data.RigDefinition,
                    context.Resolve(ports.DefaultWeight),
                    maskOffsets,
                    channelWeights,
                    context.Resolve(ref ports.Output)
                );
            }
        }
    }
}
