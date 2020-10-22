using System.Diagnostics;

using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Burst;

namespace Unity.Animation
{
    [NodeDefinition(guid: "7e669400e5a54932a5cbfd21ef5ec64a", version: 1, category: "Animation Core", description: "Samples an AnimationCurve at a given time")]
    public class EvaluateCurveNode
        : SimulationKernelNodeDefinition<EvaluateCurveNode.SimPorts, EvaluateCurveNode.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "a28d3cca5864417d91350f603503a984", description: "The AnimationCurve to sample")]
            public MessageInput<EvaluateCurveNode, AnimationCurve> AnimationCurve;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "b2e7e6a9a5f445d9a30562b665cf3420", description: "Sample time", defaultValue: 0f)]
            public DataInput<EvaluateCurveNode, float> Time;

            [PortDefinition(guid: "38e583f3a02b44da9a5d2efc72981767", description: "Curve value at given time")]
            public DataOutput<EvaluateCurveNode, float> Output;
        }

        struct Data : INodeData, IMsgHandler<AnimationCurve>
        {
            public void HandleMessage(in MessageContext ctx, in AnimationCurve curve)
            {
                ctx.UpdateKernelData(new KernelData
                {
                    AnimationCurve = curve
                });
            }
        }

        struct KernelData : IKernelData
        {
            public AnimationCurve AnimationCurve;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            internal static void ValidateIsCreated(AnimationCurve animationCurve)
            {
                if (!animationCurve.IsCreated)
                    throw new System.ArgumentNullException("AnimationCurve is null.");
            }

            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                ref var output = ref context.Resolve(ref ports.Output);

                ValidateIsCreated(data.AnimationCurve);

                var time = context.Resolve(ports.Time);

                output = AnimationCurveEvaluator.Evaluate(time, ref data.AnimationCurve);
            }
        }
    }
}
