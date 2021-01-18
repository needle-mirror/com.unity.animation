using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "92bf06d2ddbf4b4ba0bd3cf610f1f4e6", version: 1, isHidden: true)]
    public class FloatAddNode
        : KernelNodeDefinition<FloatAddNode.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<FloatAddNode, float> InputA;
            public DataInput<FloatAddNode, float> InputB;
            public DataOutput<FloatAddNode, float> Output;
        }

        struct KernelData : IKernelData {}

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) + context.Resolve(ports.InputB);
            }
        }
    }
}
