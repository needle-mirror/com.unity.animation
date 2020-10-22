using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "0df2a515c02040f28f0f30fff4653b6e", version: 1,   isHidden: true)]
    public class FloatMulNode
        : KernelNodeDefinition<FloatMulNode.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<FloatMulNode, float> InputA;
            public DataInput<FloatMulNode, float> InputB;
            public DataOutput<FloatMulNode, float> Output;
        }

        struct KernelData : IKernelData {}

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) * context.Resolve(ports.InputB);
            }
        }
    }
}
