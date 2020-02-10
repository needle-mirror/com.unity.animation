using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core", description:"Base clip sampling node", isHidden:true)]
    public class ClipNode
        : NodeDefinition<ClipNode.Data, ClipNode.SimPorts, ClipNode.KernelData, ClipNode.KernelDefs, ClipNode.Kernel>
        , IMsgHandler<Rig>
        , IMsgHandler<BlobAssetReference<Clip>>
        , IMsgHandler<bool>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<ClipNode, Rig> Rig;
            [PortDefinition(description:"The clip asset to sample")]
            public MessageInput<ClipNode, BlobAssetReference<Clip>> Clip;
            [PortDefinition(description:"Is this an additive clip", defaultValue:false)]
            public MessageInput<ClipNode, bool> Additive;
            [PortDefinition(description:"Clip duration")]
            public MessageOutput<ClipNode, float> Duration;
        }

        static readonly ProfilerMarker k_ProfileSampleClip = new ProfilerMarker("Animation.SampleClip");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Sample time")]
            public DataInput<ClipNode, float> Time;

            [PortDefinition(description:"Resulting stream")]
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
            public ProfilerMarker                    ProfileSampleClip;
            public int                               Additive;
       }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == default)
                    throw new System.InvalidOperationException($"ClipNode has invalid RigDefinition.");

                if (data.ClipInstance == default)
                    throw new System.InvalidOperationException($"ClipNode has invalid Clip.");

                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    return;

                data.ProfileSampleClip.Begin();
                Core.EvaluateClip(data.ClipInstance, context.Resolve(ports.Time), ref outputStream, data.Additive);
                data.ProfileSampleClip.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileSampleClip = k_ProfileSampleClip;
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

            if(kData.RigDefinition.IsCreated)
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
