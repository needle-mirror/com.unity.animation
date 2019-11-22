using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class RootMotionFromVelocityNode
        : NodeDefinition<RootMotionFromVelocityNode.Data, RootMotionFromVelocityNode.SimPorts, RootMotionFromVelocityNode.KernelData, RootMotionFromVelocityNode.KernelDefs, RootMotionFromVelocityNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
            , IMsgHandler<float>
    {
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.RootMotionFromVelocityNode");

        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<RootMotionFromVelocityNode, BlobAssetReference<RigDefinition>> RigDefinition;
            public MessageInput<RootMotionFromVelocityNode, float> SampleRate;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<RootMotionFromVelocityNode, float> DeltaTime;
            public DataInput<RootMotionFromVelocityNode, Buffer<float>> Input;
            public DataOutput<RootMotionFromVelocityNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public float SampleRate;

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

                // Fill the destination stream with default values.
                var inputStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Input));
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition,context.Resolve(ref ports.Output));

                AnimationStreamUtils.MemCpy(ref outputStream, ref inputStream);

                float deltaTime = context.Resolve(ports.DeltaTime) * data.SampleRate;

                var rootVelocity = new RigidTransform(outputStream.GetLocalToParentRotation(0), outputStream.GetLocalToParentTranslation(0));

                rootVelocity.pos *= deltaTime;
                rootVelocity.rot.value.xyz *= (deltaTime / rootVelocity.rot.value.w);
                rootVelocity.rot.value.w = 1;
                rootVelocity.rot = math.normalize(rootVelocity.rot);

                outputStream.SetLocalToParentTranslation( 0, rootVelocity.pos);
                outputStream.SetLocalToParentRotation(0, rootVelocity.rot);

                data.ProfileMarker.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigDefinition)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            kData.RigDefinition = rigDefinition;
            Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigDefinition.Value.Bindings.CurveCount));
        }
        public void HandleMessage(in MessageContext ctx, in float msg)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.SampleRate = msg;
        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);
    }
}
