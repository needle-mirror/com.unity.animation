using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "aac0d8d10f594218aa77961604da05e2", version: 1, category: "Animation Core/Blend Trees", description: "Computes 1D BlendTree weights based on parameter input", isHidden: true)]
    public class ComputeBlendTree1DWeightsNode
        : SimulationKernelNodeDefinition<ComputeBlendTree1DWeightsNode.SimPorts, ComputeBlendTree1DWeightsNode.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "6195a5fe9d5642469dc78d2efb5fbde6", description: "BlendTree 1D properties")]
            public MessageInput<ComputeBlendTree1DWeightsNode, BlobAssetReference<BlendTree1D>> BlendTree;
        }

        struct Data : INodeData, IMsgHandler<BlobAssetReference<BlendTree1D>>
        {
            public void HandleMessage(MessageContext ctx, in BlobAssetReference<BlendTree1D> blendTree)
            {
                ctx.UpdateKernelData(new KernelData
                {
                    BlendTree = blendTree
                });

                if (blendTree.IsCreated)
                {
                    ctx.Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Weights, Buffer<float>.SizeRequest(blendTree.Value.Motions.Length));
                    ctx.Set.SetPortArraySize(ctx.Handle, (InputPortID)KernelPorts.MotionDurations, (ushort)blendTree.Value.Motions.Length);
                }
                else
                {
                    ctx.Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Weights, Buffer<float>.SizeRequest(0));
                    ctx.Set.SetPortArraySize(ctx.Handle, (InputPortID)KernelPorts.MotionDurations, 0);
                }
            }
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "9eafced4b7074559aa0af1ee256120e3", description: "Blend parameter value")]
            public DataInput<ComputeBlendTree1DWeightsNode, float> BlendParameter;
            [PortDefinition(guid: "2dcc708d7e114a21a16b3b439a095f87", displayName: "Motion Duration", description: "Duration of each motion used by this BlendTree")]
            public PortArray<DataInput<ComputeBlendTree1DWeightsNode, float>> MotionDurations;

            [PortDefinition(guid: "e6cab610f6ec45189aa698b773e673ba", description: "Resulting motion weights, size of buffer is equal to the amount of motions")]
            public DataOutput<ComputeBlendTree1DWeightsNode, Buffer<float>> Weights;
            [PortDefinition(guid: "31b3a792162f47a2ba5e670c2ebf6399", description: "Current motion duration")]
            public DataOutput<ComputeBlendTree1DWeightsNode, float> Duration;
        }
        struct KernelData : IKernelData
        {
            public BlobAssetReference<BlendTree1D> BlendTree;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                Core.ValidateIsCreated(data.BlendTree);

                var weights = context.Resolve(ref ports.Weights);

                Core.ComputeBlendTree1DWeights(data.BlendTree, context.Resolve(in ports.BlendParameter), ref weights);

                var motionsDuration = context.Resolve(in ports.MotionDurations);
                var duration = 0.0f;
                for (int i = 0; i < motionsDuration.Length; i++)
                {
                    duration += weights[i] * motionsDuration[i] / data.BlendTree.Value.MotionSpeeds[i];
                }

                context.Resolve(ref ports.Duration) = duration;
            }
        }
    }
}
