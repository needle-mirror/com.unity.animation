using Unity.DataFlowGraph;

namespace Unity.Animation
{
    public interface IRigContextHandler : ITaskPort<IRigContextHandler>
    {}

    public interface IRigContextHandler<T> : IRigContextHandler where T : INodeData, IMsgHandler<Rig>
    {}
}
