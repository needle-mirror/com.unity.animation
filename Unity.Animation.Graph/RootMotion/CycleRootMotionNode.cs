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
    [NodeDefinition(guid: "147518e22e1c492ab58d348c8a60a0ff", version: 1, category: "Animation Core/Root Motion", description: "Computes and sets the total root motion offset amount based on the number of cycles for a given clip. This node is internally used by the UberClipNode.")]
    public class CycleRootMotionNode
        : NodeDefinition<CycleRootMotionNode.Data, CycleRootMotionNode.SimPorts, CycleRootMotionNode.KernelData, CycleRootMotionNode.KernelDefs, CycleRootMotionNode.Kernel>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.CycleRootMotionNode");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "98d02bd676584f3ba67f8d116d66260e", isHidden: true)]
            public MessageInput<CycleRootMotionNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "f3875741d6e74043b55c0bfe2889efcd", description: "Clip cycle count")]
            public DataInput<CycleRootMotionNode, int> Cycle;
            [PortDefinition(guid: "72dd27ce795844129161fd037ae6c018", description: "Animation stream at the start of the clip, when t = 0")]
            public DataInput<CycleRootMotionNode, Buffer<AnimatedData>> Start;
            [PortDefinition(guid: "7968d4d4eb3642d5a424524ac6e396c9", description: "Animation stream at the end of the clip, when t = duration")]
            public DataInput<CycleRootMotionNode, Buffer<AnimatedData>> Stop;
            [PortDefinition(guid: "9a3bd2dae1aa4432805d64fe535a953e", description: "The current animation stream")]
            public DataInput<CycleRootMotionNode, Buffer<AnimatedData>> Input;

            [PortDefinition(guid: "72914f27b609451bbe91432e44319c0f", description: "Resulting animation stream with updated root motion values")]
            public DataOutput<CycleRootMotionNode, Buffer<AnimatedData>> Output;
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

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == default)
                    return;

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.Begin();
#endif

                // Fill the destination stream with default values.
                var startStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Start));
                var stopStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Stop));
                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));

                outputStream.CopyFrom(ref inputStream);

                var startX = new RigidTransform(startStream.GetLocalToParentRotation(0), startStream.GetLocalToParentTranslation(0));
                var stopX = new RigidTransform(stopStream.GetLocalToParentRotation(0), stopStream.GetLocalToParentTranslation(0));
                var x = new RigidTransform(outputStream.GetLocalToParentRotation(0), outputStream.GetLocalToParentTranslation(0));
                var cycleX = GetCycleX(x, startX, stopX, context.Resolve(ports.Cycle));

                outputStream.SetLocalToParentRotation(0, cycleX.rot);
                outputStream.SetLocalToParentTranslation(0, cycleX.pos);

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

        static RigidTransform GetCycleX(RigidTransform x, RigidTransform startX, RigidTransform stopX, int cycle)
        {
            if (cycle == 0)
            {
                return x;
            }
            else
            {
                bool swapStartStop = cycle < 0;

                RigidTransform swapStartX = mathex.select(startX, stopX, swapStartStop);
                RigidTransform swapStopX = mathex.select(stopX, startX, swapStartStop);
                RigidTransform cycleX = mathex.rigidPow(math.mul(swapStopX, math.inverse(swapStartX)), math.asuint(math.abs(cycle)));

                return math.mul(cycleX, x);
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
