using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "e5053e4830af45c6aabd999397d54a3b", version: 1, category: "Animation Core/Utils", description: "Computes the inverse animation stream")]
    public class InversePoseNode
        : NodeDefinition<InversePoseNode.Data, InversePoseNode.SimPorts, InversePoseNode.KernelData, InversePoseNode.KernelDefs, InversePoseNode.Kernel>
        , IRigContextHandler
    {
#pragma warning restore 0618

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
                if (data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                // Fill the destination stream with default values.
                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));

                Core.InversePose(ref outputStream, ref inputStream);
            }
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            kData.RigDefinition = rig;
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
