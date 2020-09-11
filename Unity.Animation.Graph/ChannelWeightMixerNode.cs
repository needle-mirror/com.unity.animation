using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "61d4abe4d0d84cae81d9bfde78b9d066", version: 1, category: "Animation Core/Mixers", description: "Blends two animation streams given per channel weight values. Weight masks can be built using the WeightBuilderNode.")]
    public class ChannelWeightMixerNode
        : NodeDefinition<ChannelWeightMixerNode.Data, ChannelWeightMixerNode.SimPorts, ChannelWeightMixerNode.KernelData, ChannelWeightMixerNode.KernelDefs, ChannelWeightMixerNode.Kernel>
        , IRigContextHandler
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "acd8325eb6134d27b54ccb0a13aa7c8e", isHidden: true)]
            public MessageInput<ChannelWeightMixerNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "8450950e37524046abc3358d307799b7", description: "Input stream 0")]
            public DataInput<ChannelWeightMixerNode, Buffer<AnimatedData>> Input0;
            [PortDefinition(guid: "0ec77f40188a4214951bb2ec027e8915", description: "Input stream 1")]
            public DataInput<ChannelWeightMixerNode, Buffer<AnimatedData>> Input1;
            [PortDefinition(guid: "aba316c6e62b4d58af903dbe9d6b57ad", description: "Blend weight that applies to all channels")]
            public DataInput<ChannelWeightMixerNode, float> Weight;
            [PortDefinition(guid: "33f6be8416f74339bcbf36e9d10bb132", description: "Channel specific weights which are also modulated by input Weight")]
            public DataInput<ChannelWeightMixerNode, Buffer<WeightData>> WeightMasks;

            [PortDefinition(guid: "5defde9b920a445781873403d363b93a", description: "Resulting stream")]
            public DataOutput<ChannelWeightMixerNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                outputStream.ValidateIsNotNull();

                var inputStream0 = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input0));
                var inputStream1 = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input1));

                var weight = context.Resolve(ports.Weight);
                var weightMasks = context.Resolve(ports.WeightMasks);

                Core.ValidateBufferLengthsAreEqual(Core.WeightDataSize(outputStream.Rig), weightMasks.Length);

                if (inputStream0.IsNull && inputStream1.IsNull)
                    outputStream.ResetToDefaultValues();
                else if (inputStream0.IsNull && !inputStream1.IsNull)
                {
                    outputStream.ResetToDefaultValues();
                    Core.Blend(ref outputStream, ref outputStream, ref inputStream1, weight, weightMasks);
                }
                else if (!inputStream0.IsNull && inputStream1.IsNull)
                {
                    outputStream.ResetToDefaultValues();
                    Core.Blend(ref outputStream, ref inputStream0, ref outputStream, weight, weightMasks);
                }
                else
                    Core.Blend(ref outputStream, ref inputStream0, ref inputStream1, weight, weightMasks);
            }
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            kData.RigDefinition = rig;
            Set.SetBufferSize(
                ctx.Handle,
                (OutputPortID)KernelPorts.Output,
                Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
            );
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
