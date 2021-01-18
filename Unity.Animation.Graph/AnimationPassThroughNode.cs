using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "2536cbbc55e04672be26d5de7f0af285", version: 1, isHidden: true)]
    internal class KernelPhasePassThroughNodeBufferAnimatedData
        : SimulationKernelNodeDefinition<KernelPhasePassThroughNodeBufferAnimatedData.SimPorts, KernelPhasePassThroughNodeBufferAnimatedData.KernelDefs>
        , Unity.Animation.IRigContextHandler<KernelPhasePassThroughNodeBufferAnimatedData.Data>
    {
        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                if (context.Resolve(ref ports.Output).Length == context.Resolve(ports.Input).Length)
                    context.Resolve(ref ports.Output).CopyFrom(context.Resolve(ports.Input));
            }
        }

        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<KernelPhasePassThroughNodeBufferAnimatedData, Unity.Animation.Rig> Context;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<KernelPhasePassThroughNodeBufferAnimatedData, Buffer<Unity.Animation.AnimatedData>> Input;
            public DataOutput<KernelPhasePassThroughNodeBufferAnimatedData, Buffer<Unity.Animation.AnimatedData>> Output;
        }

        private struct Data : INodeData, IMsgHandler<Rig>
        {
            public void HandleMessage(MessageContext ctx, in Unity.Animation.Rig msg)
            {
                var thisHandle = ctx.Set.CastHandle<KernelPhasePassThroughNodeBufferAnimatedData>(ctx.Handle);
                ctx.Set.SetBufferSize(thisHandle, KernelPorts.Output, Buffer<Unity.Animation.AnimatedData>.SizeRequest(msg.Value.Value.Bindings.StreamSize));
            }
        }

        public struct KernelData : IKernelData
        {
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Context;
    }
}
