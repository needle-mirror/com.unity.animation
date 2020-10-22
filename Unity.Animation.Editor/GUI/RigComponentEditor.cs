using UnityEngine;
using UnityEditor;
using Unity.Animation.Hybrid;
using UnityEditor.IMGUI.Controls;

namespace Unity.Animation.Editor
{
    [CustomEditor(typeof(RigComponent))]
    [CanEditMultipleObjects]
    class RigComponentEditor : UnityEditor.Editor
    {
        SerializedProperty skeletonRootBoneProp;
        SerializedProperty bonesProp;
        SerializedProperty excludeBonesProp;
        SerializedProperty invalidBonesProp;

        SerializedProperty translationChannelsProp;
        SerializedProperty rotationChannelsProp;
        SerializedProperty scaleChannelsProp;
        SerializedProperty floatChannelsProp;
        SerializedProperty intChannelsProp;

        public void OnEnable()
        {
            skeletonRootBoneProp    = serializedObject.FindProperty("m_SkeletonRootBone");
            invalidBonesProp        = serializedObject.FindProperty("m_InvalidBones");
            bonesProp               = serializedObject.FindProperty(nameof(RigComponent.Bones));
            excludeBonesProp        = serializedObject.FindProperty("m_ExcludeBones");
            translationChannelsProp = serializedObject.FindProperty(nameof(RigComponent.TranslationChannels));
            rotationChannelsProp    = serializedObject.FindProperty(nameof(RigComponent.RotationChannels));
            scaleChannelsProp       = serializedObject.FindProperty(nameof(RigComponent.ScaleChannels));
            floatChannelsProp       = serializedObject.FindProperty(nameof(RigComponent.FloatChannels));
            intChannelsProp         = serializedObject.FindProperty(nameof(RigComponent.IntChannels));


            if (m_TreeViewState == null) m_TreeViewState = new TreeViewState();
            m_TreeView = new RigBoneTreeView(m_TreeViewState);
            if (targets.Length == 1)
            {
                m_TreeView.EditMode = false;
                m_TreeView.RigComponent = target as RigComponent;
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

        static readonly GUIContent  s_SkeletonRootBone              = EditorGUIUtility.TrTextContent("Skeleton Root Bone", "This sets the top most root bone of the skeleton. This transform must be below the RigComponent in the hierarchy. When empty the transform of the RigComponent is used as the root");
        static readonly GUIContent  s_EditButton                    = EditorGUIUtility.TrTextContent("Edit");
        static readonly GUIContent  s_FinishButton                  = EditorGUIUtility.TrTextContent("Finish");
        static readonly GUIContent  s_MultipleTargetsNotSupported   = EditorGUIUtility.TrTextContent("Displaying a bone hierarchy for more than one target object is not supported");
        static readonly GUIContent  s_RemoveMessage                 = EditorGUIUtility.TrTextContent("Remove this message");
        static readonly GUIContent  s_CheckAll                      = EditorGUIUtility.TrTextContent("Check All");
        static readonly GUIContent  s_UncheckAll                    = EditorGUIUtility.TrTextContent("Uncheck All");


        SearchField m_SearchField;
        RigBoneTreeView m_TreeView;
        [SerializeField] TreeViewState m_TreeViewState;

        static readonly string      kNoBonesToDisplay                           = L10n.Tr($"No bones have been selected");
        static readonly string      kSkeletonRootBoneChanged                    = L10n.Tr("Skeleton Root Bone Changed");
        static readonly string      kSkeletonRootBoneMustBeChildOfRigComponent  = L10n.Tr("A Skeleton Root Bone must be a child of, or the transform of this Rig Component");
        static readonly string      kCheckAllUndo                               = L10n.Tr("Added all bones to RigComponent");
        static readonly string      kUnCheckAllUndo                             = L10n.Tr("Removed all bones from RigComponent");
        static readonly string      kInvalidOrNullTransforms                    = L10n.Tr("{0} empty or missing transforms were found and have been removed.");
        static readonly int         kInvalidBoneFieldHash                       = "s_InvalidBoneFieldHash".GetHashCode();

        static GUIContent s_InvalidBones;
        static GUIContent invalidBones => s_InvalidBones ?? (s_InvalidBones = new GUIContent(L10n.Tr("This RigComponent contained invalid bones that have been removed"), EditorGUIUtility.IconContent("console.warnicon").image, null));

        static GUIContent s_TransformContent;
        static GUIContent transformContent => s_TransformContent ?? (s_TransformContent = new GUIContent(string.Empty, AssetPreview.GetMiniTypeThumbnail(typeof(Transform))));

        static GUIStyle s_SearchFieldStyle;
        static GUIStyle SearchFieldStyle => s_SearchFieldStyle ?? (s_SearchFieldStyle = "SearchTextField");


        void ReadOnlyTransformField(Rect position, Transform actualTargetObject)
        {
            var mouseOver   = position.Contains(Event.current.mousePosition);
            int id          = GUIUtility.GetControlID(kInvalidBoneFieldHash, FocusType.Keyboard, position);
            var evt         = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                {
                    var content = transformContent;
                    content.text = actualTargetObject.name;
                    EditorStyles.objectField.Draw(position, content, id, DragAndDrop.activeControlID == id, mouseOver);
                    break;
                }
                case EventType.MouseDrag:
                {
                    if (!mouseOver)
                        break;
                    if (EditorGUI.showMixedValue)
                        break;
                    DragAndDrop.StartDrag("Dragging Bone Transform");
                    evt.Use();
                    break;
                }
                case EventType.MouseDown:
                {
                    // Ignore right clicks
                    if (Event.current.button != 0)
                        break;
                    if (!mouseOver)
                        break;

                    // One click shows where the referenced object is
                    if (Event.current.clickCount == 1)
                    {
                        GUIUtility.keyboardControl = id;

                        // ping object
                        bool anyModifiersPressed = evt.shift || evt.control || evt.alt || evt.command;
                        if (!anyModifiersPressed)
                            EditorGUIUtility.PingObject(actualTargetObject);
                        evt.Use();
                    }

                    if (actualTargetObject)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new[] { actualTargetObject };
                        evt.Use();
                    }
                    break;
                }
            }
        }

