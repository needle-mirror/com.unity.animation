using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class FloatSubNode
        : NodeDefinition<FloatSubNode.Data, FloatSubNode.SimPorts, FloatSubNode.KernelData, FloatSubNode.KernelDefs, FloatSubNode.Kernel>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
        }

        static readonly ProfilerMarker k_ProfileFloatAdd = new ProfilerMarker("Animation.FloatAdd");

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
            // Assets.
            public ProfilerMarker ProfileFloatAdd;
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfileFloatAdd.Begin();
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) - context.Resolve(ports.InputB);
                data.ProfileFloatAdd.End();
            }
        }
    }
}
