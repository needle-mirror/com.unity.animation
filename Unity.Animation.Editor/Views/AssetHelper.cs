using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    [InitializeOnLoad]
    static class AssetHelper
    {
        // Double-click an asset to load it and show it in the Animation Graph window -- step 1
        [OnOpenAsset(1)]
        public static bool OpenAnimationGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is BaseAssetModel)
            {
                string path = AssetDatabase.GetAssetPath(instanceId);
                return OpenAnimationGraphAssetInWindow(path) != null;
            }

            return false;
        }

        internal static AnimationGraphWindow OpenAnimationGraphAssetInWindow(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<BaseAssetModel>(path);
            if (asset == null)
                return null;

            AnimationGraphWindow animGraphWindow = ShowAnimationGraphEditorWindow();

            animGraphWindow.SetCurrentSelection(path, GtfoWindow.OpenMode.OpenAndFocus);

            return animGraphWindow;
        }

        [MenuItem("Window/Animation/Animation Graph", priority = MenuUtility.WindowPriority)]
        static void ShowNewAnimationGraphEditorWindow()
        {
            ShowAnimationGraphEditorWindow();
        }

        static AnimationGraphWindow ShowAnimationGraphEditorWindow()
        {
            var animGraphWindows = Resources.FindObjectsOfTypeAll(typeof(AnimationGraphWindow)).OfType<AnimationGraphWindow>().Where(obj => obj.GetType() == typeof(AnimationGraphWindow)).ToArray();
            AnimationGraphWindow window;
            if (animGraphWindows.Length > 0)
            {
                window = animGraphWindows[0];
            }
            else
            {
                window = ScriptableObject.CreateInstance<AnimationGraphWindow>();
            }
            window.Show();
            window.Focus();

            return window;
        }
    }
}
