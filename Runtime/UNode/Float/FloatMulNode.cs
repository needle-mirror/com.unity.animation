using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class FloatMulNode
        : NodeDefinition<FloatMulNode.Data, FloatMulNode.SimPorts, FloatMulNode.KernelData, FloatMulNode.KernelDefs, FloatMulNode.Kernel>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
        }

        static readonly ProfilerMarker k_ProfileFloatMul = new ProfilerMarker("Animation.FloatMul");

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
            // Assets.
            public ProfilerMarker ProfileFloatMul;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfileFloatMul.Begin();
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) * context.Resolve(ports.InputB);
                data.ProfileFloatMul.End();
            }
        }
    }
}
