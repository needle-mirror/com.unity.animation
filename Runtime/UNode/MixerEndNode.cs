using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class MixerEndNode
        : NodeDefinition<MixerEndNode.Data, MixerEndNode.SimPorts, MixerEndNode.KernelData, MixerEndNode.KernelDefs, MixerEndNode.Kernel>
        , IMsgHandler<BlobAssetReference<RigDefinition>>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<MixerEndNode, BlobAssetReference<RigDefinition>> RigDefinition;
        }

        static readonly ProfilerMarker k_ProfileMixerEnd = new ProfilerMarker("Animation.MixerEnd");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<MixerEndNode, Buffer<float>>  DefaultPoseInput;
            public DataInput<MixerEndNode, Buffer<float>>  Input;
            public DataOutput<MixerEndNode, Buffer<float>> Output;
            public DataInput<MixerEndNode, float>          SumWeight;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileMixerEnd;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    throw new System.InvalidOperationException($"MixerEndNode Output is invalid.");

                var defaultPoseInputStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.DefaultPoseInput));
                var inputStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));

                data.ProfileMixerEnd.Begin();

                Core.MixerEnd(ref outputStream, ref inputStream, ref defaultPoseInputStream, context.Resolve(ports.SumWeight));

                data.ProfileMixerEnd.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMixerEnd = k_ProfileMixerEnd;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
        {
            GetKernelData(ctx.Handle).RigDefinition = rigBindings;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigBindings.Value.Bindings.CurveCount));
        }
    }
}

