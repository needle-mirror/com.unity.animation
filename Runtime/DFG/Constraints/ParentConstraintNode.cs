using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Collections;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

using System;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Constraints", description:"Parent constraint based on multiple sources")]
    [PortGroupDefinition(portGroupSizeDescription:"Source Count", groupIndex:1, minInstance:1, maxInstance:-1)]
    public class ParentConstraintNode
        : NodeDefinition<ParentConstraintNode.Data, ParentConstraintNode.SimPorts, ParentConstraintNode.KernelData, ParentConstraintNode.KernelDefs, ParentConstraintNode.Kernel>
        , IMsgHandler<ParentConstraintNode.SetupMessage>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.ParentConstraintNode");
#endif

        [Serializable]
        public struct SetupMessage
        {
            public int Index;
            public bool3 LocalTranslationAxesMask;
            public bool3 LocalRotationAxesMask;
        }

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<ParentConstraintNode, Rig> Rig;
            [PortDefinition(displayName:"Setup", description:"Parent constraint properties")]
            public MessageInput<ParentConstraintNode, SetupMessage> ConstraintSetup;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Constrained animation stream")]
            public DataInput<ParentConstraintNode, Buffer<AnimatedData>>  Input;
            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<ParentConstraintNode, Buffer<AnimatedData>> Output;

            [PortDefinition(description:"Constraint weight", defaultValue:1f)]
            public DataInput<ParentConstraintNode, float> Weight;

            [PortDefinition(displayName:"Source Transform", description:"Transform of source", portGroupIndex:1, defaultValue:"identity", defaultValueType:DefaultValueType.Reference)]
            public PortArray<DataInput<ParentConstraintNode, float4x4>> SourceTx;
            [PortDefinition(displayName:"Source Transform Offset", description:"Transform offset of source", portGroupIndex:1, defaultValue:"identity", defaultValueType:DefaultValueType.Reference)]
            public PortArray<DataInput<ParentConstraintNode, float4x4>> SourceOffsets;
            [PortDefinition(displayName:"Source Weight", description:"Weight of source", portGroupIndex:1, defaultValue:1f)]
            public PortArray<DataInput<ParentConstraintNode, float>> SourceWeights;
        }

        public struct Data : INodeData { }

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfilerMarker;
#endif
            public BlobAssetReference<RigDefinition> RigDefinition;
            public int Index;
            public bool3 LocalTranslationAxesMask;
            public bool3 LocalRotationAxesMask;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
            {
                var input = ctx.Resolve(ports.Input);
                var output = ctx.Resolve(ref ports.Output);
                if (input.Length != output.Length)
                    throw new InvalidOperationException($"ParentConstrainNode: Input Length '{input.Length}' doesn't match Output Length '{output.Length}'");

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.Begin();
#endif

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                if (stream.IsNull)
                    throw new ArgumentNullException("ParentConstrainNode: Invalid output stream");

                var srcTxPorts = ctx.Resolve(ports.SourceTx);
                var srcOffsetPorts = ctx.Resolve(ports.SourceOffsets);
                var srcWeightPorts = ctx.Resolve(ports.SourceWeights);
                if (srcTxPorts.Length != srcOffsetPorts.Length || srcOffsetPorts.Length != srcWeightPorts.Length)
                    throw new ArgumentException("ParentConstrainNode: SourceTx, SourceOffsets and SourceWeights sizes must be the same");

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

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.End();
#endif
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

#if !UNITY_DISABLE_ANIMATION_PROFILING
            kData.ProfilerMarker = k_ProfilerMarker;
#endif

            kData.Index = -1;
            kData.LocalTranslationAxesMask = new bool3(true);
            kData.LocalRotationAxesMask = new bool3(true);

            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Weight, 1f);
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
