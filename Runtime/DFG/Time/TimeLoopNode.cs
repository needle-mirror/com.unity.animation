using Unity.Burst;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class TimeLoopNode
        : NodeDefinition<TimeLoopNode.Data, TimeLoopNode.SimPorts, TimeLoopNode.KernelData, TimeLoopNode.KernelDefs, TimeLoopNode.Kernel>
            , IMsgHandler<float>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<TimeLoopNode, float> Duration;
        }

        static readonly ProfilerMarker k_ProfileTimeLoop = new ProfilerMarker("Animation.TimeLoop");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<TimeLoopNode, float> InputTime;
            public DataOutput<TimeLoopNode, float> OutputTime;
            public DataOutput<TimeLoopNode, float> Cycle;
            public DataOutput<TimeLoopNode, float> NormalizedTime;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public ProfilerMarker ProfileTimeLoop;
            public float Duration;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfileTimeLoop.Begin();

                var time = context.Resolve(ports.InputTime);
                var normalizedTime = time / data.Duration;
                var normalizedTimeInt = (int)normalizedTime;

                var cycle = math.select(normalizedTimeInt, normalizedTimeInt - 1, normalizedTime < 0);
                normalizedTime = math.select(normalizedTime - normalizedTimeInt, normalizedTime - normalizedTimeInt + 1, normalizedTime < 0);

                context.Resolve(ref ports.Cycle) = cycle;
                context.Resolve(ref ports.NormalizedTime) = normalizedTime;
                context.Resolve(ref ports.OutputTime) = normalizedTime * data.Duration;

                data.ProfileTimeLoop.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileTimeLoop = k_ProfileTimeLoop;
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            GetKernelData(ctx.Handle).Duration = msg;
        }
    }
}
