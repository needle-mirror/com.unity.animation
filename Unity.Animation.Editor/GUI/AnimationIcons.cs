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
        [SerializeField] Texture2D Bone_Icon = default;
        public static Texture2D BoneIcon => Instance.Bone_Icon;
    }
}
