using Unity.Burst;
using Unity.DataFlowGraph;

namespace Unity.Animation
{
    internal class StoreFloatValueNode : SimulationKernelNodeDefinition<StoreFloatValueNode.SimPorts, StoreFloatValueNode.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition
        {
            public MessageInput<StoreFloatValueNode, float> Value;
        }

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataOutput<StoreFloatValueNode, float> Output;
        }

        private struct Data : INodeData, IMsgHandler<float>
        {
            public void HandleMessage(MessageContext ctx, in float value)
            {
                ctx.UpdateKernelData(new KernelData(){StoredValue = value});
            }
        }

        public struct KernelData : IKernelData
        {
            // Assets.
            public float StoredValue;
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = data.StoredValue;
            }
        }
    }
}
