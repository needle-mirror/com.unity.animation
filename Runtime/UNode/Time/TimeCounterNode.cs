using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.Profiling;
using Unity.Mathematics;

namespace Unity.Animation
{
    public class TimeCounterNode
        : NodeDefinition<TimeCounterNode.Data, TimeCounterNode.SimPorts, TimeCounterNode.KernelData, TimeCounterNode.KernelDefs, TimeCounterNode.Kernel>
            , IMsgHandler<float>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<TimeCounterNode, float> Time;
            public MessageInput<TimeCounterNode, float> Speed;
        }

        static readonly ProfilerMarker k_ProfileTimeCounter = new ProfilerMarker("Animation.TimeCounter");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<TimeCounterNode, float> DeltaTime;
            public DataOutput<TimeCounterNode, float> OutputDeltaTime;
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
            public float Speed;

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

                var deltaTime = context.Resolve(ports.DeltaTime) * data.Speed;
                m_Time = math.select(m_Time + deltaTime, data.Time, data.SetTime != 0);

                context.Resolve(ref ports.Time) =  m_Time;
                context.Resolve(ref ports.OutputDeltaTime) = deltaTime;

                data.ProfileTimeCounter.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.Speed = 1.0f;
        }

        public override void OnUpdate(NodeHandle handle)
        {
            base.OnUpdate(handle);

            ref var nodeData = ref GetNodeData(handle);
            ref var kernelData = ref GetKernelData(handle);

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
            else if (ctx.Port == SimulationPorts.Speed)
            {
                kernelData.Speed = msg;
            }
        }
    }
}
