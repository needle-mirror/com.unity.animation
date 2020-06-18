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
    [NodeDefinition(guid: "0d7b1e6e91364decbb7284a9014ebbce", version: 1, category: "Animation Core/Root Motion", description: "Computes the delta root motion from a previous and current animation stream. This node is internally used by the UberClipNode.")]
    public class DeltaRootMotionNode
        : NodeDefinition<DeltaRootMotionNode.Data, DeltaRootMotionNode.SimPorts, DeltaRootMotionNode.KernelData, DeltaRootMotionNode.KernelDefs, DeltaRootMotionNode.Kernel>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.DeltaRootMotionNode");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "e9eb8fd87a844058a675a311bd9b641a", isHidden: true)]
            public MessageInput<DeltaRootMotionNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "c5b993e40ed84ce088ed1e0656c5d12c", description: "Previous animation stream with root motion")]
            public DataInput<DeltaRootMotionNode, Buffer<AnimatedData>> Previous;
            [PortDefinition(guid: "0d6c7a4cfeec4974ac5cc5561a5a72e7", description: "Current animation stream with root motion")]
            public DataInput<DeltaRootMotionNode, Buffer<AnimatedData>> Current;

            [PortDefinition(guid: "1360ef37a50a4942a87fd07f5c0fb2cd", description: "Resulting animation stream with updated delta root motion values")]
            public DataOutput<DeltaRootMotionNode, Buffer<AnimatedData>> Output;
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
                var prevStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Previous));
                var currentStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Current));
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));

                AnimationStreamUtils.MemCpy(ref outputStream, ref currentStream);

                // current = prev * delta
                // delta = Inv(prev) * current
                var prevX = new RigidTransform(prevStream.GetLocalToParentRotation(0), prevStream.GetLocalToParentTranslation(0));
                var x = new RigidTransform(currentStream.GetLocalToParentRotation(0), currentStream.GetLocalToParentTranslation(0));
                var deltaX = math.mul(math.inverse(prevX), x);

                outputStream.SetLocalToParentTranslation(0, deltaX.pos);
                outputStream.SetLocalToParentRotation(0, deltaX.rot);

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

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
