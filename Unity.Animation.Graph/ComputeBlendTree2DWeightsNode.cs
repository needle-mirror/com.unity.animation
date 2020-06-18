using Unity.Burst;
using Unity.Mathematics;
using Unity.Entities;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(guid: "79412b5295e542daad10167c1b418522", version: 1, category: "Animation Core/Blend Trees", description: "Computes 2D BlendTree weights based on parameter input", isHidden: true)]
    public class ComputeBlendTree2DWeightsNode
        : NodeDefinition<ComputeBlendTree2DWeightsNode.Data, ComputeBlendTree2DWeightsNode.SimPorts, ComputeBlendTree2DWeightsNode.KernelData, ComputeBlendTree2DWeightsNode.KernelDefs, ComputeBlendTree2DWeightsNode.Kernel>
        , IMsgHandler<BlobAssetReference<BlendTree2DSimpleDirectional>>
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileMarker = new ProfilerMarker("Animation.ComputeBlendTree2DWeights");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
            [PortDefinition(guid: "de45bb03e06142c1bd27361e890369fd", description: "BlendTree 2D properties")]
            public MessageInput<ComputeBlendTree2DWeightsNode, BlobAssetReference<BlendTree2DSimpleDirectional>> BlendTree;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "5db3565dc5a148f0acf662ae66c0a1d9", description: "Blend parameter X value")]
            public DataInput<ComputeBlendTree2DWeightsNode, float> BlendParameterX;
            [PortDefinition(guid: "87fd5d8fe6ba44b9ae86d5d7bf680ca0", description: "Blend parameter Y value")]
            public DataInput<ComputeBlendTree2DWeightsNode, float> BlendParameterY;
            [PortDefinition(guid: "e2ce9ea098cc422b9daa5fddd9e03c71", displayName: "Motion Duration", description: "Duration of each motion used by this BlendTree")]
            public PortArray<DataInput<ComputeBlendTree2DWeightsNode, float>> MotionDurations;

            [PortDefinition(guid: "0c799b1077314f9b819c960b8c2039cb", description: "Resulting motion weights, size of buffer is equal to the amount of motions")]
            public DataOutput<ComputeBlendTree2DWeightsNode, Buffer<float>> Weights;
            [PortDefinition(guid: "263544bcaef14db5982111b2ca206e2a", description: "Current motion duration")]
            public DataOutput<ComputeBlendTree2DWeightsNode, float> Duration;
        }
        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileMarker;
#endif
            public BlobAssetReference<BlendTree2DSimpleDirectional> BlendTree;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                if (!data.BlendTree.IsCreated)
                    throw new System.InvalidOperationException("ComputeBlendTree2DWeightsNode: BlendTree is invalid.");

                var weights = context.Resolve(ref ports.Weights);

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileMarker.Begin();
#endif

                var blendParameter = new float2(context.Resolve(in ports.BlendParameterX), context.Resolve(in ports.BlendParameterY));

                Core.ComputeBlendTree2DSimpleDirectionalWeights(data.BlendTree, blendParameter, ref weights);

                var motionsDuration = context.Resolve(in ports.MotionDurations);
                var duration = 0.0f;
                for (int i = 0; i < motionsDuration.Length; i++)
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

        public void HandleMessage(in MessageContext ctx, in BlobAssetReference<BlendTree2DSimpleDirectional> blendTree)
        {
            ref var kData = ref GetKernelData(ctx.Handle);

            kData.BlendTree = blendTree;
            if (blendTree.IsCreated)
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
