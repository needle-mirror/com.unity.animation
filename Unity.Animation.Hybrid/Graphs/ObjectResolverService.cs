using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Unity.Entities;

namespace Unity.Animation.Hybrid
{
    internal static class ObjectResolverService
    {
        static Dictionary<ulong, IObjectResolver> s_AvailableResolvers;
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
            }
        }

        internal static Dictionary<ulong, IObjectResolver> AvailableResolvers
        {
            get
            {
                if (s_AvailableResolvers == null)
                {
                    IEnumerable<Type> types = from a in CachedAssemblies
                        from t in GetTypesSafe(a)
                        where (typeof(IObjectResolver).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                        select t;

                    s_AvailableResolvers = new Dictionary<ulong, IObjectResolver>();

                    foreach (Type type in types)
                    {
                        var objectResolver = (IObjectResolver)Activator.CreateInstance(type);
                        var typeHash = TypeHash.CalculateStableTypeHash(objectResolver.Type);
                        s_AvailableResolvers[typeHash] = objectResolver;
                    }
                }

                return s_AvailableResolvers;
            }
        }

        internal static GraphVariant ResolveObjectReference(ulong portTypeHash, UnityEngine.Object objectReference, Component context)
        {
            return GetObjectResolver(portTypeHash).ResolveValue(objectReference, context);
        }

        private static IObjectResolver GetObjectResolver(ulong portTypeHash)
        {
            if (AvailableResolvers.ContainsKey(portTypeHash))
            {
                return AvailableResolvers[portTypeHash];
            }
            throw new Exception($"Unable to find object reference resolver for type: {portTypeHash}");
        }

        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                UnityEngine.Debug.LogWarning("Can't load assembly '" + assembly.GetName() + "'. Problematic types follow.");
                foreach (TypeLoadException tle in e.LoaderExceptions.Cast<TypeLoadException>())
                {
                    UnityEngine.Debug.LogWarning("Can't load type '" + tle.TypeName + "': " + tle.Message);
                }

                return new Type[0];
            }
        }

        internal static readonly string[] k_BlackListedAssemblies =
        {
            "Unity.DataFlowGraph",
            "Unity.DataFlowGraph.Tests",
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
    }

    internal interface IObjectResolver
    {
        Type Type { get; }
        GraphVariant ResolveValue(UnityEngine.Object objectReference, Component context);
    }
}
