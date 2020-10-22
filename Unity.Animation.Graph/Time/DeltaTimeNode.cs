using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;
using UnityEngine;

namespace Unity.Animation
{
    [System.Obsolete("DeltaTimeNode has been deprecated. Set the time data of the nodes with SetData, or use a KernelPassThroughNodeFloat. (RemovedAfter 2020-12-04)", false)]
    [NodeDefinition(guid: "9f614062d66a439f9a7070de6b880b93", version: 1, category: "Animation Core/Time", description: "Computes delta time")]
    public class DeltaTimeNode
        : SimulationKernelNodeDefinition<DeltaTimeNode.SimPorts, DeltaTimeNode.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition {}

        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "15abad4f4f924fd9a18ee7ea014b03b7", description: "Delta time")]
            public DataOutput<DeltaTimeNode, float> DeltaTime;
        }

        struct Data : INodeData, IInit, IUpdate
        {
            public void Init(InitContext ctx)
            {
                ctx.RegisterForUpdate();
            }

            public void Update(in UpdateContext ctx)
            {
                ctx.UpdateKernelData(new KernelData
                {
                    DeltaTime = Time.deltaTime
                });
            }
        }

        struct KernelData : IKernelData
        {
            public float DeltaTime;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, KernelData data, ref KernelDefs ports) =>
                context.Resolve(ref ports.DeltaTime) = data.DeltaTime;
        }
    }
}
