using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class NormalizedTimeNode
        : NodeDefinition<NormalizedTimeNode.Data, NormalizedTimeNode.SimPorts, NormalizedTimeNode.KernelData, NormalizedTimeNode.KernelDefs, NormalizedTimeNode.Kernel>
            , IMsgHandler<float>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<NormalizedTimeNode, float> Duration;
        }

        static readonly ProfilerMarker k_ProfileTimeLoop = new ProfilerMarker("Animation.NormalizedTime");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<NormalizedTimeNode, float> InputTime;
            public DataOutput<NormalizedTimeNode, float> OutputTime;
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
                context.Resolve(ref ports.OutputTime) = context.Resolve(ports.InputTime) * data.Duration;
                data.ProfileTimeLoop.End();
            }
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            GetKernelData(ctx.Handle).Duration = msg;
        }
    }
}
