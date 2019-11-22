using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class DeltaRootMotionNode
        : NodeDefinition<DeltaRootMotionNode.Data, DeltaRootMotionNode.SimPorts, DeltaRootMotionNode.KernelData, DeltaRootMotionNode.KernelDefs, DeltaRootMotionNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
    {
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.DeltaRootMotionNode");

        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<DeltaRootMotionNode, BlobAssetReference<RigDefinition>> RigDefinition;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<DeltaRootMotionNode, Buffer<float>> Prev;
            public DataInput<DeltaRootMotionNode, Buffer<float>> Current;
            public DataOutput<DeltaRootMotionNode, Buffer<float>> Output;
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

                // Fill the destination stream with default values.
                var prevStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Prev));
                var currentStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Current));
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition,context.Resolve(ref ports.Output));

                AnimationStreamUtils.MemCpy(ref outputStream, ref currentStream);

                // current = prev * delta
                // delta = Inv(prev) * current
                var prevX = new RigidTransform(prevStream.GetLocalToParentRotation(0), prevStream.GetLocalToParentTranslation(0));
                var x = new RigidTransform(currentStream.GetLocalToParentRotation(0), currentStream.GetLocalToParentTranslation(0));
                var deltaX = math.mul(math.inverse(prevX), x);

                outputStream.SetLocalToParentTranslation( 0,deltaX.pos);
                outputStream.SetLocalToParentRotation(0, deltaX.rot);

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

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);
    }
}
