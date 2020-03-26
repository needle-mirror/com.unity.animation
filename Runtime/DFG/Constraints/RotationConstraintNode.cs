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
    [NodeDefinition(category:"Animation Core/Constraints", description:"Rotation constraint based on multiple sources")]
    [PortGroupDefinition(portGroupSizeDescription:"Source Count", groupIndex:1, minInstance:1, maxInstance:-1)]
    public class RotationConstraintNode
        : NodeDefinition<RotationConstraintNode.Data, RotationConstraintNode.SimPorts, RotationConstraintNode.KernelData, RotationConstraintNode.KernelDefs, RotationConstraintNode.Kernel>
        , IMsgHandler<RotationConstraintNode.SetupMessage>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.RotationConstraintNode");
#endif

        [Serializable]
        public struct SetupMessage
        {
            public int Index;
            public bool3 LocalAxesMask;
        }

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<RotationConstraintNode, Rig> Rig;
            [PortDefinition(displayName:"Setup", description:"Rotation constraint properties")]
            public MessageInput<RotationConstraintNode, SetupMessage> ConstraintSetup;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Constrained animation stream")]
            public DataInput<RotationConstraintNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<RotationConstraintNode, Buffer<AnimatedData>> Output;

            [PortDefinition(description:"Constraint weight", defaultValue:1f)]
            public DataInput<RotationConstraintNode, float> Weight;
            [PortDefinition(description:"Extra local rotation offset to apply to the constrained bone", defaultValue:"identity", defaultValueType:DefaultValueType.Reference)]
            public DataInput<RotationConstraintNode, quaternion> LocalOffset;

            [PortDefinition(displayName:"Source Rotation", description:"Rotation of source", portGroupIndex:1, defaultValue:"identity", defaultValueType:DefaultValueType.Reference)]
            public PortArray<DataInput<RotationConstraintNode, quaternion>> SourceRotations;
            [PortDefinition(displayName:"Source Rotation Offset", description:"Rotation offset of source", portGroupIndex:1, defaultValue:"identity", defaultValueType:DefaultValueType.Reference)]
            public PortArray<DataInput<RotationConstraintNode, quaternion>> SourceOffsets;
            [PortDefinition(displayName:"Source Weight", description:"Weight of source", portGroupIndex:1, defaultValue:1f)]
            public PortArray<DataInput<RotationConstraintNode, float>> SourceWeights;
        }

        public struct Data : INodeData { }

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfilerMarker;
#endif
            public BlobAssetReference<RigDefinition> RigDefinition;

            public int Index;
            public bool3 LocalAxesMask;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
            {
                var input = ctx.Resolve(ports.Input);
                var output = ctx.Resolve(ref ports.Output);
                if (input.Length != output.Length)
                    throw new InvalidOperationException($"RotationConstrainNode: Input Length '{input.Length}' doesn't match Output Length '{output.Length}'");

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.Begin();
#endif

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                if (stream.IsNull)
                    throw new ArgumentNullException($"RotationConstrainNode: Invalid output stream");

                var srcRotationPorts = ctx.Resolve(ports.SourceRotations);
                var srcOffsetPorts = ctx.Resolve(ports.SourceOffsets);
                var srcWeightPorts = ctx.Resolve(ports.SourceWeights);
                if (srcRotationPorts.Length != srcOffsetPorts.Length || srcOffsetPorts.Length != srcWeightPorts.Length)
                    throw new ArgumentException($"RotationConstrainNode: SourceRotations, SourceOffsets and SourceWeights sizes must be the same");

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
            kData.LocalAxesMask = new bool3(true);

            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Weight, 1f);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.LocalOffset, quaternion.identity);
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
            kData.LocalAxesMask = msg.LocalAxesMask;
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
