using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;
using Unity.Burst;

namespace Unity.Animation
{
    public class WeightNode
        : NodeDefinition<WeightNode.Data, WeightNode.SimPorts, WeightNode.KernelData, WeightNode.KernelDefs, WeightNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
    {
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.WeightNode");

        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<WeightNode, BlobAssetReference<RigDefinition>> RigDefinition;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<WeightNode, Buffer<float>> Input;
            public DataInput<WeightNode, Buffer<float>> Weights;
            public DataOutput<WeightNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileMarker;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                data.ProfileMarker.Begin();

                // Fill the destination stream with default values.
                var inputStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Input));
                var weightArray = context.Resolve(ports.Weights);
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition,context.Resolve(ref ports.Output));

                Core.WeightPose(ref outputStream, ref inputStream, ref weightArray);

                data.ProfileMarker.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigDefinition)
        {
            ref var kernelData = ref GetKernelData(ctx.Handle);

            kernelData.RigDefinition = rigDefinition;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigDefinition.Value.Bindings.CurveCount));
        }
    }
}
