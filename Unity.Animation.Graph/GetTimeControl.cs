using Unity.DataFlowGraph.Attributes;
using Unity.DataFlowGraph;

namespace Unity.Animation
{
    [NodeDefinition(guid: "ceb74689c6a54c79be25a31b73241071", version: 1, "Animation High-Level")]
    public class GetTimeControl
        : SimulationNodeDefinition<GetTimeControl.SimPorts>
        , ITimeControlHandler<GetTimeControl.NodeData>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "8906c7f9699942f5bfb87c71efc0a3f3")]
            public MessageInput<GetTimeControl, TimeControl> Input;
            [PortDefinition(guid: "135d89198fd045c5939ceaafc8687979")]
            public MessageOutput<GetTimeControl, float> Output;
        }

        public struct NodeData : INodeData,
                                 IMsgHandler<TimeControl>
        {
            public void HandleMessage(MessageContext ctx, in TimeControl msg)
            {
                ctx.EmitMessage(SimulationPorts.Output, msg.AbsoluteTime);
            }
        }

        public InputPortID GetPort(NodeHandle handle)
        {
            return (InputPortID)SimulationPorts.Input;
        }
    }
}
