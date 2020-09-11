using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "ad8ef6033fd64cff8c1bbcb0b2b30291", version: 1, category: "Animation Core/Utils", description: "Gets an integer value from the AnimationStream")]
    public class GetAnimationStreamIntNode
        : NodeDefinition<GetAnimationStreamIntNode.Data, GetAnimationStreamIntNode.SimPorts, GetAnimationStreamIntNode.KernelData, GetAnimationStreamIntNode.KernelDefs, GetAnimationStreamIntNode.Kernel>
        , IRigContextHandler
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "40137712b11041f79fd9dd1267ed0cbc", isHidden: true)]
            public MessageInput<GetAnimationStreamIntNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "115d1c89a20249d29320e2c48f12a55f", description: "Input stream")]
            public DataInput<GetAnimationStreamIntNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "b7a6a3c6e69d44139c8957a3e4f67e62", description: "Index in stream")]
            public DataInput<GetAnimationStreamIntNode, int> Index;

            [PortDefinition(guid: "46dbcf94d6944c66b53d296597fdf75d", description: "Value")]
            public DataOutput<GetAnimationStreamIntNode, int> Output;
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

                context.Resolve(ref ports.Output) = stream.GetInt(context.Resolve(ports.Index));
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
    [NodeDefinition(guid: "9be251e51f4c4285835e8ec200827c2b", version: 1, category: "Animation Core/Utils", description: "Sets an integer value in the AnimationStream")]
    public class SetAnimationStreamIntNode
        : NodeDefinition<SetAnimationStreamIntNode.Data, SetAnimationStreamIntNode.SimPorts, SetAnimationStreamIntNode.KernelData, SetAnimationStreamIntNode.KernelDefs, SetAnimationStreamIntNode.Kernel>
        , IRigContextHandler
    {
#pragma warning restore 0618

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "cbad1651d4cd461d9229170fe64b6d79", isHidden: true)]
            public MessageInput<SetAnimationStreamIntNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "71ae6c1a84f64fa392e0fde5ead8e569", description: "Input stream")]
            public DataInput<SetAnimationStreamIntNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "91fd45ff2a5b478f93d062eb8d66c4ce", description: "Index in stream")]
            public DataInput<SetAnimationStreamIntNode, int> Index;
            [PortDefinition(guid: "6243111b3c8d488f8714de4b7843fd03", description: "Value to set")]
            public DataInput<SetAnimationStreamIntNode, int> Value;

            [PortDefinition(guid: "e748767ab74a49c29a02df90f7a45d38", description: "Resulting stream")]
            public DataOutput<SetAnimationStreamIntNode, Buffer<AnimatedData>> Output;
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

                stream.SetInt(context.Resolve(ports.Index), context.Resolve(ports.Value));
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
