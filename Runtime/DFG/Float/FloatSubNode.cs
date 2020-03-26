using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(isHidden:true)]
    public class FloatSubNode
        : NodeDefinition<FloatSubNode.Data, FloatSubNode.SimPorts, FloatSubNode.KernelData, FloatSubNode.KernelDefs, FloatSubNode.Kernel>
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileFloatSub = new ProfilerMarker("Animation.FloatSub");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
        }

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
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileFloatSub;
#endif
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileFloatSub.Begin();
#endif
                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) - context.Resolve(ports.InputB);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileFloatSub.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileFloatSub = k_ProfileFloatSub;
        }
#endif
    }
}
