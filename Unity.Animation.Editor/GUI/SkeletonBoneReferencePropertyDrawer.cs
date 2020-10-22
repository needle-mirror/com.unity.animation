using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.Animation.Authoring.Editor
{
    [CustomPropertyDrawer(typeof(SkeletonBoneReference))]
    class SkeletonBoneReferencePropertyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var prevMixedValue = EditorGUI.showMixedValue;
            try
            {
                var showFullPath = SkeletonAttributeHelper.HasShowFullPathAttribute(fieldInfo, property);
                EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                AnimationGUI.BoneField(position, property, label, showFullPath);
            }
            finally
            {
                EditorGUI.showMixedValue = prevMixedValue;
            }
        }
    }
}
