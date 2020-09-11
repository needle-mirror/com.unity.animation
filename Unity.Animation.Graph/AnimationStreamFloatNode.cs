using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "559f48b3f95f4facb346a1738b028792", version: 1, category: "Animation Core/Utils", description: "Gets a float value from the AnimationStream")]
    public class GetAnimationStreamFloatNode
        : NodeDefinition<GetAnimationStreamFloatNode.Data, GetAnimationStreamFloatNode.SimPorts, GetAnimationStreamFloatNode.KernelData, GetAnimationStreamFloatNode.KernelDefs, GetAnimationStreamFloatNode.Kernel>
        , IRigContextHandler
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "753f33bf35ba4093bfda46a882526f4c", isHidden: true)]
            public MessageInput<GetAnimationStreamFloatNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "5cceb8478b6f48908dfe909b6a95896f", description: "Input stream")]
            public DataInput<GetAnimationStreamFloatNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "2a4bcdce5d9849ad826897c44992d297", description: "Index in stream")]
            public DataInput<GetAnimationStreamFloatNode, int> Index;

            [PortDefinition(guid: "47114a90a5fc4a8995959c8f8ea75865", description: "Value")]
            public DataOutput<GetAnimationStreamFloatNode, float> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var stream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                stream.ValidateIsNotNull();

                context.Resolve(ref ports.Output) = stream.GetFloat(context.Resolve(ports.Index));
            }
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            GetKernelData(ctx.Handle).RigDefinition = rig;
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }

#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "75c83d51cb9a4eae8ca36d22068f9943", version: 1, category: "Animation Core/Utils", description: "Sets a float value in the AnimationStream")]
    public class SetAnimationStreamFloatNode
        : NodeDefinition<SetAnimationStreamFloatNode.Data, SetAnimationStreamFloatNode.SimPorts, SetAnimationStreamFloatNode.KernelData, SetAnimationStreamFloatNode.KernelDefs, SetAnimationStreamFloatNode.Kernel>
        , IRigContextHandler
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "7f06ce9ed09347acb98706cb707c9bae", isHidden: true)]
            public MessageInput<SetAnimationStreamFloatNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "f3a390bef90f4b40a5aba7372672632e", description: "Input stream")]
            public DataInput<SetAnimationStreamFloatNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "032508f3610f4e72ac7e7c81a04636c1", description: "Index in stream")]
            public DataInput<SetAnimationStreamFloatNode, int>  Index;
            [PortDefinition(guid: "3644a616440b44d0a287e91947599135", description: "Value to set")]
            public DataInput<SetAnimationStreamFloatNode, float> Value;

            [PortDefinition(guid: "d81cd7f253cc477893acfba2aea8564c", description: "Resulting stream")]
            public DataOutput<SetAnimationStreamFloatNode, Buffer<AnimatedData>> Output;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                var input = context.Resolve(ports.Input);
                var output = context.Resolve(ref ports.Output);

                Core.ValidateBufferLengthsAreEqual(output.Length, input.Length);

                output.CopyFrom(input);
                var stream = AnimationStream.Create(data.RigDefinition, output);
                stream.ValidateIsNotNull();

                stream.SetFloat(context.Resolve(ports.Index), context.Resolve(ports.Value));
            }
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
