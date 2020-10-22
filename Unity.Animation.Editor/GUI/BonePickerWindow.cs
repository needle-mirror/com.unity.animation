using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JetBrains.Annotations;
using UnityEditor.IMGUI.Controls;

namespace Unity.Animation.Authoring.Editor
{
    class BonePickerWindow : EditorWindow
    {
        public const string BonePickerWindowClosedCommand       = "BonePickerWindowClosed";
        public const string BonePickerWindowUpdatedCommand      = "BonePickerWindowUpdated";
        public const string BonePickerWindowCancelledCommand    = "BonePickerWindowCancelled";

        [SerializeField] SkeletonBoneReference reference;
        [SerializeField] SkeletonBoneReference originalValue;

        EditorWindow msgDestination;
        int msgControlID;

        [SerializeField] bool haveClosedPicker;
        [SerializeField] bool showSkeletonSelection;
        [SerializeField] SearchField m_SearchField;
        BoneSelectionTreeView m_TreeView;
        [SerializeField] TreeViewState m_TreeViewState;
        Rect buttonRect;

        static BonePickerWindow current;
        public static int GetControlID()
        {
            if (current == null)
                return 0;
            return current.msgControlID;
        }

        public static bool GetValue(int id, out SkeletonBoneReference reference)
        {
            reference = default;
            if (current == null ||
                current.msgControlID != id)
                return false;
            reference = current.reference;
            return true;
        }

        public static BonePickerWindow TogglePicker(Rect buttonRect, SkeletonBoneReference reference, string searchFilter, EditorWindow msgDestination, bool showSkeletonSelection, int controlID)
        {
            if (reference.Skeleton == null && !showSkeletonSelection)
                return null;


            // If the exact same picker we're trying to open is already open, we close it instead
            if (current != null &&
                current.msgControlID == controlID)
            {
                current.Close();
                return null;
            }


            var selector = ScriptableObject.CreateInstance<BonePickerWindow>();
            selector.msgControlID = controlID;
            selector.msgDestination = msgDestination;
            selector.showSkeletonSelection = showSkeletonSelection;
            selector.Init(reference, searchFilter);
            selector.buttonRect = GUIUtility.GUIToScreenRect(buttonRect);
            selector.haveClosedPicker = false;

            EditorApplication.delayCall += () => selector.PresentWindow();
            return selector;
        }

        Vector2 CalcWindowSize()
        {
            return new Vector2(
                Mathf.Clamp(buttonRect.width, 400, 800),
                Mathf.Clamp(m_TreeView.totalHeight, 300, 600));
        }

        void PresentWindow()
        {
            ShowAsDropDown(buttonRect, CalcWindowSize());
        }

