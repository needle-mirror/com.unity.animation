using Unity.Mathematics;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "9663c48edb9446789059aaf3b0d0068c", version: 1, isHidden: true)]
    public class FloatRcpNode
        : KernelNodeDefinition<FloatRcpNode.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<FloatRcpNode, float> Input;
            public DataOutput<FloatRcpNode, float> Output;
        }

        struct KernelData : IKernelData {}

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = math.rcp(context.Resolve(ports.Input));
            }
        }
    }
}
