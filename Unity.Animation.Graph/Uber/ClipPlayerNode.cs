using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "6e6e48c0c4c84663999acbb0d491ce79", version: 1, category: "Animation Core", description: "Evaluates an animation clip given a clip configuration and time value")]
    public class ClipPlayerNode
        : NodeDefinition<ClipPlayerNode.Data, ClipPlayerNode.SimPorts, ClipPlayerNode.KernelData, ClipPlayerNode.KernelDefs, ClipPlayerNode.Kernel>
        , IMsgHandler<BlobAssetReference<Clip>>
        , IMsgHandler<float>
        , IMsgHandler<ClipConfiguration>
        , IMsgHandler<bool>
        , IRigContextHandler
    {
#pragma warning restore 0618

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

        public struct Data : INodeData
        {
            internal NodeHandle<TimeCounterNode> TimeNode;
            internal NodeHandle<UberClipNode> ClipNode;
        }

        public struct KernelData : IKernelData
        {
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var data = ref GetNodeData(ctx.Handle);

            data.TimeNode = Set.Create<TimeCounterNode>();
            data.ClipNode = Set.Create<UberClipNode>();

            // connect kernel ports
            Set.Connect(data.TimeNode, TimeCounterNode.KernelPorts.Time, data.ClipNode, UberClipNode.KernelPorts.Time);
            Set.Connect(data.TimeNode, TimeCounterNode.KernelPorts.OutputDeltaTime, data.ClipNode, UberClipNode.KernelPorts.DeltaTime);

            ctx.ForwardInput(SimulationPorts.Rig, data.ClipNode, UberClipNode.SimulationPorts.Rig);
            ctx.ForwardInput(SimulationPorts.Clip, data.ClipNode, UberClipNode.SimulationPorts.Clip);
            ctx.ForwardInput(SimulationPorts.Time, data.TimeNode, TimeCounterNode.SimulationPorts.Time);
            ctx.ForwardInput(SimulationPorts.Configuration, data.ClipNode, UberClipNode.SimulationPorts.Configuration);
            ctx.ForwardInput(SimulationPorts.Additive, data.ClipNode, UberClipNode.SimulationPorts.Additive);

            ctx.ForwardInput(KernelPorts.DeltaTime, data.TimeNode, TimeCounterNode.KernelPorts.DeltaTime);
            ctx.ForwardInput(KernelPorts.Speed, data.TimeNode, TimeCounterNode.KernelPorts.Speed);
            ctx.ForwardOutput(KernelPorts.Output, data.ClipNode, UberClipNode.KernelPorts.Output);
        }

        protected override void Destroy(DestroyContext ctx)
        {
            var data = GetNodeData(ctx.Handle);

            Set.Destroy(data.TimeNode);
            Set.Destroy(data.ClipNode);
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<Clip> clip)
        {
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
        }

        public void HandleMessage(in MessageContext ctx, in ClipConfiguration msg)
        {
        }

        public void HandleMessage(in MessageContext ctx, in bool msg)
        {
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
