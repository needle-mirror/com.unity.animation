using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "e3250beaaf4e498ea3a0fa0cdecd47f3", version: 1, isHidden: true)]
    public class FloatRcpSimNode
        : NodeDefinition<FloatRcpSimNode.Data, FloatRcpSimNode.SimPorts>
        , IMsgHandler<float>
    {
#pragma warning restore 0618
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<FloatRcpSimNode, float> Input;
            public MessageOutput<FloatRcpSimNode, float> Output;
        }

        public struct Data : INodeData
        {
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            ctx.EmitMessage(SimulationPorts.Output, math.rcp(msg));
        }
    }
}
