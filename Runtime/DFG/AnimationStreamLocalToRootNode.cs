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
    [NodeDefinition(category:"Animation Core/Utils", description:"Gets the local to root information of a bone in the AnimationStream")]
    public class GetAnimationStreamLocalToRootNode
        : NodeDefinition<GetAnimationStreamLocalToRootNode.Data, GetAnimationStreamLocalToRootNode.SimPorts, GetAnimationStreamLocalToRootNode.KernelData, GetAnimationStreamLocalToRootNode.KernelDefs, GetAnimationStreamLocalToRootNode.Kernel>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.GetAnimationStreamLocalToRootNode");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<GetAnimationStreamLocalToRootNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Input stream")]
            public DataInput<GetAnimationStreamLocalToRootNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Bone index in stream")]
            public DataInput<GetAnimationStreamLocalToRootNode, int> Index;

            [PortDefinition(description:"Local to root translation")]
            public DataOutput<GetAnimationStreamLocalToRootNode, float3> Translation;
            [PortDefinition(description:"Local to root rotation")]
            public DataOutput<GetAnimationStreamLocalToRootNode, quaternion> Rotation;
            [PortDefinition(description:"Local to root transform")]
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

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
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

                stream.GetLocalToRootTR(context.Resolve(ports.Index), out float3 translation, out quaternion rotation);
                context.Resolve(ref ports.Translation) = translation;
                context.Resolve(ref ports.Rotation) = rotation;
                context.Resolve(ref ports.Transform) = new float4x4(rotation, translation);

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

    [NodeDefinition(category: "Animation Core/Utils", description: "Sets the local to root information of a bone in the AnimationStream")]
    public class SetAnimationStreamLocalToRootNode
        : NodeDefinition<SetAnimationStreamLocalToRootNode.Data, SetAnimationStreamLocalToRootNode.SimPorts, SetAnimationStreamLocalToRootNode.KernelData, SetAnimationStreamLocalToRootNode.KernelDefs, SetAnimationStreamLocalToRootNode.Kernel>
        , IRigContextHandler
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.SetAnimationStreamLocalToRootNode");
#endif

        public enum SetFromMode : uint
        {
            Translation         = 1 << 0,
            Rotation            = 1 << 1,
            TranslationRotation = Translation | Rotation
        };

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<SetAnimationStreamLocalToRootNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Input stream")]
            public DataInput<SetAnimationStreamLocalToRootNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Bone index in stream")]
            public DataInput<SetAnimationStreamLocalToRootNode, int> Index;
            [PortDefinition(description:"Modes to set local to root information", isStatic:true, defaultValue:SetFromMode.TranslationRotation)]
            public DataInput<SetAnimationStreamLocalToRootNode, SetFromMode> Mode;

            [PortDefinition(description:"Local to root translation", defaultValue:"zero", defaultValueType:DefaultValueType.Reference)]
            public DataInput<SetAnimationStreamLocalToRootNode, float3> Translation;
            [PortDefinition(description:"Local to root rotation", defaultValue:"identity", defaultValueType:DefaultValueType.Reference)]
            public DataInput<SetAnimationStreamLocalToRootNode, quaternion> Rotation;

            [PortDefinition(description:"Resulting stream")]
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

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
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

                switch (mode)
                {
                    case SetFromMode.Translation:
                        stream.SetLocalToRootTranslation(index, context.Resolve(ports.Translation));
                        break;
                    case SetFromMode.Rotation:
                        stream.SetLocalToRootRotation(index, context.Resolve(ports.Rotation));
                        break;
                    case SetFromMode.TranslationRotation:
                        stream.SetLocalToRootTR(index, context.Resolve(ports.Translation), context.Resolve(ports.Rotation));
                        break;
                    default:
                        throw new System.InvalidOperationException($"SetAnimationStreamLocalToRootNode SetFromMode value is unknown `{mode}`.");
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
            Set.SetData(ctx.Handle, (InputPortID)KernelPorts.Mode, SetFromMode.TranslationRotation);
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

