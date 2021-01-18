using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.DataFlowGraph;
using Unity.Profiling;

namespace Unity.Animation
{
    public sealed class TypeRegistry
    {
        private readonly Dictionary<int, MethodInfo> m_CreateMethodsMap = new Dictionary<int, MethodInfo>();
        private readonly ProfilerMarker k_RegisterTypeMarker = new ProfilerMarker($"Unity.Animation.{nameof(TypeRegistry)} : Register types");
        public static TypeRegistry Instance { get; } = new TypeRegistry();

        internal TypeRegistry() {}

        private NodeHandle CreateNode<T>(NodeSet set)
            where T : NodeDefinition, new()
        {
            return set.Create<T>();
        }

        public void RegisterType(string typeToRegister)
        {
            using (k_RegisterTypeMarker.Auto())
            {
                var type = Type.GetType(typeToRegister);
                if (type == null)
                {
                    throw new ArgumentException($"Type {typeToRegister} is not found");
                }

                var hash = type.AssemblyQualifiedName.GetHashCode();
                if (m_CreateMethodsMap.ContainsKey(hash))
                {
                    return;
                }

                m_CreateMethodsMap[hash] = typeof(TypeRegistry).GetMethod(nameof(CreateNode), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(type);
            }
        }

        /// <summary>
        /// This is what is used in Compositor to circumvent the fact that DFG does not allow to call <see cref="NodeSet.Create{TDefinition}"/> with an unknown type
        /// The types need to be registered using <see cref="RegisterType"/>
        /// </summary>
        public NodeHandle CreateNodeFromHash(NodeSetAPI set, int hash)
        {
            if (!m_CreateMethodsMap.TryGetValue(hash, out var method))
            {
                throw new ArgumentException($"Could not find type with hash {hash}. It is necessary to call {nameof(RegisterType)}() with assembly qualified name of said type");
            }

            return (NodeHandle)method.Invoke(this, new object[] {set});
        }
    }
}
