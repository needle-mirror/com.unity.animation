using Unity.DataFlowGraph;

namespace Unity.Animation
{
    // IMotion interface define :
    //  1. Duration message port
    //  2. AnimationStream Output port
    public interface IMotion
    {
        float GetDuration(NodeHandle handle);
        OutputPortID AnimationStreamOutputPort { get; }
    }

    // INormalizedTimeMotion interface define :
    //  1. Duration message port
    //  2. AnimationStream Output port
    //  3. NormalizedTime Input port
    public interface INormalizedTimeMotion : IMotion
    {
        InputPortID NormalizedTimeInputPort { get; }
    }

    // IBlendTree interface define :
    //  1. Duration message port
    //  2. AnimationStream Output port
    //  3. NormalizedTime Input port
    //  4. RigDefinition message port
    //  5. Parameter message port
    //  6. RigDefinitionOut message port
    //  7. ParameterOut message port
    public interface IBlendTree : INormalizedTimeMotion
    {
        InputPortID RigDefinitionInputPort { get; }
        InputPortID ParameterInputPort { get; }

        OutputPortID RigDefinitionOutputPort { get; }
        OutputPortID ParameterOutputPort { get; }
    }
}
