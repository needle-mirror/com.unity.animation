using Unity.Mathematics;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

namespace Unity.Animation
{
    [NodeDefinition(isHidden:true)]
    public class FloatRcpNode
        : NodeDefinition<FloatRcpNode.Data, FloatRcpNode.SimPorts, FloatRcpNode.KernelData, FloatRcpNode.KernelDefs, FloatRcpNode.Kernel>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
        }

        public struct Data : INodeData
        {
        }

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.FloatRcpNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<FloatRcpNode, float> Input;
            public DataOutput<FloatRcpNode, float> Output;
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public ProfilerMarker ProfilerMarker;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfilerMarker.Begin();
                context.Resolve(ref ports.Output) = math.rcp(context.Resolve(ports.Input));
                data.ProfilerMarker.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfilerMarker;
        }

    }
}

