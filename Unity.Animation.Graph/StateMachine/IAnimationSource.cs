using Unity.Entities;
using Unity.Animation;
using Unity.DataFlowGraph;

namespace Unity.Animation.StateMachine
{
    internal interface IAnimationSource
    {
        GraphHandle                      GraphHandle    { get; set; }

        Unity.DataFlowGraph.NodeHandle   OutputNode     { get; set; }
        OutputPortID                     OutputPortID   { get; set; }
    }
}
