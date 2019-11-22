using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public class RootMotionNode
        : NodeDefinition<RootMotionNode.Data, RootMotionNode.SimPorts, RootMotionNode.KernelData, RootMotionNode.KernelDefs, RootMotionNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
    {
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.RootMotionNode");

        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<RootMotionNode, BlobAssetReference<RigDefinition>> RigDefinition;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<RootMotionNode, Buffer<float>> Input;
            public DataOutput<RootMotionNode, Buffer<float>> Output;

            public DataInput<RootMotionNode, RigidTransform>  PrevRootX;
            public DataOutput<RootMotionNode, RigidTransform> DeltaRootX;
            public DataOutput<RootMotionNode, RigidTransform> RootX;
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

                var inputStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Input));
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition,context.Resolve(ref ports.Output));
                AnimationStreamUtils.MemCpy(ref outputStream, ref inputStream);

                var deltaRootX = new RigidTransform(inputStream.GetLocalToParentRotation(0), inputStream.GetLocalToParentTranslation(0));
                context.Resolve(ref ports.DeltaRootX) = deltaRootX;

                RigidTransform prevRootX = context.Resolve(ports.PrevRootX);
                prevRootX.rot = math.normalizesafe(prevRootX.rot);
                context.Resolve(ref ports.RootX) = math.mul(prevRootX, deltaRootX);

                outputStream.SetLocalToParentTranslation(0, outputStream.Rig.Value.DefaultValues.LocalTranslations[0]);
                outputStream.SetLocalToParentRotation(0, outputStream.Rig.Value.DefaultValues.LocalRotations[0]);

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
            if (ctx.Port == SimulationPorts.RigDefinition)
            {
                ref var kData = ref GetKernelData(ctx.Handle);
                kData.RigDefinition = rigDefinition;
                Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigDefinition.Value.Bindings.CurveCount));
            }
        }
    }
}
