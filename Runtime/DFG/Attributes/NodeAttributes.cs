using System;

namespace Unity.DataFlowGraph.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeDefinitionAttribute : Attribute
    {
        public NodeDefinitionAttribute(string category = "", string description = "", bool isHidden = false)
        {
            Category = category;
            NodeDescription = description;
            IsHidden = isHidden;
        }

        public string NodeDescription { get; }
        public string Category { get; }
        public bool IsHidden { get; }
    }
}
