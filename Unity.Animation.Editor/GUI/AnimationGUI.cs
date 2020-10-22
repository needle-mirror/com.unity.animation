using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

namespace Unity.Animation.Authoring.Editor
{
    internal static class AnimationGUI
    {
        public static void BoneField(Rect position, SerializedProperty property, bool showFullPath = false) { BoneField(position, property, (GUIContent)null, EditorStyles.objectField, showFullPath); }
        public static void BoneField(Rect position, SerializedProperty property, Skeleton defaultSkeleton, bool showFullPath = false) { BoneField(position, property, defaultSkeleton, (GUIContent)null, EditorStyles.objectField, showFullPath); }
        public static void BoneField(Rect position, SerializedProperty property, GUIContent label, bool showFullPath = false) { BoneField(position, property, label, EditorStyles.objectField, showFullPath); }
        public static void BoneField(Rect position, SerializedProperty property, Skeleton defaultSkeleton, GUIContent label, bool showFullPath = false) { BoneField(position, property, defaultSkeleton, label, EditorStyles.objectField, showFullPath); }

        public static void BoneField(Rect position, SerializedProperty property, GUIContent label, GUIStyle style, bool showFullPath = false)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (property.type != typeof(SkeletonBoneReference).Name)
                throw new ArgumentException($"The given SerializedProperty points to an object that's a {property.type}, when it should be a {nameof(SkeletonBoneReference)}.", nameof(property));

            var indent = EditorGUI.indentLevel;
            try
            {
                int id = GUIUtility.GetControlID(k_BoneFieldHash, FocusType.Keyboard, position);
                label = EditorGUI.BeginProperty(position, label, property);
                position = EditorGUI.PrefixLabel(position, id, label);
                EditorGUI.indentLevel = 0;
                DoBoneField(position, id, property, (Skeleton)null, style, showFullPath);
                EditorGUI.EndProperty();
            }
            finally
            {
                EditorGUI.indentLevel = indent;
            }
        }

        public static void BoneField(Rect position, SerializedProperty property, Skeleton defaultSkeleton, GUIContent label, GUIStyle style, bool showFullPath = false)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (property.type != k_TransformBindingIDTypename)
                throw new ArgumentException($"The given SerializedProperty points to an object that's a {property.type}, when it should be a {nameof(TransformBindingID)}.", nameof(property));

