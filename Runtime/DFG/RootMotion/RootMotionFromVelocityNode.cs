using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Root Motion", description:"Computes root motion values from a baked clip. Used internally by the UberClipNode.")]
    public class RootMotionFromVelocityNode
        : NodeDefinition<RootMotionFromVelocityNode.Data, RootMotionFromVelocityNode.SimPorts, RootMotionFromVelocityNode.KernelData, RootMotionFromVelocityNode.KernelDefs, RootMotionFromVelocityNode.Kernel>
        , IMsgHandler<float>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.RootMotionFromVelocityNode");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<RootMotionFromVelocityNode, Rig> Rig;
            [PortDefinition(description:"Clip sample rate")]
            public MessageInput<RootMotionFromVelocityNode, float> SampleRate;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"The current delta time")]
            public DataInput<RootMotionFromVelocityNode, float> DeltaTime;
            [PortDefinition(description:"The current animation stream")]
            public DataInput<RootMotionFromVelocityNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Resulting animation stream with updated root motion values")]
            public DataOutput<RootMotionFromVelocityNode, Buffer<AnimatedData>> Output;
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
            public float SampleRate;
        }

        [BurstCompile]
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

                AnimationStreamUtils.MemCpy(ref outputStream, ref inputStream);

                float deltaTime = context.Resolve(ports.DeltaTime) * data.SampleRate;

                var rootVelocity = new RigidTransform(outputStream.GetLocalToParentRotation(0), outputStream.GetLocalToParentTranslation(0));

                rootVelocity.pos *= deltaTime;
                rootVelocity.rot.value.xyz *= (deltaTime / rootVelocity.rot.value.w);
                rootVelocity.rot.value.w = 1;
                rootVelocity.rot = math.normalize(rootVelocity.rot);

                outputStream.SetLocalToParentTranslation( 0, rootVelocity.pos);
                outputStream.SetLocalToParentRotation(0, rootVelocity.rot);

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

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.SampleRate = msg;
        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
