using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

namespace Unity.Animation.Authoring.Editor
{
    internal sealed class AnimationIcons : ScriptableObject
    {
        static AnimationIcons m_Instance;
        static AnimationIcons Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = ScriptableObject.CreateInstance<AnimationIcons>();
                    m_Instance.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_Instance;
            }
        }

        // This field is set in Unity when selecting the script in the inspector
        [SerializeField] Texture2D m_BoneIcon = default;
        public static Texture2D BoneIcon => Instance.m_BoneIcon;


        Texture2D m_SkeletonIcon = default;
        public static Texture2D SkeletonIcon => (Instance.m_SkeletonIcon != null) ? Instance.m_SkeletonIcon : (Instance.m_SkeletonIcon = EditorGUIUtility.IconContent("Avatar Icon").image as Texture2D);


        Texture2D m_WarnIcon = default;
        public static Texture2D WarnIcon => (Instance.m_WarnIcon != null) ? Instance.m_WarnIcon : (Instance.m_WarnIcon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D);


        Texture2D m_TransformIcon = default;
        public static Texture2D TransformIcon => (Instance.m_TransformIcon != null) ? Instance.m_TransformIcon : (Instance.m_TransformIcon = EditorGUIUtility.IconContent("Transform Icon").image as Texture2D);


        Texture2D m_OverrideRemovedOverlay = default;
        public static Texture2D OverrideRemovedOverlay => (Instance.m_OverrideRemovedOverlay != null) ? Instance.m_OverrideRemovedOverlay : (Instance.m_OverrideRemovedOverlay = EditorGUIUtility.IconContent("PrefabOverlayRemoved Icon").image as Texture2D);


        Texture2D m_OverrideAddedOverlay = default;
        public static Texture2D OverrideAddedOverlay => (Instance.m_OverrideAddedOverlay != null) ? Instance.m_OverrideAddedOverlay : (Instance.m_OverrideAddedOverlay = EditorGUIUtility.IconContent("PrefabOverlayAdded Icon").image as Texture2D);


        Texture2D m_OverrideModifiedOverlay = default;
        public static Texture2D OverrideModifiedOverlay => (Instance.m_OverrideModifiedOverlay != null) ? Instance.m_OverrideModifiedOverlay : (Instance.m_OverrideModifiedOverlay = EditorGUIUtility.IconContent("PrefabOverlayModified Icon").image as Texture2D);
    }
}
