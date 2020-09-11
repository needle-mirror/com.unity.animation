using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "7921f59316444412ae7120293a3cfe74", version: 1, category: "Animation Core/Utils", description: "Creates weight masks based on passed channel indices and weights")]
    [PortGroupDefinition(portGroupSizeDescription: "Number of channels", groupIndex: 1, minInstance: 1, maxInstance: -1)]
    public class WeightBuilderNode
        : NodeDefinition<WeightBuilderNode.Data, WeightBuilderNode.SimPorts, WeightBuilderNode.KernelData, WeightBuilderNode.KernelDefs, WeightBuilderNode.Kernel>
        , IRigContextHandler
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "197cf10d665a4f4981fad39738643998", isHidden: true)]
            public MessageInput<WeightBuilderNode, Rig> Rig;
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

        public struct Data : INodeData
        {
            internal NodeHandle<ConvertChannelIndicesNode> ConvertNode;
            internal NodeHandle<ComputeWeightDataNode> ComputeWeightDataNode;
        }

        public struct KernelData : IKernelData {}

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var data = ref GetNodeData(ctx.Handle);

            data.ConvertNode = Set.Create<ConvertChannelIndicesNode>();
            data.ComputeWeightDataNode = Set.Create<ComputeWeightDataNode>();

            Set.Connect(data.ConvertNode, ConvertChannelIndicesNode.KernelPorts.WeightCount, data.ComputeWeightDataNode, ComputeWeightDataNode.KernelPorts.WeightCount);
            Set.Connect(data.ConvertNode, ConvertChannelIndicesNode.KernelPorts.WeightDataOffsets, data.ComputeWeightDataNode, ComputeWeightDataNode.KernelPorts.WeightDataOffsets);

            ctx.ForwardInput(KernelPorts.DefaultWeight, data.ComputeWeightDataNode, ComputeWeightDataNode.KernelPorts.DefaultWeight);
            ctx.ForwardInput(KernelPorts.ChannelIndices, data.ConvertNode, ConvertChannelIndicesNode.KernelPorts.ChannelIndices);
            ctx.ForwardInput(KernelPorts.ChannelWeights, data.ComputeWeightDataNode, ComputeWeightDataNode.KernelPorts.ChannelWeights);
            ctx.ForwardOutput(KernelPorts.Output, data.ComputeWeightDataNode, ComputeWeightDataNode.KernelPorts.Output);
        }

        protected override void Destroy(DestroyContext ctx)
        {
            var nodeData = GetNodeData(ctx.Handle);
            Set.Destroy(nodeData.ConvertNode);
            Set.Destroy(nodeData.ComputeWeightDataNode);
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
            Set.SendMessage(nodeData.ConvertNode, ConvertChannelIndicesNode.SimulationPorts.Rig, rig);
            Set.SendMessage(nodeData.ComputeWeightDataNode, ComputeWeightDataNode.SimulationPorts.Rig, rig);
#pragma warning restore 0618
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }

#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    internal class ConvertChannelIndicesNode
        : NodeDefinition<ConvertChannelIndicesNode.Data, ConvertChannelIndicesNode.SimPorts, ConvertChannelIndicesNode.KernelData, ConvertChannelIndicesNode.KernelDefs, ConvertChannelIndicesNode.Kernel>
        , IMsgHandler<Rig>
    {
#pragma warning restore 0618

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

        public struct Data : INodeData {}

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
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

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            GetKernelData(ctx.Handle).RigDefinition = rig;
            Set.SetBufferSize(
                ctx.Handle,
                (OutputPortID)KernelPorts.WeightDataOffsets,
                Buffer<int>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.CurveCount : 0)
            );
        }
    }

#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    internal class ComputeWeightDataNode
        : NodeDefinition<ComputeWeightDataNode.Data, ComputeWeightDataNode.SimPorts, ComputeWeightDataNode.KernelData, ComputeWeightDataNode.KernelDefs, ComputeWeightDataNode.Kernel>
        , IMsgHandler<Rig>
    {
#pragma warning restore 0618

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

        public struct Data : INodeData {}

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
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

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            GetKernelData(ctx.Handle).RigDefinition = rig;
            Set.SetBufferSize(
                ctx.Handle,
                (OutputPortID)KernelPorts.Output,
                Buffer<WeightData>.SizeRequest(rig.Value.IsCreated ? Core.WeightDataSize(rig.Value) : 0)
            );
        }
    }
}
