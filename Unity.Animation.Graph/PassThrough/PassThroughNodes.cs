using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Burst;


namespace Unity.Animation
{
    public class SimPassThroughNode<T>
        : SimulationNodeDefinition<SimPassThroughNode<T>.SimPorts>
        where T : struct
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<SimPassThroughNode<T>, T> Input;
            public MessageOutput<SimPassThroughNode<T>, T> Output;
        }

        struct Data : INodeData, IMsgHandler<T>
        {
            public void HandleMessage(MessageContext ctx, in T msg) =>
                ctx.EmitMessage(SimulationPorts.Output, msg);
        }
    }

    public abstract class KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>
        : KernelNodeDefinition<KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>.KernelDefs>
        where TFinalNodeDefinition : NodeDefinition
        where T : struct
        where TKernel : struct, IGraphKernel<KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>.KernelData, KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<TFinalNodeDefinition, T> Input;
            public DataOutput<TFinalNodeDefinition, T> Output;
        }

        public struct KernelData : IKernelData {}
    }

    // Until this is fixed in Burst, we must avoid IGraphKernel implementations which include any generics.
    [NodeDefinition(guid: "3c094ff41dd2403c81c356e60d66d222", version: 1, isHidden: true)]
    public class KernelPassThroughNodeFloat : KernelPassThroughNode<KernelPassThroughNodeFloat, float, KernelPassThroughNodeFloat.Kernel>
    {
        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = context.Resolve(ports.Input);
            }
        }
    }

    public abstract class KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>
        : SimulationKernelNodeDefinition<KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.SimPorts, KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.KernelDefs>
        where TFinalNodeDefinition : NodeDefinition
        where T : struct
        where TKernel : struct, IGraphKernel<KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.KernelData, KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<TFinalNodeDefinition, int> BufferSize;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<TFinalNodeDefinition, Buffer<T>> Input;
            public DataOutput<TFinalNodeDefinition, Buffer<T>> Output;
        }

        public struct Data : INodeData, IMsgHandler<int>
        {
            public void HandleMessage(MessageContext ctx, in int msg) =>
                ctx.Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<T>.SizeRequest(msg));
        }

        public struct KernelData : IKernelData
        {
        }
    }


    // Until this is fixed in Burst, we must avoid IGraphKernel implementations which include any generics.
    [NodeDefinition(guid: "d998849d1bc04708acf7e23da56c0a87", version: 1, isHidden: true)]
    public class KernelPassThroughNodeBufferFloat : KernelPassThroughNodeBuffer<KernelPassThroughNodeBufferFloat, AnimatedData, KernelPassThroughNodeBufferFloat.Kernel>
    {
        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output).CopyFrom(context.Resolve(ports.Input));
            }
        }
    }
}
