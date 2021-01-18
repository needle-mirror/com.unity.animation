using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    internal static class ConversionService
    {
        static IEnumerable<Assembly> s_Assemblies;

        internal static readonly string[] k_BlackListedAssemblies =
        {
            "boo.lang",
            "castle.core",
            "excss.unity",
            "jetbrains",
            "lucene",
            "microsoft",
            "mono",
            "moq",
            "nunit",
            "system.web",
            "unityscript",
            "visualscriptingassembly-csharp"
        };

        internal static IEnumerable<Assembly> CachedAssemblies
        {
            get
            {
                return s_Assemblies ?? (s_Assemblies = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic
                            && !k_BlackListedAssemblies.Any(b => a.GetName().Name.ToLower().Contains(b))
                            && !a.GetName().Name.EndsWith(".Tests"))
                        .ToList());
            }
            set
            {
                s_Assemblies = value;
                m_AvailableTagTypes = null;
            }
        }

        static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                Debug.LogWarning("Can't load assembly '" + assembly.GetName() + "'. Problematic types follow.");
                foreach (TypeLoadException tle in e.LoaderExceptions.Cast<TypeLoadException>())
                {
                    Debug.LogWarning("Can't load type '" + tle.TypeName + "': " + tle.Message);
                }

                return new Type[0];
            }
        }

        public class Phase
        {
            public Type Type { get; internal set; }
            public string Description { get; internal set; }
            public ulong Hash { get; internal set; }
        }

        public static bool TryGetPhaseFromAssemblyQualifiedName(string assemblyQualifiedName, out Phase phase)
        {
            phase = GetPhases().Find(p => p.Type.AssemblyQualifiedName == assemblyQualifiedName);
            if (phase == null)
                return false;
            return true;
        }

        static List<Phase> m_AvailableTagTypes;
        static public List<Phase> GetPhases(bool sorted = false)
        {
            if (m_AvailableTagTypes == null)
            {
                var types = from a in CachedAssemblies
                    from t in GetTypesSafe(a)
                    from b in t.GetBaseTypes()
                    where t.IsVisible && !t.IsGenericType && b.IsGenericType &&
                    typeof(BaseGraphLoaderSystem<, ,>).IsAssignableFrom(b.GetGenericTypeDefinition())
                    select b.GetGenericArguments()[1];

                m_AvailableTagTypes = new List<Phase>();
                foreach (var t in types.Distinct())
                {
                    if (t.GetInterface(typeof(IComponentData).Name, false) == null)
                        throw new TypeLoadException("Invalid Phase Type");
                    var attr = t.GetCustomAttribute<PhaseAttribute>();

                    m_AvailableTagTypes.Add(
                        new Phase
                        {
                            Type = t,
                            Description = attr != null ? attr.Description : t.FullName,
                            Hash = TypeHash.CalculateStableTypeHash(t)
                        });
                }
            }

            if (sorted)
            {
                m_AvailableTagTypes.Sort((x, y) => string.Compare(x.Description, y.Description, true));
            }
            return m_AvailableTagTypes;
        }
    }
}
