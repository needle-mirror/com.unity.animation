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
    [System.Obsolete("RootMotionNode has been deprecated. Use the IAnimatedRootMotion component instead. (RemovedAfter 2020-08-16)")]
    [NodeDefinition(guid: "bcaa66b19c1947a9bd447f543602b7d3", version: 1, category: "Animation Core/Root Motion", description: "Extracts root motion values from animation stream so these can be used for different operations (i.e store state values in entity components)")]
    public class RootMotionNode
        : NodeDefinition<RootMotionNode.Data, RootMotionNode.SimPorts, RootMotionNode.KernelData, RootMotionNode.KernelDefs, RootMotionNode.Kernel>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.RootMotionNode");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "efbce8bfa97f4514999bf6d60a35a29a", isHidden: true)]
            public MessageInput<RootMotionNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "60d7a34c8716403bbaea02a7cd0d3369", description: "The current animation stream with delta root motion values")]
            public DataInput<RootMotionNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "ce074fc0bc0e422f915527256409c159", description: "Resulting animation stream without root motion")]
            public DataOutput<RootMotionNode, Buffer<AnimatedData>> Output;
            [PortDefinition(guid: "3fa402c9c39e496f961cf7e64f4e4221", displayName: "Previous Root Motion Transform", description: "Previous root motion")]
            public DataInput<RootMotionNode, float4x4> PrevRootX;

            [PortDefinition(guid: "74887b6c68d6496283dc64b56286968a", displayName: "Delta Root Motion Transform", description: "Current delta root motion")]
            public DataOutput<RootMotionNode, float4x4> DeltaRootX;
            [PortDefinition(guid: "7702d041834c41ae978f353eec0e839a", displayName: "Root Motion Transform", description: "Current root motion")]
            public DataOutput<RootMotionNode, float4x4> RootX;
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

                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                AnimationStreamUtils.MemCpy(ref outputStream, ref inputStream);

                var deltaRootX = new RigidTransform(inputStream.GetLocalToParentRotation(0), inputStream.GetLocalToParentTranslation(0));
                context.Resolve(ref ports.DeltaRootX) = math.float4x4(deltaRootX);

                RigidTransform prevRootX = new RigidTransform(context.Resolve(ports.PrevRootX));
                prevRootX.rot = math.normalizesafe(prevRootX.rot);
                context.Resolve(ref ports.RootX) = math.float4x4(math.mul(prevRootX, deltaRootX));

                var defaultStream = AnimationStream.FromDefaultValues(outputStream.Rig);
                outputStream.SetLocalToParentTranslation(0, defaultStream.GetLocalToParentTranslation(0));
                outputStream.SetLocalToParentRotation(0, defaultStream.GetLocalToParentRotation(0));

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
