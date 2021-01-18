using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class ComponentBinding
    {
        [SerializeField, HideInInspector]
        internal string Name;
        [SerializeField, HideInInspector]
        internal ComponentBindingIdentifier Identifier;
    }

    [Serializable]
    struct ComponentBindingIdentifier : IEquatable<ComponentBindingIdentifier>
    {
        [SerializeField, HideInInspector]
        internal TypeHandle Type;

        public bool Equals(ComponentBindingIdentifier other)
        {
            return Equals(Type.Resolve(), other.Type.Resolve());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is ComponentBindingIdentifier cbd && Equals(cbd);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public static bool operator==(ComponentBindingIdentifier left, ComponentBindingIdentifier right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(ComponentBindingIdentifier left, ComponentBindingIdentifier right)
        {
            return !left.Equals(right);
        }
    }
}
