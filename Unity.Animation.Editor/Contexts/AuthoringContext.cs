using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal abstract class AuthoringContext<T> : IAuthoringContext
        where T : OutputNodeModel
    {
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

        public abstract Type DefaultDataType { get; }
        public abstract Type PassThroughForDefaultDataType { get; }
        public abstract Type ContextType { get; }
        public abstract Type GameObjectContextType { get; }
        public abstract Type ContextHandlerType { get; }

        public Type OutputNodeType => typeof(T);

        protected Dictionary<Type, Type> m_ConstantEditorByType = new Dictionary<Type, Type>();

        protected virtual void BuildConstantEditorMappings()
        {
            var constantEditorTypes =
                from a in CachedAssemblies
                from t in a.GetTypesSafe()
                where t.IsClass && !t.IsGenericType
                from i in t.GetInterfaces()
                where i == typeof(IReferenceConstantProvider)
                select t;

            foreach (var t in constantEditorTypes)
            {
                var baseType = t.BaseType;
                if (baseType != null && baseType.IsGenericType && baseType.GenericTypeArguments.Length == 2)
                {
                    m_ConstantEditorByType.Add(baseType.GenericTypeArguments[1], t);
                }
            }
            m_ConstantEditorByType.Add(typeof(PortGroup), typeof(PortGroupConstant));
            //m_ConstantEditorByType.Add(typeof(float3), typeof(Float3Constant));
            //m_ConstantEditorByType.Add(typeof(quaternion), typeof(QuaternionConstant));
            //m_ConstantEditorByType.Add(typeof(float4x4), typeof(Float44Constant));
            //m_ConstantEditorByType.Add(typeof(Unity.Animation.AnimationCurve), typeof(CurveConstantNodeModel));
        }

        public Type GetDomainConstantEditorType(Type type)
        {
            if (!m_ConstantEditorByType.TryGetValue(type, out Type constantEditorType))
                return null;
            return constantEditorType;
        }

        protected AuthoringContext()
        {
            BuildConstantEditorMappings();
        }
    }
}
