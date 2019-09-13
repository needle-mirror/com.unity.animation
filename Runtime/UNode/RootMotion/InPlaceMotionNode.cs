using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    // use this if the motion of a clip is not on root transform
    // it extracts motion from a specified transform and project it on root transform
    public class InPlaceMotionNode
        : NodeDefinition<InPlaceMotionNode.Data, InPlaceMotionNode.SimPorts, InPlaceMotionNode.KernelData, InPlaceMotionNode.KernelDefs, InPlaceMotionNode.Kernel>
            , IMsgHandler<BlobAssetReference<RigDefinition>>
            , IMsgHandler<ClipConfiguration>
    {
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.InPlaceMotionNode");

        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<InPlaceMotionNode, BlobAssetReference<RigDefinition>> RigDefinition;
            public MessageInput<InPlaceMotionNode, ClipConfiguration> Configuration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<InPlaceMotionNode, Buffer<float>> Input;
            public DataOutput<InPlaceMotionNode, Buffer<float>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ClipConfiguration Configuration;

            public int TranslationIndex;
            public int RotationIndex;

            public ProfilerMarker ProfileMarker;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == default)
                    return;

                data.ProfileMarker.Begin();

                // Fill the destination stream with default values.
                var inputStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,context.Resolve(ports.Input));
                var outputStream = AnimationStreamProvider.Create(data.RigDefinition,context.Resolve(ref ports.Output));

                AnimationStreamUtils.MemCpy(ref outputStream, ref inputStream);

                var defaultStream = AnimationStreamProvider.CreateReadOnly(data.RigDefinition,
                    ref data.RigDefinition.Value.DefaultValues.LocalTranslations,
                    ref data.RigDefinition.Value.DefaultValues.LocalRotations,
                    ref data.RigDefinition.Value.DefaultValues.LocalScales,
                    ref data.RigDefinition.Value.DefaultValues.Floats,
                    ref data.RigDefinition.Value.DefaultValues.Integers);

                // motion is the node used to create the motion ex: hips
                // motionProj is the the projection of motion
                // motionProj * inPlaceMotion = motion
                // inPlaceMotion = inv(motionProj) * motion;
                // ipmt = inv(mpr) * inv(mpt) * mt

                var defaultRotation = defaultStream.GetLocalToRigRotation(data.RotationIndex);

                var rootTranslation = outputStream.GetLocalToRigTranslation(0);
                var rootRotation = outputStream.GetLocalToRigRotation(0);

                var motionTranslation = outputStream.GetLocalToRigTranslation(data.TranslationIndex);
                var motionRotation = outputStream.GetLocalToRigRotation(data.RotationIndex);

                motionRotation = math.mul(motionRotation, math.conjugate(defaultRotation));

                float3 motionProjTranslation = new float3();
                quaternion motionProjRotation = new quaternion();

                ProjectMotionNode(motionTranslation, motionRotation, out motionProjTranslation, out motionProjRotation, (data.Configuration.Mask & (int)ClipConfigurationMask.BankPivot) != 0);

                var invMotionProjRotation = math.conjugate(motionProjRotation);

                motionTranslation = math.mul(invMotionProjRotation, motionTranslation - motionProjTranslation);

                motionRotation = math.mul(invMotionProjRotation, motionRotation);
                motionRotation = math.mul(motionRotation, defaultRotation);

                rootTranslation = motionProjTranslation + math.mul(motionProjRotation, rootTranslation);
                rootRotation = math.mul(motionProjRotation, rootRotation);

                outputStream.SetLocalToRigTranslation(data.TranslationIndex, motionTranslation);
                outputStream.SetLocalToRigRotation(data.RotationIndex, motionRotation);

                outputStream.SetLocalToRigTranslation(0, rootTranslation);
                outputStream.SetLocalToRigRotation(0, rootRotation);

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
                SetMotionIndices(ref kData);
            }
        }

        public void HandleMessage(in MessageContext ctx, in ClipConfiguration msg)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            kData.Configuration = msg;
            SetMotionIndices(ref kData);
        }

        private void SetMotionIndices(ref KernelData kData)
        {
            if (kData.Configuration.MotionID != 0 && kData.RigDefinition.IsCreated)
            {
                kData.TranslationIndex = Core.FindBindingIndex(ref kData.RigDefinition.Value.Bindings.TranslationBindings, kData.Configuration.MotionID);
                kData.RotationIndex = Core.FindBindingIndex(ref kData.RigDefinition.Value.Bindings.RotationBindings, kData.Configuration.MotionID);

                if (kData.TranslationIndex < 0 || kData.RotationIndex < 0)
                {
                    Debug.LogWarning("InPlaceMotionNode. Could not find the specified MotionID on the Rig. Using index 0 instead.");

                    kData.RotationIndex = math.max(0, kData.RotationIndex);
                    kData.TranslationIndex = math.max(0, kData.TranslationIndex);
                }
            }
            else
            {
                kData.TranslationIndex = 0;
                kData.RotationIndex = 0;
            }
        }

        // this is the default projection
        // todo: support Mecanim parameters for motion projection
        public static void ProjectMotionNode(float3 t, quaternion q, out float3 projT, out quaternion projQ, bool bankPivot)
        {
            if (bankPivot)
            {
                projT = math.mul(q, new float3(0, 1, 0));
                projT = t - projT * (t.y / projT.y);
            }
            else
            {
                projT.x = t.x;
                projT.y = 0;
                projT.z = t.z;
            }

            projQ.value.x = 0;
            projQ.value.y = q.value.y / q.value.w;
            projQ.value.z = 0;
            projQ.value.w = 1;
            projQ = math.normalize(projQ);
        }
    }
}
