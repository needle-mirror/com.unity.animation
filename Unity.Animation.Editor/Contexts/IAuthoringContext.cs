using System;

namespace Unity.Animation.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class ContextAttribute : Attribute
    {
        public ContextAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    interface IAuthoringContext
    {
        Type DefaultDataType { get; }
        Type PassThroughForDefaultDataType { get; }
        Type ContextType { get; }
        Type GameObjectContextType { get; }
        Type ContextHandlerType { get; }
        Type OutputNodeType { get; }

        Type GetDomainConstantEditorType(Type type);
    }
}
