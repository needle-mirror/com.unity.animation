using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Time", description:"Computes normalized time [0, 1] given an input time and duration")]
    public class NormalizedTimeNode
        : NodeDefinition<NormalizedTimeNode.Data, NormalizedTimeNode.SimPorts, NormalizedTimeNode.KernelData, NormalizedTimeNode.KernelDefs, NormalizedTimeNode.Kernel>
        , IMsgHandler<float>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(description:"Duration")]
            public MessageInput<NormalizedTimeNode, float> Duration;
        }

        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.NormalizedTime");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Unbound time")]
            public DataInput<NormalizedTimeNode, float> InputTime;
            [PortDefinition(description:"Normalized time")]
            public DataOutput<NormalizedTimeNode, float> OutputTime;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public ProfilerMarker ProfilerMarker;
            public float Duration;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfilerMarker.Begin();
                context.Resolve(ref ports.OutputTime) = context.Resolve(ports.InputTime) * data.Duration;
                data.ProfilerMarker.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfileMarker;
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            GetKernelData(ctx.Handle).Duration = msg;
        }
    }
}
