using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "e3250beaaf4e498ea3a0fa0cdecd47f3", version: 1, isHidden: true)]
    public class FloatRcpSimNode
        : SimulationNodeDefinition<FloatRcpSimNode.SimPorts>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<FloatRcpSimNode, float> Input;
            public MessageOutput<FloatRcpSimNode, float> Output;
        }

        struct Data : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float msg) =>
                ctx.EmitMessage(SimulationPorts.Output, math.rcp(msg));
        }
    }
}
