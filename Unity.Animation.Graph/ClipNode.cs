using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "9b734ccaddd64c97af0b4b5800866db7", version: 1, category: "Animation Core", description: "Base clip sampling node", isHidden: true)]
    public class ClipNode
        : NodeDefinition<ClipNode.Data, ClipNode.SimPorts, ClipNode.KernelData, ClipNode.KernelDefs, ClipNode.Kernel>
        , IMsgHandler<BlobAssetReference<Clip>>
        , IMsgHandler<bool>
        , IRigContextHandler
    {
#pragma warning restore 0618

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

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public BlobAssetReference<Clip>          Clip;
            public BlobAssetReference<ClipInstance>  ClipInstance;
            public int                               Additive;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
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

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.RigDefinition = rig;
            Set.SetBufferSize(
                ctx.Handle,
                (OutputPortID)KernelPorts.Output,
                Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
            );

            if (rig.Value.IsCreated & kData.Clip.IsCreated)
            {
                kData.ClipInstance = ClipManager.Instance.GetClipFor(kData.RigDefinition, kData.Clip);
            }
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<Clip> clip)
        {
            ref var data = ref GetNodeData(ctx.Handle);
            ref var kData = ref GetKernelData(ctx.Handle);

            kData.Clip = clip;
            ctx.EmitMessage(SimulationPorts.Duration, clip.Value.Duration);

            if (kData.RigDefinition.IsCreated)
            {
                kData.ClipInstance = ClipManager.Instance.GetClipFor(kData.RigDefinition, kData.Clip);
            }
        }

        public void HandleMessage(in MessageContext ctx, in bool msg)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.Additive = msg ? 1 : 0;
        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);
        internal Data ExposeNodeData(NodeHandle handle) => GetNodeData(handle);

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
