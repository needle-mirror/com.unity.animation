using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

using System;

namespace Unity.Animation
{
    [NodeDefinition(isHidden:true)]
    [Obsolete("MixerBeginNode is obsolete, use NMixerNode instead (RemovedAfter 2020-02-18)", false)]
    public class MixerBeginNode
        : NodeDefinition<MixerBeginNode.Data, MixerBeginNode.SimPorts, MixerBeginNode.KernelData, MixerBeginNode.KernelDefs, MixerBeginNode.Kernel>
        , IMsgHandler<Rig>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<MixerBeginNode, Rig> Rig;
        }

        static readonly ProfilerMarker k_ProfileMixerBegin = new ProfilerMarker("Animation.MixerBegin");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataOutput<MixerBeginNode, Buffer<AnimatedData>> Output;
            public DataOutput<MixerBeginNode, float>                SumWeight;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileMixerBegin;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    throw new System.InvalidOperationException($"MixerBeginNode Output is invalid.");

                data.ProfileMixerBegin.Begin();

                Core.MixerBegin(ref outputStream);

                context.Resolve(ref ports.SumWeight) = 0.0f;

                data.ProfileMixerBegin.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMixerBegin = k_ProfileMixerBegin;
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            GetKernelData(ctx.Handle).RigDefinition = rig;
            Set.SetBufferSize(
                ctx.Handle,
                (OutputPortID)KernelPorts.Output,
                Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );
        }
    }
}

