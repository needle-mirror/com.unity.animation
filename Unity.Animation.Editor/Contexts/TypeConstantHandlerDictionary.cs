using System;
using System.Collections.Generic;

namespace Unity.Animation.Editor
{
    internal class TypeConstantHandlerDictionary
    {
        static Lazy<TypeConstantHandlerDictionary> s_Instance = new Lazy<TypeConstantHandlerDictionary>();
        internal static TypeConstantHandlerDictionary Instance => s_Instance.Value;

        Dictionary<Type, IConstantHandler> m_Dictionary = new Dictionary<Type, IConstantHandler>();

        internal void AddTypeHandler(Type type, IConstantHandler handler)
        {
            if (!m_Dictionary.ContainsKey(type))
            {
                m_Dictionary.Add(type, handler);
            }
        }

        internal object GetValueFromDefaultString(Type type, string str)
        {
            if (!m_Dictionary.TryGetValue(type, out IConstantHandler handler))
                return null;
            return handler.GetValueFromDefaultString(str);
        }
    }
}
