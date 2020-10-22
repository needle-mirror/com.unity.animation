using System;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Mathematics;
using Unity.Entities;

namespace Unity.Animation
{
    [NodeDefinition(guid: "bf4c5a78bcd94ce7a5ad2d0efd5fa50b", version: 1, category: "Animation Core/Constraints", description: "Two bone IK solver")]
    public class TwoBoneIKNode
        : SimulationKernelNodeDefinition<TwoBoneIKNode.SimPorts, TwoBoneIKNode.KernelDefs>
        , IRigContextHandler<TwoBoneIKNode.Data>
    {
        [Serializable]
        public struct SetupMessage
        {
            public int RootIndex;
            public int MidIndex;
            public int TipIndex;

            public RigidTransform TargetOffset;
        }

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "333243e5ba1b4cff93931c03de198e70", isHidden: true)]
            public MessageInput<TwoBoneIKNode, Rig> Rig;
            [PortDefinition(guid: "d86d0bb4849f4bf18a6cb707ebd3db2c", displayName: "Setup", description: "Two bone IK properties")]
            public MessageInput<TwoBoneIKNode, SetupMessage> ConstraintSetup;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "55cc66b4bcde41d3849b5c8403c3df36", description: "Constrained animation stream")]
            public DataInput<TwoBoneIKNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "1fd2e17847354cffb011e3c8c6eb3218", description: "Resulting animation stream")]
            public DataOutput<TwoBoneIKNode, Buffer<AnimatedData>> Output;

            [PortDefinition(guid: "06e6b83ca095476d88ae1f05ba180705", description: "Constraint weight", defaultValue: 1f)]
            public DataInput<TwoBoneIKNode, float> Weight;
            [PortDefinition(guid: "92dfc7cebc6d47d89e1f28650a8b718b", description: "IK goal position weight", defaultValue: 1f)]
            public DataInput<TwoBoneIKNode, float> TargetPositionWeight;
            [PortDefinition(guid: "be2b285734ad4efdb06b01e035bae0ee", description: "IK goal rotation weight", defaultValue: 1f)]
            public DataInput<TwoBoneIKNode, float> TargetRotationWeight;
            [PortDefinition(guid: "a9076de32ec4452e8bffb618b2a92661", description: "Target transform", defaultValue: "identity", defaultValueType: DefaultValueType.Reference)]
            public DataInput<TwoBoneIKNode, float4x4> Target;

            [PortDefinition(guid: "957aecbd6a5d4c339ddfecdf2fb6a8c0", description: "IK hint weight", defaultValue: 0f)]
            public DataInput<TwoBoneIKNode, float> HintWeight;
            [PortDefinition(guid: "4565216d6b774cddac133611b381573c", description: "Target hint position")]
            public DataInput<TwoBoneIKNode, float3> Hint;
        }

        struct Data : INodeData, IInit
            , IMsgHandler<Rig>, IMsgHandler<SetupMessage>
        {
            KernelData m_KernelData;

            public void Init(InitContext ctx)
            {
                m_KernelData.RootIndex = -1;
                m_KernelData.MidIndex = -1;
                m_KernelData.TipIndex = -1;
                m_KernelData.TargetOffset = RigidTransform.identity;

                ctx.SetInitialPortValue(KernelPorts.Weight, 1f);
                ctx.SetInitialPortValue(KernelPorts.TargetPositionWeight, 1f);
                ctx.SetInitialPortValue(KernelPorts.TargetRotationWeight, 1f);
                ctx.SetInitialPortValue(KernelPorts.Target, float4x4.identity);

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
                m_KernelData.RootIndex = msg.RootIndex;
                m_KernelData.MidIndex = msg.MidIndex;
                m_KernelData.TipIndex = msg.TipIndex;
                m_KernelData.TargetOffset = msg.TargetOffset;

                ctx.UpdateKernelData(m_KernelData);
            }
        }

        struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;

            public int RootIndex;
            public int MidIndex;
            public int TipIndex;

            public RigidTransform TargetOffset;
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

                var ikData = new Core.TwoBoneIKData
                {
                    RootIndex = data.RootIndex,
                    MidIndex = data.MidIndex,
                    TipIndex = data.TipIndex,
                    TargetOffset = data.TargetOffset,
                    Target = new RigidTransform(ctx.Resolve(ports.Target)),
                    Hint = ctx.Resolve(ports.Hint),
                    TargetPositionWeight = ctx.Resolve(ports.TargetPositionWeight),
                    TargetRotationWeight = ctx.Resolve(ports.TargetRotationWeight),
                    HintWeight = ctx.Resolve(ports.HintWeight)
                };

                Core.SolveTwoBoneIK(ref stream, ikData, ctx.Resolve(ports.Weight));
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Rig;
    }
}
