using Unity.DataFlowGraph;

namespace Unity.Animation
{
    public abstract class ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>
        : KernelNodeDefinition<ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>.KernelDefs>
        where TFinalNodeDefinition : NodeDefinition
        where TInput : struct
        where TOutput : struct
        where TKernel : struct, IGraphKernel<ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>.KernelData, ConvertToBase<TFinalNodeDefinition, TInput, TOutput, TKernel>.KernelDefs>
    {
        public struct KernelDefs : IKernelPortDefinition
        {
            public DataInput<TFinalNodeDefinition, TInput> Input;
            public DataOutput<TFinalNodeDefinition, TOutput> Output;
        }

        public struct KernelData : IKernelData {}
    }
}
