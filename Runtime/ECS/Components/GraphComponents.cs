using Unity.Entities;
using Unity.DataFlowGraph;

using System;

namespace Unity.Animation
{
    public interface IGraphOutput : ISystemStateComponentData
    {
        GraphValue<Buffer<AnimatedData>> Buffer { get; set; }
    }

    [Obsolete("GraphOutput is obsolete, use DataFlowGraph Component nodes instead (RemovedAfter 2020-02-18)", false)]
    public struct GraphOutput : IGraphOutput
    {
        public GraphValue<Buffer<AnimatedData>> Buffer { get; set; }
    }
}
