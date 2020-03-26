using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(isHidden:true)]
    public class FloatAddNode
        : NodeDefinition<FloatAddNode.Data, FloatAddNode.SimPorts, FloatAddNode.KernelData, FloatAddNode.KernelDefs, FloatAddNode.Kernel>
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileFloatAdd = new ProfilerMarker("Animation.FloatAdd");
#endif

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
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileFloatAdd;
#endif
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileFloatAdd.Begin();
#endif

                context.Resolve(ref ports.Output) = context.Resolve(ports.InputA) + context.Resolve(ports.InputB);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileFloatAdd.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileFloatAdd = k_ProfileFloatAdd;
        }
#endif
    }
}
