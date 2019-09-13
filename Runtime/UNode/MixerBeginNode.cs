using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class MixerBeginNode
        : NodeDefinition<MixerBeginNode.Data, MixerBeginNode.SimPorts, MixerBeginNode.KernelData, MixerBeginNode.KernelDefs, MixerBeginNode.Kernel>
        , IMsgHandler<BlobAssetReference<RigDefinition>>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<MixerBeginNode, BlobAssetReference<RigDefinition>> RigDefinition;
        }

        static readonly ProfilerMarker k_ProfileMixerBegin = new ProfilerMarker("Animation.MixerBegin");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataOutput<MixerBeginNode, Buffer<float>> Output;
            public DataOutput<MixerBeginNode, float>         SumWeight;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileMixerBegin;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    throw new System.InvalidOperationException($"MixerBeginNode Output is invalid.");

                data.ProfileMixerBegin.Begin();

                Core.MixerBegin(ref outputStream);

                context.Resolve(ref ports.SumWeight) = 0.0f;

                data.ProfileMixerBegin.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMixerBegin = k_ProfileMixerBegin;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
        {
            GetKernelData(ctx.Handle).RigDefinition = rigBindings;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigBindings.Value.Bindings.CurveCount));
        }
    }
}

