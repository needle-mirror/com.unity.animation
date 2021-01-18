using Unity.Burst;
using Unity.DataFlowGraph;
using Unity.DataFlowGraph.Attributes;

namespace Unity.Animation
{
    [NodeDefinition(guid: "be1a7512286b41479d93a3e4e1db9534", version: 1, category: "Animation Core/Utils", description: "Gets a value given an index from a buffer")]
    public class GetBufferElementValueNode
        : KernelNodeDefinition<GetBufferElementValueNode.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            [PortDefinition(guid: "bfb9c7c192af4e0b9e01a20ae9de6713", description: "Input buffer")]
            public DataInput<GetBufferElementValueNode, Buffer<float>> Input;
            [PortDefinition(guid: "f58169dc597a4d788909b525a723496e", description: "Index in buffer")]
            public DataInput<GetBufferElementValueNode, int> Index;

            [PortDefinition(guid: "28cb77b3c65c49fabe33810d9665efa5", description: "Value")]
            public DataOutput<GetBufferElementValueNode, float> Output;
        }

        struct KernelData : IKernelData
        {
        }

        [BurstCompile /*(FloatMode = FloatMode.Fast)*/]
        struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                var buffer = context.Resolve(in ports.Input);
                var index = context.Resolve(in ports.Index);
                Core.ValidateBufferIndexBounds(index, buffer.Length);

                context.Resolve(ref ports.Output) = buffer[index];
            }
        }
    }
}
