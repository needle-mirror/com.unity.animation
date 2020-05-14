using UnityEditor;
using UnityEngine;

namespace Unity.Animation.Editor
{
    public static class CreateSkeletonMenu
    {
        [MenuItem("Animation/Rig/Setup Rig Transforms")]
        public static void FillRigBones()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                EditorUtility.DisplayDialog("Error", "No GameObject selected.", "OK");
                return;
            }

            var rigComponent = go.GetComponent<Hybrid.RigComponent>();
            if (rigComponent == null)
            {
                Undo.RecordObject(rigComponent, "Add RigComponent");
                rigComponent = go.AddComponent<Hybrid.RigComponent>();
            }

            if (rigComponent != null)
            {
                Undo.RecordObject(rigComponent, "Setup Rig Transforms");

                if (rigComponent.Bones != null && rigComponent.Bones.Length >= 1 && rigComponent.Bones[0] != null)
                    go = rigComponent.Bones[0].gameObject;
                rigComponent.Bones = go.GetComponentsInChildren<Transform>();
            }
        }
    }
}
