using Unity.Collections;
using Unity.Entities;

namespace Unity.Animation.StateMachine
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(StateMachineSystemGroup))]
    internal class GenerateRenderGraphSystem : SystemBase
    {
        EntityCommandBufferSystem   m_ECB;
        ProcessDefaultAnimationGraph     m_PreAnimationGraph;

        protected override void OnCreate()
        {
            m_ECB = World.GetExistingSystem<EndInitializationEntityCommandBufferSystem>();
            m_PreAnimationGraph = World.GetExistingSystem<ProcessDefaultAnimationGraph>();
            RequireSingletonForUpdate<GraphManager>();
        }

        protected override void OnUpdate()
        {
            var context = Core.StateMachinesContext.Create(this, m_ECB.CreateCommandBuffer().AsParallelWriter());
            Entities
                .WithName("UpdateRenderGraph")
                .WithoutBurst()
                .WithAll<StateMachineSystemState, GraphEntityState, StateMachineVersion>()
                .ForEach(
                    (Entity entity, DynamicBuffer<InputReference> inputs, in GraphEntityState state) =>
                    {
                        var inputArray = inputs.ToNativeArray(Allocator.Temp);
                        var stateMachineAspect = context.GetStateMachineAspect(entity);
                        var rig = EntityManager.GetComponentData<Rig>(state.m_ContextEntity);
                        Core.UpdateRenderGraph(stateMachineAspect, m_PreAnimationGraph, m_PreAnimationGraph.Set, rig, state.m_ContextEntityComponentNodeHandle, ref inputArray, EntityManager);
                        inputArray.Dispose();
                    })
                .Run();
        }
    }
}
