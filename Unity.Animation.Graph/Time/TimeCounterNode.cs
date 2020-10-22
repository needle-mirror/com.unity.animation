using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Mathematics;

namespace Unity.Animation
{
    [NodeDefinition(guid: "3fbf9c93754341edaff155b9fa8363b1", version: 1, category: "Animation Core/Time", description: "Accumulates and output's current time based on scale and delta time")]
    public class TimeCounterNode
        : SimulationKernelNodeDefinition<TimeCounterNode.SimPorts, TimeCounterNode.KernelDefs>
    {
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

        struct Data : INodeData, IInit, IUpdate, IMsgHandler<float>
        {
            KernelData m_KernelData;
            int m_SetTime;

            public void Init(InitContext ctx) =>
                ctx.RegisterForUpdate();

            public void Update(in UpdateContext ctx)
            {
                if (m_SetTime != 0)
                {
                    m_SetTime = 0;
                }
                else
                {
                    m_KernelData.SetTime = 0;
                    ctx.UpdateKernelData(m_KernelData);
                }
            }

            public void HandleMessage(in MessageContext ctx, in float msg)
            {
                if (ctx.Port == SimulationPorts.Time)
                {
                    m_KernelData.Time = msg;
                    m_KernelData.SetTime = 1;
                    m_SetTime = 1;

                    ctx.UpdateKernelData(m_KernelData);
                }
            }
        }

        struct KernelData : IKernelData
        {
            public int SetTime;
            public float Time;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            float m_Time;
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var deltaTime = context.Resolve(ports.DeltaTime) * context.Resolve(ports.Speed);
                m_Time = math.select(m_Time + deltaTime, data.Time, data.SetTime != 0);

                context.Resolve(ref ports.Time) =  m_Time;
                context.Resolve(ref ports.OutputDeltaTime) = deltaTime;
            }
        }
    }
}
