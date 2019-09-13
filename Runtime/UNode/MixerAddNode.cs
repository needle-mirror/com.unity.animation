using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;
using Unity.Mathematics;

namespace Unity.Animation
{
    public class MixerAddNode
        : NodeDefinition<MixerAddNode.Data, MixerAddNode.SimPorts, MixerAddNode.KernelData, MixerAddNode.KernelDefs, MixerAddNode.Kernel>
        , IMsgHandler<BlobAssetReference<RigDefinition>>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<MixerAddNode, BlobAssetReference<RigDefinition>> RigDefinition;
        }

        static readonly ProfilerMarker k_ProfileMixerAdd = new ProfilerMarker("Animation.MixerAdd");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<MixerAddNode, Buffer<float>>  Add;
            public DataInput<MixerAddNode, Buffer<float>>  Input;
            public DataInput<MixerAddNode, float>          Weight;
            public DataInput<MixerAddNode, float>          SumWeightInput;

            public DataOutput<MixerAddNode, Buffer<float>> Output;
            public DataOutput<MixerAddNode, float>         SumWeightOutput;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileMixerAdd;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    throw new System.InvalidOperationException($"MixerAddNode Output is invalid.");

                var addStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Add));
                var inputStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                var weight = context.Resolve(ports.Weight);
                var sumWeight = context.Resolve(ports.SumWeightInput);

                data.ProfileMixerAdd.Begin();

                sumWeight = Core.MixerAdd(ref outputStream, ref inputStream, ref addStream, weight, sumWeight);

                context.Resolve(ref ports.SumWeightOutput) = sumWeight;

                data.ProfileMixerAdd.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMixerAdd = k_ProfileMixerAdd;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
        {
            GetKernelData(ctx.Handle).RigDefinition = rigBindings;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigBindings.Value.Bindings.CurveCount));
        }
    }
}

