using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Utils", description:"Remaps one animation stream to another given a known remapping table")]
    public class RigRemapperNode
        : NodeDefinition<RigRemapperNode.Data, RigRemapperNode.SimPorts, RigRemapperNode.KernelData, RigRemapperNode.KernelDefs, RigRemapperNode.Kernel>
        , IMsgHandler<BlobAssetReference<RigRemapTable>>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.RigRemapperNode");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(displayName:"Source Rig", description:"Source rig to remap animation from")]
            public MessageInput<RigRemapperNode, Rig> SourceRig;
            [PortDefinition(isHidden:true)]
            public MessageInput<RigRemapperNode, Rig> DestinationRig;

            [PortDefinition(displayName:"Rig Remap Table", description:"Remap table between source and destination rigs")]
            public MessageInput<RigRemapperNode, BlobAssetReference<RigRemapTable>> RemapTable;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(displayName:"Default Pose", description:"Override default animation stream output values. If not provided output animation stream is populated with default values from the rig destination rig definition.")]
            public DataInput<RigRemapperNode, Buffer<AnimatedData>> DefaultPoseInput;
            [PortDefinition(description:"Animation stream to remap from")]
            public DataInput<RigRemapperNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<RigRemapperNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileMarker;
#endif
            public BlobAssetReference<RigDefinition> SourceRigDefinition;
            public BlobAssetReference<RigDefinition> DestinationRigDefinition;
            public BlobAssetReference<RigRemapTable> RemapTable;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.SourceRigDefinition == default ||
                    data.DestinationRigDefinition == default ||
                    data.RemapTable == default)
                    return;

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.Begin();
#endif

                // Fill the destination stream with default values.
                var defaultStream = AnimationStream.CreateReadOnly(data.DestinationRigDefinition, context.Resolve(ports.DefaultPoseInput));
                var destinationStream = AnimationStream.Create(data.DestinationRigDefinition, context.Resolve(ref ports.Output));
                if (defaultStream.IsNull)
                    AnimationStreamUtils.SetDefaultValues(ref destinationStream);
                else
                    AnimationStreamUtils.MemCpy(ref destinationStream, ref defaultStream);

                var sourceStream = AnimationStream.CreateReadOnly(data.SourceRigDefinition, context.Resolve(ports.Input));
                Core.RigRemapper(data.RemapTable, ref destinationStream, ref sourceStream);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }
#endif

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
