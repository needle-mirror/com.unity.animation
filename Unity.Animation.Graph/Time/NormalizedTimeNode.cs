using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "7f966a89b3ed4a6bb24144f7ca8749ba", version: 1, category: "Animation Core/Time", description: "Computes normalized time [0, 1] given an input time and duration")]
    public class NormalizedTimeNode
        : NodeDefinition<NormalizedTimeNode.Data, NormalizedTimeNode.SimPorts, NormalizedTimeNode.KernelData, NormalizedTimeNode.KernelDefs, NormalizedTimeNode.Kernel>
        , IMsgHandler<float>
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "28656bd191824b79a53bb0b5bb610400", description: "Duration")]
            public MessageInput<NormalizedTimeNode, float> Duration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "5a7635b182b746a49513f4b650da4fc0", description: "Unbound time")]
            public DataInput<NormalizedTimeNode, float> InputTime;
            [PortDefinition(guid: "9e835dcf0e9c44dfa3bfaf679a355e5f", description: "Normalized time")]
            public DataOutput<NormalizedTimeNode, float> OutputTime;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public float Duration;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.OutputTime) = context.Resolve(ports.InputTime) * data.Duration;
            }
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            GetKernelData(ctx.Handle).Duration = msg;
        }
    }
}
