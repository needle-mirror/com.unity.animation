using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Collections;

namespace Unity.Animation
{
    [NodeDefinition(guid: "6f515a98326e46108f9f2f9c251c3afb", version: 1, category: "Animation Core/Constraints", description: "Twist correction is mainly used to redistribute a percentage of the source rotation over a leaf bone in order to correct mesh deformation artifacts.")]
    [PortGroupDefinition(portGroupSizeDescription: "Twist Bone Count", groupIndex: 1, minInstance: 1, maxInstance: -1)]
    public class TwistCorrectionNode
        : SimulationKernelNodeDefinition<TwistCorrectionNode.SimPorts, TwistCorrectionNode.KernelDefs>
        , IRigContextHandler<TwistCorrectionNode.Data>
    {
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

        struct Data : INodeData, IInit, IMsgHandler<Rig>
        {
            public void Init(InitContext ctx)
            {
                ctx.SetInitialPortValue(KernelPorts.Weight, 1f);
                ctx.SetInitialPortValue(KernelPorts.LocalTwistAxis, TwistAxis.Y);
                ctx.SetInitialPortValue(KernelPorts.SourceRotation, quaternion.identity);
                ctx.SetInitialPortValue(KernelPorts.SourceDefaultRotation, quaternion.identity);
            }

            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                ctx.UpdateKernelData(new KernelData
                {
                    RigDefinition = rig
                });

                ctx.Set.SetBufferSize(
                    ctx.Handle,
                    (OutputPortID)KernelPorts.Output,
                    Buffer<AnimatedData>.SizeRequest(rig.Value.IsCreated ? rig.Value.Value.Bindings.StreamSize : 0)
                );
            }
        }

        struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext ctx, in KernelData data, ref KernelDefs ports)
            {
                var input = ctx.Resolve(ports.Input);
                var output = ctx.Resolve(ref ports.Output);

                Core.ValidateBufferLengthsAreEqual(output.Length, input.Length);

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                stream.ValidateIsNotNull();

                var twistIndexPorts = ctx.Resolve(ports.TwistIndices);
                var twistWeightPorts = ctx.Resolve(ports.TwistWeights);

                Core.ValidateBufferLengthsAreEqual(twistWeightPorts.Length, twistIndexPorts.Length);

                var twistIndexArray = new NativeArray<int>(twistIndexPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                var twistWeightArray = new NativeArray<float>(twistWeightPorts.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                twistIndexPorts.CopyTo(twistIndexArray);
                twistWeightPorts.CopyTo(twistWeightArray);

                var twistData = new Core.TwistCorrectionData
                {
                    SourceRotation = ctx.Resolve(ports.SourceRotation),
                    SourceInverseDefaultRotation = math.conjugate(ctx.Resolve(ports.SourceDefaultRotation)),
                    LocalTwistAxis = Convert(ctx.Resolve(ports.LocalTwistAxis)),
                    TwistIndices = twistIndexArray,
                    TwistWeights = twistWeightArray
                };
                Core.SolveTwistCorrection(ref stream, twistData, ctx.Resolve(ports.Weight));
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

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Rig;
    }
}
