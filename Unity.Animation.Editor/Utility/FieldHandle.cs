using System;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Unity.Animation.Model
{
    static class FieldHandleExtensions
    {
        public static FieldHandle GenerateFieldHandle(this FieldInfo f)
        {
            Assert.IsNotNull(f);
            return new FieldHandle(f);
        }
    }

    [Serializable]
    struct FieldHandle : IEquatable<FieldHandle>
    {
        public FieldHandle(FieldInfo info)
        {
            m_FieldName = info.Name;
            m_FieldDeclaringType = info.DeclaringType.GenerateTypeHandle();
        }

        [SerializeField]
        string m_FieldName;

        [SerializeField]
        TypeHandle m_FieldDeclaringType;

        public string Resolve()
        {
            var type = m_FieldDeclaringType.Resolve();
            var fieldName = m_FieldName;
            if (type.GetFields().Any(f => f.Name == fieldName))
                return fieldName;
            foreach (var f in type.GetFields())
            {
                var attr = f.GetCustomAttributes<FormerlySerializedAsAttribute>().SingleOrDefault(a => a.oldName == fieldName);
                if (attr != null)
                    return f.Name;
            }
            return "";
        }

        public Type ResolveType()
        {
            if (Hybrid.AuthoringComponentService.TryGetComponentByRuntimeType(m_FieldDeclaringType.Resolve(), out var componentInfo))
            {
                return componentInfo.RuntimeFields[m_FieldName].FieldInfo.FieldType;
            }
            return null;
        }

        public bool Equals(FieldHandle other)
        {
            return string.Equals(Resolve(), other.Resolve());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is FieldHandle th && Equals(th);
        }

        public override int GetHashCode()
        {
            return m_FieldName.GetHashCode() * m_FieldDeclaringType.GetHashCode();
        }

        public static bool operator==(FieldHandle left, FieldHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(FieldHandle left, FieldHandle right)
        {
            return !left.Equals(right);
        }
    }
}
