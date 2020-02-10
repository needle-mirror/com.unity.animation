using System.Runtime.CompilerServices;

using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Animation
{
    public enum BlendingMode
    {
        Override,
        Additive,
    }

    [NodeDefinition(category:"Animation Core/Mixers", description:"Blends animation streams based on an ordered layer approach. Each layer can blend in either override or additive mode. Weight masks can be built using the WeightBuilderNode.")]
    [PortGroupDefinition(portGroupSizeDescription:"Number of layers", groupIndex:1, minInstance:2, maxInstance:-1, simulationPortToDrive:"LayerCount")]
    public class LayerMixerNode
        : NodeDefinition<LayerMixerNode.Data, LayerMixerNode.SimPorts, LayerMixerNode.KernelData, LayerMixerNode.KernelDefs, LayerMixerNode.Kernel>
        , IMsgHandler<Rig>
        , IMsgHandler<BlendingMode>
        , IMsgHandler<ushort>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<LayerMixerNode, Rig> Rig;
            [PortDefinition(isHidden:true)]
            public MessageInput<LayerMixerNode, ushort> LayerCount;
            [PortDefinition(displayName:"Blending Mode", description:"Type of blending to apply", portGroupIndex:1, isStatic:true)]
            public PortArray<MessageInput<LayerMixerNode, BlendingMode>> BlendingModes;
        }

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.LayerMixPose");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(displayName:"Input", description:"Animation stream to blend", portGroupIndex:1)]
            public PortArray<DataInput<LayerMixerNode, Buffer<AnimatedData>>> Inputs;
            [PortDefinition(displayName:"Weight", description:"Layer weight", portGroupIndex:1, defaultValue:1f)]
            public PortArray<DataInput<LayerMixerNode, float>> Weights;
            [PortDefinition(displayName:"Weight Mask", description:"Channel specific weights which are also modulated by the layer weight", portGroupIndex:1)]
            public PortArray<DataInput<LayerMixerNode, Buffer<WeightData>>> WeightMasks;

            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<LayerMixerNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
            public GraphValue<Buffer<AnimatedData>> GraphValue;
        }

        public unsafe struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition>    RigDefinition;

            public UnsafeList*                          BlendingModes;

            public int                                  LayerCount;

            public ProfilerMarker                       ProfilerMarker;

            public JobHandle                            ResizeListJob;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public unsafe void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var inputArray = context.Resolve(ports.Inputs);
                var weightArray = context.Resolve(ports.Weights);
                var weightMaskArray = context.Resolve(ports.WeightMasks);

                if(inputArray.Length != data.LayerCount)
                    throw new System.InvalidOperationException($"LayerMixerNode: Inputs Port array length mismatch. Expecting '{data.LayerCount}' but was '{inputArray.Length}'.");

                if(weightArray.Length != data.LayerCount)
                    throw new System.InvalidOperationException($"LayerMixerNode: Weights Port array length mismatch. Expecting '{data.LayerCount}' but was '{weightArray.Length}'.");

                if(weightMaskArray.Length != data.LayerCount)
                    throw new System.InvalidOperationException($"LayerMixerNode: WeightMasks Port array length mismatch. Expecting '{data.LayerCount}' but was '{weightArray.Length}'.");

                data.ProfilerMarker.Begin();

                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                AnimationStreamUtils.SetDefaultValues(ref outputStream);

                int expectedWeightDataSize = Core.WeightDataSize(data.RigDefinition);
                for (int i = 0; i < inputArray.Length; ++i)
                {
                    var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, inputArray[i].ToNative(context));
                    if (weightArray[i] > 0 && !inputStream.IsNull)
                    {
                        var weightMasks = weightMaskArray[i].ToNative(context);
                        if(weightMasks.Length == expectedWeightDataSize)
                        {
                            if (ItemAt<BlendingMode>(data.BlendingModes, i) == BlendingMode.Override)
                                Core.BlendOverrideLayer(ref outputStream, ref inputStream, weightArray[i], weightMasks);
                            else
                                Core.BlendAdditiveLayer(ref outputStream, ref inputStream, weightArray[i], weightMasks);
                        }
                        else
                        {
                            if (ItemAt<BlendingMode>(data.BlendingModes, i) == BlendingMode.Override)
                                Core.BlendOverrideLayer(ref outputStream, ref inputStream, weightArray[i]);
                            else
                                Core.BlendAdditiveLayer(ref outputStream, ref inputStream, weightArray[i]);
                        }
                    }
                }

                data.ProfilerMarker.End();
            }
        }

        internal static unsafe ref T ItemAt<T>(UnsafeList* list, int index)
            where T : struct
        {
            return ref Unsafe.AsRef<T>((byte*)list->Ptr + index * Unsafe.SizeOf<T>());
        }

        protected unsafe override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfilerMarker;

            kData.BlendingModes = UnsafeList.Create(Unsafe.SizeOf<BlendingMode>(), UnsafeUtility.AlignOf<BlendingMode>(), 0, Allocator.Persistent);

            // Since the kernel is double buffered we need to wait until all kernel evaluation finish
            // before we can change allocation
            // (A GraphValue on _any_ DataOutput port from this node will do).
            GetNodeData(ctx.Handle).GraphValue = Set.CreateGraphValue<Buffer<AnimatedData>>(ctx.Handle, (OutputPortID) KernelPorts.Output);
        }
        protected unsafe override void Destroy(NodeHandle handle)
        {
            ref var kData = ref GetKernelData(handle);

            DisposeKernelMemory(ref kData);

            Set.ReleaseGraphValue(GetNodeData(handle).GraphValue);
        }

        protected unsafe void ResizeKernelMemory(ref KernelData kData)
        {
            kData.ResizeListJob.Complete();

            Set.GetGraphValueResolver(out var safeToResize);
            var resizeJob1 = new ResizeBlendingModeJob
            {
                List = kData.BlendingModes,
                Options = NativeArrayOptions.ClearMemory,
                Length = kData.LayerCount
            };
            kData.ResizeListJob = resizeJob1.Schedule(safeToResize);
            Set.InjectDependencyFromConsumer(kData.ResizeListJob);
        }

        protected unsafe void DisposeKernelMemory(ref KernelData kData)
        {
            kData.ResizeListJob.Complete();

            Set.GetGraphValueResolver(out var safeToDispose);
            var disposeJob = kData.BlendingModes->Dispose(safeToDispose);
            Set.InjectDependencyFromConsumer(disposeJob);
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

            ResizeKernelMemory(ref kData);
        }

        public unsafe void HandleMessage(in MessageContext ctx, in BlendingMode blendingMode)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            if (ctx.ArrayIndex >= kData.LayerCount)
                throw new System.ArgumentOutOfRangeException($"LayerMixerNode: BlendingModes port array index '{ctx.ArrayIndex}' was out of bounds, LayerMixer only has '{kData.LayerCount}' layer.");

            kData.ResizeListJob.Complete();

            ItemAt<BlendingMode>(kData.BlendingModes, ctx.ArrayIndex) = blendingMode;
        }

        public unsafe void HandleMessage(in MessageContext ctx, in ushort layerCount)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            ref var data = ref GetNodeData(ctx.Handle);

            if(layerCount != kData.LayerCount)
            {
                kData.LayerCount = layerCount;

                Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.SimulationPorts.BlendingModes, layerCount);
                Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.Inputs, layerCount);
                Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.Weights, layerCount);
                Set.SetPortArraySize(ctx.Handle, (InputPortID)LayerMixerNode.KernelPorts.WeightMasks, layerCount);

                ResizeKernelMemory(ref kData);
            }
        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);

        [BurstCompile]
        unsafe struct ResizeBlendingModeJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList* List;
            public NativeArrayOptions Options;
            public int Length;

            public void Execute()
            {
                if (List->Allocator == Allocator.Invalid)
                    List->Allocator = Allocator.Persistent;

                List->Resize<BlendingMode>(Length, Options);
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
