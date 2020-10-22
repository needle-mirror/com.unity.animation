using Unity.Burst;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "87d20894ea0f4dda9561374a1fef063e", version: 1, category: "Animation Core/Time", description: "Computes looping time and cycle count given a duration and unbound time")]
    public class TimeLoopNode
        : SimulationKernelNodeDefinition<TimeLoopNode.SimPorts, TimeLoopNode.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "967a157223c2426e8ba2a0cbdfc801a1", description: "Duration")]
            public MessageInput<TimeLoopNode, float> Duration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "c8cf3f694056417bb986c6499eb74436", description: "Unbound time")]
            public DataInput<TimeLoopNode, float> InputTime;
            [PortDefinition(guid: "0821d3d978d84e38880232ab0ff09974", description: "Time (computed from normalized time multiplied by duration)")]
            public DataOutput<TimeLoopNode, float> OutputTime;
            [PortDefinition(guid: "e455fa9a87b248b28ea4a413c68c82a6", description: "Number of duration cycles")]
            public DataOutput<TimeLoopNode, int> Cycle;
            [PortDefinition(guid: "69ee1fe6b1bb4584bdc4d90b9879a093", description: "Normalized time")]
            public DataOutput<TimeLoopNode, float> NormalizedTime;
        }

        struct Data : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(in MessageContext ctx, in float msg)
            {
                ctx.UpdateKernelData(new KernelData
                {
                    Duration = msg
                });
            }
        }

        struct KernelData : IKernelData
        {
            public float Duration;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var time = context.Resolve(ports.InputTime);
                var normalizedTime = time / data.Duration;
                var normalizedTimeInt = (int)normalizedTime;

                var cycle = math.select(normalizedTimeInt, normalizedTimeInt - 1, normalizedTime < 0);
                normalizedTime = math.select(normalizedTime - normalizedTimeInt, normalizedTime - normalizedTimeInt + 1, normalizedTime < 0);

                context.Resolve(ref ports.Cycle) = cycle;
                context.Resolve(ref ports.NormalizedTime) = normalizedTime;
                context.Resolve(ref ports.OutputTime) = normalizedTime * data.Duration;
            }
        }
    }
}
