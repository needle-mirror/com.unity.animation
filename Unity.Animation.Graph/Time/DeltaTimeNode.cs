using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using UnityEngine;

#if !UNITY_DISABLE_ANIMATION_PROFILING
using Unity.Profiling;
#endif

namespace Unity.Animation
{
    [NodeDefinition(guid: "9f614062d66a439f9a7070de6b880b93", version: 1, category: "Animation Core/Time", description: "Computes delta time")]
    public class DeltaTimeNode
        : NodeDefinition<DeltaTimeNode.Data, DeltaTimeNode.SimPorts, DeltaTimeNode.KernelData, DeltaTimeNode.KernelDefs, DeltaTimeNode.Kernel>
    {
#if !UNITY_DISABLE_ANIMATION_PROFILING
        static readonly ProfilerMarker k_ProfileDeltaTime = new ProfilerMarker("Animation.DeltaTime");
#endif

        public struct SimPorts : ISimulationPortDefinition
        {
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "15abad4f4f924fd9a18ee7ea014b03b7", description: "Delta time")]
            public DataOutput<DeltaTimeNode, float> DeltaTime;
        }

        public struct Data : INodeData
        {
        }

        public struct KernelData : IKernelData
        {
#if !UNITY_DISABLE_ANIMATION_PROFILING
            public ProfilerMarker ProfileDeltaTime;
#endif
            public float DeltaTime;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileDeltaTime.Begin();
#endif

                context.Resolve(ref ports.DeltaTime) = data.DeltaTime;

#if !UNITY_DISABLE_ANIMATION_PROFILING
                data.ProfileDeltaTime.End();
#endif
            }
        }

#if !UNITY_DISABLE_ANIMATION_PROFILING
        protected override void Init(InitContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.ProfileDeltaTime = k_ProfileDeltaTime;
        }

#endif

        protected override void OnUpdate(in UpdateContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.DeltaTime = Time.deltaTime;
        }
    }
}
