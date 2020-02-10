using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Utils", description:"Outputs the default values of a RigDefinition as an animation stream (i.e. the bind pose)")]
    public class DefaultValuesNode
        : NodeDefinition<DefaultValuesNode.Data, DefaultValuesNode.SimPorts, DefaultValuesNode.KernelData, DefaultValuesNode.KernelDefs, DefaultValuesNode.Kernel>
        , IMsgHandler<Rig>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<DefaultValuesNode, Rig> Rig;
        }

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.DefaultValuesNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Default stream values")]
            public DataOutput<DefaultValuesNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfilerMarker;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var stream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                if (stream.IsNull)
                    throw new System.InvalidOperationException("DefaultValuesNode ouput is invalid.");

                data.ProfilerMarker.Begin();
                AnimationStreamUtils.SetDefaultValues(ref stream);
                data.ProfilerMarker.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfilerMarker;
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
