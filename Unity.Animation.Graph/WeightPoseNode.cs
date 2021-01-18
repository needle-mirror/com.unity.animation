using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using Unity.Burst;

namespace Unity.Animation
{
    [NodeDefinition(guid: "6daa5f5c6f7e4944ae29a32f6e60219d", version: 1, category: "Animation Core/Utils", description: "Applies a set of weights to an animation stream")]
    public class WeightPoseNode
        : SimulationKernelNodeDefinition<WeightPoseNode.SimPorts, WeightPoseNode.KernelDefs>
        , IRigContextHandler<WeightPoseNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "b1ad5e24921d46c6a5c42f3ac54a4bfc", isHidden: true)]
            public MessageInput<WeightPoseNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "9605259413914ff19f0e2c97ff3fa5be", description: "Input stream")]
            public DataInput<WeightPoseNode, Buffer<AnimatedData>>  Input;
            [PortDefinition(guid: "7501e6c443454813b4f3ae6645f70413", description: "Weights to apply on stream")]
            public DataInput<WeightPoseNode, Buffer<WeightData>> WeightMasks;

            [PortDefinition(guid: "c204fb5eb8ad4d5ca785b29d75fc08d5", description: "Resulting weighted stream")]
            public DataOutput<WeightPoseNode, Buffer<AnimatedData>> Output;
        }

        struct Data : INodeData, IMsgHandler<Rig>
        {
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
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                if (data.RigDefinition == BlobAssetReference<RigDefinition>.Null)
                    return;

                var inputStream = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input));
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                Core.WeightPose(ref outputStream, ref inputStream, context.Resolve(ports.WeightMasks));
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) =>
            (InputPortID)SimulationPorts.Rig;
    }
}
