using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "e5053e4830af45c6aabd999397d54a3b", version: 1, category: "Animation Core/Utils", description: "Computes the inverse animation stream")]
    public class InversePoseNode
        : SimulationKernelNodeDefinition<InversePoseNode.SimPorts, InversePoseNode.KernelDefs>
        , IRigContextHandler<InversePoseNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "6aed15c49e5041a098049f831fe80163", isHidden: true)]
            public MessageInput<InversePoseNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "232062db78b240dca4b1390b5d0e7133", description: "Input stream")]
            public DataInput<InversePoseNode, Buffer<AnimatedData>> Input;

            [PortDefinition(guid: "235370092edd4f1eb0ffdcadb9f20885", description: "Resulting inversed stream")]
            public DataOutput<InversePoseNode, Buffer<AnimatedData>> Output;
        }

        struct Data : INodeData, IMsgHandler<Rig>
        {
            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                ctx.UpdateKernelData(new KernelData
                {
                    RigDefinition = rig
                });

                ctx.Set.SetBufferSize(
                    ctx.Handle,
                    (OutputPortID)KernelPorts.Output,
                    Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );
            }
        }

        struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                // Fill the destination stream with default values.
                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));

                Core.InversePose(ref outputStream, ref inputStream);
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
