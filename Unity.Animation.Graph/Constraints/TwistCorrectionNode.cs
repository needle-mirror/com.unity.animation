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
    [NodeDefinition(guid: "6f515a98326e46108f9f2f9c251c3afb", version: 1, category: "Animation Core/Constraints", description: "Twist correction is mainly used to redistribute a percentage of the source rotation over a leaf bone in order to correct mesh deformation artifacts.")]
    [PortGroupDefinition(portGroupSizeDescription: "Twist Bone Count", groupIndex: 1, minInstance: 1, maxInstance: -1)]
    public class TwistCorrectionNode
        : NodeDefinition<TwistCorrectionNode.Data, TwistCorrectionNode.SimPorts, TwistCorrectionNode.KernelData, TwistCorrectionNode.KernelDefs, TwistCorrectionNode.Kernel>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.TwistCorrectionNode");
#endif
        public enum TwistAxis
        {
            X, Y, Z
        };

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "caa9251733f2457ea17846b8218f21f5", isHidden: true)]
            public MessageInput<TwistCorrectionNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "fb3aa28263ca41c597334c64fa72f2d6", description: "Constrained animation stream")]
            public DataInput<TwistCorrectionNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "ef48a63dba7342ac98daee94e04e032d", description: "Resulting animation stream")]
            public DataOutput<TwistCorrectionNode, Buffer<AnimatedData>> Output;

            [PortDefinition(guid: "80d8c2d01696487c80650d88d90ffaf1", description: "Constraint Weight", defaultValue: 1f)]
            public DataInput<TwistCorrectionNode, float> Weight;
            [PortDefinition(guid: "54d9a28f88b14fbcb7891b22cb8f6c0e", description: "Local Twist Axis", isStatic: true, defaultValue: TwistCorrectionNode.TwistAxis.Y)]
            public DataInput<TwistCorrectionNode, TwistAxis> LocalTwistAxis;

            [PortDefinition(guid: "66c5ef437395453dbcfbb8db213d0302", displayName: "Source Rotation", description: "Current rotation of the source")]
            public DataInput<TwistCorrectionNode, quaternion> SourceRotation;
            [PortDefinition(guid: "288e5ce189d9433aa08d5b91223d8e4d", displayName: "Source Default Rotation", description: "Default or initial rotation of the source at setup. This is used to compute the delta twist rotation")]
            public DataInput<TwistCorrectionNode, quaternion> SourceDefaultRotation;

            [PortDefinition(guid: "12e0b4566cfb45858d9673aa403bc2fa", displayName: "Twist Bone Index", description: "Twist bones driven by the source", portGroupIndex: 1, defaultValue: -1)]
            public PortArray<DataInput<TwistCorrectionNode, int>>  TwistIndices;
            [PortDefinition(guid: "d7ca08c609794eed8c1128a43651ae83", displayName: "Twist Bone Weight", description: "Twist bone weights", portGroupIndex: 1, defaultValue: 0f, minValueUI: -1f, maxValueUI: 1f)]
            public PortArray<DataInput<TwistCorrectionNode, float>> TwistWeights;
        }

        public struct Data : INodeData {}

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfilerMarker;
#endif
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
            {
                var input = ctx.Resolve(ports.Input);
                var output = ctx.Resolve(ref ports.Output);
                if (input.Length != output.Length)
                    throw new InvalidOperationException($"TwistCorrectionNode: Input Length '{input.Length}' doesn't match Output Length '{output.Length}'");

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.Begin();
#endif
                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                if (stream.IsNull)
                    throw new ArgumentNullException($"TwistCorrectionNode: Invalid output stream");

                var twistIndexPorts = ctx.Resolve(ports.TwistIndices);
                var twistWeightPorts = ctx.Resolve(ports.TwistWeights);
                if (twistIndexPorts.Length != twistWeightPorts.Length)
                    throw new ArgumentException($"TwistCorrectionNode: TwistIndices and TwistWeights sizes must be the same");

                var twistIndexArray = new NativeArray<int>(twistIndexPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var twistWeightArray = new NativeArray<float>(twistWeightPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                twistIndexPorts.CopyTo(twistIndexArray);
                twistWeightArray.CopyTo(twistWeightArray);

                var twistData = new Core.TwistCorrectionData
                {
                    SourceRotation = ctx.Resolve(ports.SourceRotation),
                    SourceInverseDefaultRotation = math.conjugate(ctx.Resolve(ports.SourceDefaultRotation)),
                    LocalTwistAxis = Convert(ctx.Resolve(ports.LocalTwistAxis)),
                    TwistIndices = twistIndexArray,
                    TwistWeights = twistWeightArray
                };
                Core.SolveTwistCorrection(ref stream, twistData, ctx.Resolve(ports.Weight));

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.End();
#endif
            }

            static float3 Convert(TwistAxis axis)
            {
                switch (axis)
                {
                    case TwistAxis.X:
                        return math.float3(1f, 0f, 0f);
                    case TwistAxis.Z:
                        return math.float3(0f, 0f, 1f);
                    case TwistAxis.Y:
                    default:
                        return math.float3(0f, 1f, 0f);
                }
            }
        }

        protected override void Init(InitContext ctx)
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfilerMarker;
#endif

            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Weight, 1f);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.LocalTwistAxis, TwistAxis.Y);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.SourceRotation, quaternion.identity);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.SourceDefaultRotation,  quaternion.identity);
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

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
