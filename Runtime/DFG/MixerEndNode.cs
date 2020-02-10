using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

using System;

namespace Unity.Animation
{
    [NodeDefinition(isHidden:true)]
    [Obsolete("MixerEndNode is obsolete, use NMixerNode instead (RemovedAfter 2020-02-18)", false)]
    public class MixerEndNode
        : NodeDefinition<MixerEndNode.Data, MixerEndNode.SimPorts, MixerEndNode.KernelData, MixerEndNode.KernelDefs, MixerEndNode.Kernel>
        , IMsgHandler<Rig>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<MixerEndNode, Rig> Rig;
        }

        static readonly ProfilerMarker k_ProfileMixerEnd = new ProfilerMarker("Animation.MixerEnd");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<MixerEndNode, Buffer<AnimatedData>>  DefaultPoseInput;
            public DataInput<MixerEndNode, Buffer<AnimatedData>>  Input;
            public DataOutput<MixerEndNode, Buffer<AnimatedData>> Output;
            public DataInput<MixerEndNode, float>                 SumWeight;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileMixerEnd;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    throw new System.InvalidOperationException($"MixerEndNode Output is invalid.");

                var defaultPoseInputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.DefaultPoseInput));
                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));

                data.ProfileMixerEnd.Begin();

                Core.MixerEnd(ref outputStream, ref inputStream, ref defaultPoseInputStream, context.Resolve(ports.SumWeight));

                data.ProfileMixerEnd.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMixerEnd = k_ProfileMixerEnd;
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

