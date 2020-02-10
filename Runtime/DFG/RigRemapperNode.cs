using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Utils", description:"Remaps one animation stream to another given a known remapping table")]
    public class RigRemapperNode
        : NodeDefinition<RigRemapperNode.Data, RigRemapperNode.SimPorts, RigRemapperNode.KernelData, RigRemapperNode.KernelDefs, RigRemapperNode.Kernel>
        , IMsgHandler<Rig>
        , IMsgHandler<BlobAssetReference<RigRemapTable>>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(displayName:"Source Rig", description:"Source rig to remap animation from")]
            public MessageInput<RigRemapperNode, Rig> SourceRig;
            [PortDefinition(isHidden:true)]
            public MessageInput<RigRemapperNode, Rig> DestinationRig;

            [PortDefinition(displayName:"Rig Remap Table", description:"Remap table between source and destination rigs")]
            public MessageInput<RigRemapperNode, BlobAssetReference<RigRemapTable>> RemapTable;
        }

        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.RigRemapperNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Animation stream to remap from")]
            public DataInput<RigRemapperNode, Buffer<AnimatedData>>  Input;
            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<RigRemapperNode, Buffer<AnimatedData>> Output;
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
                var destinationStream = AnimationStream.Create(data.DestinationRigDefinition, context.Resolve(ref ports.Output));
                AnimationStreamUtils.SetDefaultValues(ref destinationStream);

                var sourceStream = AnimationStream.CreateReadOnly(data.SourceRigDefinition, context.Resolve(ports.Input));
                Core.RigRemapper(data.RemapTable, ref destinationStream, ref sourceStream);

                data.ProfileMarker.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            if (ctx.Port == SimulationPorts.DestinationRig)
            {
                kData.DestinationRigDefinition = rig;
                Set.SetBufferSize(
                    ctx.Handle,
                    (OutputPortID)KernelPorts.Output,
                    Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                    );
            }
            else if (ctx.Port == SimulationPorts.SourceRig)
            {
                kData.SourceRigDefinition = rig;
            }
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigRemapTable> remapTable)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.RemapTable = remapTable;
        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.DestinationRig;
    }
}
