using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Collections;

using System;

namespace Unity.Animation
{
    [NodeDefinition(guid: "a952e1cf248a46dcae1ee07d836b5a6b", version: 1, category: "Animation Core/Constraints", description: "Rotation constraint based on multiple sources")]
    [PortGroupDefinition(portGroupSizeDescription: "Source Count", groupIndex: 1, minInstance: 1, maxInstance: -1)]
    public class RotationConstraintNode
        : SimulationKernelNodeDefinition<RotationConstraintNode.SimPorts, RotationConstraintNode.KernelDefs>
        , IRigContextHandler<RotationConstraintNode.Data>
    {
        [Serializable]
        public struct SetupMessage
        {
            public int Index;
            public bool3 LocalAxesMask;
        }

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "102486d60787452d95658cc23de7a4c2", isHidden: true)]
            public MessageInput<RotationConstraintNode, Rig> Rig;
            [PortDefinition(guid: "9b9bb03ccab743069fdf1c189d64f938", displayName: "Setup", description: "Rotation constraint properties")]
            public MessageInput<RotationConstraintNode, SetupMessage> ConstraintSetup;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "a4e523e048ee40a1b522818190f38b5e", description: "Constrained animation stream")]
            public DataInput<RotationConstraintNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "dd5656b7a89240d4b0f252fd5ecc7666", description: "Resulting animation stream")]
            public DataOutput<RotationConstraintNode, Buffer<AnimatedData>> Output;

            [PortDefinition(guid: "6ef78ae9e9984402873fff63e7bd2533", description: "Constraint weight", defaultValue: 1f)]
            public DataInput<RotationConstraintNode, float> Weight;
            [PortDefinition(guid: "f3a2a53ed58c422e89dc7e160f78dae9", description: "Extra local rotation offset to apply to the constrained bone", defaultValue: "identity", defaultValueType: DefaultValueType.Reference)]
            public DataInput<RotationConstraintNode, quaternion> LocalOffset;

            [PortDefinition(guid: "2c1951d564a546c78c37aa81da2373cf", displayName: "Source Rotation", description: "Rotation of source", portGroupIndex: 1, defaultValue: "identity", defaultValueType: DefaultValueType.Reference)]
            public PortArray<DataInput<RotationConstraintNode, quaternion>> SourceRotations;
            [PortDefinition(guid: "8811154e824644f2b5aeb34b96ade150", displayName: "Source Rotation Offset", description: "Rotation offset of source", portGroupIndex: 1, defaultValue: "identity", defaultValueType: DefaultValueType.Reference)]
            public PortArray<DataInput<RotationConstraintNode, quaternion>> SourceOffsets;
            [PortDefinition(guid: "348b1669f081455e942a81101ffd990a", displayName: "Source Weight", description: "Weight of source", portGroupIndex: 1, defaultValue: 1f)]
            public PortArray<DataInput<RotationConstraintNode, float>> SourceWeights;
        }

        struct Data : INodeData, IInit
            , IMsgHandler<Rig>, IMsgHandler<SetupMessage>
        {
            KernelData m_KernelData;

            public void Init(InitContext ctx)
            {
                m_KernelData.Index = -1;
                m_KernelData.LocalAxesMask = new bool3(true);

                ctx.SetInitialPortValue(KernelPorts.Weight, 1f);
                ctx.SetInitialPortValue(KernelPorts.LocalOffset, quaternion.identity);
                ctx.UpdateKernelData(m_KernelData);
            }

            public void HandleMessage(in MessageContext ctx, in Rig rig)
            {
                m_KernelData.RigDefinition = rig;

                ctx.Set.SetBufferSize(
                    ctx.Handle,
                    (OutputPortID)KernelPorts.Output,
                    Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );

                ctx.UpdateKernelData(m_KernelData);
            }

            public void HandleMessage(in MessageContext ctx, in SetupMessage msg)
            {
                m_KernelData.Index = msg.Index;
                m_KernelData.LocalAxesMask = msg.LocalAxesMask;

                ctx.UpdateKernelData(m_KernelData);
            }
        }

        struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;

            public int Index;
            public bool3 LocalAxesMask;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
            {
                var input = ctx.Resolve(ports.Input);
                var output = ctx.Resolve(ref ports.Output);
                Core.ValidateBufferLengthsAreEqual(output.Length, input.Length);

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                stream.ValidateIsNotNull();

                var srcRotationPorts = ctx.Resolve(ports.SourceRotations);
                var srcOffsetPorts = ctx.Resolve(ports.SourceOffsets);
                var srcWeightPorts = ctx.Resolve(ports.SourceWeights);

                Core.ValidateBufferLengthsAreEqual(srcOffsetPorts.Length, srcRotationPorts.Length);
                Core.ValidateBufferLengthsAreEqual(srcOffsetPorts.Length, srcWeightPorts.Length);

                var srcRotationArray = new NativeArray<quaternion>(srcRotationPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var srcOffsetArray = new NativeArray<quaternion>(srcOffsetPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var srcWeightArray = new NativeArray<float>(srcWeightPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                srcRotationPorts.CopyTo(srcRotationArray);
                srcOffsetPorts.CopyTo(srcOffsetArray);
                srcWeightPorts.CopyTo(srcWeightArray);

                var constraintData = new Core.RotationConstraintData
                {
                    Index = data.Index,
                    LocalAxesMask = data.LocalAxesMask,
                    LocalOffset = ctx.Resolve(ports.LocalOffset),
                    SourceRotations = srcRotationArray,
                    SourceOffsets = srcOffsetArray,
                    SourceWeights = srcWeightArray
                };
                Core.SolveRotationConstraint(ref stream, constraintData, ctx.Resolve(ports.Weight));
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Rig;
    }
}
