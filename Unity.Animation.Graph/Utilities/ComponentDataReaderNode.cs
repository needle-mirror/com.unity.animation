using System;
using System.Reflection;
using Unity.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation
{
    internal class ComponentDataFieldReaderNode<TData, TField> :
        SimulationNodeDefinition<ComponentDataFieldReaderNode<TData, TField>.Ports>
        where TData : struct, IComponentData
        where TField : unmanaged
    {
        [Managed]
        public struct Data : IInit, INodeData, IUpdate, IMsgHandler<EntityContext>, IMsgHandler<int>
        {
            public TField CachedValue;
            public bool FirstFrame;
            public int TypeIndex;
            public int FieldOffset;
            public EntityContext Context;

            public void Init(InitContext ctx)
            {
                ctx.RegisterForUpdate();
                TypeIndex = TypeManager.GetTypeIndex<TData>();
                CachedValue = default;
                FieldOffset = -1;
                FirstFrame = true;
            }

            public void HandleMessage(MessageContext ctx, in int msg)
            {
                if (ctx.Port == SimulationPorts.FieldOffset)
                    FieldOffset = msg;
            }

            public void HandleMessage(MessageContext ctx, in EntityContext msg)
            {
                Context = msg;
            }

            unsafe delegate void* GetComponentDataRawRwDelegate(Entity e, int componentTypeIndex);
            GetComponentDataRawRwDelegate m_GetComponentDataRawRwMi;
            unsafe void* GetComponentDataRawRW(Entity entity, int typeIndex, EntityManager entityManager)
            {
                if (m_GetComponentDataRawRwMi == null)
                {
                    var mi = typeof(EntityManager).GetMethod("GetComponentDataRawRW", BindingFlags.NonPublic | BindingFlags.Instance);
                    m_GetComponentDataRawRwMi = (GetComponentDataRawRwDelegate)Delegate.CreateDelegate(typeof(GetComponentDataRawRwDelegate), entityManager, mi);
                }

                Assert.IsNotNull(m_GetComponentDataRawRwMi);
                return m_GetComponentDataRawRwMi(entity, typeIndex);
            }

            unsafe void* GetComponentData(Entity e, int typeIndex, int offset, EntityManager entityManager)
            {
                var componentPtr = GetComponentDataRawRW(e, typeIndex, entityManager);
                return (byte*)componentPtr + offset;
            }

            unsafe public void Update(UpdateContext ctx)
            {
                var entity = Context.e;
                if (entity == Entity.Null)
                    return;
                var dstManager = Context.Manager;
                if (dstManager.HasComponent<TData>(entity))
                {
                    UnsafeUtility.CopyPtrToStructure<TField>(
                        (byte*)GetComponentData(entity, TypeIndex, FieldOffset, dstManager),
                        out var fieldValue);

                    if (!CachedValue.Equals(fieldValue) || FirstFrame)
                    {
                        ctx.EmitMessage(SimulationPorts.Value, fieldValue);
                        CachedValue = fieldValue;
                        FirstFrame = false;
                    }
                }
            }
        }

        public struct Ports : ISimulationPortDefinition
        {
            public MessageInput<ComponentDataFieldReaderNode<TData, TField>, EntityContext> EntityContext;
            public MessageInput<ComponentDataFieldReaderNode<TData, TField>, int> FieldOffset;
            public MessageOutput<ComponentDataFieldReaderNode<TData, TField>, TField> Value;
        }

        public InputPortID GetPort(NodeHandle handle)
        {
            return (InputPortID)SimulationPorts.EntityContext;
        }
    }
}
