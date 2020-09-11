using Unity.Mathematics;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "9663c48edb9446789059aaf3b0d0068c", version: 1, isHidden: true)]
    public class FloatRcpNode
        : NodeDefinition<FloatRcpNode.Data, FloatRcpNode.SimPorts, FloatRcpNode.KernelData, FloatRcpNode.KernelDefs, FloatRcpNode.Kernel>
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
        }

        public struct Data : INodeData
        {
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<FloatRcpNode, float> Input;
            public DataOutput<FloatRcpNode, float> Output;
        }

        public struct KernelData : IKernelData
        {
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = math.rcp(context.Resolve(ports.Input));
            }
        }
    }
}
