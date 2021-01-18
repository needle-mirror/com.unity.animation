using System;
using System.Collections.Generic;
using System.Linq;
using Unity.DataFlowGraph;
using Unity.Entities;

namespace Unity.Animation.Editor
{
    internal class Helpers
    {
        public static bool IsComponentDataType(Type t)
        {
            if (typeof(IComponentData).IsAssignableFrom(t))
                return true;
            return false;
        }

        public static Type GetUnderlyingBufferElementType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Buffer<>))
            {
                return type.GenericTypeArguments.Where(t => typeof(IBufferElementData).IsAssignableFrom(t)).FirstOrDefault();
            }
            return null;
        }

        public static bool IsBufferElementDataType(Type t)
        {
            return GetUnderlyingBufferElementType(t) != null;
        }

        public static bool IsComponentNodeCompatible(Type t) => IsComponentDataType(t) || IsBufferElementDataType(t);

        //We assume that if this function returns null then it’s a compositor node that needs to be flattened
        internal static Type GetDefinedType(IRNodeDefinition node)
        {
            return node.NodeType ?? Type.GetType(node.GetUnresolvedTypeName());
        }
    }

    internal class NodeIDGenerator
    {
        private int CurrentID = 0;
        public NodeID CreateUniqueID()
        {
            return (NodeID)CurrentID++;
        }
    }

    internal class GraphPath
    {
        private List<string> _stack = new List<string>();

        public void Push(string str)
        {
            _stack.Add(str);
        }

        public void Pop()
        {
            if (_stack.Count < 1)
            {
                throw new InvalidOperationException($"Trying to pop from an empty {nameof(GraphPath)} object");
            }
            _stack.RemoveAt(_stack.Count - 1);
        }

        public GraphPath Combine(GraphPath other)
        {
            var newPath = new GraphPath();
            newPath._stack.AddRange(this._stack);
            newPath._stack.AddRange(other._stack);
            return newPath;
        }

        public override string ToString()
        {
            return String.Join("/", _stack) + "/";
        }
    }

    class NodeCache
    {
        private NodeIDGenerator m_NodeIDGenerator = new NodeIDGenerator();

        private Dictionary<NodeID, string> m_NodeIDToPath = new Dictionary<NodeID, string>();
        private Dictionary<string, NodeID> m_PathToNodeID = new Dictionary<string, NodeID>();
        private Dictionary<NodeID, IRNodeDefinition> m_NodeIDToNodeDef = new Dictionary<NodeID, IRNodeDefinition>();

        public NodeID GetNodeID(GraphPath path, string nodeName)
        {
            string nodePath = path + nodeName;
            if (!m_PathToNodeID.ContainsKey(nodePath))
            {
                return NodeID.Invalid;
            }
            return m_PathToNodeID[nodePath];
        }

        public IRNodeDefinition GetNode(GraphPath path, string nodeName)
        {
            string nodePath = path + nodeName;
            return GetNode(m_PathToNodeID[nodePath]);
        }

        public IRNodeDefinition GetNode(NodeID id)
        {
            return m_NodeIDToNodeDef[id];
        }

        public string GetNodePath(NodeID id)
        {
            return m_NodeIDToPath[id];
        }

        public string GetPortPath(PortReference portRef)
        {
            return $"{GetNodePath(portRef.ID)}.{portRef.Port.ID}.{portRef.Port.Index}";
        }

        public NodeID RegisterNode(IRNodeDefinition nodeDef, GraphPath path)
        {
            string nodePath = path + nodeDef.Name;
            if (m_PathToNodeID.ContainsKey(nodePath))
            {
                throw new InvalidOperationException($"Node at {nodePath} is already registered");
            }
            NodeID id = m_NodeIDGenerator.CreateUniqueID();
            m_NodeIDToPath.Add(id, nodePath);
            m_PathToNodeID.Add(nodePath, id);
            m_NodeIDToNodeDef.Add(id, nodeDef);
            return id;
        }
    }
}
