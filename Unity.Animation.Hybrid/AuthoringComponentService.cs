using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Entities;
using UnityEditor;

#if UNITY_EDITOR

namespace Unity.Animation.Hybrid
{
    internal class RuntimeFieldInfo
    {
        public FieldInfo FieldInfo;
        public int Offset;
    }

    internal class AuthoringComponentInfo
    {
        public Type AuthoringType;
        public Type RuntimeType;
        public Dictionary<string, RuntimeFieldInfo> RuntimeFields;
    }

    internal static class AuthoringComponentService
    {
        const BindingFlags k_FieldFlags = BindingFlags.Instance | BindingFlags.Public;
        const string k_GeneratedAuthoringComponentTypeSuffix = "Authoring";

        static Dictionary<Type, AuthoringComponentInfo> s_ComponentsByAuthoringType;
        static Dictionary<string, AuthoringComponentInfo> s_ComponentsByAuthoringAssemblyQualifiedName;
        static Dictionary<Type, AuthoringComponentInfo> s_ComponentsByRuntimeType;
        static Dictionary<string, AuthoringComponentInfo> s_ComponentsByRuntimeAssemblyQualifiedName;

        public static bool TryGetComponentByRuntimeType(Type type, out AuthoringComponentInfo handle)
        {
            Generate();
            return s_ComponentsByRuntimeType.TryGetValue(type, out handle);
        }

        public static bool TryGetComponentByAuthoringType(Type type, out AuthoringComponentInfo handle)
        {
            Generate();
            return s_ComponentsByAuthoringType.TryGetValue(type, out handle);
        }

        public static bool TryGetComponentByAuthoringAssemblyQualifiedName(string assemblyQualifiedName, out AuthoringComponentInfo handle)
        {
            Generate();
            return s_ComponentsByAuthoringAssemblyQualifiedName.TryGetValue(assemblyQualifiedName, out handle);
        }

        public static bool TryGetComponentByRuntimeAssemblyQualifiedName(string assemblyQualifiedName, out AuthoringComponentInfo handle)
        {
            Generate();
            return s_ComponentsByRuntimeAssemblyQualifiedName.TryGetValue(assemblyQualifiedName, out handle);
        }

        public static IReadOnlyList<AuthoringComponentInfo> GetComponentInfos()
        {
            Generate();
            return s_ComponentsByAuthoringType.Select(c => c.Value).ToList();
        }

        static void Generate()
        {
            if (s_ComponentsByAuthoringType == null)
            {
                s_ComponentsByAuthoringType = new Dictionary<Type, AuthoringComponentInfo>();
                s_ComponentsByRuntimeType = new Dictionary<Type, AuthoringComponentInfo>();
                s_ComponentsByAuthoringAssemblyQualifiedName = new Dictionary<string, AuthoringComponentInfo>();
                s_ComponentsByRuntimeAssemblyQualifiedName = new Dictionary<string, AuthoringComponentInfo>();

                var generatedTypes = TypeCache.GetTypesWithAttribute<GenerateAuthoringComponentAttribute>();

                var filteredTypes = GetFilteredTypesDerivedFrom<MonoBehaviour>(k_GeneratedAuthoringComponentTypeSuffix);
                foreach (var type in generatedTypes)
                {
                    var generatedAuthoringType = FindType(filteredTypes, $"{type.FullName}{k_GeneratedAuthoringComponentTypeSuffix}");
                    if (generatedAuthoringType != null)
                    {
                        var fields = new Dictionary<string, RuntimeFieldInfo>();
                        foreach (var f in type.GetFields(k_FieldFlags))
                        {
                            fields.Add(f.Name, new RuntimeFieldInfo()
                            {
                                FieldInfo = f,
                                Offset = UnsafeUtility.GetFieldOffset(f)
                            });
                        }

                        if (fields.Any())
                        {
                            var component = new AuthoringComponentInfo()
                            {
                                AuthoringType = generatedAuthoringType,
                                RuntimeType = type,
                                RuntimeFields = fields
                            };
                            s_ComponentsByAuthoringType.Add(generatedAuthoringType, component);
                            s_ComponentsByRuntimeType.Add(type, component);
                            s_ComponentsByAuthoringAssemblyQualifiedName.Add(generatedAuthoringType.AssemblyQualifiedName, component);
                            s_ComponentsByRuntimeAssemblyQualifiedName.Add(type.AssemblyQualifiedName, component);
                        }
                    }
                }
            }
        }

        static List<Type> GetFilteredTypesDerivedFrom<T>(string filter)
        {
            var filteredTypes = new List<Type>(10);
            var types = TypeCache.GetTypesDerivedFrom<T>();
            foreach (var type in types)
            {
                if (type.FullName.Contains(filter))
                    filteredTypes.Add(type);
            }

            return filteredTypes;
        }

        internal static Type FindType(List<Type> types, string typeStr)
        {
            foreach (var type in types)
            {
                if (type.FullName == typeStr)
                    return type;
            }

            return null;
        }
    }
}
#endif // UNITY_EDITOR
