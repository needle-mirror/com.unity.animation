using Unity.DataFlowGraph;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Animation
{
    public static class ResolvedPortArrayExt
    {
        public static void CopyTo<TDefinition, TType>(
            this RenderContext.ResolvedInputPortArray<TDefinition, TType> src,
            NativeArray<TType> dst
        )
            where TType : struct
            where TDefinition : NodeDefinition
        {
            Core.ValidateBufferLengthsAreEqual(src.Length, dst.Length);
            for (var i = 0; i < src.Length; ++i)
                dst[i] = src[i];
        }
    }

    public static class NodeSetExt
    {
        public static void Connect<TType, TData>(this NodeSet set, NodeHandle<TType> from, DataOutput<TType, TData> port, NodeHandle<ComponentNode> to, NodeSet.ConnectionType type = NodeSet.ConnectionType.Normal)
            where TType : NodeDefinition
            where TData : struct, IComponentData
        {
            set.Connect(from, port, to, ComponentNode.Input<TData>(), type);
        }

        public static void Connect<TType, TData>(this NodeSet set, NodeHandle<ComponentNode> from, NodeHandle<TType> to, DataInput<TType, TData> port, NodeSet.ConnectionType type = NodeSet.ConnectionType.Normal)
            where TType : NodeDefinition
            where TData : struct, IComponentData
        {
            set.Connect(from, ComponentNode.Output<TData>(), to, port, type);
        }

        public static void Connect<TType, TBuffer>(this NodeSet set, NodeHandle<TType> from, DataOutput<TType, Buffer<TBuffer>> port, NodeHandle<ComponentNode> to, NodeSet.ConnectionType type = NodeSet.ConnectionType.Normal)
            where TType : NodeDefinition
            where TBuffer : struct, IBufferElementData
        {
            set.Connect(from, port, to, ComponentNode.Input<TBuffer>(), type);
        }

        public static void Connect<TType, TBuffer>(this NodeSet set, NodeHandle<ComponentNode> from, NodeHandle<TType> to, DataInput<TType, Buffer<TBuffer>> port, NodeSet.ConnectionType type = NodeSet.ConnectionType.Normal)
            where TType : NodeDefinition
            where TBuffer : struct, IBufferElementData
        {
            set.Connect(from, ComponentNode.Output<TBuffer>(), to, port, type);
        }
    }
}
