using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Utils", description:"Gets a float value from the AnimationStream")]
    public class GetAnimationStreamFloatNode
        : NodeDefinition<GetAnimationStreamFloatNode.Data, GetAnimationStreamFloatNode.SimPorts, GetAnimationStreamFloatNode.KernelData, GetAnimationStreamFloatNode.KernelDefs, GetAnimationStreamFloatNode.Kernel>
        , IMsgHandler<Rig>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<GetAnimationStreamFloatNode, Rig> Rig;
        }

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.GetAnimationStreamFloatNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Input stream")]
            public DataInput<GetAnimationStreamFloatNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Index in stream")]
            public DataInput<GetAnimationStreamFloatNode, int> Index;

            [PortDefinition(description:"Value")]
            public DataOutput<GetAnimationStreamFloatNode, float> Output;
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
                var stream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                if (stream.IsNull)
                    throw new System.InvalidOperationException("GetAnimationStreamFloatNode input is invalid.");

                data.ProfilerMarker.Begin();
                context.Resolve(ref ports.Output) = stream.GetFloat(context.Resolve(ports.Index));
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
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }

    [NodeDefinition(category: "Animation Core/Utils", description: "Sets a float value in the AnimationStream")]
    public class SetAnimationStreamFloatNode
        : NodeDefinition<SetAnimationStreamFloatNode.Data, SetAnimationStreamFloatNode.SimPorts, SetAnimationStreamFloatNode.KernelData, SetAnimationStreamFloatNode.KernelDefs, SetAnimationStreamFloatNode.Kernel>
        , IMsgHandler<Rig>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<SetAnimationStreamFloatNode, Rig> Rig;
        }

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.SetAnimationStreamFloatNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Input stream")]
            public DataInput<SetAnimationStreamFloatNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Index in stream")]
            public DataInput<SetAnimationStreamFloatNode, int>  Index;
            [PortDefinition(description:"Value to set")]
            public DataInput<SetAnimationStreamFloatNode, float> Value;

            [PortDefinition(description:"Resulting stream")]
            public DataOutput<SetAnimationStreamFloatNode, Buffer<AnimatedData>> Output;
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
                var input = context.Resolve(ports.Input);
                var output = context.Resolve(ref ports.Output);
                if (input.Length != output.Length)
                    throw new System.InvalidOperationException($"SetAnimationStreamFloatNode: Input Length '{input.Length}' does not match Output Length '{output.Length}'");

                data.ProfilerMarker.Begin();

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                if (stream.IsNull)
                    throw new System.InvalidOperationException($"SetAnimationStreamFloatNode output is invalid.");

                stream.SetFloat(context.Resolve(ports.Index), context.Resolve(ports.Value));
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

