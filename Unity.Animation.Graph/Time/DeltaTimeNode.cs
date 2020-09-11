using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using UnityEngine;

namespace Unity.Animation
{
#pragma warning disable 0618 // TODO : Convert to new DFG API then remove this directive
    [System.Obsolete("DeltaTimeNode has been deprecated. Set the time data of the nodes with SetData, or use a KernelPassThroughNodeFloat. (RemovedAfter 2020-12-04)", false)]
    [NodeDefinition(guid: "9f614062d66a439f9a7070de6b880b93", version: 1, category: "Animation Core/Time", description: "Computes delta time")]
    public class DeltaTimeNode
        : NodeDefinition<DeltaTimeNode.Data, DeltaTimeNode.SimPorts, DeltaTimeNode.KernelData, DeltaTimeNode.KernelDefs, DeltaTimeNode.Kernel>
    {
#pragma warning restore 0618

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
            public float DeltaTime;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.DeltaTime) = data.DeltaTime;
            }
        }

        protected override void OnUpdate(in UpdateContext ctx)
        {
            ref var kData = ref GetKernelData(ctx.Handle);
            kData.DeltaTime = Time.deltaTime;
        }
    }
}
