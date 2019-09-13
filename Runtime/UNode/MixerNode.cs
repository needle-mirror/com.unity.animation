using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class MixerNode
        : NodeDefinition<MixerNode.Data, MixerNode.SimPorts, MixerNode.KernelData, MixerNode.KernelDefs, MixerNode.Kernel>
        , IMsgHandler<BlobAssetReference<RigDefinition>>
        , IMsgHandler<float>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<MixerNode, BlobAssetReference<RigDefinition>> RigDefinition;
            public MessageInput<MixerNode, float> Blend;
        }

        static readonly ProfilerMarker k_ProfileMixPose = new ProfilerMarker("Animation.MixPose");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<MixerNode, Buffer<float>> Input0;
            public DataInput<MixerNode, Buffer<float>> Input1;
            public DataOutput<MixerNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileMixPose;

            // Instance data.
            public float Blend;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    throw new System.InvalidOperationException($"MixerNode Output is invalid.");

                var inputStream1 = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input0));
                var inputStream2 = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input1));

                data.ProfileMixPose.Begin();

                if (inputStream1.IsNull && inputStream2.IsNull)
                    AnimationStreamUtils.SetDefaultValues(ref outputStream);
                else if (inputStream1.IsNull && !inputStream2.IsNull)
                {
                    AnimationStreamUtils.SetDefaultValues(ref outputStream);
                    Core.Blend(ref outputStream, ref outputStream, ref inputStream2, data.Blend);
                }
                else if (!inputStream1.IsNull && inputStream2.IsNull)
                {
                    AnimationStreamUtils.SetDefaultValues(ref outputStream);
                    Core.Blend(ref outputStream, ref inputStream1, ref outputStream, data.Blend);
                }
                else
                    Core.Blend(ref outputStream, ref inputStream1, ref inputStream2, data.Blend);

                data.ProfileMixPose.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMixPose = k_ProfileMixPose;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigBindings)
        {
            GetKernelData(ctx.Handle).RigDefinition = rigBindings;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigBindings.Value.Bindings.CurveCount));
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            GetKernelData(ctx.Handle).Blend = msg;
        }
    }
}
