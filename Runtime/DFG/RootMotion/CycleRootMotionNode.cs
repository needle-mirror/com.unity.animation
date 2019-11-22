using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class CycleRootMotionNode
        : NodeDefinition<CycleRootMotionNode.Data, CycleRootMotionNode.SimPorts, CycleRootMotionNode.KernelData, CycleRootMotionNode.KernelDefs, CycleRootMotionNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
    {
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.CycleRootMotionNode");

        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<CycleRootMotionNode, BlobAssetReference<RigDefinition>> RigDefinition;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<CycleRootMotionNode, float> Cycle;
            public DataInput<CycleRootMotionNode, Buffer<float>> Start;
            public DataInput<CycleRootMotionNode, Buffer<float>> Stop;
            public DataInput<CycleRootMotionNode, Buffer<float>> Input;
            public DataOutput<CycleRootMotionNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;

            public ProfilerMarker ProfileMarker;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == default)
                    return;

                data.ProfileMarker.Begin();

                // Fill the destination stream with default values.
                var startStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Start));
                var stopStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Stop));
                var inputStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Input));
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition,context.Resolve(ref ports.Output));

                AnimationStreamUtils.MemCpy(ref outputStream, ref inputStream);

                var startX = new RigidTransform(startStream.GetLocalToParentRotation(0), startStream.GetLocalToParentTranslation(0));
                var stopX = new RigidTransform(stopStream.GetLocalToParentRotation(0), stopStream.GetLocalToParentTranslation(0));
                var x = new RigidTransform(outputStream.GetLocalToParentRotation(0), outputStream.GetLocalToParentTranslation(0));
                var cycleX = GetCycleX(x, startX, stopX, (int)context.Resolve(ports.Cycle));

                outputStream.SetLocalToParentRotation(0, cycleX.rot);
                outputStream.SetLocalToParentTranslation(0, cycleX.pos);

                data.ProfileMarker.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }

        public override void OnUpdate(NodeHandle handle)
        {
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigDefinition)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            if (ctx.Port == SimulationPorts.RigDefinition)
            {
                kData.RigDefinition = rigDefinition;
                Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigDefinition.Value.Bindings.CurveCount));
            }
        }

        public static RigidTransform GetCycleX(RigidTransform x, RigidTransform startX, RigidTransform stopX, int cycle)
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
                RigidTransform cycleX = mathex.rigidPow(math.mul(swapStopX, math.inverse(swapStartX)), math.abs(cycle));

                return math.mul(cycleX, x);
            }
        }
    }
}
