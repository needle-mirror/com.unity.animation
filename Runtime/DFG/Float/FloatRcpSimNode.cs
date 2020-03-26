using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(isHidden:true)]
    public class FloatRcpSimNode
        : NodeDefinition<FloatRcpSimNode.Data, FloatRcpSimNode.SimPorts>
        , IMsgHandler<float>
    {
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
