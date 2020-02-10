using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Profiling;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Root Motion", description:"Extracts root motion values from animation stream so these can be used for different operations (i.e store state values in entity components)")]
    public class RootMotionNode
        : NodeDefinition<RootMotionNode.Data, RootMotionNode.SimPorts, RootMotionNode.KernelData, RootMotionNode.KernelDefs, RootMotionNode.Kernel>
        , IMsgHandler<Rig>
        , IRigContextHandler
    {
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.RootMotionNode");

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<RootMotionNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"The current animation stream with delta root motion values")]
            public DataInput<RootMotionNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Resulting animation stream without root motion")]
            public DataOutput<RootMotionNode, Buffer<AnimatedData>> Output;
            [PortDefinition(displayName:"Previous Root Motion Transform", description:"Previous root motion")]
            public DataInput<RootMotionNode, float4x4> PrevRootX;

            [PortDefinition(displayName:"Delta Root Motion Transform", description:"Current delta root motion")]
            public DataOutput<RootMotionNode, float4x4> DeltaRootX;
            [PortDefinition(displayName:"Root Motion Transform", description:"Current root motion")]
            public DataOutput<RootMotionNode, float4x4> RootX;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfileMarker;
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                data.ProfileMarker.Begin();

                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Input));
                var outputStream = AnimationStream.Create(data.RigDefinition,context.Resolve(ref ports.Output));
                AnimationStreamUtils.MemCpy(ref outputStream, ref inputStream);

                var deltaRootX = new RigidTransform(inputStream.GetLocalToParentRotation(0), inputStream.GetLocalToParentTranslation(0));
                context.Resolve(ref ports.DeltaRootX) = math.float4x4(deltaRootX);

                RigidTransform prevRootX = new RigidTransform(context.Resolve(ports.PrevRootX));
                prevRootX.rot = math.normalizesafe(prevRootX.rot);
                context.Resolve(ref ports.RootX) = math.float4x4(math.mul(prevRootX, deltaRootX));

                var defaultStream = AnimationStream.FromDefaultValues(outputStream.Rig);
                outputStream.SetLocalToParentTranslation(0, defaultStream.GetLocalToParentTranslation(0));
                outputStream.SetLocalToParentRotation(0, defaultStream.GetLocalToParentRotation(0));

                data.ProfileMarker.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
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
