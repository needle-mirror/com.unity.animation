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
    [NodeDefinition(guid: "e1446b44998a48a4803fd10b9508857f", version: 1, category: "Animation Core/Constraints", description: "Aim constraint based on multiple sources")]
    [PortGroupDefinition(portGroupSizeDescription: "Source Count", groupIndex: 1, minInstance: 1, maxInstance: -1)]
    public class AimConstraintNode
        : NodeDefinition<AimConstraintNode.Data, AimConstraintNode.SimPorts, AimConstraintNode.KernelData, AimConstraintNode.KernelDefs, AimConstraintNode.Kernel>
        , IMsgHandler<AimConstraintNode.SetupMessage>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.AimConstraintNode");
#endif

        [Serializable]
        public struct SetupMessage
        {
            public int Index;
            public float3 LocalAimAxis;
            public bool3 LocalAxesMask;
        }

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "691f5329e50149fca0f721d99bc24b0a", isHidden: true)]
            public MessageInput<AimConstraintNode, Rig> Rig;
            [PortDefinition(guid: "164bc1866708475491e9329bf2aa29e0", displayName: "Setup", description: "Aim constraint properties")]
            public MessageInput<AimConstraintNode, SetupMessage> ConstraintSetup;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "0ec7455707ba481da8d850a554186c30", description: "Constrained animation stream")]
            public DataInput<AimConstraintNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "af6572d18c8a43eb99056a48a2b0a05c", description: "Resulting animation stream")]
            public DataOutput<AimConstraintNode, Buffer<AnimatedData>> Output;

            [PortDefinition(guid: "2987902310a447ffb6ce201111ddb512", description: "Constraint weight", defaultValue: 1f)]
            public DataInput<AimConstraintNode, float> Weight;
            [PortDefinition(guid: "7107632ecabf47bdbbdcad245c7483f3", description: "Extra local offset to apply to the constrained bone expressed in Euler angles (degrees)", defaultValue: "zero", defaultValueType: DefaultValueType.Reference)]
            public DataInput<AimConstraintNode, float3> LocalOffset;
            [PortDefinition(guid: "9a8ecc1095b84d7da5e2902feb626246", description: "Extra local offset rotation order", defaultValue: math.RotationOrder.Default)]
            public DataInput<AimConstraintNode, math.RotationOrder> LocalOffsetRotationOrder;
            [PortDefinition(guid: "452289c848ec4f70bfd272ea6f43804e", description: "Minimum angle limit (degrees)", defaultValue: -180f, minValueUI: -180f, maxValueUI: 180f)]
            public DataInput<AimConstraintNode, float> MinAngleLimit;
            [PortDefinition(guid: "8574fb7d7a744d7f96b6faadce83dfd8", description: "Maximum angle limit (degrees)", defaultValue: 180f, minValueUI: -180f, maxValueUI: 180f)]
            public DataInput<AimConstraintNode, float> MaxAngleLimit;

            [PortDefinition(guid: "7c8c9cb5b21d42b09cefdc1ca6acdc46", displayName: "Source Position", description: "Position of source", portGroupIndex: 1, defaultValue: "zero", defaultValueType: DefaultValueType.Reference)]
            public PortArray<DataInput<AimConstraintNode, float3>> SourcePositions;
            [PortDefinition(guid: "ca51dfae5afb4078a74ea4f4a7a76413", displayName: "Source Rotation Offset", description: "Rotation offset of source", portGroupIndex: 1, defaultValue: "identity", defaultValueType: DefaultValueType.Reference)]
            public PortArray<DataInput<AimConstraintNode, quaternion>> SourceOffsets;
            [PortDefinition(guid: "9018ec9b19d84714a2fe0a2a9ec18580", displayName: "Source Weight", description: "Weight of source", portGroupIndex: 1, defaultValue: 1f)]
            public PortArray<DataInput<AimConstraintNode, float>> SourceWeights;
        }

        public struct Data : INodeData {}

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfilerMarker;
#endif
            public BlobAssetReference<RigDefinition> RigDefinition;

            public int Index;
            public float3 LocalAimAxis;
            public bool3 LocalAxesMask;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
            {
                var input = ctx.Resolve(ports.Input);
                var output = ctx.Resolve(ref ports.Output);
                if (input.Length != output.Length)
                    throw new InvalidOperationException($"AimConstrainNode: Input Length '{input.Length}' doesn't match Output Length '{output.Length}'");

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.Begin();
#endif

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                if (stream.IsNull)
                    throw new ArgumentNullException($"AimConstrainNode: Invalid output stream");

                var srcPositionPorts = ctx.Resolve(ports.SourcePositions);
                var srcOffsetPorts = ctx.Resolve(ports.SourceOffsets);
                var srcWeightPorts = ctx.Resolve(ports.SourceWeights);
                if (srcPositionPorts.Length != srcOffsetPorts.Length || srcOffsetPorts.Length != srcWeightPorts.Length)
                    throw new ArgumentException($"AimConstrainNode: SourcePositions, SourceOffsets and SourceWeights sizes must be the same");

                var srcPositionArray = new NativeArray<float3>(srcPositionPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var srcOffsetArray = new NativeArray<quaternion>(srcOffsetPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var srcWeightArray = new NativeArray<float>(srcWeightPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                srcPositionPorts.CopyTo(srcPositionArray);
                srcOffsetPorts.CopyTo(srcOffsetArray);
                srcWeightPorts.CopyTo(srcWeightArray);

                var localOffset = ctx.Resolve(ports.LocalOffset);
                var localOffsetQuat = math.lengthsq(localOffset) > 0f ?
                    quaternion.Euler(localOffset, ctx.Resolve(ports.LocalOffsetRotationOrder)) : quaternion.identity;

                var constraintData = new Core.AimConstraintData
                {
                    Index = data.Index,
                    LocalOffset = localOffsetQuat,
                    LocalAimAxis = data.LocalAimAxis,
                    LocalAxesMask = data.LocalAxesMask,
                    MinAngleLimit = math.radians(ctx.Resolve(ports.MinAngleLimit)),
                    MaxAngleLimit = math.radians(ctx.Resolve(ports.MaxAngleLimit)),
                    SourcePositions = srcPositionArray,
                    SourceOffsets = srcOffsetArray,
                    SourceWeights = srcWeightArray
                };
                Core.SolveAimConstraint(ref stream, constraintData, ctx.Resolve(ports.Weight));

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
            kData.LocalAimAxis = math.up();
            kData.LocalAxesMask = new bool3(true);

            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Weight, 1f);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.LocalOffsetRotationOrder, math.RotationOrder.Default);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.MinAngleLimit, -180f);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.MaxAngleLimit,  180f);
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
            kData.LocalAimAxis = msg.LocalAimAxis;
            kData.LocalAxesMask = msg.LocalAxesMask;
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
