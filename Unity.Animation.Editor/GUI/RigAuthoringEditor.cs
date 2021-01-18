using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Unity.Animation.Hybrid;
using UnityEditor.IMGUI.Controls;

namespace Unity.Animation.Authoring.Editor
{
    [CustomEditor(typeof(RigAuthoring))]
    class RigAuthoringEditor : UnityEditor.Editor
    {
        private const string k_SkeletonProperty = "m_Skeleton";
        private const string k_TargetSkeletonRoot = "m_TargetSkeletonRoot";

        private const string k_TransformChannels = "m_TransformChannels";

        static readonly GUIContent  s_EditButton                    = EditorGUIUtility.TrTextContent("Edit");
        static readonly GUIContent  s_FinishButton                  = EditorGUIUtility.TrTextContent("Finish");
        static readonly GUIContent  s_MultipleTargetsNotSupported   = EditorGUIUtility.TrTextContent("Displaying a bone hierarchy for more than one target object is not supported");
        static readonly GUIContent  s_TransformChannels             = EditorGUIUtility.TrTextContent("Transform Channels");
        static readonly GUIContent  s_TargetSkeletonRoot            = EditorGUIUtility.TrTextContent("Target Skeleton Root");


        static GUIStyle s_EmptyStyle;
        static GUIStyle EmptyStyle => s_EmptyStyle ?? (s_EmptyStyle = new GUIStyle());
        static GUIStyle s_SearchFieldStyle;
        static GUIStyle SearchFieldStyle => s_SearchFieldStyle ?? (s_SearchFieldStyle = "SearchTextField");

        static readonly string  kNoBonesToDisplay   = L10n.Tr($"No (active) bones available in this Skeleton");
        static readonly string  kUpdateBones        = L10n.Tr("Update Bones");

        private SerializedProperty m_SkeletonProperty;
        private SerializedProperty m_TargetSkeletonRoot;

        private SerializedObject m_SkeletonObject;
        private SerializedProperty m_TransformChannelsProperty;

        private bool m_NoSkeletonFoldout = false;

        SearchField m_SearchField;
        BoneMappingTreeView m_TreeView;
        [SerializeField] BoneMappingTreeViewState m_TreeViewState;

