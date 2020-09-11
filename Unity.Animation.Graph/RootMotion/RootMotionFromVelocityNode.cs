using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "5dfa885842654d86b6b2921233ff4994", version: 1, category: "Animation Core/Root Motion", description: "Computes root motion values from a baked clip. Used internally by the UberClipNode.")]
    public class RootMotionFromVelocityNode
        : NodeDefinition<RootMotionFromVelocityNode.Data, RootMotionFromVelocityNode.SimPorts, RootMotionFromVelocityNode.KernelData, RootMotionFromVelocityNode.KernelDefs, RootMotionFromVelocityNode.Kernel>
        , IMsgHandler<float>
        , IRigContextHandler
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "c43f6e41afb2491b93787e74af1cf755", isHidden: true)]
            public MessageInput<RootMotionFromVelocityNode, Rig> Rig;
            [PortDefinition(guid: "053eb86f39254af8b28ba650d6bb619e", description: "Clip sample rate")]
            public MessageInput<RootMotionFromVelocityNode, float> SampleRate;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "d331e6acbd5b46b2a096434df4b126f3", description: "The current delta time")]
            public DataInput<RootMotionFromVelocityNode, float> DeltaTime;
            [PortDefinition(guid: "e53e6ee6a0ae489e8ce0e3eb303d4f5e", description: "The current animation stream")]
            public DataInput<RootMotionFromVelocityNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "0792d80403b84722ac3d5a930d232b0d", description: "Resulting animation stream with updated root motion values")]
            public DataOutput<RootMotionFromVelocityNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public float SampleRate;
        }

        [BurstCompile]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                // Fill the destination stream with default values.
                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));

                outputStream.CopyFrom(ref inputStream);

                float deltaTime = context.Resolve(ports.DeltaTime) * data.SampleRate;

                var rootVelocity = new RigidTransform(outputStream.GetLocalToParentRotation(0), outputStream.GetLocalToParentTranslation(0));

                rootVelocity.pos *= deltaTime;
                rootVelocity.rot.value.xyz *= (deltaTime / rootVelocity.rot.value.w);
                rootVelocity.rot.value.w = 1;
                rootVelocity.rot = math.normalize(rootVelocity.rot);

                outputStream.SetLocalToParentTranslation(0, rootVelocity.pos);
                outputStream.SetLocalToParentRotation(0, rootVelocity.rot);
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

        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.SampleRate = msg;
        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
