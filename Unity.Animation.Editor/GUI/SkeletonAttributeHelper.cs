using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.Animation.Authoring.Editor
{
    static class SkeletonAttributeHelper
    {
        static readonly Type kSkeletonReferenceAttribute    = typeof(SkeletonReferenceAttribute);
        static readonly Type kShowFullPathAttribute         = typeof(ShowFullPathAttribute);

        interface ISkeletonProvider
        {
            Skeleton GetSkeleton(SerializedProperty property, object obj);
        }

        class ClassPropertySkeletonProvider : ISkeletonProvider
        {
            ClassPropertySkeletonProvider(IMemberInfo[] members) { this.members = members; }
            IMemberInfo[] members;
            public Skeleton GetSkeleton(SerializedProperty property, object obj)
            {
                for (int i = 0; i < members.Length; i++)
                    obj = members[i]?.GetValue(obj);
                return obj as Skeleton;
            }

            interface IMemberInfo
            {
                object GetValue(object parent);
                Type ReturnType { get; }
            }

            class FieldInfoWrapper : IMemberInfo
            {
                public FieldInfo fieldInfo;
                public object GetValue(object parent) { return fieldInfo.GetValue(parent); }
                public Type ReturnType { get { return fieldInfo.FieldType; } }
            }

            class PropertyInfoWrapper : IMemberInfo
            {
                public PropertyInfo propertyInfo;
                public object GetValue(object parent) { return propertyInfo.GetValue(parent); }
                public Type ReturnType { get { return propertyInfo.PropertyType; } }
            }

            static IMemberInfo GetMember(Type type, string name)
            {
                const BindingFlags AllMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
                var fieldInfo = type.GetField(name, AllMembers);
                if (fieldInfo != null)
                    return new FieldInfoWrapper { fieldInfo = fieldInfo };
                var propertyInfo = type.GetProperty(name, AllMembers);
                if (propertyInfo != null)
                    return new PropertyInfoWrapper { propertyInfo = propertyInfo };
                return null;
            }

            static readonly List<IMemberInfo> s_Members = new List<IMemberInfo>();
            public static ClassPropertySkeletonProvider FindMemberChain(Type type, string path)
            {
                s_Members.Clear();
                var lastIndex = path.LastIndexOf('.');
                IMemberInfo member;
                while (lastIndex != -1)
                {
                    var firstPart = path.Remove(lastIndex);
                    path = path.Substring(lastIndex + 1);
                    member = GetMember(type, firstPart);
                    if (member == null)
                        return null;

                    s_Members.Add(member);
                    type = member.ReturnType;
                    lastIndex = path.LastIndexOf('.');
                }

                member = GetMember(type, path);
                if (member == null || member.ReturnType != typeof(Skeleton))
                    return null;

                s_Members.Add(member);
                var result = new ClassPropertySkeletonProvider(s_Members.ToArray());
                s_Members.Clear();
                return result;
            }
        }


        static readonly Dictionary<Type, Dictionary<string, bool>>              s_ShowFullPath          = new Dictionary<Type, Dictionary<string, bool>>();
        static readonly Dictionary<Type, Dictionary<string, ISkeletonProvider>> s_SkeletonProviders     = new Dictionary<Type, Dictionary<string, ISkeletonProvider>>();

        public static bool HasShowFullPathAttribute(FieldInfo fieldInfo, SerializedProperty property)
        {
            var targetObjectType = property.serializedObject.targetObject.GetType();
            if (!s_ShowFullPath.TryGetValue(targetObjectType, out var fieldAttributes))
                s_ShowFullPath[targetObjectType] = fieldAttributes = new Dictionary<string, bool>();

            var propertyPath = property.propertyPath;
            if (!fieldAttributes.TryGetValue(propertyPath, out var showFullPath))
                fieldAttributes[propertyPath] = showFullPath = (fieldInfo.GetCustomAttribute(kShowFullPathAttribute) != null);

            return showFullPath;
        }

        static ISkeletonProvider CreateSkeletonProvider(FieldInfo fieldInfo, SerializedProperty property, Type targetObjectType)
        {
            var attribute = fieldInfo.GetCustomAttribute(kSkeletonReferenceAttribute) as SkeletonReferenceAttribute;
            if (attribute == null)
                return null;

            var searchPath = attribute.RelativeSkeletonPath;
            var skeletonPropertyPath = property.propertyPath;
            var index = skeletonPropertyPath.LastIndexOf('.');
            skeletonPropertyPath = (index == -1) ? searchPath : $"{skeletonPropertyPath.Substring(0, index)}.{searchPath}";
            if (skeletonPropertyPath == null)
                return null;

            var provider = ClassPropertySkeletonProvider.FindMemberChain(targetObjectType, skeletonPropertyPath);
            if (provider != null)
                return provider;

            return null;
        }

        public static Skeleton FindSkeleton(FieldInfo fieldInfo, SerializedProperty property, out bool mixed)
        {
            mixed = false;
            var targetObjects = property.serializedObject.targetObjects;
            Skeleton skeleton = null;
            for (int i = 0; i < targetObjects.Length; i++)
            {
                var targetObject     = targetObjects[i];
                var targetObjectType = targetObject.GetType();
                if (!s_SkeletonProviders.TryGetValue(targetObjectType, out var fieldAttributes))
                    s_SkeletonProviders[targetObjectType] = fieldAttributes = new Dictionary<string, ISkeletonProvider>();

                var propertyPath = property.propertyPath;
                if (!fieldAttributes.TryGetValue(propertyPath, out var skeletonProvider))
                    fieldAttributes[propertyPath] = skeletonProvider = CreateSkeletonProvider(fieldInfo, property, targetObjectType);

                var foundSkeleton = skeletonProvider?.GetSkeleton(property, targetObject);
                if (i > 0 &&                    // first skeleton is always valid
                    skeleton != foundSkeleton)  // next skeletons must be the same as previous
                {
                    mixed = true;
                    return null;
                }
                skeleton = foundSkeleton;
            }
            return skeleton;
        }
    }
}
