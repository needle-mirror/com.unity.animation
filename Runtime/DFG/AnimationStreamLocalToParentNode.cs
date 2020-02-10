using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Mathematics;
using Unity.Profiling;

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Utils", description:"Gets the local to parent information of a bone in the AnimationStream")]
    public class GetAnimationStreamLocalToParentNode
        : NodeDefinition<GetAnimationStreamLocalToParentNode.Data, GetAnimationStreamLocalToParentNode.SimPorts, GetAnimationStreamLocalToParentNode.KernelData, GetAnimationStreamLocalToParentNode.KernelDefs, GetAnimationStreamLocalToParentNode.Kernel>
        , IMsgHandler<Rig>
        , IRigContextHandler
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(isHidden:true)]
            public MessageInput<GetAnimationStreamLocalToParentNode, Rig> Rig;
        }

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.GetAnimationStreamLocalToParentNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Input stream")]
            public DataInput<GetAnimationStreamLocalToParentNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Bone index in stream")]
            public DataInput<GetAnimationStreamLocalToParentNode, int> Index;

            [PortDefinition(description:"Local to parent translation")]
            public DataOutput<GetAnimationStreamLocalToParentNode, float3> Translation;
            [PortDefinition(description:"Local to parent rotation")]
            public DataOutput<GetAnimationStreamLocalToParentNode, quaternion> Rotation;
            [PortDefinition(description:"Local to parent scale")]
            public DataOutput<GetAnimationStreamLocalToParentNode, float3> Scale;
            [PortDefinition(description:"Local to parent transform")]
            public DataOutput<GetAnimationStreamLocalToParentNode, float4x4> Transform;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfilerMarker;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var stream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                if (stream.IsNull)
                    throw new System.InvalidOperationException($"GetAnimationStreamLocalToParentNode input is invalid.");

                data.ProfilerMarker.Begin();

                stream.GetLocalToParentTRS(context.Resolve(ports.Index), out float3 translation, out quaternion rotation, out float3 scale);
                context.Resolve(ref ports.Translation) = translation;
                context.Resolve(ref ports.Rotation) = rotation;
                context.Resolve(ref ports.Scale) = scale;
                context.Resolve(ref ports.Transform) = float4x4.TRS(translation, rotation, scale);

                data.ProfilerMarker.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfilerMarker;
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            GetKernelData(ctx.Handle).RigDefinition = rig;
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }

    [NodeDefinition(category: "Animation Core/Utils", description: "Sets the local to parent information of a bone in the AnimationStream")]
    public class SetAnimationStreamLocalToParentNode
        : NodeDefinition<SetAnimationStreamLocalToParentNode.Data, SetAnimationStreamLocalToParentNode.SimPorts, SetAnimationStreamLocalToParentNode.KernelData, SetAnimationStreamLocalToParentNode.KernelDefs, SetAnimationStreamLocalToParentNode.Kernel>
        , IMsgHandler<Rig>
        , IRigContextHandler
    {
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
            [PortDefinition(isHidden:true)]
            public MessageInput<SetAnimationStreamLocalToParentNode, Rig> Rig;
        }

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Animation.SetAnimationStreamLocalToParentNode");

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Input stream")]
            public DataInput<SetAnimationStreamLocalToParentNode, Buffer<AnimatedData>> Input;
            [PortDefinition(description:"Bone index in stream")]
            public DataInput<SetAnimationStreamLocalToParentNode, int> Index;
            [PortDefinition(description:"Modes to set local to parent information", isStatic:true, defaultValue:SetFromMode.TranslationRotationScale)]
            public DataInput<SetAnimationStreamLocalToParentNode, SetFromMode> Mode;

            [PortDefinition(description:"Local to parent translation", defaultValue:"zero", defaultValueType:DefaultValueType.Reference)]
            public DataInput<SetAnimationStreamLocalToParentNode, float3> Translation;
            [PortDefinition(description:"Local to parent rotation", defaultValue:"identity", defaultValueType:DefaultValueType.Reference)]
            public DataInput<SetAnimationStreamLocalToParentNode, quaternion> Rotation;
            [PortDefinition(description:"Local to parent scale", defaultValue:"1,1,1", defaultValueType:DefaultValueType.ComplexValue)]
            public DataInput<SetAnimationStreamLocalToParentNode, float3> Scale;

            [PortDefinition(description:"Resulting stream")]
            public DataOutput<SetAnimationStreamLocalToParentNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public BlobAssetReference<RigDefinition> RigDefinition;
            public ProfilerMarker ProfilerMarker;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var input = context.Resolve(ports.Input);
                var output = context.Resolve(ref ports.Output);
                if (input.Length != output.Length)
                    throw new System.InvalidOperationException($"SetAnimationStreamLocalToParentNode: Input Length '{input.Length}' does not match Output Length '{output.Length}'");

                data.ProfilerMarker.Begin();

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                if (stream.IsNull)
                    throw new System.InvalidOperationException($"SetAnimationStreamLocalToParentNode output is invalid.");

                var mode = context.Resolve(ports.Mode);
                var index = context.Resolve(ports.Index);

                if (mode == SetFromMode.TranslationRotationScale)
                {
                    stream.SetLocalToParentTRS(index, context.Resolve(ports.Translation), context.Resolve(ports.Rotation), context.Resolve(ports.Scale));
                }
                else
                {
                    if ((mode & SetFromMode.Translation) != 0)
                        stream.SetLocalToParentTranslation(index, context.Resolve(ports.Translation));
                    if ((mode & SetFromMode.Rotation) != 0)
                        stream.SetLocalToParentRotation(index, context.Resolve(ports.Rotation));
                    if ((mode & SetFromMode.Scale) != 0)
                        stream.SetLocalToParentScale(index, context.Resolve(ports.Scale));
                }

                data.ProfilerMarker.End();
            }
        }

        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfilerMarker = k_ProfilerMarker;

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

