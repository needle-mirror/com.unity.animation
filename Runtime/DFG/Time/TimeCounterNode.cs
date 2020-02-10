using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;
using Unity.Mathematics;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Time", description:"Accumulates and output's current time based on scale and delta time")]
    public class TimeCounterNode
        : NodeDefinition<TimeCounterNode.Data, TimeCounterNode.SimPorts, TimeCounterNode.KernelData, TimeCounterNode.KernelDefs, TimeCounterNode.Kernel>
        , IMsgHandler<float>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(description:"Set internal time to this value")]
            public MessageInput<TimeCounterNode, float> Time;
        }

        static readonly ProfilerMarker k_ProfileTimeCounter = new ProfilerMarker("Animation.TimeCounter");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Delta time")]
            public DataInput<TimeCounterNode, float> DeltaTime;
            [PortDefinition(displayName:"Time Scale", description:"Delta time scale factor")]
            public DataInput<TimeCounterNode, float> Speed;

            [PortDefinition(description:"Resulting delta time")]
            public DataOutput<TimeCounterNode, float> OutputDeltaTime;
            [PortDefinition(description:"Resulting time")]
            public DataOutput<TimeCounterNode, float> Time;
        }

        public struct Data : INodeData
        {
            public int SetTime;
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public ProfilerMarker ProfileTimeCounter;

            public int SetTime;
            public float Time;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            float m_Time;
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                data.ProfileTimeCounter.Begin();

                var deltaTime = context.Resolve(ports.DeltaTime) * context.Resolve(ports.Speed);
                m_Time = math.select(m_Time + deltaTime, data.Time, data.SetTime != 0);

                context.Resolve(ref ports.Time) =  m_Time;
                context.Resolve(ref ports.OutputDeltaTime) = deltaTime;

                data.ProfileTimeCounter.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileTimeCounter = k_ProfileTimeCounter;
        }

        protected override void OnUpdate(in UpdateContext ctx)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);
            ref var kernelData = ref GetKernelData(ctx.Handle);

            if (nodeData.SetTime != 0)
            {
                nodeData.SetTime = 0;
                kernelData.SetTime = 1;
            }
            else
            {
                kernelData.SetTime = 0;
            }
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);
            ref var kernelData = ref GetKernelData(ctx.Handle);

            if (ctx.Port == SimulationPorts.Time)
            {
                kernelData.Time = msg;
                kernelData.SetTime = 0;
                nodeData.SetTime = 1;
            }
        }
    }
}
