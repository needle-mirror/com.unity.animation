using System;

namespace Unity.DataFlowGraph.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeDefinitionAttribute : Attribute
    {
        public NodeDefinitionAttribute(string guid, int version, string category = "", string description = "", bool isHidden = false)
        {
            Category = category;
            NodeDescription = description;
            IsHidden = isHidden;
            Guid = guid;
            Version = version;
        }

        public string NodeDescription { get; }
        public string Category { get; }
        public bool IsHidden { get; }
        public string Guid { get; }
        public int Version { get; }
    }
}
