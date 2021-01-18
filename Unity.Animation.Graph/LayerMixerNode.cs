using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    public enum BlendingMode
    {
        Override,
        Additive,
    }

    [NodeDefinition(guid: "368c53e919534c6f99c0d1b2577e3e2b", version: 1, category: "Animation Core/Mixers", description: "Blends animation streams based on an ordered layer approach. Each layer can blend in either override or additive mode. Weight masks can be built using the WeightBuilderNode.")]
    [PortGroupDefinition(portGroupSizeDescription: "Number of layers", groupIndex: 1, minInstance: 2, maxInstance: -1, simulationPortToDrive: "LayerCount")]
    public class LayerMixerNode
        : SimulationKernelNodeDefinition<LayerMixerNode.SimPorts, LayerMixerNode.KernelDefs>
        , IRigContextHandler<LayerMixerNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "78359c86e5bf400cb82958f6e40d2318", isHidden: true)]
            public MessageInput<LayerMixerNode, Rig> Rig;
            [PortDefinition(guid: "61f0790526654efd839f0f59bc5bf623", isHidden: true)]
            public MessageInput<LayerMixerNode, ushort> LayerCount;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "ac6a07f92da64d109d60c2dc36ba8c74", displayName: "Input", description: "Animation stream to blend", portGroupIndex: 1)]
            public PortArray<DataInput<LayerMixerNode, Buffer<AnimatedData>>> Inputs;
            [PortDefinition(guid: "911ebce1cfed48ecbed487a18ee4b35a", displayName: "Weight", description: "Layer weight", portGroupIndex: 1, defaultValue: 1f)]
            public PortArray<DataInput<LayerMixerNode, float>> Weights;
            [PortDefinition(guid: "22037e358f7549f58680c30a9f5b3fd5", displayName: "Weight Mask", description: "Channel specific weights which are also modulated by the layer weight", portGroupIndex: 1)]
            public PortArray<DataInput<LayerMixerNode, Buffer<WeightData>>> WeightMasks;
            [PortDefinition(guid: "e47e505bc758484fbbf4f2ccb40710db", displayName: "Blending Mode", description: "Type of blending to apply", portGroupIndex: 1, isStatic: true, defaultValue: BlendingMode.Override)]
            public PortArray<DataInput<LayerMixerNode, BlendingMode>> BlendingModes;

            [PortDefinition(guid: "1cccdcd2e0804c578a247871e981f73e", description: "Resulting animation stream")]
            public DataOutput<LayerMixerNode, Buffer<AnimatedData>> Output;
        }

        internal struct Data : INodeData, IMsgHandler<Rig>, IMsgHandler<ushort>
        {
            internal KernelData m_KernelData;

            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                m_KernelData.RigDefinition = rig;

                ctx.Set.SetBufferSize(
                    ctx.Handle,
                    (OutputPortID)KernelPorts.Output,
                    Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );

                ctx.UpdateKernelData(m_KernelData);
            }

            public void HandleMessage(MessageContext ctx, in ushort layerCount)
            {
                if (layerCount != m_KernelData.LayerCount)
                {
                    m_KernelData.LayerCount = layerCount;
                    ctx.Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.Inputs, layerCount);
                    ctx.Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.Weights, layerCount);
                    ctx.Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.WeightMasks, layerCount);
                    ctx.Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.BlendingModes, layerCount);

                    ctx.UpdateKernelData(m_KernelData);
                }
            }
        }

        internal struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition>    RigDefinition;
            public int                                  LayerCount;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public unsafe void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                var inputArray = context.Resolve(ports.Inputs);
                var weightArray = context.Resolve(ports.Weights);
                var weightMaskArray = context.Resolve(ports.WeightMasks);
                var blendingModeArray = context.Resolve(ports.BlendingModes);

                Core.ValidateBufferLengthsAreEqual(data.LayerCount, inputArray.Length);
                Core.ValidateBufferLengthsAreEqual(data.LayerCount, weightArray.Length);
                Core.ValidateBufferLengthsAreEqual(data.LayerCount, weightMaskArray.Length);
                Core.ValidateBufferLengthsAreEqual(data.LayerCount, blendingModeArray.Length);

                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                outputStream.ResetToDefaultValues();
                outputStream.ClearMasks();

                int expectedWeightDataSize = Core.WeightDataSize(data.RigDefinition);
                for (int i = 0; i < inputArray.Length; ++i)
                {
                    var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, inputArray[i].ToNative(context));
                    if (weightArray[i] > 0 && !inputStream.IsNull)
                    {
                        var weightMasks = weightMaskArray[i].ToNative(context);
                        if (weightMasks.Length == expectedWeightDataSize)
                        {
                            if (blendingModeArray[i] == BlendingMode.Override)
                                Core.BlendOverrideLayer(ref outputStream, ref inputStream, weightArray[i], weightMasks);
                            else
                                Core.BlendAdditiveLayer(ref outputStream, ref inputStream, weightArray[i], weightMasks);
                        }
                        else
                        {
                            if (blendingModeArray[i] == BlendingMode.Override)
                                Core.BlendOverrideLayer(ref outputStream, ref inputStream, weightArray[i]);
                            else
                                Core.BlendAdditiveLayer(ref outputStream, ref inputStream, weightArray[i]);
                        }
                    }
                }
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
