using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Mixers", description:"Blends N animation streams together given weights per stream")]
    [PortGroupDefinition(portGroupSizeDescription:"Number of animation streams", groupIndex:1, minInstance:2, maxInstance:-1)]
    public class NMixerNode
        : NodeDefinition<NMixerNode.Data, NMixerNode.SimPorts, NMixerNode.KernelData, NMixerNode.KernelDefs, NMixerNode.Kernel>
        , IMsgHandler<Rig>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<NMixerNode, Rig> Rig;
        }

        static readonly ProfilerMarker k_ProfileNMixer = new ProfilerMarker("Animation.NMixer");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(displayName:"Default Pose", description:"Override default animation stream values when sum of weights is less than 1")]
            public DataInput<NMixerNode, Buffer<AnimatedData>> DefaultPoseInput;
            [PortDefinition(displayName:"Input", description:"Animation stream to blend", portGroupIndex:1)]
            public PortArray<DataInput<NMixerNode, Buffer<AnimatedData>>> Inputs;
            [PortDefinition(displayName:"Weight", description:"Blend weight", portGroupIndex:1)]
            public PortArray<DataInput<NMixerNode, float>> Weights;

            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<NMixerNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileNMixer;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (outputStream.IsNull)
                {
                    throw new System.InvalidOperationException($"NMixerNode Output is invalid.");
                }

                var inputArray = context.Resolve(ports.Inputs);
                var weightArray = context.Resolve(ports.Weights);

                if(inputArray.Length != weightArray.Length )
                    throw new System.InvalidOperationException($"NMixerNode: Inputs And Weight Port array length mismatch. Expecting '{weightArray.Length}' but was '{inputArray.Length}'.");

                var sumWeight = 0.0f;

                data.ProfileNMixer.Begin();

                Core.MixerBegin(ref outputStream);

                for (int i = 0; i < inputArray.Length; ++i)
                {
                    var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, inputArray[i].ToNative(context));

                    if (!inputStream.IsNull)
                    {
                        sumWeight = Core.MixerAdd(ref outputStream, ref outputStream, ref inputStream, weightArray[i], sumWeight);
                    }
                }

                var defaultPoseInputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.DefaultPoseInput));
                Core.MixerEnd(ref outputStream, ref outputStream, ref defaultPoseInputStream, sumWeight);        

                data.ProfileNMixer.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileNMixer = k_ProfileNMixer;
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

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