        [UsedImplicitly]
        private void OnEnable()
        {
            hideFlags = HideFlags.DontSave;

            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void Init(SkeletonBoneReference reference, string searchFilter)
        {
            this.reference = reference;
            this.originalValue = reference;

            current = this;

            if (m_TreeViewState == null) m_TreeViewState = new TreeViewState();
            m_TreeView = new BoneSelectionTreeView(m_TreeViewState);
            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

            m_TreeView.searchString = searchFilter;
            m_TreeView.SelectionHasChanged += TreeViewSelectionChanged;
            m_TreeView.DoubleClicked += Close;
            Refresh();
        }

        void SendModifiedValueCommand()
        {
            if (msgDestination != null)
                msgDestination.SendEvent(EditorGUIUtility.CommandEvent(BonePickerWindowUpdatedCommand));
        }

        void SendCancelCommand()
        {
            if (msgDestination != null)
                msgDestination.SendEvent(EditorGUIUtility.CommandEvent(BonePickerWindowCancelledCommand));
        }

        void TreeViewSelectionChanged(SkeletonBoneReference newReference)
        {
            if (newReference.ID == TransformBindingID.Invalid)
                return;
            Undo.RecordObject(this, "Changed Bone");
            reference = newReference;
            SendModifiedValueCommand();
        }

        [UsedImplicitly]
        public void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        private void OnDestroy()
        {
            if (msgDestination != null)
                msgDestination.SendEvent(EditorGUIUtility.CommandEvent(BonePickerWindowClosedCommand));
            if (current == this)
                current = null;
        }

        public void UndoRedoPerformed()
        {
            Refresh();
            if (msgDestination != null) msgDestination.Repaint();
            // Resize the dropdown
            PresentWindow();
        }

        public void Refresh()
        {
            if (m_TreeView != null)
            {
                m_TreeView.Skeleton = reference.Skeleton;
                m_TreeView.Reload();
                m_TreeView.ExpandAll();
                m_TreeView.Select(reference.ID);
            }
        }

        static GUIStyle s_SearchFieldStyle;
        static GUIStyle SearchFieldStyle => s_SearchFieldStyle ?? (s_SearchFieldStyle = "SearchTextField");

        static readonly string      s_PleaseSelectSkeleton  = L10n.Tr("Please select a skeleton to select bones from");
        static readonly GUIContent  s_SkeletonField         = EditorGUIUtility.TrTextContent("Skeleton");
        static readonly Type        s_SkeletonType          = typeof(Skeleton);

        static readonly int         kBonePickerWindowHash   = nameof(BonePickerWindow).GetHashCode();

        private void OnGUI()
        {
            var pos = position;
            pos.x = 0;
            pos.y = 0;
            GUI.Box(pos, GUIContent.none, EditorStyles.helpBox);

            // vertical space of 2
            GUILayoutUtility.GetRect(0, 10000, 0, 2);

            if (showSkeletonSelection)
            {
                var objectRect = GUILayoutUtility.GetRect(0, 10000, 0, EditorGUIUtility.singleLineHeight + 2);
                objectRect.xMin += 3;
                objectRect.xMax -= 3;
                objectRect.yMin += 1;
                objectRect.yMax -= 1;
                EditorGUI.BeginChangeCheck();
                var prevLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 60;
                Skeleton newSkeleton = reference.Skeleton;
                try
                {
                    newSkeleton = EditorGUI.ObjectField(objectRect, s_SkeletonField, reference.Skeleton, s_SkeletonType, true) as Skeleton;
                }
                finally { EditorGUIUtility.labelWidth = prevLabelWidth; }
                if (EditorGUI.EndChangeCheck() && reference.Skeleton != newSkeleton)
                {
                    Undo.RecordObject(this, "Changed Skeleton");
                    reference.Skeleton = newSkeleton;
                    Refresh();
                    // Resize the dropdown
                    PresentWindow();
                }
            }
            var foundSkeleton = reference.Skeleton != null;
            if (!foundSkeleton)
            {
                EditorGUILayout.HelpBox(s_PleaseSelectSkeleton, MessageType.Warning);
                var pickerWindowID = EditorGUIUtility.GetControlID(kBonePickerWindowHash, FocusType.Passive);
                if (!haveClosedPicker)
                {
                    EditorGUIUtility.ShowObjectPicker<Skeleton>(null, true, string.Empty, pickerWindowID);
                    haveClosedPicker = true;
                }
                if (Event.current.commandName == "ObjectSelectorUpdated" & EditorGUIUtility.GetObjectPickerControlID() == pickerWindowID)
                {
                    var newSkeleton = EditorGUIUtility.GetObjectPickerObject() as Skeleton;
                    if (newSkeleton != null && reference.Skeleton != newSkeleton)
                    {
                        Undo.RecordObject(this, "Changed Skeleton");
                        reference.Skeleton = newSkeleton;
                        Refresh();
                        // Resize the dropdown
                        PresentWindow();
                    }
                }
            }
            else
                haveClosedPicker = true;

            if (foundSkeleton)
            {
                var searchRect = GUILayoutUtility.GetRect(0, 10000, 0, EditorGUIUtility.singleLineHeight + 2);
                searchRect.xMin += 3;
                searchRect.xMax -= 3;
                searchRect.yMin += 2;
                if (string.IsNullOrWhiteSpace(m_TreeView.searchString) || foundSkeleton)
                    m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString, SearchFieldStyle, GUIStyle.none, GUIStyle.none);
                else
                    m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString);

                // space of 2
                GUILayoutUtility.GetRect(0, 10000, 0, 2);

                var treeRect = GUILayoutUtility.GetRect(0, 100000, 0, position.height);
                m_TreeView.OnGUI(treeRect);
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                reference = originalValue;
                SendModifiedValueCommand();
                SendCancelCommand();
                Close();
            }
        }
    }
}
