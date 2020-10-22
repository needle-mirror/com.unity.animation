using System.IO;
using UnityEngine;
using UnityEditor;
using Unity.Animation.Hybrid;
using Unity.Animation.Authoring.Editor;

namespace Unity.Animation.Editor
{
    [CustomEditor(typeof(RigAuthoring))]
    [CanEditMultipleObjects]
    class RigAuthoringEditor : UnityEditor.Editor
    {
        [MenuItem("CONTEXT/RigAuthoring/Create Skeleton Asset", false, 611)]
        private static void OnCreateSkeletonAsset(MenuCommand command)
        {
            var rigAuthoring = command.context as RigAuthoring;

            var gameObjectHierarchy = rigAuthoring.gameObject;

            string newAssetName = Path.GetFileNameWithoutExtension(gameObjectHierarchy.name);

            string message = L10n.Tr($"Create a new skeleton for the game object hierarchy '{gameObjectHierarchy.name}':");
            string newAssetPath = EditorUtility.SaveFilePanelInProject(L10n.Tr("Create New Skeleton"), newAssetName, "asset", message);

            if (newAssetPath == "")
                return;

            var asset = CreateInstance<Authoring.Skeleton>();
            asset.PopulateFromGameObjectHierarchy(gameObjectHierarchy);
            asset.CreateAsset(newAssetPath);

            rigAuthoring.Skeleton = asset;
        }
    }
}
