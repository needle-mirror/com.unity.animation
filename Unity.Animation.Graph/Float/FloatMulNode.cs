using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "0df2a515c02040f28f0f30fff4653b6e", version: 1,   isHidden: true)]
    public class FloatMulNode
        : NodeDefinition<FloatMulNode.Data, FloatMulNode.SimPorts, FloatMulNode.KernelData, FloatMulNode.KernelDefs, FloatMulNode.Kernel>
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<FloatMulNode, float> InputA;
            public DataInput<FloatMulNode, float> InputB;
            public DataOutput<FloatMulNode, float> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) * context.Resolve(ports.InputB);
            }
        }
    }
}
