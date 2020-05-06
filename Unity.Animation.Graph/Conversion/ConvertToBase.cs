using Unity.DataFlowGraph;

namespace Unity.Animation
{
    public abstract class ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>
        : NodeDefinition<ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>.Data, ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>.SimPorts, ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>.KernelData, ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>.KernelDefs, TKernel>
        where TFinalNodeDefinition : NodeDefinition
        where TInput : struct
        where TOutput : struct
        where TKernel : struct, IGraphKernel<ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>.KernelData, ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>.KernelDefs>
    {
        public struct SimPorts : ISimulationPortDefinition {}

        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<TFinalNodeDefinition, TInput> Input;
            public DataOutput<TFinalNodeDefinition, TOutput> Output;
        }

        public struct Data : INodeData {}

        public struct KernelData : IKernelData
        {
            public DataInput<TFinalNodeDefinition, TInput>   Input;
            public DataOutput<TFinalNodeDefinition, TOutput> Output;
        }
    }
}
