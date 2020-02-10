using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Collections;
using Unity.Profiling;

using System;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Constraints", description:"Position constraint based on multiple sources")]
    [PortGroupDefinition(portGroupSizeDescription:"Source Count", groupIndex:1, minInstance:1, maxInstance:-1)]
    public class PositionConstraintNode
        : NodeDefinition<PositionConstraintNode.Data, PositionConstraintNode.SimPorts, PositionConstraintNode.KernelData, PositionConstraintNode.KernelDefs, PositionConstraintNode.Kernel>
        , IMsgHandler<Rig>
        , IMsgHandler<PositionConstraintNode.SetupMessage>
        , IRigContextHandler
    {
        [Serializable]
        public struct SetupMessage
        {
            public int Index;
            public bool3 LocalAxesMask;
        }

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<PositionConstraintNode, Rig> Rig;
            [PortDefinition(displayName:"Setup", description:"Position constraint properties")]
            public MessageInput<PositionConstraintNode, SetupMessage> ConstraintSetup;
        }

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.PositionConstraintNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Constrained animation stream")]
            public DataInput<PositionConstraintNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<PositionConstraintNode, Buffer<AnimatedData>> Output;

            [PortDefinition(description:"Constraint weight", defaultValue:1f)]
            public DataInput<PositionConstraintNode, float> Weight;
            [PortDefinition(description:"Extra local offset to apply to the constrained bone")]
            public DataInput<PositionConstraintNode, float3> LocalOffset;

            [PortDefinition(displayName:"Source Position", description:"Position of source", portGroupIndex:1)]
            public PortArray<DataInput<PositionConstraintNode, float3>> SourcePositions;
            [PortDefinition(displayName:"Source Offset", description:"Position offset of source", portGroupIndex:1)]
            public PortArray<DataInput<PositionConstraintNode, float3>> SourceOffsets;
            [PortDefinition(displayName:"Source Weight", description:"Weight of source", portGroupIndex:1, defaultValue:1f)]
            public PortArray<DataInput<PositionConstraintNode, float>> SourceWeights;
        }

        public struct Data : INodeData { }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfilerMarker;

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
                    throw new InvalidOperationException($"PositionConstrainNode: Input Length '{input.Length}' doesn't match Output Length '{output.Length}'");

                data.ProfilerMarker.Begin();

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                if (stream.IsNull)
                    throw new ArgumentNullException("PositionConstrainNode: Invalid output stream");

                var srcPositionPorts = ctx.Resolve(ports.SourcePositions);
                var srcOffsetPorts = ctx.Resolve(ports.SourceOffsets);
                var srcWeightPorts = ctx.Resolve(ports.SourceWeights);
                if (srcPositionPorts.Length != srcOffsetPorts.Length || srcOffsetPorts.Length != srcWeightPorts.Length)
                    throw new ArgumentException("PositionConstrainNode: SourcePositions, SourceOffsets and SourceWeights sizes must be the same");

                var srcPositionArray = new NativeArray<float3>(srcPositionPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var srcOffsetArray   = new NativeArray<float3>(srcOffsetPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var srcWeightArray   = new NativeArray<float>(srcWeightPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                srcPositionPorts.CopyTo(srcPositionArray);
                srcOffsetPorts.CopyTo(srcOffsetArray);
                srcWeightPorts.CopyTo(srcWeightArray);

                var constraintData = new Core.PositionConstraintData
                {
                    Index = data.Index,
                    LocalAxesMask = data.LocalAxesMask,
                    LocalOffset = ctx.Resolve(ports.LocalOffset),
                    SourcePositions = srcPositionArray,
                    SourceOffsets = srcOffsetArray,
                    SourceWeights = srcWeightArray
                };
                Core.SolvePositionConstraint(ref stream, constraintData, ctx.Resolve(ports.Weight));

                data.ProfilerMarker.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfilerMarker;
            kData.Index = -1;
            kData.LocalAxesMask = new bool3(true);

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
            kData.LocalAxesMask = msg.LocalAxesMask;
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
