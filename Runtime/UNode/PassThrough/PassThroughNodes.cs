using Unity.DataFlowGraph;
using Unity.Profiling;
using Unity.Burst;

namespace Unity.Animation
{
    public class SimPassThroughNode<T>
        : NodeDefinition<SimPassThroughNode<T>.Data, SimPassThroughNode<T>.SimPorts>
        , IMsgHandler<T>
        where T : struct
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<SimPassThroughNode<T>, T> Input;
            public MessageOutput<SimPassThroughNode<T>, T> Output;
        }

        public struct Data : INodeData { }

        public void HandleMessage(in MessageContext ctx, in T msg) =>
            EmitMessage(ctx.Handle, SimulationPorts.Output, msg);
    }

    public abstract class KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>
        : NodeDefinition<KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>.Data, KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>.SimPorts, KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>.KernelData, KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>.KernelDefs, TKernel>
        where TFinalNodeDefinition : INodeDefinition
        where T : struct
        where TKernel : struct, IGraphKernel<KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>.KernelData, KernelPassThroughNode<TFinalNodeDefinition, T, TKernel>.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition { }

        static readonly ProfilerMarker k_ProfileKernelPassThrough = new ProfilerMarker("Animation.KernelPassThrough");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<TFinalNodeDefinition, T> Input;
            public DataOutput<TFinalNodeDefinition, T> Output;
        }

        public struct Data : INodeData { }

        public struct KernelData : IKernelData
        {
            public ProfilerMarker ProfilePassThrough;
        }
    }

    // Until this is fixed in Burst, we must avoid IGraphKernel implementations which include any generics.
    public class KernelPassThroughNodeFloat : KernelPassThroughNode<KernelPassThroughNodeFloat, float, KernelPassThroughNodeFloat.Kernel>
    {
        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfilePassThrough.Begin();
                context.Resolve(ref ports.Output) = context.Resolve(ports.Input);
                data.ProfilePassThrough.End();
            }
        }
    }

    public abstract class KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>
        : NodeDefinition<KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.Data, KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.SimPorts, KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.KernelData, KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.KernelDefs, TKernel>
        , IMsgHandler<int>
        where TFinalNodeDefinition : INodeDefinition, IMsgHandler<int>
        where T : struct
        where TKernel : struct, IGraphKernel<KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.KernelData, KernelPassThroughNodeBuffer<TFinalNodeDefinition, T, TKernel>.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<TFinalNodeDefinition, int> BufferSize;
        }

        static readonly ProfilerMarker k_ProfileKernelPassThroughBuffer = new ProfilerMarker("Animation.KernelPassThroughBuffer");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<TFinalNodeDefinition, Buffer<T>> Input;
            public DataOutput<TFinalNodeDefinition, Buffer<T>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public ProfilerMarker ProfilePassThrough;
        }

        public void HandleMessage(in MessageContext ctx, in  int msg)
        {
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(msg));
        }
    }

    // Until this is fixed in Burst, we must avoid IGraphKernel implementations which include any generics.
    public class KernelPassThroughNodeBufferFloat : KernelPassThroughNodeBuffer<KernelPassThroughNodeBufferFloat, float, KernelPassThroughNodeBufferFloat.Kernel>
    {
        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfilePassThrough.Begin();
                context.Resolve(ref ports.Output).CopyFrom(context.Resolve(ports.Input));
                data.ProfilePassThrough.End();
            }
        }
    }
}
