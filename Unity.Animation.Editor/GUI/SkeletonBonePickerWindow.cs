using System;
using Unity.Animation.Hybrid;
using UnityEngine;
using UnityEditor;
using JetBrains.Annotations;
using UnityEditor.IMGUI.Controls;

namespace Unity.Animation.Authoring.Editor
{
    class SkeletonBonePickerWindow : EditorWindow
    {
        public const string WindowClosedCommand       = "SkeletonBonePickerWindowClosed";
        public const string WindowUpdatedCommand      = "SkeletonBonePickerWindowUpdated";
        public const string WindowCancelledCommand    = "SkeletonBonePickerWindowCancelled";

        [SerializeField] SkeletonBoneReference reference;
        [SerializeField] SkeletonBoneReference originalValue;

        EditorWindow msgDestination;
        int msgControlID;

        bool needToOpenPicker;
        bool pickerWasOpen;
        [SerializeField] bool showSkeletonSelection;
        [SerializeField] SearchField m_SearchField;

        SkeletonBoneSelectionTreeView m_TreeView;
        [SerializeField] TreeViewState m_TreeViewState;
        Rect buttonRect;

        bool focusOnFirstField = false;

        static SkeletonBonePickerWindow current;
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

        public static SkeletonBonePickerWindow TogglePicker(Rect buttonRect, SkeletonBoneReference reference, string searchFilter, EditorWindow msgDestination, bool showSkeletonSelection, int controlID)
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


            var selector = ScriptableObject.CreateInstance<SkeletonBonePickerWindow>();
            selector.msgControlID = controlID;
            selector.msgDestination = msgDestination;
            selector.showSkeletonSelection = showSkeletonSelection;
            selector.Init(reference, searchFilter);
            selector.buttonRect = GUIUtility.GUIToScreenRect(buttonRect);
            selector.needToOpenPicker = true;
            selector.pickerWasOpen = false;

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
            focusOnFirstField = true;

            if (m_TreeViewState == null) m_TreeViewState = new TreeViewState();
            m_TreeView = new SkeletonBoneSelectionTreeView(m_TreeViewState);
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
                msgDestination.SendEvent(EditorGUIUtility.CommandEvent(WindowUpdatedCommand));
        }

        void SendCancelCommand()
        {
            if (msgDestination != null)
                msgDestination.SendEvent(EditorGUIUtility.CommandEvent(WindowCancelledCommand));
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
                msgDestination.SendEvent(EditorGUIUtility.CommandEvent(WindowClosedCommand));
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

        static readonly int         kWindowHash                 = nameof(SkeletonBonePickerWindow).GetHashCode();
        const string                kSkeletonFieldControlName   = "SkeletonField";

        private void OnGUI()
        {
            var pickerWindowID = EditorGUIUtility.GetControlID(kWindowHash, FocusType.Passive);

            var pos = position;
            pos.x = 0;
            pos.y = 0;
            GUI.Box(pos, GUIContent.none, EditorStyles.helpBox);

            // vertical space of 2
            GUILayoutUtility.GetRect(0, 10000, 0, 2);

            if (showSkeletonSelection)
            {
                var objectRect = GUILayoutUtility.GetRect(0, 10000, 0, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
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
                    GUI.SetNextControlName(kSkeletonFieldControlName);
                    newSkeleton = EditorGUI.ObjectField(objectRect, s_SkeletonField, reference.Skeleton, s_SkeletonType, true) as Skeleton;
                }
                finally { EditorGUIUtility.labelWidth = prevLabelWidth; }
                if (EditorGUI.EndChangeCheck() && reference.Skeleton != newSkeleton)
                {
                    Undo.RecordObject(this, "Changed Skeleton");
                    reference.Skeleton = newSkeleton;
                    Refresh();
                    if (EditorGUIUtility.GetObjectPickerControlID() == 0)
                    {
                        // Resize the dropdown
                        PresentWindow();
                    }
                }
            }
            var foundSkeleton = reference.Skeleton != null;
            if (!foundSkeleton)
            {
                EditorGUILayout.HelpBox(s_PleaseSelectSkeleton, MessageType.Warning);
                if (needToOpenPicker)
                {
                    EditorGUIUtility.ShowObjectPicker<Skeleton>(null, true, string.Empty, pickerWindowID);
                    needToOpenPicker = false;
                }
                if (!string.IsNullOrEmpty(Event.current.commandName))
                {
                    if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == pickerWindowID)
                    {
                        var newSkeleton = EditorGUIUtility.GetObjectPickerObject() as Skeleton;
                        if (newSkeleton != null && reference.Skeleton != newSkeleton)
                        {
                            Undo.RecordObject(this, "Changed Skeleton");
                            reference.Skeleton = newSkeleton;
                            Refresh();
                        }
                    }
                }
            }
            else
                needToOpenPicker = false;

            int objectPickerControlID = EditorGUIUtility.GetObjectPickerControlID();
            // picker has closed
            if (pickerWasOpen && objectPickerControlID == 0)
            {
                // Assume we've changed the skeleton & we need to resize the dropdown
                PresentWindow();
            }
            pickerWasOpen = (objectPickerControlID != 0);
            if (foundSkeleton)
            {
                var searchRect = GUILayoutUtility.GetRect(0, 10000, 0, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                searchRect.xMin += 3;
                searchRect.xMax -= 3;
                searchRect.yMin += 2;

                if (string.IsNullOrWhiteSpace(m_TreeView.searchString) || foundSkeleton)
                    m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString, SearchFieldStyle, GUIStyle.none, GUIStyle.none);
                else
                    m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString);
                if (objectPickerControlID == 0 && focusOnFirstField)
                {
                    m_SearchField.SetFocus();
                    focusOnFirstField = false;
                }

                // space of 2
                GUILayoutUtility.GetRect(0, 10000, 0, 2);

                var treeRect = GUILayoutUtility.GetRect(0, 100000, 0, position.height);
                m_TreeView.OnGUI(treeRect);
            }
            else
            {
                if (showSkeletonSelection &&        // If the first field is the skeleton-field
                    focusOnFirstField &&            // And we want to focus on the first field
                    reference.Skeleton == null &&   // And our skeleton is not set
                    objectPickerControlID == 0 && // And we don't have a picker open
                    string.IsNullOrWhiteSpace(m_TreeView.searchString)) // And we didn't set a search value (assume that if we did, we started typing on field)
                {
                    // Focus on the skeleton selection field
                    GUI.FocusControl(kSkeletonFieldControlName);
                    focusOnFirstField = false;
                }
            }

            if (objectPickerControlID == 0 &&
                Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.KeypadEnter:
                    case KeyCode.Return:
                    {
                        Close();
                        break;
                    }
                    case KeyCode.Escape:
                    {
                        reference = originalValue;
                        SendModifiedValueCommand();
                        SendCancelCommand();
                        Close();
                        break;
                    }
                }
            }
        }
    }
}
