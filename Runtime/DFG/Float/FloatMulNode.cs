using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(isHidden:true)]
    public class FloatMulNode
        : NodeDefinition<FloatMulNode.Data, FloatMulNode.SimPorts, FloatMulNode.KernelData, FloatMulNode.KernelDefs, FloatMulNode.Kernel>
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileFloatMul = new ProfilerMarker("Animation.FloatMul");
#endif

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
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileFloatMul;
#endif
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileFloatMul.Begin();
#endif

                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) * context.Resolve(ports.InputB);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileFloatMul.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileFloatMul = k_ProfileFloatMul;
        }
#endif
    }
}
