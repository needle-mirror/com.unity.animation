using System;

namespace Unity.Animation
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class PhaseAttribute : Attribute
    {
        public PhaseAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; }
    }
}
