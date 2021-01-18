using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "6e6e48c0c4c84663999acbb0d491ce79", version: 1, category: "Animation Core", description: "Evaluates an animation clip given a clip configuration and time value")]
    public class ClipPlayerNode
        : SimulationKernelNodeDefinition<ClipPlayerNode.SimPorts, ClipPlayerNode.KernelDefs>
        , IRigContextHandler<ClipPlayerNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "bd18527f905e42958967f3209e3e2749", isHidden: true)]
            public MessageInput<ClipPlayerNode, Rig> Rig;
            [PortDefinition(guid: "d54d5e84fe9c41c98e7884cdb38d3cdb", description: "Clip to sample")]
            public MessageInput<ClipPlayerNode, BlobAssetReference<Clip>> Clip;
            [PortDefinition(guid: "dc9fbabe48b449b283ab713cd9149718", description: "Time to sample the clip at")]
            public MessageInput<ClipPlayerNode, float> Time;
            [PortDefinition(guid: "3564fb2d5d474e8f8f263788acc915d7", description: "Clip configuration data")]
            public MessageInput<ClipPlayerNode, ClipConfiguration> Configuration;
            [PortDefinition(guid: "5f1665002b224c16a5c85bd8ebe31c62", description: "Is an additive clip")]
            public MessageInput<ClipPlayerNode, bool> Additive;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "51283be210a94c1a8e0b78f1ac7f2ef4", description: "Delta time")]
            public DataInput<ClipPlayerNode, float> DeltaTime;
            [PortDefinition(guid: "2d24fba28ea14bd7bbe8568026579ed9", displayName: "Time scale", description: "Delta time scale factor")]
            public DataInput<ClipPlayerNode, float> Speed;

            [PortDefinition(guid: "8d62299023174e0dad6394c6065d0b49", description: "Resulting animation stream")]
            public DataOutput<ClipPlayerNode, Buffer<AnimatedData>> Output;
        }

        struct Data : INodeData, IInit, IDestroy
            , IMsgHandler<Rig>
            , IMsgHandler<BlobAssetReference<Clip>>
            , IMsgHandler<float>
            , IMsgHandler<ClipConfiguration>
            , IMsgHandler<bool>
        {
            NodeHandle<TimeCounterNode> m_TimeNode;
            NodeHandle<UberClipNode> m_ClipNode;

            public void Init(InitContext ctx)
            {
                m_TimeNode = ctx.Set.Create<TimeCounterNode>();
                m_ClipNode = ctx.Set.Create<UberClipNode>();

                // connect kernel ports
                ctx.Set.Connect(m_TimeNode, TimeCounterNode.KernelPorts.Time, m_ClipNode, UberClipNode.KernelPorts.Time);
                ctx.Set.Connect(m_TimeNode, TimeCounterNode.KernelPorts.OutputDeltaTime, m_ClipNode, UberClipNode.KernelPorts.DeltaTime);

                ctx.ForwardInput(SimulationPorts.Rig, m_ClipNode, UberClipNode.SimulationPorts.Rig);
                ctx.ForwardInput(SimulationPorts.Clip, m_ClipNode, UberClipNode.SimulationPorts.Clip);
                ctx.ForwardInput(SimulationPorts.Time, m_TimeNode, TimeCounterNode.SimulationPorts.Time);
                ctx.ForwardInput(SimulationPorts.Configuration, m_ClipNode, UberClipNode.SimulationPorts.Configuration);
                ctx.ForwardInput(SimulationPorts.Additive, m_ClipNode, UberClipNode.SimulationPorts.Additive);

                ctx.ForwardInput(KernelPorts.DeltaTime, m_TimeNode, TimeCounterNode.KernelPorts.DeltaTime);
                ctx.ForwardInput(KernelPorts.Speed, m_TimeNode, TimeCounterNode.KernelPorts.Speed);
                ctx.ForwardOutput(KernelPorts.Output, m_ClipNode, UberClipNode.KernelPorts.Output);
            }

            public void Destroy(DestroyContext ctx)
            {
                ctx.Set.Destroy(m_TimeNode);
                ctx.Set.Destroy(m_ClipNode);
            }

            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
            }

            public void HandleMessage(MessageContext ctx, in BlobAssetReference<Clip> clip)
            {
            }

            public void HandleMessage(MessageContext ctx, in float msg)
            {
            }

            public void HandleMessage(MessageContext ctx, in ClipConfiguration msg)
            {
            }

            public void HandleMessage(MessageContext ctx, in bool msg)
            {
            }
        }

        struct KernelData : IKernelData
        {
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Rig;
    }
}
