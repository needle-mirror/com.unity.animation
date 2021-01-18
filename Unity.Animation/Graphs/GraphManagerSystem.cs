using Unity.Entities;

namespace Unity.Animation
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal class GraphManagerSystem : SystemBase
    {
        protected override void OnCreate()
        {
            if (!HasSingleton<GraphManager>())
            {
                EntityManager.CreateEntity(typeof(GraphManager));

                var graphManager = new GraphManager();
                graphManager.Initialize();
                SetSingleton(graphManager);
            }
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnDestroy()
        {
            if (HasSingleton<GraphManager>())
            {
                var graphManager = GetSingleton<GraphManager>();
                graphManager.Dispose();
                SetSingleton(graphManager);
            }
        }
    }
}
