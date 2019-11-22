using UnityEngine; // Time.deltaTime
using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class ClipNode
        : NodeDefinition<ClipNode.Data, ClipNode.SimPorts, ClipNode.KernelData, ClipNode.KernelDefs, ClipNode.Kernel>
            , IMsgHandler<BlobAssetReference<ClipInstance>>
            , IMsgHandler<float>
            , IMsgHandler<int>
            , IMotion
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<ClipNode, BlobAssetReference<ClipInstance>> ClipInstance;
            public MessageInput<ClipNode, int> Additive;
            public MessageOutput<ClipNode, float> Duration;
        }

        static readonly ProfilerMarker k_ProfileSampleClip = new ProfilerMarker("Animation.SampleClip");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<ClipNode, float> Time;
            public DataOutput<ClipNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
            public float Duration;
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<ClipInstance> ClipInstance;
            public ProfilerMarker ProfileSampleClip;
            public int                              Additive;
       }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.ClipInstance == default)
                    return;

                var outputStream = AnimationStreamProvider.Create(data.ClipInstance.Value.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    return;

                data.ProfileSampleClip.Begin();
                Core.EvaluateClip(ref data.ClipInstance.Value, context.Resolve(ports.Time), ref outputStream, data.Additive);
                data.ProfileSampleClip.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileSampleClip = k_ProfileSampleClip;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<ClipInstance> clipInstance)
        {
            ref var data = ref GetNodeData(ctx.Handle);
            ref var kData = ref GetKernelData(ctx.Handle);

            kData.ClipInstance = clipInstance;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(clipInstance.Value.RigDefinition.Value.Bindings.CurveCount));

            data.Duration = clipInstance.Value.Clip.Duration;
            EmitMessage(ctx.Handle, SimulationPorts.Duration,data.Duration);
        }

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
        }

        public void HandleMessage(in MessageContext ctx, in int msg)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.Additive = msg;
        }

        public OutputPortID AnimationStreamOutputPort =>
            (OutputPortID)KernelPorts.Output;

        public float GetDuration(NodeHandle handle) =>
            GetNodeData(handle).Duration;
    }
}
