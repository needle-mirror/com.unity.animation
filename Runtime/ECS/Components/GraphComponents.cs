using Unity.Entities;
using Unity.DataFlowGraph;

namespace Unity.Animation
{
    public interface IGraphInput : INodeMemoryInputTag, IComponentData
    {
        NodeHandle Node { get; set; }
        InputPortID Port { get; set; }
    }

    public interface IGraphOutput : ISystemStateComponentData
    {
        GraphValue<Buffer<float>> Buffer { get; set; }
    }

    // TODO : This should eventually be defined outside of the
    // Dots.Animation package as it will be custom to user defined
    // animation pipelines
    public struct GraphOutput : IGraphOutput
    {
        public GraphValue<Buffer<float>> Buffer { get; set; }
    }
}
