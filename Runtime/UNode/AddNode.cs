using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class AddNode
        : NodeDefinition<AddNode.Data, AddNode.SimPorts, AddNode.KernelData, AddNode.KernelDefs, AddNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
    {
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.AddNode");

        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<AddNode, BlobAssetReference<RigDefinition>> RigDefinition;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<AddNode, Buffer<float>> InputA;
            public DataInput<AddNode, Buffer<float>> InputB;
            public DataOutput<AddNode, Buffer<float>> Output;
        }

        public struct Data : INodeData { }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileMarker;
        }

        [BurstCompile(FloatMode = FloatMode.Fast)]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                data.ProfileMarker.Begin();

                // Fill the destination stream with default values.
                var inputStreamA = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.InputA));
                var inputStreamB = AnimationStreamProvider.CreateReadOnly(data.RigDefinition, context.Resolve(ports.InputB));
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition, context.Resolve(ref ports.Output));

                Core.AddPose(ref outputStream, ref inputStreamA, ref inputStreamB);

                data.ProfileMarker.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }

        public override void OnUpdate(NodeHandle handle) { }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigDefinition)
        {
            ref var kernelData = ref GetKernelData(ctx.Handle);

            kernelData.RigDefinition = rigDefinition;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigDefinition.Value.Bindings.CurveCount));
        }
    }
}

