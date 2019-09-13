using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class FloatAddNode
        : NodeDefinition<FloatAddNode.Data, FloatAddNode.SimPorts, FloatAddNode.KernelData, FloatAddNode.KernelDefs, FloatAddNode.Kernel>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
        }

        static readonly ProfilerMarker k_ProfileFloatAdd = new ProfilerMarker("Animation.FloatAdd");

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
            // Assets.
            public ProfilerMarker ProfileFloatAdd;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfileFloatAdd.Begin();
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) + context.Resolve(ports.InputB);
                data.ProfileFloatAdd.End();
            }
        }
    }
}
