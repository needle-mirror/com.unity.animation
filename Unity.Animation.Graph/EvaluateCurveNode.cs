using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Burst;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(category: "Animation Core", description: "Samples an AnimationCurve at a given time")]
    public class EvaluateCurveNode
        : NodeDefinition<EvaluateCurveNode.Data, EvaluateCurveNode.SimPorts, EvaluateCurveNode.KernelData, EvaluateCurveNode.KernelDefs, EvaluateCurveNode.Kernel>
        , IMsgHandler<AnimationCurve>
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.EvaluateCurveNode");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(description: "The AnimationCurve to sample")]
            public MessageInput<EvaluateCurveNode, AnimationCurve> AnimationCurve;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description: "Sample time", defaultValue: 0f)]
            public DataInput<EvaluateCurveNode, float> Time;

            [PortDefinition(description: "Curve value at given time")]
            public DataOutput<EvaluateCurveNode, float> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileMarker;
#endif
            public AnimationCurve AnimationCurve;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                ref var output = ref context.Resolve(ref ports.Output);

                if (!data.AnimationCurve.IsCreated)
                    throw new System.InvalidOperationException("EvaluateCurveNode has invalid AnimationCurve.");

                var time = context.Resolve(ports.Time);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.Begin();
#endif

                output = AnimationCurveEvaluator.Evaluate(time, ref data.AnimationCurve);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }

#endif

        public void HandleMessage(in MessageContext ctx, in AnimationCurve curve)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.AnimationCurve = curve;
        }
    }
}