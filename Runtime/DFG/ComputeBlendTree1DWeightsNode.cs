using Unity.Burst;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(category:"Animation Core/Blend Trees", description:"Computes 1D BlendTree weights based on parameter input", isHidden:true)]
    public class ComputeBlendTree1DWeightsNode
        : NodeDefinition<ComputeBlendTree1DWeightsNode.Data, ComputeBlendTree1DWeightsNode.SimPorts, ComputeBlendTree1DWeightsNode.KernelData, ComputeBlendTree1DWeightsNode.KernelDefs, ComputeBlendTree1DWeightsNode.Kernel>
        , IMsgHandler<BlobAssetReference<BlendTree1D>>
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.ComputeBlendTree1DWeights");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(description:"BlendTree 1D properties")]
            public MessageInput<ComputeBlendTree1DWeightsNode, BlobAssetReference<BlendTree1D>> BlendTree;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(description:"Blend parameter value")]
            public DataInput<ComputeBlendTree1DWeightsNode, float> BlendParameter;
            [PortDefinition(displayName:"Motion Duration", description:"Duration of each motion used by this BlendTree")]
            public PortArray<DataInput<ComputeBlendTree1DWeightsNode, float>> MotionDurations;

            [PortDefinition(description:"Resulting motion weights, size of buffer is equal to the amount of motions")]
            public DataOutput<ComputeBlendTree1DWeightsNode, Buffer<float>> Weights;
            [PortDefinition(description:"Current motion duration")]
            public DataOutput<ComputeBlendTree1DWeightsNode, float> Duration;
        }
        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileMarker;
#endif
            public BlobAssetReference<BlendTree1D> BlendTree;
        }

        [BurstCompile/*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (!data.BlendTree.IsCreated)
                    throw new System.InvalidOperationException($"ComputeBlendTree1DWeightsNode: BlendTree is invalid.");

                var weights = context.Resolve(ref ports.Weights);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.Begin();
#endif

                Core.ComputeBlendTree1DWeights(data.BlendTree, context.Resolve(in ports.BlendParameter), ref weights);

                var motionsDuration = context.Resolve(in ports.MotionDurations);
                var duration = 0.0f;
                for(int i=0;i<motionsDuration.Length;i++)
                {
                    duration += weights[i] * motionsDuration[i] / data.BlendTree.Value.MotionSpeeds[i];
                }

                context.Resolve(ref ports.Duration) = duration;

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileMarker = k_ProfileMarker;
        }
#endif

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<BlendTree1D> blendTree)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            kData.BlendTree = blendTree;
            if(blendTree.IsCreated)
            {
                Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Weights, Buffer<float>.SizeRequest(blendTree.Value.Motions.Length));
                Set.SetPortArraySize(ctx.Handle, (InputPortID)KernelPorts.MotionDurations, (ushort)blendTree.Value.Motions.Length);
            }
            else
            {
                Set.SetBufferSize(ctx.Handle, (OutputPortID)KernelPorts.Weights, Buffer<float>.SizeRequest(0));
                Set.SetPortArraySize(ctx.Handle, (InputPortID)KernelPorts.MotionDurations, 0);
            }
        }
    }
}
