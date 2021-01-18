using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "8d7926b8dbcf4072b7cf1a10e29de888", version: 1, category: "Animation Core/Utils", description: "Remaps one animation stream to another given a known remapping table")]
    public class RigRemapperNode
        : SimulationKernelNodeDefinition<RigRemapperNode.SimPorts, RigRemapperNode.KernelDefs>
        , IRigContextHandler<RigRemapperNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "29d7c6f4946c46d199d9d13e11332983", displayName: "Source Rig", description: "Source rig to remap animation from")]
            public MessageInput<RigRemapperNode, Rig> SourceRig;
            [PortDefinition(guid: "a5532ac30bf242b6b276a4803d0a0582", isHidden: true)]
            public MessageInput<RigRemapperNode, Rig> DestinationRig;

            [PortDefinition(guid: "a158ddc749144e2b8553a117d0d3d297", displayName: "Rig Remap Table", description: "Remap table between source and destination rigs")]
            public MessageInput<RigRemapperNode, BlobAssetReference<RigRemapTable>> RemapTable;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "774b612d90fe4e91a79cbacbc9de2b05", displayName: "Default Pose", description: "Override default animation stream output values. If not provided output animation stream is populated with default values from the rig destination rig definition.")]
            public DataInput<RigRemapperNode, Buffer<AnimatedData>> DefaultPoseInput;
            [PortDefinition(guid: "43fff4c80729422a860e00c0bb1c1bd5", description: "Animation stream to remap from")]
            public DataInput<RigRemapperNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "611e6adea3394f9b91be3e2e2db34a56", description: "Resulting animation stream")]
            public DataOutput<RigRemapperNode, Buffer<AnimatedData>> Output;
        }

        internal struct Data : INodeData, IMsgHandler<Rig>, IMsgHandler<BlobAssetReference<RigRemapTable>>
        {
            internal KernelData m_KernelData;

            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                if (ctx.Port == SimulationPorts.DestinationRig)
                {
                    m_KernelData.DestinationRigDefinition = rig;
                    ctx.Set.SetBufferSize(
                        ctx.Handle,
                        (OutputPortID)KernelPorts.Output,
                        Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                    );
                }
                else if (ctx.Port == SimulationPorts.SourceRig)
                {
                    m_KernelData.SourceRigDefinition = rig;
                }

                ctx.UpdateKernelData(m_KernelData);
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<RigRemapTable> remapTable)
            {
                m_KernelData.RemapTable = remapTable;
                ctx.UpdateKernelData(m_KernelData);
            }
        }

        internal struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> SourceRigDefinition;
            public BlobAssetReference<RigDefinition> DestinationRigDefinition;
            public BlobAssetReference<RigRemapTable> RemapTable;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                if (data.DestinationRigDefinition == default)
                    return;

                // Fill the destination stream with default values.
                var defaultStream = AnimationStream.CreateReadOnly(data.DestinationRigDefinition, context.Resolve(ports.DefaultPoseInput));
                var destinationStream = AnimationStream.Create(data.DestinationRigDefinition, context.Resolve(ref ports.Output));
                if (defaultStream.IsNull)
                    destinationStream.ResetToDefaultValues();
                else
                    destinationStream.CopyFrom(ref defaultStream);

                if (data.SourceRigDefinition != default && data.RemapTable != default)
                {
                    var sourceStream = AnimationStream.CreateReadOnly(data.SourceRigDefinition, context.Resolve(ports.Input));
                    if (!sourceStream.IsNull)
                        Core.RigRemapper(data.RemapTable, ref destinationStream, ref sourceStream);
                }
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.DestinationRig;
    }
}
