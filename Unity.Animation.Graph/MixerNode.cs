using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [NodeDefinition(guid: "fd4af8a57de143148add1ffef327bd73", version: 1, category: "Animation Core/Mixers", description: "Blends two animation streams given an input weight value")]
    public class MixerNode
        : NodeDefinition<MixerNode.Data, MixerNode.SimPorts, MixerNode.KernelData, MixerNode.KernelDefs, MixerNode.Kernel>
        , IRigContextHandler
    {
#pragma warning restore 0618
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "f77388cf9f22485d924e530a5906cad1", isHidden: true)]
            public MessageInput<MixerNode, Rig> Rig;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "929d52c8aa744becbaa7cd9cb041c4b9", description: "Input stream 0")]
            public DataInput<MixerNode, Buffer<AnimatedData>> Input0;
            [PortDefinition(guid: "ca260edfcfe04ddd8252601b85c4c314", description: "Input stream 1")]
            public DataInput<MixerNode, Buffer<AnimatedData>> Input1;
            [PortDefinition(guid: "8be16b6cc26742f290ddfb2fc51abbc7", description: "Blend weight")]
            public DataInput<MixerNode, float> Weight;

            [PortDefinition(guid: "f9393266c7ed4984afb26bc952b294f6", description: "Resulting stream")]
            public DataOutput<MixerNode, Buffer<AnimatedData>> Output;
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
                var outputStream = AnimationStream.Create(data.RigDefinition, context.Resolve(ref ports.Output));
                outputStream.ValidateIsNotNull();

                var inputStream1 = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input0));
                var inputStream2 = AnimationStream.CreateReadOnly(data.RigDefinition, context.Resolve(ports.Input1));

                var weight = context.Resolve(in ports.Weight);

                outputStream.ClearMasks();

                if (inputStream1.IsNull && inputStream2.IsNull)
                    outputStream.ResetToDefaultValues();
                else if (inputStream1.IsNull && !inputStream2.IsNull)
                {
                    outputStream.ResetToDefaultValues();
                    Core.Blend(ref outputStream, ref outputStream, ref inputStream2, weight);
                }
                else if (!inputStream1.IsNull && inputStream2.IsNull)
                {
                    outputStream.ResetToDefaultValues();
                    Core.Blend(ref outputStream, ref inputStream1, ref outputStream, weight);
                }
                else
                    Core.Blend(ref outputStream, ref inputStream1, ref inputStream2, weight);
            }
        }

        public void HandleMessage(in MessageContext ctx, in Rig rig)
        {
            GetKernelData(ctx.Handle).RigDefinition = rig.Value;
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
