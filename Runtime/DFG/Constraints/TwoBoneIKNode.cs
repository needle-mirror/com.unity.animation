using System;
using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Mathematics;
using Unity.Entities;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Constraints", description:"Two bone IK solver")]
    public class TwoBoneIKNode
        : NodeDefinition<TwoBoneIKNode.Data, TwoBoneIKNode.SimPorts, TwoBoneIKNode.KernelData, TwoBoneIKNode.KernelDefs, TwoBoneIKNode.Kernel>
        , IMsgHandler<TwoBoneIKNode.SetupMessage>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.TwoBoneIKNode");
#endif

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
            [PortDefinition(isHidden:true)]
            public MessageInput<TwoBoneIKNode, Rig> Rig;
            [PortDefinition(displayName:"Setup", description:"Two bone IK properties")]
            public MessageInput<TwoBoneIKNode, SetupMessage> ConstraintSetup;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Constrained animation stream")]
            public DataInput<TwoBoneIKNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Resulting animation stream")]
            public DataOutput<TwoBoneIKNode, Buffer<AnimatedData>> Output;

            [PortDefinition(description:"Constraint weight", defaultValue:1f)]
            public DataInput<TwoBoneIKNode, float> Weight;
            [PortDefinition(description:"IK goal position weight", defaultValue:1f)]
            public DataInput<TwoBoneIKNode, float> TargetPositionWeight;
            [PortDefinition(description:"IK goal rotation weight", defaultValue:1f)]
            public DataInput<TwoBoneIKNode, float> TargetRotationWeight;
            [PortDefinition(description:"Target transform", defaultValue:"identity", defaultValueType:DefaultValueType.Reference)]
            public DataInput<TwoBoneIKNode, float4x4> Target;

            [PortDefinition(description:"IK hint weight", defaultValue:0f)]
            public DataInput<TwoBoneIKNode, float> HintWeight;
            [PortDefinition(description:"Target hint position")]
            public DataInput<TwoBoneIKNode, float3> Hint;
        }

        public struct Data : INodeData { }

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfilerMarker;
#endif
            public BlobAssetReference<RigDefinition> RigDefinition;

            public int RootIndex;
            public int MidIndex;
            public int TipIndex;

            public RigidTransform TargetOffset;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, KernelData data, ref KernelDefs ports)
            {
                var input = ctx.Resolve(ports.Input);
                var output = ctx.Resolve(ref ports.Output);
                if (input.Length != output.Length)
                    throw new InvalidOperationException($"TwoBoneIKNode: Input Length '{input.Length}' does not match Output Length '{output.Length}'");

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.Begin();
#endif

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                if (stream.IsNull)
                    throw new ArgumentNullException("TwoBoneIKNode: Invalid output stream");

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
            kData.RootIndex = -1;
            kData.MidIndex = -1;
            kData.TipIndex = -1;
            kData.TargetOffset = RigidTransform.identity;

            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Weight, 1f);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.TargetPositionWeight, 1f);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.TargetRotationWeight, 1f);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Target, float4x4.identity);
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
            kData.RootIndex    = msg.RootIndex;
            kData.MidIndex     = msg.MidIndex;
            kData.TipIndex     = msg.TipIndex;
            kData.TargetOffset = msg.TargetOffset;
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
