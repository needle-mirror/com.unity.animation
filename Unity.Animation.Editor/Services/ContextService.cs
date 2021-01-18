using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class RegisterReducerAttribute : Attribute
    {
        public RegisterReducerAttribute()
        {
        }
    }


    internal static class ContextService
    {
        internal struct ContextEntry
        {
            internal ContextEntry(Type stencilType, string name)
            {
                StencilType = stencilType;
                Name = name;
            }

            public Type StencilType { get; }
            public string Name { get; }
        }

        internal interface IReducerRegister
        {
            void RegisterReducers(Store store);
        }

        static List<Type> s_RegisterReducerCallbacks;

        public static IEnumerable<Type> RegisterReducerCallbacks
        {
            get
            {
                BuildRegisterReducerCallbacks();
                return s_RegisterReducerCallbacks;
            }
        }

        static IEnumerable<ContextEntry> s_AvailableContexts;

        internal static IEnumerable<ContextEntry> AvailableContexts
        {
            get
            {
                BuildContextEntries();
                return s_AvailableContexts;
            }
        }

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

        static IEnumerable<Assembly> s_Assemblies;

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
                s_AvailableContexts = null;
            }
        }

        static void BuildContextEntries()
        {
            if (s_AvailableContexts == null)
            {
                var contextEntries = new List<ContextEntry>();
                var availableStencils =
                    from a in CachedAssemblies
                    from t in a.GetTypesSafe()
                    where !t.IsAbstract && t.IsClass && !t.IsGenericType
                    where typeof(Stencil).IsAssignableFrom(t)
                    select t;
                foreach (var s in availableStencils)
                {
                    if (!typeof(BaseGraphStencil).IsAssignableFrom(s) && !typeof(StateMachineStencil).IsAssignableFrom(s))
                    {
                        continue;
                    }
                    var context = s.GetCustomAttribute<ContextAttribute>();
                    var name = context != null ? context.Name : s.Name;
                    contextEntries.Add(new ContextEntry(s, name));
                }

                s_AvailableContexts = contextEntries;
            }
        }

        static void BuildRegisterReducerCallbacks()
        {
            if (s_RegisterReducerCallbacks == null)
            {
                s_RegisterReducerCallbacks = new List<Type>();
                var availableCallbacks =
                    from a in CachedAssemblies
                    from t in a.GetTypesSafe()
                    where !t.IsAbstract && t.IsClass && !t.IsGenericType
                    where typeof(IReducerRegister).IsAssignableFrom(t)
                    select t;
                foreach (var callback in availableCallbacks)
                {
                    var register = callback.GetCustomAttribute<RegisterReducerAttribute>();
                    if (register != null)
                        s_RegisterReducerCallbacks.Add(callback);
                }
            }
        }
    }
}
