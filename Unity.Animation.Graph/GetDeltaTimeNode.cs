using Unity.DataFlowGraph.Attributes;
using Unity.DataFlowGraph;
using UnityEngine;

namespace Unity.Animation
{
    [NodeDefinition(guid: "25c236b500d04f6e90e80169b8335e5f", version: 1, "Animation High-Level")]
    public class GetDeltaTimeNode
        : SimulationNodeDefinition<GetDeltaTimeNode.SimPorts>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "c8de5e0a58514b03903fb466ee7a8033")]
            public MessageOutput<GetDeltaTimeNode, float> Output;
        }

        private struct Data : INodeData, IInit, IUpdate
        {
            public void Init(InitContext ctx)
            {
                ctx.RegisterForUpdate();
            }

            public void Update(UpdateContext ctx)
            {
                ctx.EmitMessage(SimulationPorts.Output, Time.deltaTime);
            }
        }
    }
}
