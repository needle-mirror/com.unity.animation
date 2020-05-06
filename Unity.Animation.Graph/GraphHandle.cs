namespace Unity.Animation
{
    /// <summary>
    /// A GraphHandle enables you to reason about a group of nodes in an AnimationSystemBase NodeSet.
    /// The AnimationSystemBase manages the lifecycle of these nodes created using AnimationSystemBase.CreateManagedNode.
    /// You can create a valid GraphHandle for a specific animation system using AnimationSystemBase.CreateManagedGraph() and can
    /// explicitly release a GraphHandle (and nodes associated with it) using
    /// AnimationSystemBase.Dispose(GraphHandle).
    /// </summary>
    public struct GraphHandle : System.IEquatable<GraphHandle>
    {
        // Intentionally not using SystemID for equality checks to minimize amount of process in NativeMultiHashMap.
        // The SystemID is only used to validate if the GraphHandle can be employed with a specific AnimationSystemBase.
        internal readonly ushort SystemID;
        internal readonly ushort GraphID;

        internal GraphHandle(ushort systemId, ushort id)
        {
            SystemID = systemId;
            GraphID = id;
        }

        public bool Equals(GraphHandle other) =>
            this == other;

        public static bool operator==(GraphHandle lhs, GraphHandle rhs) =>
            lhs.GraphID == rhs.GraphID;

        public static bool operator!=(GraphHandle lhs, GraphHandle rhs) =>
            lhs.GraphID != rhs.GraphID;

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return obj is GraphHandle handle && Equals(handle);
        }

        public override int GetHashCode() => GraphID;

        internal bool IsValid(ushort systemID)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (GraphID == 0)
                throw new System.ArgumentException("GraphHandle is invalid, use AnimationSystemBase.CreateManagedGraph() to create a valid handle");
            if (SystemID != systemID)
                throw new System.ArgumentException($"GraphHandle [SystemId: {SystemID}] is incompatible with this system [Id: {systemID}]");

            return true;
#else
            return GraphID != 0 && SystemID == systemID;
#endif
        }
    }
}
