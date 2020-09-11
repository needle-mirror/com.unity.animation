using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "6db594d74f974544a51bbdb5c987f869", version: 1, category: "Animation Core/Utils", description: "Outputs the default values of a RigDefinition as an animation stream (i.e. the bind pose)")]
    public class DefaultValuesNode
        : NodeDefinition<DefaultValuesNode.Data, DefaultValuesNode.SimPorts, DefaultValuesNode.KernelData, DefaultValuesNode.KernelDefs, DefaultValuesNode.Kernel>
        , IRigContextHandler
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "6c011cf806dd48509bba003c96636ae7", isHidden: true)]
            public MessageInput<DefaultValuesNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "1568359a111d43eabf7c782912bdcc78", description: "Default stream values")]
            public DataOutput<DefaultValuesNode, Buffer<AnimatedData>> Output;
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
                var stream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                stream.ValidateIsNotNull();

                stream.ResetToDefaultValues();
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
