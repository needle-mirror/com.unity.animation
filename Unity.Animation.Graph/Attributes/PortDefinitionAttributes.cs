using System;

namespace Unity.DataFlowGraph.Attributes
{
    public enum DefaultValueType
    {
        Value,
        ComplexValue,
        Reference
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class PortDefinitionAttribute : Attribute
    {
        public PortDefinitionAttribute(string guid, string displayName = null, string description = null, bool isHidden = false, int portGroupIndex = -1, object defaultValue = null, DefaultValueType defaultValueType = DefaultValueType.Value, bool isStatic = false, object minValueUI = null, object maxValueUI = null)
        {
            DisplayName = displayName;
            Description = description;
            IsHidden = isHidden;
            PortGroupIndex = portGroupIndex;
            DefaultValue = defaultValue;
            DefaultValueType = defaultValueType;
            IsStatic = isStatic;
            MinValueUI = minValueUI;
            MaxValueUI = maxValueUI;
            Guid = guid;
        }

        public string DisplayName { get; }
        public string Description { get; }
        public bool IsHidden { get; }
        public int PortGroupIndex { get; }
        public object DefaultValue { get; }
        public DefaultValueType DefaultValueType { get; }
        public bool IsStatic { get; }
        public object MinValueUI { get; }
        public object MaxValueUI { get; }
        public string Guid { get; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class PortGroupDefinitionAttribute : Attribute
    {
        public PortGroupDefinitionAttribute(string portGroupSizeDescription, int groupIndex, int minInstance = 0, int maxInstance = -1, string simulationPortToDrive = null)
        {
            PortGroupSizeDescription = portGroupSizeDescription;
            PortGroupIndex = groupIndex;
            MinInstance = minInstance;
            MaxInstance = maxInstance;
            SimulationPortToDrive = simulationPortToDrive;
        }

        public string PortGroupSizeDescription { get; }
        public int PortGroupIndex { get; }
        public int MinInstance { get; }
        public int MaxInstance { get; }
        public string SimulationPortToDrive { get; }
    }
}
