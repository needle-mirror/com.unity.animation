using System.IO;

using UnityEditor;
using UnityEditor.Animations;

namespace Unity.Animation.Editor
{
    public static class BlendTreeMenu
    {
        [MenuItem("Animation/Blend Tree/Create Blend Tree")]
        public static void CreateBlendTreeAsset()
        {
            var path = "Assets/blendtree.asset";
            if (Selection.assetGUIDs.Length > 0)
            {
                var selectedPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                if (!AssetDatabase.IsValidFolder(selectedPath))
                {
                    selectedPath = Path.GetDirectoryName(selectedPath);
                }

                path = Path.Combine(selectedPath, "blendtree.asset");
            }

            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var blendTree = new BlendTree();
            AssetDatabase.CreateAsset(blendTree, path);
        }
    }
}
