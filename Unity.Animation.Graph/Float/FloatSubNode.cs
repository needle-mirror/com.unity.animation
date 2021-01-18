using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "27c8103da3474303893705dccba909bf", version: 1, isHidden: true)]
    public class FloatSubNode
        : KernelNodeDefinition<FloatSubNode.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<FloatSubNode, float> InputA;
            public DataInput<FloatSubNode, float> InputB;
            public DataOutput<FloatSubNode, float> Output;
        }

        struct KernelData : IKernelData {}

        [BurstCompile]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) - context.Resolve(ports.InputB);
            }
        }
    }
}