        public void OnEnable()
        {
            m_SkeletonProperty = serializedObject.FindProperty(k_SkeletonProperty);

            m_TargetSkeletonRoot = serializedObject.FindProperty(k_TargetSkeletonRoot);

            if (m_TreeViewState == null) m_TreeViewState = new BoneMappingTreeViewState();
            m_TreeView = new BoneMappingTreeView(m_TreeViewState);
            if (targets.Length == 1)
            {
                var rigAuthoring = target as RigAuthoring;
                var skeleton = rigAuthoring.Skeleton;

                if (skeleton != null)
                {
                    m_SkeletonObject = new SerializedObject(skeleton);
                    m_TransformChannelsProperty = m_SkeletonObject.FindProperty(k_TransformChannels);
                }

                m_TreeView.EditMode = false;
                m_TreeView.RigAuthoring = rigAuthoring;
                m_TreeView.Reload();
                m_TreeView.ExpandAll();
            }

            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        public void UndoRedoPerformed()
        {
            m_TreeView.Reload();
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rigAuthoring = target as RigAuthoring;

            EditorGUI.BeginChangeCheck();
            {
                EditorGUI.BeginChangeCheck();
                {
                    EditorGUILayout.PropertyField(m_SkeletonProperty);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();

                    Undo.RecordObject(rigAuthoring, kUpdateBones);
                    rigAuthoring.ClearAllManualOverrides();
                }

                EditorGUI.BeginChangeCheck();

                var rect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(rect, s_TargetSkeletonRoot, m_TargetSkeletonRoot);
                var newTargetSkeletonRoot = EditorGUI.ObjectField(rect, s_TargetSkeletonRoot, m_TargetSkeletonRoot.objectReferenceValue, typeof(Transform), true) as Transform;
                if (EditorGUI.EndChangeCheck())
                {
                    if (newTargetSkeletonRoot == null || newTargetSkeletonRoot.IsChildOf(rigAuthoring.transform))
                    {
                        Undo.RecordObject(rigAuthoring, kUpdateBones);
                        rigAuthoring.TargetSkeletonRoot = newTargetSkeletonRoot;
                    }
                }
                EditorGUI.EndProperty();
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                var skeleton = m_SkeletonProperty.objectReferenceValue as Skeleton;
                if (skeleton != null)
                {
                    m_SkeletonObject = new SerializedObject(skeleton);
                    m_TransformChannelsProperty = m_SkeletonObject.FindProperty(k_TransformChannels);
                }
                else
                {
                    m_SkeletonObject = null;
                    m_TransformChannelsProperty = null;
                }

                m_TreeView.RigAuthoring = rigAuthoring;
                m_TreeView.Reload();
                m_TreeView.ExpandAll();
            }

            EditorGUI.BeginChangeCheck();
            {
                var skeleton = m_SkeletonObject?.targetObject as Skeleton;
                ShowTransformChannelHierarchy(skeleton, m_TransformChannelsProperty);
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                Undo.RecordObject(rigAuthoring, kUpdateBones);

                // This ensures our tree view updates properly when we change the rootbone override
                m_TreeView.Reload();
            }
        }

        void ShowTransformChannelHierarchy(Skeleton skeleton, SerializedProperty transformChannelsProperty)
        {
            if (skeleton == null)
            {
                m_NoSkeletonFoldout = EditorGUILayout.Foldout(m_NoSkeletonFoldout, s_TransformChannels);
                if (m_NoSkeletonFoldout)
                {
                    EditorGUILayout.HelpBox(kNoBonesToDisplay, MessageType.Warning);
                }
                return;
            }

            EditorGUILayout.PropertyField(transformChannelsProperty, s_TransformChannels, false);
            if (!transformChannelsProperty.isExpanded)
                return;

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox(s_MultipleTargetsNotSupported);
                return;
            }

            // Only enable turning on editmode when the rigComponent is actually editable
            bool editMode = false;
            if (transformChannelsProperty.editable)
            {
                if (GUILayout.Button(m_TreeView.EditMode ? s_FinishButton : s_EditButton))
                {
                    m_TreeView.EditMode = !m_TreeView.EditMode;
                    m_TreeView.Reload();
                    GUIUtility.ExitGUI();
                }
                editMode = m_TreeView.EditMode;
            }
            else
                m_TreeView.EditMode = false;

            // Only show the root bone in edit mode, but hide it when we only have 1 bone to show,
            // UNLESS we have the rootBone set to a value, in which case show it again
            var rootChannel = skeleton.Root;
            if (editMode)
            {
                using (var disabledScope = new EditorGUI.DisabledScope(!m_TreeView.HaveMoreThan1Child && (rootChannel == TransformBindingID.Root || rootChannel == TransformBindingID.Invalid)))
                {
                    EditorGUI.BeginChangeCheck();
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        m_TreeView.Reload();
                        m_TreeView.ExpandAll();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            // Do not bother showing the search field and Check/UnCheck all buttons when we have less than two bones
            if (m_TreeView.HaveMoreThan1Child)
            {
                // Search is always shown when we have more than 1 bone
                var searchRect = GUILayoutUtility.GetRect(0, 10000, 0, EditorGUIUtility.singleLineHeight);
                if (string.IsNullOrWhiteSpace(m_TreeView.searchString))
                    m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString, SearchFieldStyle, EmptyStyle, EmptyStyle);
                else
                    m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString);
            }
            else
            {
                m_TreeView.searchString = string.Empty;
            }

            // Only show the tree when we have any bones to display
            if (!m_TreeView.IsEmpty)
            {
                var treeRect = GUILayoutUtility.GetRect(0, 100000, 0, m_TreeView.totalHeight);
                m_TreeView.OnGUI(treeRect);
            }
            else // otherwise, show a warning message instead
                EditorGUILayout.HelpBox(kNoBonesToDisplay, MessageType.Warning);
        }

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
