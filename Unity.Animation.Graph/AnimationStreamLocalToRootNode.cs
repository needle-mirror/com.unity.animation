using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Mathematics;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(guid: "1c0e9a16f02c4635a006534424e0c7b1", version: 2, category: "Animation Core/Utils", description: "Gets the local to root information of a bone in the AnimationStream")]
    public class GetAnimationStreamLocalToRootNode
        : NodeDefinition<GetAnimationStreamLocalToRootNode.Data, GetAnimationStreamLocalToRootNode.SimPorts, GetAnimationStreamLocalToRootNode.KernelData, GetAnimationStreamLocalToRootNode.KernelDefs, GetAnimationStreamLocalToRootNode.Kernel>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.GetAnimationStreamLocalToRootNode");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "13209707a0294832a191b8f64f68e63e", isHidden: true)]
            public MessageInput<GetAnimationStreamLocalToRootNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "a15a6ac083264ad2aa2501bb0dbb06ee", description: "Input stream")]
            public DataInput<GetAnimationStreamLocalToRootNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "3b815effbee14709a0f4b316c836f7ec", description: "Bone index in stream")]
            public DataInput<GetAnimationStreamLocalToRootNode, int> Index;

            [PortDefinition(guid: "ba1673240b7f45e68ea2d2c264178432", description: "Local to root translation")]
            public DataOutput<GetAnimationStreamLocalToRootNode, float3> Translation;
            [PortDefinition(guid: "bc3ebb4f4c7b41bc8e5d781c4edfcef9", description: "Local to root rotation")]
            public DataOutput<GetAnimationStreamLocalToRootNode, quaternion> Rotation;
            [PortDefinition(guid: "3ef7495177374ac19c0f749d6612b6cc", description: "Local to root scale")]
            public DataOutput<GetAnimationStreamLocalToRootNode, float3> Scale;
            [PortDefinition(guid: "1be1089afbe644b999c11b8c87ad1f44", description: "Local to root transform")]
            public DataOutput<GetAnimationStreamLocalToRootNode, float4x4> Transform;
        }

        public struct Data : INodeData
        {
        }

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
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var stream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                if (stream.IsNull)
                    throw new System.InvalidOperationException($"GetAnimationStreamLocalToRootNode input is invalid.");

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.Begin();
#endif

                stream.GetLocalToRootTRS(context.Resolve(ports.Index), out float3 translation, out quaternion rotation, out float3 scale);
                context.Resolve(ref ports.Translation) = translation;
                context.Resolve(ref ports.Rotation) = rotation;
                context.Resolve(ref ports.Scale) = scale;
                context.Resolve(ref ports.Transform) = float4x4.TRS(translation, rotation, scale);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfilerMarker;
        }

#endif

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            GetKernelData(ctx.Handle).RigDefinition = rig;
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }

    [NodeDefinition(guid: "d169075041a14a55ad2839660df1f826", version: 2, category: "Animation Core/Utils", description: "Sets the local to root information of a bone in the AnimationStream")]
    public class SetAnimationStreamLocalToRootNode
        : NodeDefinition<SetAnimationStreamLocalToRootNode.Data, SetAnimationStreamLocalToRootNode.SimPorts, SetAnimationStreamLocalToRootNode.KernelData, SetAnimationStreamLocalToRootNode.KernelDefs, SetAnimationStreamLocalToRootNode.Kernel>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.SetAnimationStreamLocalToRootNode");
#endif

        public enum SetFromMode : uint
        {
            Translation              = 1 << 0,
            Rotation                 = 1 << 1,
            Scale                    = 1 << 2,
            TranslationRotation      = Translation | Rotation,
            TranslationScale         = Translation | Scale,
            RotationScale            = Rotation | Scale,
            TranslationRotationScale = Translation | Rotation | Scale
        };

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "78cc61d047334ae28284190bbd93e5db", isHidden: true)]
            public MessageInput<SetAnimationStreamLocalToRootNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "815d613070ae4a99b552133b5c3639fb", description: "Input stream")]
            public DataInput<SetAnimationStreamLocalToRootNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "c974e77c39514d99bf577908b3d60966", description: "Bone index in stream")]
            public DataInput<SetAnimationStreamLocalToRootNode, int> Index;
            [PortDefinition(guid: "08811c01c2a1481f843c91eb59316568", description: "Modes to set local to root information", isStatic: true, defaultValue: SetFromMode.TranslationRotation)]
            public DataInput<SetAnimationStreamLocalToRootNode, SetFromMode> Mode;

            [PortDefinition(guid: "96bc08b3e69849f19419aa116fdd7ed8", description: "Local to root translation", defaultValue: "zero", defaultValueType: DefaultValueType.Reference)]
            public DataInput<SetAnimationStreamLocalToRootNode, float3> Translation;
            [PortDefinition(guid: "b4f432efde51485aaa34b41f05f70cf8", description: "Local to root rotation", defaultValue: "identity", defaultValueType: DefaultValueType.Reference)]
            public DataInput<SetAnimationStreamLocalToRootNode, quaternion> Rotation;
            [PortDefinition(guid: "32e0df0a45534a7b99eab94235903d31", description: "Local to root scale", defaultValue: "1,1,1", defaultValueType: DefaultValueType.ComplexValue)]
            public DataInput<SetAnimationStreamLocalToRootNode, float3> Scale;

            [PortDefinition(guid: "4a9bee390af346598ca0c0e2096c5cbd", description: "Resulting stream")]
            public DataOutput<SetAnimationStreamLocalToRootNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

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
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var input = context.Resolve(ports.Input);
                var output = context.Resolve(ref ports.Output);
                if (input.Length != output.Length)
                    throw new System.InvalidOperationException($"SetAnimationStreamLocalToRootNode: Input Length '{input.Length}' does not match Output Length '{output.Length}'");

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfilerMarker.Begin();
#endif

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                if (stream.IsNull)
                    throw new System.InvalidOperationException($"SetAnimationStreamLocalToRootNode output is invalid.");

                var mode = context.Resolve(ports.Mode);
                var index = context.Resolve(ports.Index);

                if (mode == SetFromMode.TranslationRotationScale)
                {
                    stream.SetLocalToRootTRS(index, context.Resolve(ports.Translation), context.Resolve(ports.Rotation), context.Resolve(ports.Scale));
                }
                else if (mode == SetFromMode.TranslationRotation)
                {
                    stream.SetLocalToRootTR(index, context.Resolve(ports.Translation), context.Resolve(ports.Rotation));
                }
                else
                {
                    if ((mode & SetFromMode.Translation) != 0)
                        stream.SetLocalToRootTranslation(index, context.Resolve(ports.Translation));
                    if ((mode & SetFromMode.Rotation) != 0)
                        stream.SetLocalToRootRotation(index, context.Resolve(ports.Rotation));
                    if ((mode & SetFromMode.Scale) != 0)
                        stream.SetLocalToRootScale(index, context.Resolve(ports.Scale));
                }

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

            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Rotation, quaternion.identity);
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Scale, mathex.one());
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Mode, SetFromMode.TranslationRotationScale);
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
