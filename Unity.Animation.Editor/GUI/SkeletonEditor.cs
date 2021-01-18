using System.Collections.Generic;
using System.IO;
using Unity.Animation.Hybrid;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Animation.Authoring.Editor
{
    [CustomEditor(typeof(Skeleton))]
    class SkeletonEditor : UnityEditor.Editor
    {
        private const string k_InspectorRoot = "m_Root";
        private const string k_TransformChannels = "m_TransformChannels";
        private const string k_IntChannels = "m_IntChannels";
        private const string k_FloatChannels = "m_FloatChannels";
        private const string k_QuaternionChannels = "m_QuaternionChannels";

        private SerializedProperty m_SkeletonRootChannelProperty;
        private SerializedProperty m_TransformChannelsProperty;
        // Temporary -- for debugging purposes.
        private SerializedProperty m_IntChannelsProperty;
        private SerializedProperty m_FloatChannelsProperty;
        private SerializedProperty m_QuaternionChannelsProperty;

        private static HashSet<SkeletonEditor> s_ActiveSkeletonEditors = new HashSet<SkeletonEditor>();

        public void OnEnable()
        {
            m_SkeletonRootChannelProperty = serializedObject.FindProperty(k_InspectorRoot);
            m_TransformChannelsProperty = serializedObject.FindProperty(k_TransformChannels);
            m_IntChannelsProperty = serializedObject.FindProperty(k_IntChannels);
            m_FloatChannelsProperty = serializedObject.FindProperty(k_FloatChannels);
            m_QuaternionChannelsProperty = serializedObject.FindProperty(k_QuaternionChannels);

            if (m_TreeViewState == null) m_TreeViewState = new TreeViewState();
            m_TreeView = new SkeletonTreeView(m_TreeViewState);
            if (targets.Length == 1)
            {
                m_TreeView.EditMode = false;
                m_TreeView.Skeleton = target as Skeleton;
                m_TreeView.Reload();
                m_TreeView.ExpandAll();
            }

            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            s_ActiveSkeletonEditors.Add(this);
        }

        public void OnDestroy()
        {
            s_ActiveSkeletonEditors.Remove(this);
        }

        public void OnDisable()
        {
            s_ActiveSkeletonEditors.Remove(this);
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        public void UndoRedoPerformed()
        {
            m_TreeView.Reload();
            Repaint();
        }

        static readonly GUIContent  s_SkeletonRootBone              = EditorGUIUtility.TrTextContent("Skeleton Root Bone", "This sets the top most root bone of the skeleton. This transform must be below the RigComponent in the hierarchy. When empty the transform of the RigComponent is used as the root");
        static readonly GUIContent  s_EditButton                    = EditorGUIUtility.TrTextContent("Edit");
        static readonly GUIContent  s_FinishButton                  = EditorGUIUtility.TrTextContent("Finish");
        static readonly GUIContent  s_MultipleTargetsNotSupported   = EditorGUIUtility.TrTextContent("Displaying a bone hierarchy for more than one target object is not supported");
        static readonly GUIContent  s_CheckAll                      = EditorGUIUtility.TrTextContent("Check All");
        static readonly GUIContent  s_UncheckAll                    = EditorGUIUtility.TrTextContent("Uncheck All");

        SearchField m_SearchField;
        SkeletonTreeView m_TreeView;
        [SerializeField] TreeViewState m_TreeViewState;

        static readonly string  kNoBonesToDisplay   = L10n.Tr($"No (active) bones available in this Skeleton");
        static readonly string  kCheckAllUndo       = L10n.Tr("Added all bones to RigComponent");
        static readonly string  kUnCheckAllUndo     = L10n.Tr("Removed all bones from RigComponent");

        static GUIStyle s_SearchFieldStyle;
        static GUIStyle SearchFieldStyle => s_SearchFieldStyle ?? (s_SearchFieldStyle = "SearchTextField");

        void ShowTransformChannelHierarchy(Skeleton skeleton, SerializedProperty transformChannelsProperty, SerializedProperty skeletonRootChannelProperty)
        {
            EditorGUILayout.PropertyField(transformChannelsProperty, false);
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
                    AnimationGUILayout.BoneField(skeletonRootChannelProperty, skeleton, s_SkeletonRootBone);
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
                    m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString, SearchFieldStyle, GUIStyle.none, GUIStyle.none);
                else
                    m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString);

                // Check/uncheck buttons only make sense in edit mode
                if (editMode)
                {
                    var boxRect = GUILayoutUtility.GetRect(0, 100000, 0, EditorGUIUtility.singleLineHeight);
                    GUI.Box(boxRect, GUIContent.none);
                    var leftButtonRect = boxRect;
                    var rightButtonRect = boxRect;
                    leftButtonRect.width /= 2;
                    rightButtonRect.xMin = leftButtonRect.xMax;
                    if (GUI.Button(leftButtonRect, s_CheckAll))
                    {
                        CheckAll();
                        // This ensures our tree view updates properly when we change the rootbone override
                        m_TreeView.Reload();
                    }
                    if (GUI.Button(rightButtonRect, s_UncheckAll))
                    {
                        UnCheckAll();
                        // This ensures our tree view updates properly when we change the rootbone override
                        m_TreeView.Reload();
                    }
                }
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

        void CheckAll()
        {
            Undo.RecordObjects(targets, kCheckAllUndo);
            var skeleton = target as Skeleton;
            if (!string.IsNullOrEmpty(m_TreeView.searchString))
            {
                foreach (var item in m_TreeView.GetRows())
                {
                    var transformBindingID = m_TreeView.GetTransformBindingID(item.id);
                    if (transformBindingID == TransformBindingID.Invalid)
                        continue;
                    skeleton.SetTransformChannelAncestorsToActive(transformBindingID);
                }
            }
            else
            {
                var root = skeleton.Root;
                if (root == TransformBindingID.Invalid)
                    root = TransformBindingID.Root;
                skeleton.SetTransformChannelDescendantsAndAncestorsToActive(root);
            }
            m_TreeView.Reload();
        }

        void UnCheckAll()
        {
            Undo.RecordObjects(targets, kUnCheckAllUndo);
            var skeleton = target as Skeleton;
            if (!string.IsNullOrEmpty(m_TreeView.searchString))
            {
                foreach (var item in m_TreeView.GetRows())
                {
                    var transformBindingID = m_TreeView.GetTransformBindingID(item.id);
                    if (transformBindingID == TransformBindingID.Invalid)
                        continue;
                    skeleton.SetTransformChannelDescendantsToInactive(transformBindingID, includeSelf: true);
                }
            }
            else
            {
                var root = skeleton.Root;
                if (root == TransformBindingID.Invalid)
                    root = TransformBindingID.Root;
                skeleton.SetTransformChannelDescendantsToInactive(root, includeSelf: true);
            }
            m_TreeView.Reload();
        }

        // Right now, inspector is limited to barebone view of data being referenced by Skeleton.
        // It should:
        // - Have a similar view over data as the one provided in the revamped RigComponent.
        // - Group transforms and associated properties together in a tree like structure.
        // - Highlight invalid properties and easily allow to add generic properties.
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            {
                ShowTransformChannelHierarchy(target as Skeleton, m_TransformChannelsProperty, m_SkeletonRootChannelProperty);
                EditorGUILayout.PropertyField(m_IntChannelsProperty);
                EditorGUILayout.PropertyField(m_FloatChannelsProperty);
                EditorGUILayout.PropertyField(m_QuaternionChannelsProperty);
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                // This ensures our tree view updates properly when we change the rootbone override
                m_TreeView.Reload();
            }
        }

        [MenuItem("Assets/Create/DOTS/Animation/Skeleton From Prefab", false, 2)]
        private static void OnCreateSkeletonAssetFromPrefab(MenuCommand command)
        {
            var guids = Selection.assetGUIDs;
            if (guids == null || guids.Length == 0)
                return;

            var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);

            var prefabHierarchy = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabHierarchy == null)
                return;

            string newAssetDirectory = Path.GetDirectoryName(assetPath);
            string newAssetName = Path.GetFileNameWithoutExtension(assetPath);

            string message = string.Format(L10n.Tr("Create a new skeleton for the game object hierarchy '{0}':"), Path.GetFileName(assetPath));
            string newAssetPath = EditorUtility.SaveFilePanelInProject(L10n.Tr("Create New Skeleton"), newAssetName, "asset", message, newAssetDirectory);

            if (newAssetPath == "")
                return;

            var asset = CreateInstance<Skeleton>();
            asset.PopulateFromGameObjectHierarchy(prefabHierarchy);
            asset.CreateAsset(newAssetPath);
            Selection.activeObject = asset;
        }

        internal static void SelectAndFrame(SkeletonBoneReference skeletonBoneReference)
        {
            var skeleton = skeletonBoneReference.Skeleton;
            if (skeleton == null)
                return;

            Selection.activeObject = skeletonBoneReference.Skeleton;
            EditorApplication.delayCall += () => FrameInternal(skeletonBoneReference);
        }

        static void FrameInternal(SkeletonBoneReference skeletonBoneReference)
        {
            var skeleton = skeletonBoneReference.Skeleton;
            if (skeleton == null)
                return;

            foreach (var editor in s_ActiveSkeletonEditors)
            {
                if (!editor)
                    continue;
                editor.Frame(skeletonBoneReference);
            }
        }

        internal void Frame(SkeletonBoneReference skeletonBoneReference)
        {
            var skeleton = skeletonBoneReference.Skeleton;
            if (skeleton != target as Skeleton)
                return;

            m_TreeView.FrameItem(skeletonBoneReference.ID);
            m_TreeView.SetSelection(skeletonBoneReference.ID);

            // TODO: find a way to move scrollbar in the right position
            //var itemPosition = m_TreeView.GetItemPosition(skeletonBoneReference.ID);
            Repaint();
        }

        [MenuItem("Assets/Create/DOTS/Animation/Skeleton From Prefab", true)]
        private static bool OnCreateSkeletonAssetFromPrefabValidate(MenuCommand command)
        {
            var guids = Selection.assetGUIDs;
            if (guids == null || guids.Length == 0)
                return false;

            var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

            return assetType == typeof(GameObject);
        }
    }
}
