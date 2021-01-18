using Unity.DataFlowGraph;

namespace Unity.Animation
{
    internal class EnumConverter<T> : SimulationNodeDefinition<EnumConverter<T>.MessagePorts>
        where T : System.Enum
    {
        public struct MessagePorts : ISimulationPortDefinition
        {
            public MessageInput<EnumConverter<T>, int> IntValue;
            public MessageOutput<EnumConverter<T>, T> EnumValue;
        }

        private struct NodeData : INodeData, IMsgHandler<int>
        {
            public void HandleMessage(MessageContext ctx, in int value)
            {
                ctx.EmitMessage(SimulationPorts.EnumValue, (T)System.Enum.Parse(typeof(T), value.ToString()));
            }
        }
    }

    internal class DataPhaseEnumConverter<T> : SimulationKernelNodeDefinition<
        DataPhaseEnumConverter<T>.MessagePorts,
        DataPhaseEnumConverter<T>.DataPorts>
        where T : struct, System.Enum
    {
        private struct NodeData : INodeData {}
        public struct MessagePorts : ISimulationPortDefinition
        {
        }
        public struct DataPorts : IKernelPortDefinition
        {
            public DataInput<DataPhaseEnumConverter<T>, int> IntValue;
            public DataOutput<DataPhaseEnumConverter<T>, T> EnumValue;
        }

        public struct KernelData : IKernelData
        {
        }

        //[Unity.Burst.BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, DataPorts>
        {
            public void Execute(RenderContext context, in KernelData data, ref DataPorts ports)
            {
                var v = context.Resolve(ports.IntValue);

                context.Resolve(ref ports.EnumValue) = (T)System.Enum.Parse(typeof(T), v.ToString());
            }
        }
    }
}
