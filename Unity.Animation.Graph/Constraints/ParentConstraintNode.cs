using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Collections;

using System;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "a9a00f02c612463b8c82e7e64a62dab9", version: 1, category: "Animation Core/Constraints", description: "Parent constraint based on multiple sources")]
    [PortGroupDefinition(portGroupSizeDescription: "Source Count", groupIndex: 1, minInstance: 1, maxInstance: -1)]
    public class ParentConstraintNode
        : NodeDefinition<ParentConstraintNode.Data, ParentConstraintNode.SimPorts, ParentConstraintNode.KernelData, ParentConstraintNode.KernelDefs, ParentConstraintNode.Kernel>
        , IMsgHandler<ParentConstraintNode.SetupMessage>
        , IRigContextHandler
    {
#pragma warning restore 0618

        [Serializable]
        public struct SetupMessage
        {
            public int Index;
            public bool3 LocalTranslationAxesMask;
            public bool3 LocalRotationAxesMask;
        }

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "04c1c85db1f14360a9c5d86103c1412e", isHidden: true)]
            public MessageInput<ParentConstraintNode, Rig> Rig;
            [PortDefinition(guid: "f3ef40263ac8433ba9c94a225953a306", displayName: "Setup", description: "Parent constraint properties")]
            public MessageInput<ParentConstraintNode, SetupMessage> ConstraintSetup;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "2be76d458fb04c32a7ae30c604462cf5", description: "Constrained animation stream")]
            public DataInput<ParentConstraintNode, Buffer<AnimatedData>>  Input;
            [PortDefinition(guid: "06c3a94d9cb34c4e9e7901b024f87601", description: "Resulting animation stream")]
            public DataOutput<ParentConstraintNode, Buffer<AnimatedData>> Output;

            [PortDefinition(guid: "3be8ded6dda8451fb4c95e2895c64641", description: "Constraint weight", defaultValue: 1f)]
            public DataInput<ParentConstraintNode, float> Weight;

            [PortDefinition(guid: "e975a3f4ed4e42c7b126d9a1e895b21a", displayName: "Source Transform", description: "Transform of source", portGroupIndex: 1, defaultValue: "identity", defaultValueType: DefaultValueType.Reference)]
            public PortArray<DataInput<ParentConstraintNode, float4x4>> SourceTx;
            [PortDefinition(guid: "e620b55354d24e61ab56a046ec4e3b02", displayName: "Source Transform Offset", description: "Transform offset of source", portGroupIndex: 1, defaultValue: "identity", defaultValueType: DefaultValueType.Reference)]
            public PortArray<DataInput<ParentConstraintNode, float4x4>> SourceOffsets;
            [PortDefinition(guid: "24fe5915a1c748829a66cc293883a16c", displayName: "Source Weight", description: "Weight of source", portGroupIndex: 1, defaultValue: 1f)]
            public PortArray<DataInput<ParentConstraintNode, float>> SourceWeights;
        }

        public struct Data : INodeData {}

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public int Index;
            public bool3 LocalTranslationAxesMask;
            public bool3 LocalRotationAxesMask;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
            {
                var input = ctx.Resolve(ports.Input);
                var output = ctx.Resolve(ref ports.Output);

                Core.ValidateBufferLengthsAreEqual(output.Length, input.Length);

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                stream.ValidateIsNotNull();

                var srcTxPorts = ctx.Resolve(ports.SourceTx);
                var srcOffsetPorts = ctx.Resolve(ports.SourceOffsets);
                var srcWeightPorts = ctx.Resolve(ports.SourceWeights);

                Core.ValidateBufferLengthsAreEqual(srcOffsetPorts.Length, srcTxPorts.Length);
                Core.ValidateBufferLengthsAreEqual(srcOffsetPorts.Length, srcWeightPorts.Length);

                var srcTxArray = new NativeArray<RigidTransform>(srcTxPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var srcOffsetArray = new NativeArray<RigidTransform>(srcOffsetPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var srcWeightArray = new NativeArray<float>(srcWeightPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                srcWeightPorts.CopyTo(srcWeightArray);

                // TODO: Optimize
                for (int i = 0; i < srcTxPorts.Length; ++i)
                {
                    srcTxArray[i] = math.RigidTransform(srcTxPorts[i]);
                    srcOffsetArray[i] = math.RigidTransform(srcOffsetPorts[i]);
                }

                var constraintData = new Core.ParentConstraintData
                {
                    Index = data.Index,
                    LocalTranslationAxesMask = data.LocalTranslationAxesMask,
                    LocalRotationAxesMask = data.LocalRotationAxesMask,
                    SourceTx = srcTxArray,
                    SourceOffsets = srcOffsetArray,
                    SourceWeights = srcWeightArray
                };
                Core.SolveParentConstraint(ref stream, constraintData, ctx.Resolve(ports.Weight));
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            kData.Index = -1;
            kData.LocalTranslationAxesMask = new bool3(true);
            kData.LocalRotationAxesMask = new bool3(true);

#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Weight, 1f);
#pragma warning restore 0618
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            GetKernelData(ctx.Handle).RigDefinition = rig;
            Set.SetBufferSize(
                ctx.Handle,
                (OutputPortID)KernelPorts.Output,
                Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
            );
        }

        public void HandleMessage(in MessageContext ctx, in SetupMessage msg)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.Index = msg.Index;
            kData.LocalTranslationAxesMask = msg.LocalTranslationAxesMask;
            kData.LocalRotationAxesMask = msg.LocalRotationAxesMask;
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
