using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Mixers", description:"Blends two animation streams given an input weight value")]
    public class MixerNode
        : NodeDefinition<MixerNode.Data, MixerNode.SimPorts, MixerNode.KernelData, MixerNode.KernelDefs, MixerNode.Kernel>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMixPose = new ProfilerMarker("Animation.MixPose");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<MixerNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Input stream 0")]
            public DataInput<MixerNode, Buffer<AnimatedData>> Input0;
            [PortDefinition(description:"Input stream 1")]
            public DataInput<MixerNode, Buffer<AnimatedData>> Input1;
            [PortDefinition(description:"Blend weight")]
            public DataInput<MixerNode, float> Weight;

            [PortDefinition(description:"Resulting stream")]
            public DataOutput<MixerNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileMixPose;
#endif
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                    throw new System.InvalidOperationException($"MixerNode Output is invalid.");

                var inputStream1 = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input0));
                var inputStream2 = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input1));

                var weight = context.Resolve(in ports.Weight);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMixPose.Begin();
#endif

                if (inputStream1.IsNull && inputStream2.IsNull)
                    AnimationStreamUtils.SetDefaultValues(ref outputStream);
                else if (inputStream1.IsNull && !inputStream2.IsNull)
                {
                    AnimationStreamUtils.SetDefaultValues(ref outputStream);
                    Core.Blend(ref outputStream, ref outputStream, ref inputStream2, weight);
                }
                else if (!inputStream1.IsNull && inputStream2.IsNull)
                {
                    AnimationStreamUtils.SetDefaultValues(ref outputStream);
                    Core.Blend(ref outputStream, ref inputStream1, ref outputStream, weight);
                }
                else
                    Core.Blend(ref outputStream, ref inputStream1, ref inputStream2, weight);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMixPose.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMixPose = k_ProfileMixPose;
        }
#endif

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            GetKernelData(ctx.Handle).RigDefinition = rig.Value;
            Set.SetBufferSize(
                ctx.Handle,
                (OutputPortID)KernelPorts.Output,
                Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
