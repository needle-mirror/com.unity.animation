using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core", description:"Evaluates an animation clip given a clip configuration and time value")]
    public class ClipPlayerNode
        : NodeDefinition<ClipPlayerNode.Data, ClipPlayerNode.SimPorts, ClipPlayerNode.KernelData, ClipPlayerNode.KernelDefs, ClipPlayerNode.Kernel>
        , IMsgHandler<BlobAssetReference<Clip>>
        , IMsgHandler<float>
        , IMsgHandler<ClipConfiguration>
        , IMsgHandler<bool>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<ClipPlayerNode, Rig> Rig;
            [PortDefinition(description:"Clip to sample")]
            public MessageInput<ClipPlayerNode, BlobAssetReference<Clip>> Clip;
            [PortDefinition(description:"Time to sample the clip at")]
            public MessageInput<ClipPlayerNode, float> Time;
            [PortDefinition(description:"Clip configuration data")]
            public MessageInput<ClipPlayerNode, ClipConfiguration> Configuration;
            [PortDefinition(description:"Is an additive clip")]
            public MessageInput<ClipPlayerNode, bool> Additive;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Delta time")]
            public DataInput<ClipPlayerNode, float> DeltaTime;
            [PortDefinition(displayName:"Time scale", description:"Delta time scale factor")]
            public DataInput<ClipPlayerNode, float> Speed;

            [PortDefinition(description:"Resulting animation stream")]
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

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
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

        protected override void Destroy(NodeHandle handle)
        {
            var data = GetNodeData(handle);

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

