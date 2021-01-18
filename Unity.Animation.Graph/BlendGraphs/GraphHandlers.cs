using Unity.Collections;
using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation
{
    public interface IRootGraphSimulationData : IGraphSlot,
        IMsgHandler<NodeHandle<ComponentNode>>,
        IMsgHandler<BlobAssetReference<Graph>>,
        IMsgHandler<BlobAssetReference<GraphInstanceParameters>>,
        IMsgHandler<EntityManager>
    {
    }

    public interface IInputReferenceHandler : ITaskPort<IInputReferenceHandler>
    {
    }

    public interface IInputReferenceHandler<T> : IInputReferenceHandler
        where T : INodeData, IMsgHandler<NativeArray<InputReference>>
    {
    }

    public struct EntityContext
    {
        public Entity e;
        public EntityManager Manager;
    }

    public interface IEntityManagerHandler<T> : IEntityManagerHandler
        where T : INodeData, IMsgHandler<EntityManager>
    {
    }

    public interface IEntityManagerHandler : ITaskPort<IEntityManagerHandler>
    {
    }

    public struct TimeControl
    {
        public float DeltaRatio;
        public float Timescale;
        public float AbsoluteTime;
        public int SyncTagStart;
        public int SyncTagEnd;
        public float SyncRatio;
    }
    public interface ITimeControlHandler : ITaskPort<ITimeControlHandler>
    {
    }

    public interface ITimeControlHandler<T> : ITimeControlHandler
        where T : INodeData, IMsgHandler<TimeControl>
    {
    }

    public interface IGraphHandler : ITaskPort<IGraphHandler>
    {
    }

    public interface IGraphHandler<T> : IGraphHandler
        where T : INodeData, IMsgHandler<BlobAssetReference<Graph>>
    {
    }

    public interface IGraphInstanceHandler : ITaskPort<IGraphInstanceHandler>
    {
    }

    public interface IGraphInstanceHandler<T> : IGraphInstanceHandler
        where T : INodeData, IMsgHandler<BlobAssetReference<GraphInstanceParameters>>
    {
    }

    public interface IComponentNodeHandler : ITaskPort<IComponentNodeHandler>
    {
    }

    public interface IComponentNodeHandler<T> : IComponentNodeHandler
        where T : INodeData, IMsgHandler<NodeHandle<ComponentNode>>
    {
    }
}
