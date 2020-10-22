using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "d02f3e16b7044fcd9acd09adf22f2a30", version: 1, category: "Animation Core/Root Motion", description: "Extracts motion from a specified transform and projects it's values on the root transform. This node is internally used by the UberClipNode.")]
    public class InPlaceMotionNode
        : SimulationKernelNodeDefinition<InPlaceMotionNode.SimPorts, InPlaceMotionNode.KernelDefs>
        , IRigContextHandler<InPlaceMotionNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "df8397b840e1401ca4884771eaf7d90f", isHidden: true)]
            public MessageInput<InPlaceMotionNode, Rig> Rig;
            [PortDefinition(guid: "71fecbce80b4477ebf85c2b0f13dce8b", description: "Clip configuration mask")]
            public MessageInput<InPlaceMotionNode, ClipConfiguration> Configuration;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "b9bdb0c630ec4b42873e20447bd74bb1", description: "The current animation stream")]
            public DataInput<InPlaceMotionNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "a20f24488e7b400abef60bdf1365b91e", description: "Resulting animation stream with updated root transform")]
            public DataOutput<InPlaceMotionNode, Buffer<AnimatedData>> Output;
        }

        struct Data : INodeData, IMsgHandler<Rig>, IMsgHandler<ClipConfiguration>
        {
            KernelData m_KernelData;

            public void HandleMessage(in MessageContext ctx, in Rig rig)
            {
                m_KernelData.RigDefinition = rig;
                ctx.Set.SetBufferSize(
                    ctx.Handle,
                    (OutputPortID)KernelPorts.Output,
                    Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );

                SetMotionIndices(ref m_KernelData);
                ctx.UpdateKernelData(m_KernelData);
            }

            public void HandleMessage(in MessageContext ctx, in ClipConfiguration msg)
            {
                m_KernelData.Configuration = msg;
                SetMotionIndices(ref m_KernelData);
                ctx.UpdateKernelData(m_KernelData);
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
        }

        struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ClipConfiguration Configuration;

            public int TranslationIndex;
            public int RotationIndex;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == default)
                    return;

                // Fill the destination stream with default values.
                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));

                outputStream.CopyFrom(ref inputStream);

                var defaultStream = AnimationStream.FromDefaultValues(data.RigDefinition);
                var motionTranslation = outputStream.GetLocalToRootTranslation(data.TranslationIndex);
                var motionRotation = outputStream.GetLocalToRootRotation(data.RotationIndex);

                var defaultRotation = defaultStream.GetLocalToRootRotation(data.RotationIndex);
                defaultRotation = mathex.mul(motionRotation, math.conjugate(defaultRotation));
                defaultRotation = mathex.select(quaternion.identity, defaultRotation, math.dot(defaultRotation, defaultRotation) > math.FLT_MIN_NORMAL);

                ProjectMotionNode(motionTranslation, defaultRotation, out float3 motionProjTranslation, out quaternion motionProjRotation, (data.Configuration.Mask & ClipConfigurationMask.BankPivot) != 0);

                outputStream.SetLocalToParentTranslation(0, motionProjTranslation);
                outputStream.SetLocalToParentRotation(0, motionProjRotation);

                outputStream.SetLocalToRootTranslation(data.TranslationIndex, motionTranslation);
                outputStream.SetLocalToRootRotation(data.RotationIndex, motionRotation);
            }
        }

        // this is the default projection
        // todo: support Mecanim parameters for motion projection
        static void ProjectMotionNode(float3 t, quaternion q, out float3 projT, out quaternion projQ, bool bankPivot)
        {
            if (bankPivot)
            {
                projT = math.mul(q, new float3(0, 1, 0));
                projT = t - projT * (t.y / projT.y);
            }
            else
            {
                projT = math.float3(t.x, 0f, t.z);
            }

            projQ = math.normalize(math.quaternion(0f, q.value.y / q.value.w, 0f, 1f));
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
