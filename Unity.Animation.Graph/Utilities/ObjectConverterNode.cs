using Unity.DataFlowGraph;

namespace Unity.Animation
{
    internal class ObjectConverterNode<TIn, TOut> : SimulationNodeDefinition<ObjectConverterNode<TIn, TOut>.MessagePorts>
        where TOut : struct, IConvertibleObject<TIn>
    {
        public struct MessagePorts : ISimulationPortDefinition
        {
            public MessageInput<ObjectConverterNode<TIn, TOut>, TIn> Input;
            public MessageOutput<ObjectConverterNode<TIn, TOut>, TOut> Output;
        }

        private struct NodeData : INodeData, IMsgHandler<TIn>
        {
            public void HandleMessage(MessageContext ctx, in TIn value)
            {
                var convertedValue = new TOut
                {
                    Value = value
                };
                ctx.EmitMessage(SimulationPorts.Output, convertedValue);
            }
        }
    }
}
