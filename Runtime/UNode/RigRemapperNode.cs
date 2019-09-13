using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class RigRemapperNode
        : NodeDefinition<RigRemapperNode.Data, RigRemapperNode.SimPorts, RigRemapperNode.KernelData, RigRemapperNode.KernelDefs, RigRemapperNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
            , IMsgHandler<BlobAssetReference<RigRemapTable>>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<RigRemapperNode, BlobAssetReference<RigDefinition>> SourceRigDefinition;
            public MessageInput<RigRemapperNode, BlobAssetReference<RigDefinition>> DestinationRigDefinition;

            public MessageInput<RigRemapperNode, BlobAssetReference<RigRemapTable>> RemapTable;
        }

        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.RigRemapperNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<RigRemapperNode, Buffer<float>> Input;
            public DataOutput<RigRemapperNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {

        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> SourceRigDefinition;
            public BlobAssetReference<RigDefinition> DestinationRigDefinition;
            public BlobAssetReference<RigRemapTable> RemapTable;

            public ProfilerMarker ProfileMarker;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.SourceRigDefinition == default)
                    return;

                if (data.DestinationRigDefinition == default)
                    return;

                if (data.RemapTable == default)
                    return;

                data.ProfileMarker.Begin();

                // Fill the destination stream with default values.
                var destinationStream = AnimationStreamProvider.Create(data.DestinationRigDefinition, context.Resolve(ref ports.Output));
                AnimationStreamUtils.SetDefaultValues(ref destinationStream);

                var sourceStream = AnimationStreamProvider.CreateReadOnly(data.SourceRigDefinition, context.Resolve(ports.Input));
                Core.RigRemapper(data.RemapTable, ref destinationStream, ref sourceStream);

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
            ref var kData = ref GetKernelData(ctx.Handle);

            if(ctx.Port == SimulationPorts.DestinationRigDefinition)
            {
                kData.DestinationRigDefinition = rigDefinition;
                Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigDefinition.Value.Bindings.CurveCount));
            }
            else if(ctx.Port == SimulationPorts.SourceRigDefinition)
            {
                kData.SourceRigDefinition = rigDefinition;
            }
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigRemapTable> remapTable)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            kData.RemapTable = remapTable;
        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);
    }
}
