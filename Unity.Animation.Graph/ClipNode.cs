using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "9b734ccaddd64c97af0b4b5800866db7", version: 1, category: "Animation Core", description: "Base clip sampling node", isHidden: true)]
    public class ClipNode
        : SimulationKernelNodeDefinition<ClipNode.SimPorts, ClipNode.KernelDefs>
        , IRigContextHandler<ClipNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "ae9c16733c7c4aa1af6f153da7416e77", isHidden: true)]
            public MessageInput<ClipNode, Rig> Rig;
            [PortDefinition(guid: "e02fe9c080db472bb0a3fa31d9042924", description: "The clip asset to sample")]
            public MessageInput<ClipNode, BlobAssetReference<Clip>> Clip;
            [PortDefinition(guid: "a0bc2d799f8943359c152b7bcdc1a6f7", description: "Is this an additive clip", defaultValue: false)]
            public MessageInput<ClipNode, bool> Additive;
            [PortDefinition(guid: "d00cef2e74b84e73a076e63aa020dff0", description: "Clip duration")]
            public MessageOutput<ClipNode, float> Duration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "ab9fd996e02344f78a35c6d86f1f1078", description: "Sample time")]
            public DataInput<ClipNode, float> Time;

            [PortDefinition(guid: "fea8f40f69f547f398fbbdd207ded282", description: "Resulting stream")]
            public DataOutput<ClipNode, Buffer<AnimatedData>> Output;
        }

        internal struct Data : INodeData, IMsgHandler<Rig>, IMsgHandler<BlobAssetReference<Clip>> , IMsgHandler<bool>
        {
            internal KernelData m_KernelData;

            public void HandleMessage(in MessageContext ctx, in Rig rig)
            {
                m_KernelData.RigDefinition = rig;
                ctx.Set.SetBufferSize(
                    ctx.Handle,
                    (OutputPortID)KernelPorts.Output,
                    Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );

                if (rig.Value.IsCreated & m_KernelData.Clip.IsCreated)
                {
                    m_KernelData.ClipInstance = ClipManager.Instance.GetClipFor(m_KernelData.RigDefinition, m_KernelData.Clip);
                }

                ctx.UpdateKernelData(m_KernelData);
            }

            public void HandleMessage(in MessageContext ctx, in BlobAssetReference<Clip> clip)
            {
                m_KernelData.Clip = clip;
                ctx.EmitMessage(SimulationPorts.Duration, clip.Value.Duration);

                if (m_KernelData.RigDefinition.IsCreated)
                {
                    m_KernelData.ClipInstance = ClipManager.Instance.GetClipFor(m_KernelData.RigDefinition, m_KernelData.Clip);
                }

                ctx.UpdateKernelData(m_KernelData);
            }

            public void HandleMessage(in MessageContext ctx, in bool msg)
            {
                m_KernelData.Additive = msg ? 1 : 0;
                ctx.UpdateKernelData(m_KernelData);
            }
        }

        internal struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public BlobAssetReference<Clip>          Clip;
            public BlobAssetReference<ClipInstance>  ClipInstance;
            public int                               Additive;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                Core.ValidateIsCreated(data.RigDefinition);
                Core.ValidateIsCreated(data.ClipInstance);

                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    return;

                Core.EvaluateClip(data.ClipInstance, context.Resolve(ports.Time), ref outputStream, data.Additive);
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