            var indent = EditorGUI.indentLevel;
            var prevEnabled = GUI.enabled;
            GUI.enabled = prevEnabled && defaultSkeleton != null;
            try
            {
                int id = GUIUtility.GetControlID(k_BoneFieldHash, FocusType.Keyboard, position);
                label = EditorGUI.BeginProperty(position, label, property);
                position = EditorGUI.PrefixLabel(position, id, label);
                EditorGUI.indentLevel = 0;
                DoBoneField(position, id, property, defaultSkeleton, style, showFullPath);
                EditorGUI.EndProperty();
            }
            finally
            {
                GUI.enabled = prevEnabled;
                EditorGUI.indentLevel = indent;
            }
        }

        const int k_PickerButtonWidth = 19;
        static private Rect GetPickerButtonRect(Rect position) { return new Rect(position.xMax - k_PickerButtonWidth, position.y, k_PickerButtonWidth, position.height); }

        const string k_ReferenceIsNotFromCorrectSkeleton = "Bone Reference is not from correct Skeleton";
        static readonly int k_BoneFieldHash = "k_BoneFieldHash".GetHashCode();
        static readonly GUIContent  k_MixedValueContent             = EditorGUIUtility.TrTextContent("\u2014", "Mixed Values");
        static readonly GUIContent  k_EmptySkeletonBoneReference    = EditorGUIUtility.TrTextContent($"None ({ObjectNames.NicifyVariableName(nameof(SkeletonBoneReference))})");
        static readonly GUIContent  k_MissingSkeletonContent        = EditorGUIUtility.TrTextContent($"Missing Skeleton ({ObjectNames.NicifyVariableName(nameof(SkeletonBoneReference))})");
        static readonly GUIContent  k_EmptyBoneReference            = EditorGUIUtility.TrTextContent($"None ({ObjectNames.NicifyVariableName(nameof(TransformBindingID))})");
        static readonly string      k_BoneNotFoundContent           = "Unknown ({0})";
        static readonly string      k_SkeletonEmptyBoneReference    = "{0} | (None)";
        static readonly string      k_SkeletonBoneNotFoundContent   = "{0} | Unknown ({1})";
        static readonly string      k_SkeletonBoneFormat            = "{0} | {1}";
        static class Styles
        {
            public static GUIStyle objectFieldButton = GetStyle("ObjectFieldButton");

            internal static GUIStyle error = new GUIStyle() { name = "StyleNotFoundError" };

            internal static GUIStyle GetStyle(string styleName)
            {
                GUIStyle s = GUI.skin.FindStyle(styleName) ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
                if (s == null)
                {
                    Debug.LogError("Missing built-in guistyle " + styleName);
                    s = error;
                }
                return s;
            }
        }

        internal static readonly string k_SkeletonBoneReferenceArray = $"{nameof(SkeletonBoneReference)}[]";
        static readonly string k_TransformBindingIDTypename     = typeof(TransformBindingID).Name;
        static readonly string k_TransformBindingIDPath         = nameof(TransformBindingID.Path);
        static readonly string k_SkeletonBoneReferenceTypename  = typeof(SkeletonBoneReference).Name;
        static readonly string k_SkeletonBoneReferencePath      = $"{SkeletonBoneReference.nameOfID}.{k_TransformBindingIDPath}";
        static readonly string k_SkeletonBoneReferenceSkeleton  = SkeletonBoneReference.nameOfSkeleton;

        static SerializedProperty GetSerializedPropertyPath(SerializedProperty property)
        {
            if (property.type == k_TransformBindingIDTypename)
                return property.FindPropertyRelative(k_TransformBindingIDPath);
            UnityEngine.Debug.Assert(property.type == k_SkeletonBoneReferenceTypename);
            return property.FindPropertyRelative(k_SkeletonBoneReferencePath);
        }

        private static void ClearBoneReference(SerializedProperty property)
        {
            if (property.type == k_TransformBindingIDTypename)
            {
                var pathProperty = property.FindPropertyRelative(k_TransformBindingIDPath);
                if (pathProperty != null) pathProperty.stringValue = string.Empty;
            }
            else
            {
                UnityEngine.Debug.Assert(property.type == k_SkeletonBoneReferenceTypename);
                var pathProperty = property.FindPropertyRelative(k_SkeletonBoneReferencePath);
                if (pathProperty != null) pathProperty.stringValue = string.Empty;
                var skeletonProperty = property.FindPropertyRelative(k_SkeletonBoneReferenceSkeleton);
                if (skeletonProperty != null) skeletonProperty.objectReferenceValue = null;
            }
        }

        static Skeleton GetPropertySkeleton(SerializedProperty property, Skeleton defaultSkeleton)
        {
            if (property == null || property.type == k_TransformBindingIDTypename)
                return defaultSkeleton;
            UnityEngine.Debug.Assert(property.type == k_SkeletonBoneReferenceTypename);
            return property.FindPropertyRelative(k_SkeletonBoneReferenceSkeleton)?.objectReferenceValue as Skeleton;
        }

        static bool IsValidBoneReferenceValue(SerializedProperty property, Skeleton defaultSkeleton, SkeletonBoneReference newValue)
        {
            if (property.type == k_TransformBindingIDTypename)
            {
                if (newValue.Skeleton != defaultSkeleton) return false;
            }
            return true;
        }

        static bool HasStoredSkeleton(SerializedProperty property)
        {
            if (property.type == k_SkeletonBoneReferenceTypename)
                return true;
            return false;
        }

        static bool SetBoneReferenceValue(SerializedProperty property, Skeleton defaultSkeleton, SkeletonBoneReference newValue)
        {
            if (property.type == k_SkeletonBoneReferenceTypename)
            {
                var skeletonProperty = property.FindPropertyRelative(k_SkeletonBoneReferenceSkeleton);
                skeletonProperty.objectReferenceValue = newValue.Skeleton;
            }
            else
            {
                UnityEngine.Debug.Assert(property.type == k_TransformBindingIDTypename);
                if (newValue.Skeleton != defaultSkeleton) { Debug.LogError(k_ReferenceIsNotFromCorrectSkeleton); return false; }
            }
            var pathProperty = GetSerializedPropertyPath(property);
            pathProperty.stringValue = newValue.ID.Path;
            return true;
        }

        static bool TryGetSkeletonBoneReference(SerializedProperty property, Skeleton defaultSkeleton, out SkeletonBoneReference reference)
        {
            reference = default;
            if (property.hasMultipleDifferentValues)
                return false;

            var skeleton = GetPropertySkeleton(property, defaultSkeleton);
            if (skeleton == null)
                return false;

            var path = GetSerializedPropertyPath(property);
            var transformBindingID = new TransformBindingID { Path = path?.stringValue };
            if (transformBindingID == TransformBindingID.Invalid)
                return false;
            reference = new SkeletonBoneReference(skeleton, transformBindingID);
            return true;
        }

        static string GetNameFromBonePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            var lastIndex = path.LastIndexOf(Skeleton.k_PathSeparator);
            if (lastIndex == -1)
                return path;
            return path.Substring(lastIndex + 1);
        }

        static GUIContent GetObjectGUIContent(SerializedProperty property, Skeleton defaultSkeleton, bool showFullPath)
        {
            if (EditorGUI.showMixedValue)
                return k_MixedValueContent;

            if (property.hasMultipleDifferentValues)
                return k_MixedValueContent;

            var skeleton = GetPropertySkeleton(property, defaultSkeleton);
            if (skeleton == null)
            {
                if (object.ReferenceEquals(skeleton, null))
                    return k_EmptySkeletonBoneReference;
                else
                    return k_MissingSkeletonContent;
            }

            var bonePath    = GetSerializedPropertyPath(property)?.stringValue;
            var boneName    = showFullPath ? bonePath : GetNameFromBonePath(bonePath);
            if (HasStoredSkeleton(property))
            {
                var skeletonName = skeleton.name;
                if (string.IsNullOrEmpty(boneName))
                    return EditorGUIUtility.TrTextContent(string.Format(k_SkeletonEmptyBoneReference, skeletonName));
                if (!skeleton.Contains(new TransformBindingID { Path = bonePath }))
                    return EditorGUIUtility.TrTextContent(string.Format(k_SkeletonBoneNotFoundContent, skeletonName, boneName));

                return EditorGUIUtility.TrTextContent(string.Format(k_SkeletonBoneFormat, skeletonName, boneName), AnimationIcons.BoneIcon);
            }
            else
            {
                if (string.IsNullOrEmpty(boneName))
                    return k_EmptyBoneReference;
                if (!skeleton.Contains(new TransformBindingID { Path = bonePath }))
                    return EditorGUIUtility.TrTextContent(string.Format(k_BoneNotFoundContent, boneName));

                return EditorGUIUtility.TrTextContent(boneName, AnimationIcons.BoneIcon);
            }
        }

        static int startBonePickerGroup;

        static void DoBoneField(Rect position, int id, SerializedProperty property, Skeleton defaultSkeleton, GUIStyle style, bool showFullPath = false)
        {
            var mouseOver = position.Contains(Event.current.mousePosition);

            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.MouseDrag:
                {
                    if (!mouseOver)
                        break;
                    if (EditorGUI.showMixedValue)
                        break;

                    // Is the bone reference empty?
                    var path = GetSerializedPropertyPath(property)?.stringValue;
                    if (!string.IsNullOrEmpty(path) && (path != TransformBindingID.Invalid.Path) &&
                        TryGetSkeletonBoneReference(property, defaultSkeleton, out var skeletonBoneReference))
                    {
                        DragAndDrop.StartDrag("Dragging Bone");
                        // We allow the dragging of an invalid bone so we can show that it's invalid while dragging
                        DragAndDrop.SetGenericData(k_SkeletonBoneReferenceArray, new SkeletonBoneReference[] { skeletonBoneReference });

                        // We don't allow the skeleton to be invalid though
                        if (skeletonBoneReference.IsValid())
                            // Adding the skeleton as a second payload allows us to drag & drop a skeleton-bone-reference to a skeleton field
                            DragAndDrop.objectReferences = new UnityEngine.Object[] { skeletonBoneReference.Skeleton };
                    }
                    evt.Use();
                    break;
                }
                case EventType.DragUpdated:
                {
                    if (!mouseOver)
                        break;
                    var payLoad = DragAndDrop.GetGenericData(k_SkeletonBoneReferenceArray) as SkeletonBoneReference[];
                    if (payLoad == null || payLoad.Length == 0) { DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; break; }
                    if (!IsValidBoneReferenceValue(property, defaultSkeleton, payLoad[0])) { DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; break; }

                    // Do not allow dragging out references that are invalid
                    if (!payLoad[0].IsValid()) { DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; break; }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    DragAndDrop.activeControlID = id;
                    evt.Use();
                    break;
                }
                case EventType.DragPerform:
                {
                    if (!mouseOver)
                        break;
                    DragAndDrop.activeControlID = 0;
                    var payLoad = DragAndDrop.GetGenericData(k_SkeletonBoneReferenceArray) as SkeletonBoneReference[];
                    if (payLoad == null || payLoad.Length == 0) { DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; break; }
                    DragAndDrop.AcceptDrag();

                    // Do not accept references that are invalid
                    if (!payLoad[0].IsValid())
                        break;

                    // Set the new reference
                    if (SetBoneReferenceValue(property, defaultSkeleton, payLoad[0]))
                        GUI.changed = true;
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

                    GUIUtility.keyboardControl = id;
                    EditorGUIUtility.editingTextField = false;
                    bool anyModifiersPressed = evt.shift || evt.control || evt.alt || evt.command;
                    if (anyModifiersPressed)
                        break;
                    Rect buttonRect = GetPickerButtonRect(position);
                    if (!buttonRect.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.clickCount == 1)
                        {
                            // ping skeleton (if any)
                            if (TryGetSkeletonBoneReference(property, defaultSkeleton, out var skeletonBoneReference))
                                EditorGUIUtility.PingObject(skeletonBoneReference.Skeleton);
                            evt.Use();
                        }
                        else if (Event.current.clickCount == 2)
                        {
                            if (!anyModifiersPressed && TryGetSkeletonBoneReference(property, defaultSkeleton, out var skeletonBoneReference))
                                SkeletonEditor.SelectAndFrame(skeletonBoneReference);
                            evt.Use();
                        }
                    }
                    else
                    {
                        bool found = TryGetSkeletonBoneReference(property, defaultSkeleton, out var reference);
                        if (!found)
                            reference = default;

                        bool showSkeletonSelection = property.type != k_TransformBindingIDTypename;
                        if (found || showSkeletonSelection)
                        {
                            // We just set our control to have focus, so this is the editorWindow we're part of
                            // We need this window to be able to send a command back to it
                            var currentEditorWindow = EditorWindow.focusedWindow;

                            startBonePickerGroup = Undo.GetCurrentGroup();
                            Undo.IncrementCurrentGroup();
                            BonePickerWindow.TogglePicker(position, reference, string.Empty, currentEditorWindow, showSkeletonSelection, id);
                        }
                        evt.Use();
                        GUIUtility.ExitGUI();
                    }
                    break;
                }
                case EventType.ExecuteCommand:
                {
                    string commandName = evt.commandName;
                    if (commandName == BonePickerWindow.BonePickerWindowUpdatedCommand && BonePickerWindow.GetControlID() == id && GUIUtility.keyboardControl == id)
                    {
                        if (!BonePickerWindow.GetValue(id, out SkeletonBoneReference reference))
                            break;

                        property.serializedObject.ApplyModifiedProperties();
                        property.serializedObject.Update();
                        Undo.RecordObjects(property.serializedObject.targetObjects, "Modified Bone");
                        if (SetBoneReferenceValue(property, defaultSkeleton, reference))
                        {
                            property.serializedObject.ApplyModifiedProperties();
                            GUI.changed = true;
                        }
                        evt.Use();
                        break;
                    }
                    else if (commandName == BonePickerWindow.BonePickerWindowCancelledCommand && BonePickerWindow.GetControlID() == id && GUIUtility.keyboardControl == id)
                    {
                        Undo.RevertAllDownToGroup(startBonePickerGroup);
                    }
                    else if (commandName == BonePickerWindow.BonePickerWindowClosedCommand && BonePickerWindow.GetControlID() == id && GUIUtility.keyboardControl == id)
                    {
                        Undo.CollapseUndoOperations(startBonePickerGroup);
                    }
                    break;
                }
                case EventType.KeyDown:
                {
                    if (GUIUtility.keyboardControl == id)
                    {
                        if (evt.keyCode == KeyCode.Backspace || (evt.keyCode == KeyCode.Delete && (evt.modifiers & EventModifiers.Shift) == 0))
                        {
                            // Clear the reference
                            ClearBoneReference(property);
                            GUI.changed = true;
                            evt.Use();
                        }
                    }
                    break;
                }
                case EventType.Repaint:
                {
                    GUIContent objectNameContent = GetObjectGUIContent(property, defaultSkeleton, showFullPath);

                    style.Draw(position, objectNameContent, id, DragAndDrop.activeControlID == id, mouseOver);

                    Rect buttonRect = Styles.objectFieldButton.margin.Remove(GetPickerButtonRect(position));
                    Styles.objectFieldButton.Draw(buttonRect, GUIContent.none, id, DragAndDrop.activeControlID == id, mouseOver);
                    break;
                }
            }
        }
    }

    internal static class AnimationGUILayout
    {
        public static void BoneField(SerializedProperty property, params GUILayoutOption[] options)
        {
            BoneField(property, (GUIContent)null, options);
        }

        public static void BoneField(SerializedProperty property, Skeleton defaultSkeleton, params GUILayoutOption[] options)
        {
            BoneField(property, defaultSkeleton, (GUIContent)null, options);
        }

        public static void BoneField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, EditorStyles.objectField, options);
            AnimationGUI.BoneField(r, property, label);
        }

        public static void BoneField(SerializedProperty property, Skeleton defaultSkeleton, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, EditorStyles.objectField, options);
            AnimationGUI.BoneField(r, property, defaultSkeleton, label);
        }
    }
}
