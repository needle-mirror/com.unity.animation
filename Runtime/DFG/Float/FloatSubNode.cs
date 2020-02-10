using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

namespace Unity.Animation
{
    [NodeDefinition(isHidden:true)]
    public class FloatSubNode
        : NodeDefinition<FloatSubNode.Data, FloatSubNode.SimPorts, FloatSubNode.KernelData, FloatSubNode.KernelDefs, FloatSubNode.Kernel>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
        }

        static readonly ProfilerMarker k_ProfileFloatSub = new ProfilerMarker("Animation.FloatSub");

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
            public ProfilerMarker ProfileFloatSub;
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfileFloatSub.Begin();
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) - context.Resolve(ports.InputB);
                data.ProfileFloatSub.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileFloatSub = k_ProfileFloatSub;
        }
    }
}
