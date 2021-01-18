using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Hybrid;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    [CustomEditor(inspectedType: typeof(AnimationGraph), editorForChildClasses: true)]
    [InitializeOnLoad]
    class AnimationGraphEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var component = target as AnimationGraph;
            BaseAssetModel assetModel = component.Graph as BaseAssetModel;

            var fieldOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.BeginChangeCheck();
            var newAssetModel = EditorGUILayout.ObjectField(
                new GUIContent("Graph Asset"), assetModel, typeof(BaseAssetModel), false) as BaseAssetModel;
            if (EditorGUI.EndChangeCheck())
            {
                var previousGraph = component.Graph;
                component.Graph = newAssetModel;
                component.ExposedObjects.Clear();
                EditorUtility.SetDirty(component);

                if (previousGraph != null)
                {
                    var vseWindows = (GtfoWindow[])Resources.FindObjectsOfTypeAll(typeof(AnimationGraphWindow));
                    foreach (GtfoWindow window in vseWindows)
                    {
                        if ((UnityEngine.Object)window.Store.State.GraphModel.AssetModel == previousGraph &&
                            window.Store.State.WindowState.CurrentGraph.BoundObject == component.gameObject)
                            window.UnloadGraph();
                    }
                }

                if (newAssetModel != null)
                    AssetHelper.OpenAnimationGraphAssetInWindow(AssetDatabase.GetAssetPath(newAssetModel));
            }

            if (newAssetModel)
            {
                var displayOptions = ConversionService.GetPhases().Select(x => x.Description).ToArray();
                var phases = ConversionService.GetPhases();
                var selection = Math.Max(0, phases.FindIndex(t => t.Type.AssemblyQualifiedName == component.PhaseIdentification));

                EditorGUI.BeginChangeCheck();
                selection = EditorGUILayout.Popup("Phase", selection, displayOptions);

                if (EditorGUI.EndChangeCheck())
                {
                    component.PhaseIdentification = phases[selection].Type.AssemblyQualifiedName;
                    EditorUtility.SetDirty(component);
                }

                var context = ((BaseStencil)newAssetModel.GraphModel.Stencil).Context;

                EditorGUI.BeginChangeCheck();
                var newContext =
                    EditorGUILayout.ObjectField(
                        context.ContextType.Name, component.Context, context.GameObjectContextType, allowSceneObjects: true);

                if (EditorGUI.EndChangeCheck())
                {
                    component.Context = newContext as Component;
                    EditorUtility.SetDirty(component);
                }

                component.UpdateBindings();

                component.ShowBindings = EditorGUILayout.Foldout(component.ShowBindings, "Inputs");
                if (component.ShowBindings)
                {
                    EditorGUI.indentLevel++;
                    foreach (var b in (newAssetModel.GraphModel as BaseModel).InputComponentBindings)
                    {
                        if (!AuthoringComponentService.TryGetComponentByRuntimeType(b.Identifier.Type.Resolve(), out var componentInfo))
                            continue;
                        EditorGUI.BeginChangeCheck();
                        var binding = component.Inputs.Where(r => r.Identification == b.Identifier.Type.Identification).SingleOrDefault();
                        if (binding == null)
                            continue;
                        var newBinding =
                            EditorGUILayout.ObjectField(
                                b.Name, binding.Value, componentInfo.AuthoringType, allowSceneObjects: true);

                        if (EditorGUI.EndChangeCheck())
                        {
                            binding.Value = newBinding as Component;
                            EditorUtility.SetDirty(component);
                        }
                    }
                    EditorGUI.indentLevel--;
                }

                component.UpdateObjectBindings();
            }

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

        static AnimationGraphEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemCallback;
        }

        private static void HierarchyWindowItemCallback(int pID, Rect pRect)
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.AcceptDrag();
                if (DragAndDrop.objectReferences.FirstOrDefault(o => o is BaseAssetModel))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                }
            }
            else if (Event.current.type == EventType.DragPerform && pRect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.AcceptDrag();
                GameObject gameObject = EditorUtility.InstanceIDToObject(pID) as GameObject;
                if (gameObject == null)
                    return;

                foreach (var objectRef in DragAndDrop.objectReferences)
                {
                    if (objectRef is BaseAssetModel assetModel)
                    {
                        if (!gameObject.TryGetComponent(out AnimationGraph animGraph))
                            animGraph = gameObject.AddComponent<AnimationGraph>();

                        animGraph.Graph = assetModel;

                        var context = ((BaseStencil)assetModel.GraphModel.Stencil).Context;
                        if (context != null)
                        {
                            gameObject.TryGetComponent(context.GameObjectContextType, out var contextComponent);
                            if (contextComponent != null)
                                animGraph.Context = contextComponent;
                        }

                        Event.current.Use();
                        EditorUtility.SetDirty(gameObject);
                    }
                }
            }
        }
    }
}
