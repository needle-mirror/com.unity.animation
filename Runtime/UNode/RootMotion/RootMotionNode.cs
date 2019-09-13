using System;
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
            RigidTransform m_RootX;

            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                data.ProfileMarker.Begin();

                var inputStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Input));
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition,context.Resolve(ref ports.Output));

                AnimationStreamUtils.MemCpy(ref outputStream, ref inputStream);

                // current = prev * delta
                var deltaX = new RigidTransform(inputStream.GetLocalToParentRotation(0), inputStream.GetLocalToParentTranslation(0));
                m_RootX.rot = math.normalizesafe(m_RootX.rot);
                m_RootX = math.mul(m_RootX, deltaX);

                outputStream.SetLocalToParentTranslation(0, m_RootX.pos);
                outputStream.SetLocalToParentRotation(0, m_RootX.rot);

                context.Resolve(ref ports.RootX) = m_RootX;

                data.ProfileMarker.End();
            }
        }

        public override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }

        public override void OnUpdate(NodeHandle handle)
        {
        }

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<RigDefinition> rigDefinition)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            if (ctx.Port == SimulationPorts.RigDefinition)
            {
                kData.RigDefinition = rigDefinition;
                Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Output, Buffer<float>.SizeRequest(rigDefinition.Value.Bindings.CurveCount));
            }
        }

        internal KernelData ExposeKernelData(NodeHandle handle) => GetKernelData(handle);
    }
}
