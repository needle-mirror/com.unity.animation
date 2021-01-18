using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "d10f1f8e08af4dc094ec9a0503838b1d", version: 1, category: "Animation Core/Utils", description: "Adds two animation streams")]
    public class AddPoseNode
        : SimulationKernelNodeDefinition<AddPoseNode.SimPorts, AddPoseNode.KernelDefs>
        , IRigContextHandler<AddPoseNode.Data>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "a46b4937ad0241eb9039b0af7b22962a", isHidden: true)]
            public MessageInput<AddPoseNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "16d7457b41fd4206931ba315ee5b83e0", description: "Input stream A")]
            public DataInput<AddPoseNode, Buffer<AnimatedData>> InputA;
            [PortDefinition(guid: "0fe3b6f671c84554902ce26908cc7b8c", description: "Input stream B")]
            public DataInput<AddPoseNode, Buffer<AnimatedData>> InputB;

            [PortDefinition(guid: "44cd25b8e7f8469d9cc2f44615c2db2f", description: "Resulting stream")]
            public DataOutput<AddPoseNode, Buffer<AnimatedData>> Output;
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

                // Fill the destination stream with default values.
                var inputStreamA = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.InputA));
                var inputStreamB = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.InputB));
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));

                Core.AddPose(ref outputStream, ref inputStreamA, ref inputStreamB);
            }
        }

        InputPortID ITaskPort<IRigContextHandler>.GetPort(NodeHandle handle) => (InputPortID)SimulationPorts.Rig;
    }
}
