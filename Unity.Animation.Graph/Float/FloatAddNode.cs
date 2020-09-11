using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "92bf06d2ddbf4b4ba0bd3cf610f1f4e6", version: 1, isHidden: true)]
    public class FloatAddNode
        : NodeDefinition<FloatAddNode.Data, FloatAddNode.SimPorts, FloatAddNode.KernelData, FloatAddNode.KernelDefs, FloatAddNode.Kernel>
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<FloatAddNode, float> InputA;
            public DataInput<FloatAddNode, float> InputB;
            public DataOutput<FloatAddNode, float> Output;
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
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) + context.Resolve(ports.InputB);
            }
        }
    }
}
