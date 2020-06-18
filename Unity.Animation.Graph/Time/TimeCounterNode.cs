using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Mathematics;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(guid: "3fbf9c93754341edaff155b9fa8363b1", version: 1, category: "Animation Core/Time", description: "Accumulates and output's current time based on scale and delta time")]
    public class TimeCounterNode
        : NodeDefinition<TimeCounterNode.Data, TimeCounterNode.SimPorts, TimeCounterNode.KernelData, TimeCounterNode.KernelDefs, TimeCounterNode.Kernel>
        , IMsgHandler<float>
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileTimeCounter = new ProfilerMarker("Animation.TimeCounter");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "6f1144ea235449529f0e29c58009a605", description: "Set internal time to this value")]
            public MessageInput<TimeCounterNode, float> Time;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "d8ede9b577394cd68bfb0c644f2f2ad2", description: "Delta time")]
            public DataInput<TimeCounterNode, float> DeltaTime;
            [PortDefinition(guid: "8c681e55e7954532a9d56522b2cd1ea1", displayName: "Time Scale", description: "Delta time scale factor")]
            public DataInput<TimeCounterNode, float> Speed;

            [PortDefinition(guid: "310ed32c573a4849968c1eb9ee9e970c", description: "Resulting delta time")]
            public DataOutput<TimeCounterNode, float> OutputDeltaTime;
            [PortDefinition(guid: "828327bc9f834fcab7d9931d3aa9992e", description: "Resulting time")]
            public DataOutput<TimeCounterNode, float> Time;
        }

        public struct Data : INodeData
        {
            public int SetTime;
        }

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileTimeCounter;
#endif

            public int SetTime;
            public float Time;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            float m_Time;
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileTimeCounter.Begin();
#endif

                var deltaTime = context.Resolve(ports.DeltaTime) * context.Resolve(ports.Speed);
                m_Time = math.select(m_Time + deltaTime, data.Time, data.SetTime != 0);

                context.Resolve(ref ports.Time) =  m_Time;
                context.Resolve(ref ports.OutputDeltaTime) = deltaTime;

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileTimeCounter.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileTimeCounter = k_ProfileTimeCounter;
        }

#endif

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
