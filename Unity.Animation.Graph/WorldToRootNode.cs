using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Animation
{
    /// <summary>
    /// This is an *experimental* node that remaps a transform from world space to root space.
    /// </summary>
    [NodeDefinition(guid: "5241b718a3ba437f9264f23ac7d3f744", version: 1, category: "Animation Core/Utils", description: "Remaps a transform from world space to root space.")]
    public class WorldToRootNode
        : SimulationKernelNodeDefinition<WorldToRootNode.SimPorts, WorldToRootNode.KernelDefs>
        , IRigContextHandler<WorldToRootNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "92f925ab4afc486f8438f69a83c003bb", isHidden: true)]
            public MessageInput<WorldToRootNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "e866f7425fb2474e90a1e3b319a4c8a1", description: "Input stream")]
            public DataInput<WorldToRootNode, Buffer<AnimatedData>> Input;
            [PortDefinition(guid: "4872a7c6af8f4a738ac076abbe472a1d", description: "Rig root entity")]
            public DataInput<WorldToRootNode, RigRootEntity> RootEntity;
            [PortDefinition(guid: "bbfabce8be084fc0a3761297679c1fae", description: "Matrix in world space")]
            public DataInput<WorldToRootNode, LocalToWorld> LocalToWorldToRemap;

            [PortDefinition(guid: "607dc615e8b548c3bd551ac5adeb30d3", description: "Matrix in root space")]
            public DataOutput<WorldToRootNode, float4x4> Output;
        }

        struct Data : INodeData, IMsgHandler<Rig>
        {
            public void HandleMessage(MessageContext ctx, in Rig rig)
            {
                ctx.UpdateKernelData(new KernelData
                {
                    RigDefinition = rig
                });
            }
        }

        struct KernelData : IKernelData
        {
            public BlobAssetReference<RigDefinition> RigDefinition;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                var stream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                stream.ValidateIsNotNull();

                var remapToRoot = context.Resolve(ports.RootEntity).RemapToRootMatrix;
                var rootBone = stream.GetLocalToParentMatrix(0);
                var worldToRoot = mathex.inverse(mathex.mul(remapToRoot, rootBone));
                var target = context.Resolve(ports.LocalToWorldToRemap).Value;
                context.Resolve(ref ports.Output) = math.mul((float4x4)worldToRoot, target);
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
