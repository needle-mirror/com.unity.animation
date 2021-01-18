using System;
using Unity.Animation.Hybrid;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Unity.Animation.Authoring.Editor
{
    class TransformBonePickerWindow : EditorWindow
    {
        public const string WindowClosedCommand       = "TransformBonePickerWindowClosed";
        public const string WindowUpdatedCommand      = "TransformBonePickerWindowUpdated";
        public const string WindowCancelledCommand    = "TransformBonePickerWindowCancelled";

        [SerializeField] RigAuthoring rigAuthoring;
        [SerializeField] TransformBindingID destinationBindingID;
        [SerializeField] Transform currentValue;
        [SerializeField] Transform originalValue;

        EditorWindow msgDestination;
        int msgControlID;

        [SerializeField] SearchField m_SearchField;

        TransformBoneSelectionTreeView m_TreeView;
        [SerializeField] TreeViewState m_TreeViewState;
        Rect buttonRect;

        bool focusOnFirstField = false;

        static TransformBonePickerWindow current;
        public static int GetControlID()
        {
            if (current == null)
                return 0;
            return current.msgControlID;
        }

        public static bool GetValue(int id, out Transform reference)
        {
            reference = default;
            if (current == null ||
                current.msgControlID != id)
                return false;
            reference = current.currentValue;
            return true;
        }

        public static TransformBonePickerWindow TogglePicker(Rect buttonRect, RigAuthoring rigAuthoring, TransformBindingID destinationBindingID, Transform value, string searchFilter, EditorWindow msgDestination, int controlID)
        {
            // If the exact same picker we're trying to open is already open, we close it instead
            if (current != null &&
                current.msgControlID == controlID)
            {
                var prevCurrent = current;
                current = null;
                prevCurrent.Close();
                return null;
            }


            var selector = ScriptableObject.CreateInstance<TransformBonePickerWindow>();
            selector.rigAuthoring = rigAuthoring;
            selector.destinationBindingID = destinationBindingID;
            selector.msgControlID = controlID;
            selector.msgDestination = msgDestination;
            selector.Init(value, searchFilter);
            selector.buttonRect = GUIUtility.GUIToScreenRect(buttonRect);

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

        private void OnEnable()
        {
            hideFlags = HideFlags.DontSave;

            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void Init(Transform reference, string searchFilter)
        {
            this.currentValue = reference;
            this.originalValue = reference;

            current = this;
            focusOnFirstField = true;

            if (m_TreeViewState == null) m_TreeViewState = new TreeViewState();
            m_TreeView = new TransformBoneSelectionTreeView(m_TreeViewState);
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

        void TreeViewSelectionChanged(Transform newReference)
        {
            Undo.RecordObject(this, "Changed Bone");
            currentValue = newReference;
            SendModifiedValueCommand();
        }

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
                m_TreeView.RigAuthoring = rigAuthoring;
                m_TreeView.DestinationBindingID = destinationBindingID;
                m_TreeView.Reload();
                m_TreeView.ExpandAll();
                m_TreeView.Select(currentValue);
            }
        }

        static GUIStyle s_SearchFieldStyle;
        static GUIStyle SearchFieldStyle => s_SearchFieldStyle ?? (s_SearchFieldStyle = "SearchTextField");

        private void OnGUI()
        {
            var pos = position;
            pos.x = 0;
            pos.y = 0;
            GUI.Box(pos, GUIContent.none, EditorStyles.helpBox);

            // spacing
            GUILayoutUtility.GetRect(0, 10000, 0, EditorGUIUtility.standardVerticalSpacing);

            var searchRect = GUILayoutUtility.GetRect(0, 10000, 0, EditorGUIUtility.singleLineHeight + 2);
            searchRect.xMin += 3;
            searchRect.xMax -= 3;
            searchRect.yMin += 2;

            if (string.IsNullOrWhiteSpace(m_TreeView.searchString))
                m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString, SearchFieldStyle, GUIStyle.none, GUIStyle.none);
            else
                m_TreeView.searchString = m_SearchField.OnGUI(searchRect, m_TreeView.searchString);
            if (focusOnFirstField)
            {
                m_SearchField.SetFocus();
                focusOnFirstField = false;
            }

            // spacing
            GUILayoutUtility.GetRect(0, 10000, 0, EditorGUIUtility.standardVerticalSpacing);

            var treeRect = GUILayoutUtility.GetRect(0, 100000, 0, position.height);
            m_TreeView.OnGUI(treeRect);

            if (Event.current.type == EventType.KeyDown)
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
                        currentValue = originalValue;
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
