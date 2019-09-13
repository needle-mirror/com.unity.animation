using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;

namespace Unity.Animation
{
    public class ClipPlayerNode
        : NodeDefinition<ClipPlayerNode.Data, ClipPlayerNode.SimPorts, ClipPlayerNode.KernelData, ClipPlayerNode.KernelDefs, ClipPlayerNode.Kernel>
            , IMsgHandler<BlobAssetReference<ClipInstance>>
            , IMsgHandler<float>
            , IMsgHandler<ClipConfiguration>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<ClipPlayerNode, BlobAssetReference<ClipInstance>> ClipInstance;
            public MessageInput<ClipPlayerNode, float> Time;
            public MessageInput<ClipPlayerNode, float> Speed;
            public MessageInput<ClipPlayerNode, ClipConfiguration> Configuration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<ClipPlayerNode, float> DeltaTime;
            public DataOutput<ClipPlayerNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
            internal NodeHandle<TimeCounterNode> TimeNode;
            internal NodeHandle<UberClipNode> ClipNode;
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<ClipInstance> ClipInstance;
       }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var data = ref GetNodeData(ctx.Handle);

            data.TimeNode = Set.Create<TimeCounterNode>();
            data.ClipNode = Set.Create<UberClipNode>();

            // connect kernel ports
            Set.Connect(data.TimeNode, TimeCounterNode.KernelPorts.Time, data.ClipNode, UberClipNode.KernelPorts.Time);
            Set.Connect(data.TimeNode, TimeCounterNode.KernelPorts.OutputDeltaTime, data.ClipNode, UberClipNode.KernelPorts.DeltaTime);

            ctx.ForwardInput(SimulationPorts.ClipInstance, data.ClipNode, UberClipNode.SimulationPorts.ClipInstance);
            ctx.ForwardInput(SimulationPorts.Time, data.TimeNode, TimeCounterNode.SimulationPorts.Time);
            ctx.ForwardInput(SimulationPorts.Speed, data.TimeNode, TimeCounterNode.SimulationPorts.Speed);
            ctx.ForwardInput(SimulationPorts.Configuration, data.ClipNode, UberClipNode.SimulationPorts.Configuration);

            ctx.ForwardInput(KernelPorts.DeltaTime, data.TimeNode, TimeCounterNode.KernelPorts.DeltaTime);
            ctx.ForwardOutput(KernelPorts.Output, data.ClipNode, UberClipNode.KernelPorts.Output);
        }

        public override void Destroy(NodeHandle handle)
        {
            var data = GetNodeData(handle);

            Set.Destroy(data.TimeNode);
            Set.Destroy(data.ClipNode);
        }

       public void HandleMessage(in MessageContext ctx, in BlobAssetReference<ClipInstance> clipInstance)
       {
           ref var kData = ref GetKernelData(ctx.Handle);
           kData.ClipInstance = clipInstance;
       }

       public void HandleMessage(in MessageContext ctx, in float msg)
       {
       }

       public void HandleMessage(in MessageContext ctx, in ClipConfiguration msg)
       {
       }
    }
}