        void ShowInvalidBones(SerializedProperty invalidBonesProp, Transform rootBone)
        {
            if (invalidBonesProp.arraySize == 0)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(GUIContent.none, invalidBones, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();
            int invalidOrNullTransforms = 0;
            for (int index = 0; index < invalidBonesProp.arraySize; index++)
            {
                var property    = invalidBonesProp.GetArrayElementAtIndex(index);
                var transform   = property.objectReferenceValue as Transform;
                if (!transform)
                {
                    invalidOrNullTransforms++;
                    continue;
                }

                // Check if the transform's location has changed and is now a child of the RigComponent,
                // in which case ignore it, but don't remove it in case the movement is undone.
                if (transform.IsChildOf(rootBone))
                    continue; // TODO: add it back to the bones list? would potentially need to modify rootbone & check for consistency ..

                var position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                ReadOnlyTransformField(position, transform);
            }
            if (invalidOrNullTransforms > 0)
            {
                EditorGUILayout.LabelField(string.Format(kInvalidOrNullTransforms, invalidOrNullTransforms));
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(s_RemoveMessage))
            {
                invalidBonesProp.ClearArray();
                invalidBonesProp.serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndVertical();
        }

        void ShowEditSkeletonRootBone(RigComponent rigComponent, SerializedProperty skeletonRootBoneProp)
        {
            EditorGUI.BeginChangeCheck();
            var prevValue = skeletonRootBoneProp.objectReferenceValue;
            // TODO: replace this with BoneField when it's done
            EditorGUILayout.PropertyField(skeletonRootBoneProp, s_SkeletonRootBone, false);
            if (EditorGUI.EndChangeCheck())
            {
                var newSkeletonRootBone = skeletonRootBoneProp.objectReferenceValue as Transform;
                if (newSkeletonRootBone && !newSkeletonRootBone.IsChildOf(rigComponent.transform))
                {
                    Debug.LogError(kSkeletonRootBoneMustBeChildOfRigComponent);
                    skeletonRootBoneProp.objectReferenceValue = prevValue;
                }
                else
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObjects(targets, kSkeletonRootBoneChanged);
                    serializedObject.UpdateIfRequiredOrScript();
                    m_TreeView.Reload();
                    m_TreeView.ExpandAll();
                    GUIUtility.ExitGUI();
                }
            }
        }

        void ShowTransformHierarchy(RigComponent rigComponent, SerializedProperty bonesProp, SerializedProperty excludeBonesProp, SerializedProperty invalidBonesProp, SerializedProperty skeletonRootBoneProp)
        {
            EditorGUILayout.PropertyField(bonesProp, false);
            if (!bonesProp.isExpanded)
                return;

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox(s_MultipleTargetsNotSupported);
                return;
            }

            var rootBone = rigComponent.RootBone;

            // Only enable turning on editmode when the rigComponent is actually editable
            if (bonesProp.editable)
            {
                if (GUILayout.Button(m_TreeView.EditMode ? s_FinishButton : s_EditButton))
                {
                    m_TreeView.EditMode = !m_TreeView.EditMode;
                    m_TreeView.Reload();
                    GUIUtility.ExitGUI();
                }
            }
            else
                m_TreeView.EditMode = false;

            int     numberOfBonesToDisplay          = m_TreeView.NodeCount;

            ShowInvalidBones(invalidBonesProp, rootBone);

            // If we're not in edit mode and do not have any bones to display, show a warning message instead
            // to make sure the user is aware that no bones have been selected
            // Note: we will always have at least 1 bone (the rigComponent transform) in edit mode
            if (numberOfBonesToDisplay == 0)
            {
                EditorGUILayout.HelpBox(kNoBonesToDisplay, MessageType.Warning);
                return;
            }

            // Only show the root bone in edit mode, but hide it when we only have 1 bone to show,
            // UNLESS we have the rootBone set to a value, in which case show it again
            if (m_TreeView.EditMode &&
                (numberOfBonesToDisplay > 1 || rootBone != null))
                ShowEditSkeletonRootBone(rigComponent, skeletonRootBoneProp);

            // Do not bother showing the search field and Check/UnCheck all buttons when we have less than two bones
            if (numberOfBonesToDisplay > 1)
            {
                // Search is always shown when we have more than 1 bone
                var searchRect = GUILayoutUtility.GetRect(0, 10000, 0, EditorGUIUtility.singleLineHeight);
                if (string.IsNullOrWhiteSpace(m_TreeView.searchString))
                    m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString, SearchFieldStyle, GUIStyle.none, GUIStyle.none);
                else
                    m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString);

