using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Utils", description:"Computes the inverse animation stream")]
    public class InversePoseNode
        : NodeDefinition<InversePoseNode.Data, InversePoseNode.SimPorts, InversePoseNode.KernelData, InversePoseNode.KernelDefs, InversePoseNode.Kernel>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.InversePoseNode");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<InversePoseNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Input stream")]
            public DataInput<InversePoseNode, Buffer<AnimatedData>> Input;

            [PortDefinition(description:"Resulting inversed stream")]
            public DataOutput<InversePoseNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileMarker;
#endif
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.Begin();
#endif

                // Fill the destination stream with default values.
                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Input));
                var outputStream = AnimationStream.Create(data.RigDefinition,context.Resolve(ref ports.Output));

                Core.InversePose(ref outputStream, ref inputStream);

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

            kData.RigDefinition = rig;
            Set.SetBufferSize(
                ctx.Handle,
                (OutputPortID)KernelPorts.Output,
                Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
