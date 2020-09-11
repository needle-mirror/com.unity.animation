using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "6d62b890d24a4b769ae7b9481e017743", version: 1, category: "Animation Core/Mixers", description: "Blends N animation streams together given weights per stream")]
    [PortGroupDefinition(portGroupSizeDescription: "Number of animation streams", groupIndex: 1, minInstance: 2, maxInstance: -1)]
    public class NMixerNode
        : NodeDefinition<NMixerNode.Data, NMixerNode.SimPorts, NMixerNode.KernelData, NMixerNode.KernelDefs, NMixerNode.Kernel>
        , IRigContextHandler
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "a4a8fea063a9494a9b6f5a63f5625060", isHidden: true)]
            public MessageInput<NMixerNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "3bb0eda4c6fd432bb9547765dceafacc", displayName: "Default Pose", description: "Override default animation stream values when sum of weights is less than 1")]
            public DataInput<NMixerNode, Buffer<AnimatedData>> DefaultPoseInput;
            [PortDefinition(guid: "bf403237632b44fabec35cc75c516f28", displayName: "Input", description: "Animation stream to blend", portGroupIndex: 1)]
            public PortArray<DataInput<NMixerNode, Buffer<AnimatedData>>> Inputs;
            [PortDefinition(guid: "9718b3bc3af141c4a4f72f93a3283129", displayName: "Weight", description: "Blend weight", portGroupIndex: 1)]
            public PortArray<DataInput<NMixerNode, float>> Weights;

            [PortDefinition(guid: "1d6bfa5b22004f918dd21120bae997aa", description: "Resulting animation stream")]
            public DataOutput<NMixerNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                outputStream.ValidateIsNotNull();

                var inputArray = context.Resolve(ports.Inputs);
                var weightArray = context.Resolve(ports.Weights);

                Core.ValidateBufferLengthsAreEqual(inputArray.Length, weightArray.Length);

                var state = Core.MixerBegin(ref outputStream);

                for (int i = 0; i < inputArray.Length; ++i)
                {
                    var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, inputArray[i].ToNative(context));
                    if (weightArray[i] > 0 && !inputStream.IsNull)
                    {
                        state = Core.MixerAdd(ref outputStream, ref inputStream, weightArray[i], state);
                    }
                }

                var defaultPoseInputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.DefaultPoseInput));
                Core.MixerEnd(ref outputStream, ref defaultPoseInputStream, state);
            }
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
