using System;
using System.Diagnostics;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation.StateMachine
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    //[UpdateAfter(typeof(UpdateWorldTimeSystem))] // This is part of Entities.Hybrid, is this valid ?
    public class StateMachineSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(StateMachineSystemGroup))]
    internal class StateMachineSystem : SystemBase
    {
        EntityCommandBufferSystem m_ECB;

        EntityQuery m_BlackboardValueCopyQuery;

        protected override void OnCreate()
        {
            m_ECB = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            m_BlackboardValueCopyQuery = EntityManager.CreateEntityQuery(new ComponentType[] {ComponentType.ReadOnly<InputReference>(), ComponentType.ReadWrite<CharacterGameplayPropertiesCopy>()});
            RequireSingletonForUpdate<GraphManager>();
        }

        protected override void OnUpdate()
        {
            CopyBlackboardValues();

            var context = Core.StateMachinesContext.Create(this, m_ECB.CreateCommandBuffer().AsParallelWriter());

            this.Dependency = Entities
                .WithName("Initialize")
                .WithNone<StateMachineSystemState>()
                .WithAll<StateMachineVersion>()
                .ForEach(
                    (Entity entity, int entityInQueryIndex) =>
                    {
                        var stateMachineAspect = context.GetStateMachineAspect(entity);

                        stateMachineAspect.ECB.AddComponent<StateMachineSystemState>(entityInQueryIndex, entity);

                        var transitionDefinition = new TransitionDefinition();
                        ref var stateMachineInstance = ref stateMachineAspect.StateMachines.ElementAt(0);
                        Core.StateMachineInit(stateMachineAspect, ref stateMachineInstance, ref transitionDefinition);
                    })
                .ScheduleParallel(this.Dependency);
            m_ECB.AddJobHandleForProducer(this.Dependency);

            var deltaTime = World.Time.DeltaTime;
            this.Dependency = Entities
                .WithName("UpdateTimeControl")
                .WithAll<StateMachineSystemState>()
                .ForEach(
                    (Entity entity, ref TimeControl timeControl) =>
                    {
                        timeControl.DeltaRatio = deltaTime;
                        timeControl.Timescale = 1.0f;
                    })
                .ScheduleParallel(this.Dependency);

            context = Core.StateMachinesContext.Create(this, m_ECB.CreateCommandBuffer().AsParallelWriter());


            this.Dependency = Entities
                .WithName("PreUpdate")
                .WithAll<StateMachineSystemState, StateMachineVersion>()
                .ForEach(
                    (Entity entity) =>
                    {
                        var stateMachineAspect = context.GetStateMachineAspect(entity);
                        Core.PreUpdate(stateMachineAspect);
                    })
                .ScheduleParallel(this.Dependency);
            m_ECB.AddJobHandleForProducer(this.Dependency);

            context = Core.StateMachinesContext.Create(this, m_ECB.CreateCommandBuffer().AsParallelWriter());

            // Create new command buffer
            var writerECB = m_ECB.CreateCommandBuffer().AsParallelWriter();

            this.Dependency = Entities
                .WithName("Destroy")
                .WithAll<StateMachineSystemState>()
                .WithNone<StateMachineVersion>()
                .ForEach(
                    (Entity entity, int entityInQueryIndex) =>
                    {
                        // This system is responsible for removing any ISystemStateComponentData instances it adds
                        // Otherwise, the entity is never truly destroyed.
                        writerECB.RemoveComponent<StateMachineSystemState>(entityInQueryIndex, entity);
                    })
                .ScheduleParallel(this.Dependency);
            m_ECB.AddJobHandleForProducer(this.Dependency);
        }

        void CopyBlackboardValues()
        {
            var copyJob = new CopyBlackboardValuesJob
            {
                InputReferenceType = GetBufferTypeHandle<InputReference>(true),
                CharacterBlackboardValueCopyType = GetComponentTypeHandle<CharacterGameplayPropertiesCopy>(),
                EntityManager = EntityManager
            };

            copyJob.Run(m_BlackboardValueCopyQuery);
        }

        // WARNING: This job can only used with Run() on main thread since it holds an EntityManager
        [BurstCompile]
        struct CopyBlackboardValuesJob : IJobEntityBatch
        {
            [ReadOnly] public BufferTypeHandle<InputReference> InputReferenceType;

            public ComponentTypeHandle<CharacterGameplayPropertiesCopy> CharacterBlackboardValueCopyType;
            public EntityManager EntityManager;

            unsafe public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var inputReferenceAccessor = batchInChunk.GetBufferAccessor(InputReferenceType);
                var writeBuffers = batchInChunk.GetNativeArray(CharacterBlackboardValueCopyType);

                for (int i = 0; i < batchInChunk.Count; ++i)
                {
                    var inputReferences = inputReferenceAccessor[i].AsNativeArray();
                    var writeBuffer = writeBuffers[i];
                    for (int j = 0; j < inputReferences.Length; ++j)
                    {
                        var inputRef = inputReferences[j];
                        var handle = EntityManager.GetDynamicComponentTypeHandle(ComponentType.ReadWrite(inputRef.TypeIndex));
                        var byteArray = batchInChunk.GetDynamicComponentDataArrayReinterpret<byte>(handle, inputRef.Size);

                        ValidateBlackboardValueSize(batchInChunk.Count, inputRef.Size, byteArray.Length);
                        UnsafeUtility.MemCpy(
                            writeBuffer.GameplayProperties[inputRef.TypeIndex].Ptr,
                            (byte*)byteArray.GetUnsafePtr() + i * inputRef.Size,
                            inputRef.Size
                        );
                    }
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void ValidateBlackboardValueSize(int entityCount, int referenceSize, int dataLengthInBatch)
            {
                if ((entityCount * referenceSize) != dataLengthInBatch)
                    throw new InvalidOperationException("Blackboard value and input reference sizes do not match.");
            }
        }
    }
}