                // Check/uncheck buttons only make sense in edit mode
                if (m_TreeView.EditMode)
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
            var treeRect = GUILayoutUtility.GetRect(0, 100000, 0, m_TreeView.totalHeight);
            m_TreeView.OnGUI(treeRect);
        }

        void CheckAll()
        {
            Undo.RecordObjects(targets, kCheckAllUndo);
            foreach (var target in targets)
            {
                var rigComponent = target as RigComponent;
                rigComponent.IncludeBoneAndDescendants(rigComponent.RootBone);
            }
        }

        void UnCheckAll()
        {
            Undo.RecordObjects(targets, kUnCheckAllUndo);
            foreach (var target in targets)
            {
                var rigComponent = target as RigComponent;
                rigComponent.ExcludeBoneAndDescendants(rigComponent.RootBone);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            {
                ShowTransformHierarchy(target as RigComponent, bonesProp, excludeBonesProp, invalidBonesProp, skeletonRootBoneProp);
                EditorGUILayout.PropertyField(translationChannelsProp, true);
                EditorGUILayout.PropertyField(rotationChannelsProp, true);
                EditorGUILayout.PropertyField(scaleChannelsProp, true);
                EditorGUILayout.PropertyField(floatChannelsProp, true);
                EditorGUILayout.PropertyField(intChannelsProp, true);
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                // This ensures our tree view updates properly when we change the rootbone override
                m_TreeView.Reload();
            }
        }
    }
}
