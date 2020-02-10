using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

using System;

namespace Unity.Animation
{
    [NodeDefinition(isHidden:true)]
    [Obsolete("MixerAddNode is obsolete, use NMixerNode instead (RemovedAfter 2020-02-18)", false)]
    public class MixerAddNode
        : NodeDefinition<MixerAddNode.Data, MixerAddNode.SimPorts, MixerAddNode.KernelData, MixerAddNode.KernelDefs, MixerAddNode.Kernel>
        , IMsgHandler<Rig>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<MixerAddNode, Rig> Rig;
        }

        static readonly ProfilerMarker k_ProfileMixerAdd = new ProfilerMarker("Animation.MixerAdd");

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<MixerAddNode, Buffer<AnimatedData>> Add;
            public DataInput<MixerAddNode, Buffer<AnimatedData>> Input;
            public DataInput<MixerAddNode, float>                Weight;
            public DataInput<MixerAddNode, float>                SumWeightInput;

            public DataOutput<MixerAddNode, Buffer<AnimatedData>> Output;
            public DataOutput<MixerAddNode, float>                SumWeightOutput;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileMixerAdd;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    throw new System.InvalidOperationException($"MixerAddNode Output is invalid.");

                var addStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Add));
                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                var weight = context.Resolve(ports.Weight);
                var sumWeight = context.Resolve(ports.SumWeightInput);

                data.ProfileMixerAdd.Begin();

                sumWeight = Core.MixerAdd(ref outputStream, ref inputStream, ref addStream, weight, sumWeight);

                context.Resolve(ref ports.SumWeightOutput) = sumWeight;

                data.ProfileMixerAdd.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMixerAdd = k_ProfileMixerAdd;
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

