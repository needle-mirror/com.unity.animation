using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    public enum BlendingMode
    {
        Override,
        Additive,
    }

    [NodeDefinition(category: "Animation Core/Mixers", description: "Blends animation streams based on an ordered layer approach. Each layer can blend in either override or additive mode. Weight masks can be built using the WeightBuilderNode.")]
    [PortGroupDefinition(portGroupSizeDescription: "Number of layers", groupIndex: 1, minInstance: 2, maxInstance: -1, simulationPortToDrive: "LayerCount")]
    public class LayerMixerNode
        : NodeDefinition<LayerMixerNode.Data, LayerMixerNode.SimPorts, LayerMixerNode.KernelData, LayerMixerNode.KernelDefs, LayerMixerNode.Kernel>
        , IMsgHandler<ushort>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.LayerMixPose");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden: true)]
            public MessageInput<LayerMixerNode, Rig> Rig;
            [PortDefinition(isHidden: true)]
            public MessageInput<LayerMixerNode, ushort> LayerCount;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(displayName: "Input", description: "Animation stream to blend", portGroupIndex: 1)]
            public PortArray<DataInput<LayerMixerNode, Buffer<AnimatedData>>> Inputs;
            [PortDefinition(displayName: "Weight", description: "Layer weight", portGroupIndex: 1, defaultValue: 1f)]
            public PortArray<DataInput<LayerMixerNode, float>> Weights;
            [PortDefinition(displayName: "Weight Mask", description: "Channel specific weights which are also modulated by the layer weight", portGroupIndex: 1)]
            public PortArray<DataInput<LayerMixerNode, Buffer<WeightData>>> WeightMasks;
            [PortDefinition(displayName: "Blending Mode", description: "Type of blending to apply", portGroupIndex: 1, isStatic: true, defaultValue: BlendingMode.Override)]
            public PortArray<DataInput<LayerMixerNode, BlendingMode>> BlendingModes;

            [PortDefinition(description: "Resulting animation stream")]
            public DataOutput<LayerMixerNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public unsafe struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfilerMarker;
#endif
            public BlobAssetReference<RigDefinition>    RigDefinition;

            public int                                  LayerCount;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public unsafe void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var inputArray = context.Resolve(ports.Inputs);
                var weightArray = context.Resolve(ports.Weights);
                var weightMaskArray = context.Resolve(ports.WeightMasks);
                var blendingModeArray = context.Resolve(ports.BlendingModes);

                if (inputArray.Length != data.LayerCount)
                    throw new System.InvalidOperationException($"LayerMixerNode: Inputs Port array length mismatch. Expecting '{data.LayerCount}' but was '{inputArray.Length}'.");

                if (weightArray.Length != data.LayerCount)
                    throw new System.InvalidOperationException($"LayerMixerNode: Weights Port array length mismatch. Expecting '{data.LayerCount}' but was '{weightArray.Length}'.");

                if (weightMaskArray.Length != data.LayerCount)
                    throw new System.InvalidOperationException($"LayerMixerNode: WeightMasks Port array length mismatch. Expecting '{data.LayerCount}' but was '{weightMaskArray.Length}'.");

                if (blendingModeArray.Length != data.LayerCount)
                    throw new System.InvalidOperationException($"LayerMixerNode: BlendingModes Port array length mismatch. Expecting '{data.LayerCount}' but was '{blendingModeArray.Length}'.");

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.Begin();
#endif

                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                AnimationStreamUtils.SetDefaultValues(ref outputStream);

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

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected unsafe override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfilerMarker;
        }

#endif

        protected unsafe override void Destroy(NodeHandle handle)
        {
            ref var kData = ref GetKernelData(handle);
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

        public unsafe void HandleMessage(in MessageContext ctx, in ushort layerCount)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            ref var data = ref GetNodeData(ctx.Handle);

            if (layerCount != kData.LayerCount)
            {
                kData.LayerCount = layerCount;
                Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.Inputs, layerCount);
                Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.Weights, layerCount);
                Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.WeightMasks, layerCount);
                Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.BlendingModes, layerCount);
            }
        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
