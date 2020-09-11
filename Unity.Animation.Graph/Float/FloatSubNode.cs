using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "27c8103da3474303893705dccba909bf", version: 1, isHidden: true)]
    public class FloatSubNode
        : NodeDefinition<FloatSubNode.Data, FloatSubNode.SimPorts, FloatSubNode.KernelData, FloatSubNode.KernelDefs, FloatSubNode.Kernel>
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<FloatSubNode, float> InputA;
            public DataInput<FloatSubNode, float> InputB;
            public DataOutput<FloatSubNode, float> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) - context.Resolve(ports.InputB);
            }
        }
    }
}
