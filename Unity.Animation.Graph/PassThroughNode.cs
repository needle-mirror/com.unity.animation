using Unity.DataFlowGraph;

namespace Unity.Animation
{
    internal class SimulationPhasePassThroughNode<T>
        : SimulationNodeDefinition<SimulationPhasePassThroughNode<T>.SimPorts>
        where T : struct
    {
        public struct SimPorts : ISimulationPortDefinition
        {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            public MessageInput<SimulationPhasePassThroughNode<T>, T> Input;
            public MessageOutput<SimulationPhasePassThroughNode<T>, T> Output;
#pragma warning restore 649
        }

        private struct Data : INodeData, IMsgHandler<T>
        {
            public void HandleMessage(MessageContext ctx, in T msg)
            {
                ctx.EmitMessage(SimulationPorts.Output, msg);
            }
        }
    }

    internal class SimulationPhasePassThroughNode<TNodeDefinition, T>
        : SimulationNodeDefinition<SimulationPhasePassThroughNode<TNodeDefinition, T>.SimPorts>
        where T : struct
        where TNodeDefinition : NodeDefinition, IMsgHandler<T>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            public MessageInput<TNodeDefinition, T> Input;
            public MessageOutput<TNodeDefinition, T> Output;
#pragma warning restore 649
        }

        private struct Data : INodeData, IMsgHandler<T>
        {
            public void HandleMessage(MessageContext ctx, in T msg) =>
                ctx.EmitMessage(SimulationPorts.Output, msg);
        }
    }

    internal abstract class BaseDataPhasePassThroughNode<TFinalNodeDefinition, T, TKernel>
        : SimulationKernelNodeDefinition<BaseDataPhasePassThroughNode<TFinalNodeDefinition, T, TKernel>.SimPorts, BaseDataPhasePassThroughNode<TFinalNodeDefinition, T, TKernel>.KernelDefs>
        where TFinalNodeDefinition : NodeDefinition
        where T : struct
        where TKernel : struct, IGraphKernel<BaseDataPhasePassThroughNode<TFinalNodeDefinition, T, TKernel>.KernelData, BaseDataPhasePassThroughNode<TFinalNodeDefinition, T, TKernel>.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition {}

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<TFinalNodeDefinition, T> Input;
            public DataOutput<TFinalNodeDefinition, T> Output;
        }

        public struct Data : INodeData {}

        public struct KernelData : IKernelData
        {
        }
    }

    internal abstract class KernelPhasePassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>
        : SimulationKernelNodeDefinition<KernelPhasePassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.SimPorts, KernelPhasePassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.KernelDefs>
        where TFinalNodeDefinition : NodeDefinition
        where T : struct
        where TKernel : struct, IGraphKernel<KernelPhasePassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.KernelData, KernelPhasePassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.KernelDefs>
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

        private struct Data : INodeData, IMsgHandler<int>
        {
            public void HandleMessage(MessageContext ctx, in  int msg)
            {
                var thisHandle = ctx.Set.CastHandle<TFinalNodeDefinition>(ctx.Handle);
                ctx.Set.SetBufferSize(thisHandle, KernelPorts.Output, Buffer<T>.SizeRequest(msg));
            }
        }

        public struct KernelData : IKernelData
        {
        }
    }

    internal class DataPhasePassThroughNode_int
        : KernelNodeDefinition<DataPhasePassThroughNode_int.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<DataPhasePassThroughNode_int, int> Input;
            public DataOutput<DataPhasePassThroughNode_int, int> Output;
        }

        public struct KernelData : IKernelData
        {
        }

        [Unity.Burst.BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = context.Resolve(ports.Input);
            }
        }
    }

    internal class DataPhasePassThroughNode_float
        : KernelNodeDefinition<DataPhasePassThroughNode_float.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<DataPhasePassThroughNode_float, float> Input;
            public DataOutput<DataPhasePassThroughNode_float, float> Output;
        }

        public struct KernelData : IKernelData
        {
        }

        [Unity.Burst.BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = context.Resolve(ports.Input);
            }
        }
    }

    internal class DataPhasePassThroughNode<T> : KernelNodeDefinition<DataPhasePassThroughNode<T>.KernelDefs>
        where T : struct
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<DataPhasePassThroughNode<T>, T> Input;
            public DataOutput<DataPhasePassThroughNode<T>, T> Output;
        }

        public struct KernelData : IKernelData
        {
        }

        //[Unity.Burst.BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = context.Resolve(ports.Input);
            }
        }
    }
}
