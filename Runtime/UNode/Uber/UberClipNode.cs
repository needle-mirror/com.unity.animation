using System;
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;

namespace Unity.Animation
{
    public enum ClipConfigurationMask
    {
        NormalizedTime = 1 << 0,
        LoopTime = 1 << 1,
        LoopTransform = 1 << 2,
        CycleRootMotion = 1 << 3,
        DeltaRootMotion = 1 << 4,
        BankPivot = 1 << 5
    }

    public struct ClipConfiguration
    {
        public int Mask;
        public StringHash MotionID;
    }
    public class UberClipNode
        : NodeDefinition<UberClipNode.Data, UberClipNode.SimPorts, UberClipNode.KernelData, UberClipNode.KernelDefs, UberClipNode.Kernel>
            , IMsgHandler<BlobAssetReference<ClipInstance>>
            , IMsgHandler<ClipConfiguration>
            , INormalizedTimeMotion
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<UberClipNode, BlobAssetReference<ClipInstance>> ClipInstance;
            public MessageInput<UberClipNode, ClipConfiguration> Configuration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<UberClipNode, float> Time;
            public DataInput<UberClipNode, float> DeltaTime;

            public DataOutput<UberClipNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
            internal NodeHandle<KernelPassThroughNodeFloat> TimeNode;
            internal NodeHandle<KernelPassThroughNodeFloat> DeltaTimeNode;
            internal NodeHandle<KernelPassThroughNodeBufferFloat> OutputNode;

            internal NodeHandle<FloatSubNode> PrevTimeNode;
            internal NodeHandle<ConfigurableClipNode> PrevClipNode;
            internal NodeHandle<ConfigurableClipNode> ClipNode;
            internal NodeHandle<DeltaRootNode> DeltaRootNode;

            internal BlobAssetReference<ClipInstance> ClipInstance;
            internal ClipConfiguration Configuration;
        }

        public struct KernelData : IKernelData
        {
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
            }
        }

        void BuildNodes(ref Data nodeData)
        {
            var mask = nodeData.Configuration.Mask;

            var deltaRootMotion = (mask & (int)ClipConfigurationMask.DeltaRootMotion) != 0;

            if (nodeData.ClipInstance == BlobAssetReference<ClipInstance>.Null)
            {
                return;
            }

            if (deltaRootMotion)
            {
                nodeData.PrevTimeNode = Set.Create<FloatSubNode>();
                nodeData.PrevClipNode = Set.Create<ConfigurableClipNode>();
                nodeData.DeltaRootNode = Set.Create<DeltaRootNode>();
            }

            nodeData.ClipNode = Set.Create<ConfigurableClipNode>();

            if (deltaRootMotion)
            {
                Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.PrevTimeNode, FloatSubNode.KernelPorts.InputA);
                Set.Connect(nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.PrevTimeNode, FloatSubNode.KernelPorts.InputB);

                Set.Connect(nodeData.PrevTimeNode, FloatSubNode.KernelPorts.Output, nodeData.PrevClipNode, ConfigurableClipNode.KernelPorts.Time);
                Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.ClipNode, ConfigurableClipNode.KernelPorts.Time);

                Set.Connect(nodeData.PrevClipNode, ConfigurableClipNode.KernelPorts.Output, nodeData.DeltaRootNode, DeltaRootNode.KernelPorts.Prev);
                Set.Connect(nodeData.ClipNode, ConfigurableClipNode.KernelPorts.Output, nodeData.DeltaRootNode, DeltaRootNode.KernelPorts.Current);

                Set.Connect(nodeData.DeltaRootNode, DeltaRootNode.KernelPorts.Output, nodeData.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
            }
            else
            {
                Set.Connect(nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Output, nodeData.ClipNode, ConfigurableClipNode.KernelPorts.Time);
                Set.Connect(nodeData.ClipNode, ConfigurableClipNode.KernelPorts.Output, nodeData.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Input);
            }

            Set.SendMessage(nodeData.ClipNode, ConfigurableClipNode.SimulationPorts.Configuration, nodeData.Configuration);

            if (deltaRootMotion)
            {
                Set.SendMessage(nodeData.PrevClipNode, ConfigurableClipNode.SimulationPorts.Configuration, nodeData.Configuration);
                Set.SendMessage(nodeData.DeltaRootNode, DeltaRootNode.SimulationPorts.RigDefinition, nodeData.ClipInstance.Value.RigDefinition);
                Set.SendMessage(nodeData.PrevClipNode, ConfigurableClipNode.SimulationPorts.ClipInstance, nodeData.ClipInstance);
            }

            Set.SendMessage(nodeData.ClipNode, ConfigurableClipNode.SimulationPorts.ClipInstance, nodeData.ClipInstance);
            Set.SendMessage(nodeData.OutputNode, KernelPassThroughNodeBufferFloat.SimulationPorts.BufferSize,  nodeData.ClipInstance.Value.RigDefinition.Value.Bindings.CurveCount);
        }

        void ClearNodes(Data nodeData)
        {
            if (Set.Exists(nodeData.PrevTimeNode))
                Set.Destroy(nodeData.PrevTimeNode);

            if (Set.Exists(nodeData.PrevClipNode))
                Set.Destroy(nodeData.PrevClipNode);

            if (Set.Exists(nodeData.ClipNode))
                Set.Destroy(nodeData.ClipNode);

            if (Set.Exists(nodeData.DeltaRootNode))
                Set.Destroy(nodeData.DeltaRootNode);
        }

        public override void Init(InitContext ctx)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            nodeData.TimeNode = Set.Create<KernelPassThroughNodeFloat>();
            nodeData.DeltaTimeNode = Set.Create<KernelPassThroughNodeFloat>();
            nodeData.OutputNode = Set.Create<KernelPassThroughNodeBufferFloat>();

            BuildNodes(ref nodeData);

            ctx.ForwardInput(KernelPorts.Time, nodeData.TimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardInput(KernelPorts.DeltaTime, nodeData.DeltaTimeNode, KernelPassThroughNodeFloat.KernelPorts.Input);
            ctx.ForwardOutput(KernelPorts.Output,nodeData.OutputNode, KernelPassThroughNodeBufferFloat.KernelPorts.Output);
        }

        public override void Destroy(NodeHandle handle)
        {
            var nodeData = GetNodeData(handle);

            Set.Destroy(nodeData.TimeNode);
            Set.Destroy(nodeData.DeltaTimeNode);
            Set.Destroy(nodeData.OutputNode);

            ClearNodes(nodeData);
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<ClipInstance> clipInstance)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            nodeData.ClipInstance = clipInstance;

            ClearNodes(nodeData);
            BuildNodes(ref nodeData);
        }

        public void HandleMessage(in MessageContext ctx, in ClipConfiguration msg)
        {
            ref var nodeData = ref GetNodeData(ctx.Handle);

            nodeData.Configuration = msg;

            ClearNodes(nodeData);
            BuildNodes(ref nodeData);
        }

        public OutputPortID AnimationStreamOutputPort =>
            (OutputPortID) KernelPorts.Output;

        public float GetDuration(NodeHandle handle)
        {
            var nodeData = GetNodeData(handle);
            return nodeData.ClipInstance != BlobAssetReference<ClipInstance>.Null ? nodeData.ClipInstance.Value.Clip.Duration : 0;
        }

        public InputPortID NormalizedTimeInputPort =>
            (InputPortID) KernelPorts.Time;
        internal Data ExposeNodeData(NodeHandle handle) => GetNodeData(handle);
        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);
    }
}
