using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public enum BlendingMode
    {
        Override,
        Additive,
    }

    public class LayerMixerNode
        : NodeDefinition<LayerMixerNode.Data, LayerMixerNode.SimPorts, LayerMixerNode.KernelData, LayerMixerNode.KernelDefs, LayerMixerNode.Kernel>
        , IMsgHandler<BlobAssetReference<RigDefinition>>
        , IMsgHandler<BlendingMode>
        , IMsgHandler<float>
        , IMsgHandler<NativeBitSet>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<LayerMixerNode, BlobAssetReference<RigDefinition>>   RigDefinition;

            public MessageInput<LayerMixerNode, BlendingMode>                        BlendModeInput0;
            public MessageInput<LayerMixerNode, BlendingMode>                        BlendModeInput1;
            public MessageInput<LayerMixerNode, BlendingMode>                        BlendModeInput2;
            public MessageInput<LayerMixerNode, BlendingMode>                        BlendModeInput3;

            public MessageInput<LayerMixerNode, float>                               WeightInput0;
            public MessageInput<LayerMixerNode, float>                               WeightInput1;
            public MessageInput<LayerMixerNode, float>                               WeightInput2;
            public MessageInput<LayerMixerNode, float>                               WeightInput3;

            public MessageInput<LayerMixerNode, NativeBitSet>                        MaskInput0;
            public MessageInput<LayerMixerNode, NativeBitSet>                        MaskInput1;
            public MessageInput<LayerMixerNode, NativeBitSet>                        MaskInput2;
            public MessageInput<LayerMixerNode, NativeBitSet>                        MaskInput3;
        }

        static readonly ProfilerMarker k_ProfileLayerMixPose = new ProfilerMarker("Animation.LayerMixPose");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<LayerMixerNode, Buffer<float>>   Input0;
            public DataInput<LayerMixerNode, Buffer<float>>   Input1;
            public DataInput<LayerMixerNode, Buffer<float>>   Input2;
            public DataInput<LayerMixerNode, Buffer<float>>   Input3;

            public DataOutput<LayerMixerNode, Buffer<float>>  Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition>    RigDefinition;

            public float                                WeightInput0;
            public float                                WeightInput1;
            public float                                WeightInput2;
            public float                                WeightInput3;

            public BlendingMode                         BlendingModeInput0;
            public BlendingMode                         BlendingModeInput1;
            public BlendingMode                         BlendingModeInput2;
            public BlendingMode                         BlendingModeInput3;

            public NativeBitSet                         MaskInput0;
            public NativeBitSet                         MaskInput1;
            public NativeBitSet                         MaskInput2;
            public NativeBitSet                         MaskInput3;

            public ProfilerMarker                       ProfileLayerMixPose;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfileLayerMixPose.Begin();

                var outputStream = AnimationStreamProvider.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                AnimationStreamUtils.SetDefaultValues(ref outputStream);

                // TODO: clean up write mask here before processing layers.
                var inputStream0 = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input0));
                if (data.WeightInput0 > 0 && !inputStream0.IsNull)
                {
                    if (data.BlendingModeInput0 == BlendingMode.Override)
                        Core.BlendOverrideLayer(ref outputStream, ref inputStream0, data.WeightInput0, data.MaskInput0);
                    else
                        Core.BlendAdditiveLayer(ref outputStream, ref inputStream0, data.WeightInput0, data.MaskInput0);
                }

                var inputStream1 = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input1));
                if (data.WeightInput1 > 0 && !inputStream1.IsNull)
                {
                    if(data.BlendingModeInput1 == BlendingMode.Override)
                        Core.BlendOverrideLayer(ref outputStream, ref inputStream1, data.WeightInput1, data.MaskInput1);
                    else
                        Core.BlendAdditiveLayer(ref outputStream, ref inputStream1, data.WeightInput1, data.MaskInput1);
                }

                var inputStream2 = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input2));
                if (data.WeightInput2 > 0 && !inputStream2.IsNull)
                {
                    if(data.BlendingModeInput2 == BlendingMode.Override)
                        Core.BlendOverrideLayer(ref outputStream, ref inputStream2, data.WeightInput2, data.MaskInput2);
                    else
                        Core.BlendAdditiveLayer(ref outputStream, ref inputStream2, data.WeightInput2, data.MaskInput2);
                }

                var inputStream3 = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input3));
                if (data.WeightInput3 > 0 && !inputStream3.IsNull)
                {
                    if(data.BlendingModeInput3 == BlendingMode.Override)
                        Core.BlendOverrideLayer(ref outputStream, ref inputStream3, data.WeightInput3, data.MaskInput3);
                    else
                        Core.BlendAdditiveLayer(ref outputStream, ref inputStream3, data.WeightInput3, data.MaskInput3);
                }

                data.ProfileLayerMixPose.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileLayerMixPose = k_ProfileLayerMixPose;
        }

        public override void Destroy(NodeHandle handle)
        {
            ref var kData = ref GetKernelData(handle);

            if (kData.MaskInput0.IsCreated)
                kData.MaskInput0.Dispose();

            if(kData.MaskInput1.IsCreated)
                kData.MaskInput1.Dispose();

            if(kData.MaskInput2.IsCreated)
                kData.MaskInput2.Dispose();

            if(kData.MaskInput3.IsCreated)
                kData.MaskInput3.Dispose();
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.RigDefinition = rigBindings;

            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigBindings.Value.Bindings.CurveCount));

            kData.MaskInput0 = new NativeBitSet(rigBindings.Value.Bindings.BindingCount, Allocator.Persistent);
            kData.MaskInput1 = new NativeBitSet(rigBindings.Value.Bindings.BindingCount, Allocator.Persistent);
            kData.MaskInput2 = new NativeBitSet(rigBindings.Value.Bindings.BindingCount, Allocator.Persistent);
            kData.MaskInput3 = new NativeBitSet(rigBindings.Value.Bindings.BindingCount, Allocator.Persistent);

            // By default all channels are blended
            kData.MaskInput0.Set();
            kData.MaskInput1.Set();
            kData.MaskInput2.Set();
            kData.MaskInput3.Set();
        }

        public void HandleMessage(in MessageContext ctx, in BlendingMode blendingMode)
        {
            if(ctx.Port == SimulationPorts.BlendModeInput0)
                GetKernelData(ctx.Handle).BlendingModeInput0 = blendingMode;
            else if(ctx.Port ==  SimulationPorts.BlendModeInput1)
                GetKernelData(ctx.Handle).BlendingModeInput1 = blendingMode;
            else if(ctx.Port ==  SimulationPorts.BlendModeInput2)
                GetKernelData(ctx.Handle).BlendingModeInput2 = blendingMode;
            else if(ctx.Port == SimulationPorts.BlendModeInput3)
                GetKernelData(ctx.Handle).BlendingModeInput3 = blendingMode;
        }

        public void HandleMessage(in MessageContext ctx, in float weight)
        {
            if(ctx.Port == SimulationPorts.WeightInput0)
                GetKernelData(ctx.Handle).WeightInput0 = weight;
            else if(ctx.Port == SimulationPorts.WeightInput1)
                GetKernelData(ctx.Handle).WeightInput1 = weight;
            else if(ctx.Port == SimulationPorts.WeightInput2)
                GetKernelData(ctx.Handle).WeightInput2 = weight;
            else if(ctx.Port == SimulationPorts.WeightInput3)
                GetKernelData(ctx.Handle).WeightInput3 = weight;
        }
        public void HandleMessage(in MessageContext ctx, in NativeBitSet mask)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            if (kData.RigDefinition == default)
                throw new System.NullReferenceException($"Cannot set mask if RigDefinition is null");

            if (kData.RigDefinition.Value.Bindings.BindingCount != mask.Length)
                throw new System.ArgumentException($"mask length '{mask.Length}' has to match RigDefinition binding count '{kData.RigDefinition.Value.Bindings.BindingCount}'", "mask");

            if(ctx.Port == SimulationPorts.MaskInput0)
                kData.MaskInput0 = mask.Copy();
            else if(ctx.Port == SimulationPorts.MaskInput1)
                kData.MaskInput1 = mask.Copy();
            else if(ctx.Port == SimulationPorts.MaskInput2)
                kData.MaskInput2 = mask.Copy();
            else if(ctx.Port == SimulationPorts.MaskInput3)
                kData.MaskInput3 = mask.Copy();

        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);
    }
}
