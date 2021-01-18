using System;
using Unity.Animation.Hybrid;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Unity.Animation.Authoring.Editor
{
    internal static class AnimationGUI
    {
        public static void BoneField(Rect position, SerializedProperty property, bool showFullPath = false) { BoneField(position, property, (GUIContent)null, Styles.objectField, showFullPath); }
        public static void BoneField(Rect position, SerializedProperty property, Skeleton defaultSkeleton, bool showFullPath = false) { BoneField(position, property, defaultSkeleton, (GUIContent)null, Styles.objectField, showFullPath); }
        public static void BoneField(Rect position, SerializedProperty property, GUIContent label, bool showFullPath = false) { BoneField(position, property, label, Styles.objectField, showFullPath); }
        public static void BoneField(Rect position, SerializedProperty property, Skeleton defaultSkeleton, GUIContent label, bool showFullPath = false) { BoneField(position, property, defaultSkeleton, label, Styles.objectField, showFullPath); }
        public static void BoneField(Rect position, SerializedProperty property, GUIContent label, GUIStyle style, bool showFullPath = false)
        {
            var indent = EditorGUI.indentLevel;
            try
            {
                int id = GUIUtility.GetControlID(k_BoneFieldHash, FocusType.Keyboard, position);
                if (property != null)
                {
                    label = EditorGUI.BeginProperty(position, label, property);
                    if (label != null)
                        position = EditorGUI.PrefixLabel(position, id, label);
                }
                EditorGUI.indentLevel = 0;
                DoBoneField(position, id, property, (Skeleton)null, style, showFullPath);
                if (property != null)
                    EditorGUI.EndProperty();
            }
            finally
            {
                EditorGUI.indentLevel = indent;
            }
        }

        public static void BoneField(Rect position, SerializedProperty property, Skeleton defaultSkeleton, GUIContent label, GUIStyle style, bool showFullPath = false)
        {
            var indent = EditorGUI.indentLevel;
            try
            {
                int id = GUIUtility.GetControlID(k_BoneFieldHash, FocusType.Keyboard, position);
                if (property != null)
                {
                    label = EditorGUI.BeginProperty(position, label, property);
                    if (label != null)
                        position = EditorGUI.PrefixLabel(position, id, label);
                }
                EditorGUI.indentLevel = 0;
                DoBoneField(position, id, property, defaultSkeleton, style, showFullPath);
                if (property != null)
                    EditorGUI.EndProperty();
            }
            finally
            {
                EditorGUI.indentLevel = indent;
            }
        }

        static readonly int k_BoneFieldHash = "k_BoneFieldHash".GetHashCode();

        const int k_PickerButtonWidth = 19;
        static private Rect GetPickerButtonRect(Rect position) { return new Rect(position.xMax - k_PickerButtonWidth, position.y, k_PickerButtonWidth, position.height); }

        const string k_ReferenceIsNotFromCorrectSkeleton = "Bone Reference is not from correct Skeleton";
        static readonly GUIContent  k_MixedValueContent             = EditorGUIUtility.TrTextContent("\u2014", "Mixed Values");
        static readonly GUIContent  k_EmptySkeletonBoneReference    = EditorGUIUtility.TrTextContent($"None ({ObjectNames.NicifyVariableName(nameof(SkeletonBoneReference))})");
        static readonly GUIContent  k_MissingSkeletonContent        = EditorGUIUtility.TrTextContent($"<b>Missing Skeleton</b> ({ObjectNames.NicifyVariableName(nameof(SkeletonBoneReference))})", AnimationIcons.WarnIcon);
        static readonly GUIContent  k_EmptyBoneReference            = EditorGUIUtility.TrTextContent($"None ({ObjectNames.NicifyVariableName(nameof(TransformBindingID))})");

        static readonly string      k_BoneRoot                      = L10n.Tr("<root>");

        static readonly string      k_BoneUnknownText               = L10n.Tr("<b>Unknown</b> ({0})");
        static readonly string      k_SkeletonEmptyText             = L10n.Tr("<b>None</b>");
        static readonly string      k_SkeletonMissingText           = L10n.Tr("<b>Missing Skeleton</b>");
        static readonly string      k_SkeletonMismatchText          = L10n.Tr("<b>Mismatch</b> ({0})");

        static readonly string      k_BoneUnknownTooltip            = L10n.Tr("The bone path could not be found in its skeleton\n\n{0}");
        static readonly string      k_SkeletonEmptyTooltip          = L10n.Tr("The skeleton is not set\n\n{0}");
        static readonly string      k_SkeletonMissingTooltip        = L10n.Tr("The bone contains a reference to a skeleton that is missing\n\n{0}");
        static readonly string      k_SkeletonMismatchTooltip       = L10n.Tr("The bone contains a different Skeleton ({1}) than is required ({2})\n\n{0}");

        static readonly string      k_BonePathToolip                = L10n.Tr("Path: {0}");
        static readonly string      k_NoBonePathTooltip             = L10n.Tr("This reference has no path to a bone");

        static readonly string      k_SkeletonBoneFormat            = "{0} | {1}";

        static readonly GUIContent  k_NullProperty                  = EditorGUIUtility.TrTextContent("<b>Null</b>", AnimationIcons.WarnIcon);
        static readonly string      k_InvalidProperty               = L10n.Tr("<b>Unexpected Type ({0})</b>");

        static GUIContent           tempGUIContent                  = new GUIContent(string.Empty);
        static GUIContent           warningGUIContent               = new GUIContent(AnimationIcons.WarnIcon);

        static GUIContent GetObjectGUIContent(SerializedProperty property, Skeleton defaultSkeleton, bool showFullPath, bool isValid)
        {
            if (!isValid)
            {
                if (property == null)
                    return k_NullProperty;
                if (property.type != k_TransformBindingIDTypename || defaultSkeleton != null)
                {
                    tempGUIContent.text = string.Format(k_InvalidProperty, property.type);
                    tempGUIContent.image = AnimationIcons.WarnIcon;
                    return tempGUIContent;
                }
            }

            if (EditorGUI.showMixedValue || (property?.hasMultipleDifferentValues ?? false))
                return k_MixedValueContent;

            var skeleton = GetSkeletonFromSerializedProperty(property, defaultSkeleton);
            var bonePath = GetPathFromSerializedProperty(property)?.stringValue;
            var boneName = showFullPath ? bonePath : GetNameFromBonePath(bonePath);
            var tooltip = !showFullPath? string.Format(k_BonePathToolip, bonePath) : k_NoBonePathTooltip;

            if (ShowStoredSkeleton(property, defaultSkeleton))
            {
                bool needWarning = false;
                bool haveBone  = true;
                if (string.IsNullOrEmpty(boneName))
                {
                    if (skeleton == null)
                    {
                        return IsSkeletonMissing(property, defaultSkeleton) ? k_MissingSkeletonContent : k_EmptySkeletonBoneReference;
                    }
                    boneName = k_BoneRoot;
                }
                else if (skeleton != null && skeleton.GetTransformChannelState(new TransformBindingID { Path = bonePath }) != TransformChannelState.Active)
                {
                    haveBone = false;
                    needWarning = true;
                    boneName = string.Format(k_BoneUnknownText, boneName);
                    tooltip = string.Format(k_BoneUnknownTooltip, tooltip);
                }

                bool haveSkeleton = true;
                string skeletonName;
                if (skeleton == null)
                {
                    haveSkeleton = false; needWarning = true;
                    var isMissing = IsSkeletonMissing(property, defaultSkeleton);
                    skeletonName = isMissing ? k_SkeletonMissingText : k_SkeletonEmptyText;
                    tooltip = string.Format(isMissing ? k_SkeletonMissingTooltip : k_SkeletonEmptyTooltip, tooltip);
                }
                else
                {
                    skeletonName = skeleton.name;
                    if (defaultSkeleton != null && skeleton != defaultSkeleton)
                    {
                        haveSkeleton = false;
                        skeletonName = string.Format(k_SkeletonMismatchText, skeletonName);
                        needWarning = true;
                        tooltip = string.Format(k_SkeletonMismatchTooltip, tooltip, (skeleton ? skeleton.name : "None"), (defaultSkeleton ? defaultSkeleton.name : "None"));
                    }
                }

                tempGUIContent.image = (haveBone && haveSkeleton) ? AnimationIcons.BoneIcon : (needWarning ? AnimationIcons.WarnIcon : null);
                tempGUIContent.text = string.Format(k_SkeletonBoneFormat, skeletonName, boneName);
                tempGUIContent.tooltip = tooltip;
                return tempGUIContent;
            }
            else
            {
                if (string.IsNullOrEmpty(boneName))
                    return k_EmptyBoneReference;

                if (skeleton != null && skeleton.GetTransformChannelState(new TransformBindingID { Path = bonePath }) != TransformChannelState.Active)
                {
                    tempGUIContent.image = AnimationIcons.WarnIcon;
                    tempGUIContent.text = string.Format(k_BoneUnknownText, boneName);
                    tempGUIContent.tooltip = string.Format(k_BoneUnknownTooltip, tooltip);
                    return tempGUIContent;
                }

                tempGUIContent.image = AnimationIcons.BoneIcon;
                tempGUIContent.text = boneName;
                tempGUIContent.tooltip = tooltip;
                return tempGUIContent;
            }
        }

        static class Styles
        {
            public static readonly Vector2 InlineIconSize = new Vector2(12f, 12f);
            public static GUIStyle objectField = new GUIStyle(EditorStyles.objectField) { richText = true };
            public static GUIStyle objectFieldText = new GUIStyle()
            {
                richText        = objectField.richText,
                font            = objectField.font,
                fontSize        = objectField.fontSize,
                fontStyle       = objectField.fontStyle,
                fixedHeight     = objectField.fixedHeight,
                stretchWidth    = objectField.stretchWidth,
                stretchHeight   = objectField.stretchHeight,
                border          = objectField.border,
                margin          = objectField.margin,
                padding         = new RectOffset(objectField.padding.left + 15, objectField.padding.right, objectField.padding.top, objectField.padding.bottom),
                overflow        = objectField.overflow,
                fixedWidth      = objectField.fixedWidth,
                clipping        = objectField.clipping,
                wordWrap        = objectField.wordWrap,
                contentOffset   = objectField.contentOffset,
                alignment       = objectField.alignment,
                imagePosition   = objectField.imagePosition,
                active          = new GUIStyleState
                {
                    textColor   = objectField.active.textColor
                },
                normal = new GUIStyleState
                {
                    textColor = objectField.normal.textColor
                },
                hover = new GUIStyleState
                {
                    textColor = objectField.hover.textColor
                },
                onNormal = new GUIStyleState
                {
                    textColor = objectField.onNormal.textColor
                },
                onHover = new GUIStyleState
                {
                    textColor = objectField.onHover.textColor
                },
                onActive = new GUIStyleState
                {
                    textColor = objectField.onActive.textColor
                },
                focused = new GUIStyleState
                {
                    textColor = objectField.focused.textColor
                },
                onFocused = new GUIStyleState
                {
                    textColor = objectField.onFocused.textColor
                }
            };

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

        // Get the serializedProperty that points to the Path part of our TransformBindingID/SkeletonBoneReference
        static SerializedProperty GetPathFromSerializedProperty(SerializedProperty property)
        {
            if (property.type == k_TransformBindingIDTypename)
                return property.FindPropertyRelative(k_TransformBindingIDPath);
            UnityEngine.Debug.Assert(property.type == k_SkeletonBoneReferenceTypename);
            return property.FindPropertyRelative(k_SkeletonBoneReferencePath);
        }

        // Clears the TransformBindingID/SkeletonBoneReference
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

        // Gets the skeleton stored in our SkeletonBoneReference, or returns the default Skeleton for a TransformBindingID
        static Skeleton GetSkeletonFromSerializedProperty(SerializedProperty property, Skeleton defaultSkeleton)
        {
            if (property == null)
                return null;

            if (property.type == k_TransformBindingIDTypename)
                return defaultSkeleton;

            // We return the skeleton stored in our SkeletonBoneReference, even IF we have a defaultSkeleton
            UnityEngine.Debug.Assert(property.type == k_SkeletonBoneReferenceTypename);
            return property.FindPropertyRelative(k_SkeletonBoneReferenceSkeleton)?.objectReferenceValue as Skeleton;
        }

        // Returns true if the skeleton is missing, as apposed to "not set"
        static bool IsSkeletonMissing(SerializedProperty property, Skeleton defaultSkeleton)
        {
            if (property == null)
                return false;

            if (property.type == k_TransformBindingIDTypename)
                // Note: this circumvents the unity equality operator
                return !ReferenceEquals(defaultSkeleton, null);

            UnityEngine.Debug.Assert(property.type == k_SkeletonBoneReferenceTypename);
            var relativeProperty = property.FindPropertyRelative(k_SkeletonBoneReferenceSkeleton);
            if (relativeProperty == null)
                return false;

            return relativeProperty.objectReferenceInstanceIDValue != 0;
        }

        // Checks if a given SkeletonBoneReference is valid to assign to our TransformBindingID/SkeletonBoneReference
        static bool IsAssignableTo(SkeletonBoneReference assignmentValue, SerializedProperty property, Skeleton defaultSkeleton)
        {
            // The given assignmentValue needs to have a valid skeleton
            if (assignmentValue.Skeleton == null ||
                // And the given bone must be part of that skeleton
                assignmentValue.Skeleton.GetTransformChannelState(assignmentValue.ID) != TransformChannelState.Active)
                return false;

            if (property.type == k_TransformBindingIDTypename)
            {
                // If we're a TransformBindingID, the skeleton need to match the defaultSkeleton
                if (assignmentValue.Skeleton != defaultSkeleton)
                    return false;
            }
            else
            {
                UnityEngine.Debug.Assert(property.type == k_SkeletonBoneReferenceTypename);
                // If we have a default skeleton for our SkeletonBoneReference ...
                if (defaultSkeleton != null)
                {
                    // ... then the skeleton in the assignmentValue needs to match it
                    if (assignmentValue.Skeleton != defaultSkeleton)
                        return false;
                }
            }
            return true;
        }

        static bool ShowStoredSkeleton(SerializedProperty property, Skeleton defaultSkeleton)
        {
            // If we don't have a default skeleton, always show the stored skeleton (if it exists)
            if (defaultSkeleton == null)
                return true;

            // If we're a TransformBindingID we don't have a stored skeleton to show
            if (property.type == k_TransformBindingIDTypename)
                return false;

            UnityEngine.Debug.Assert(property.type == k_SkeletonBoneReferenceTypename);

            // If our stored skeleton doesn't match the stored skeleton we HAVE to show it
            var skeleton = property.FindPropertyRelative(k_SkeletonBoneReferenceSkeleton)?.objectReferenceValue as Skeleton;
            if (defaultSkeleton != skeleton)
                return true;

            // Otherwise don't bother showing it
            return false; // TODO: have an option to force show it?
        }

        static bool AssignBoneReferenceTo(SkeletonBoneReference reference, SerializedProperty property, Skeleton defaultSkeleton)
        {
            // If we have a default skeleton, our newValue.Skeleton needs to match it (we cannot assign a bone from a different skeleton)
            if (defaultSkeleton != null)
            {
                if (reference.Skeleton != defaultSkeleton)
                {
                    Debug.LogError(k_ReferenceIsNotFromCorrectSkeleton);
                    return false;
                }
            }

            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
            Undo.RecordObjects(property.serializedObject.targetObjects, "Modified Bone");

            if (property.type == k_SkeletonBoneReferenceTypename)
            {
                var skeletonProperty = property.FindPropertyRelative(k_SkeletonBoneReferenceSkeleton);
                skeletonProperty.objectReferenceValue = reference.Skeleton;
            }
            var pathProperty = GetPathFromSerializedProperty(property);
            pathProperty.stringValue = reference.ID.Path;

            property.serializedObject.ApplyModifiedProperties();
            return true;
        }

        static bool CanDropSkeleton(Skeleton dropSkeleton, SerializedProperty property, Skeleton defaultSkeleton)
        {
            // If the skeleton we're dropping is not valid, we never accept it
            if (dropSkeleton == null)
                return false;

            // If the defaultSkeleton is the same as the dropSkeleton we always accept it
            if (dropSkeleton == defaultSkeleton)
                return true;

            // If we have a default skeleton set, then we can't change the skeleton to something else, so we don't accept the drop
            if (defaultSkeleton != null)
                return false;

            // If we're a TransformBindingID we don't have a stored skeleton, so can't drop a skeleton on it
            if (property.type == k_TransformBindingIDTypename)
                return false;

            UnityEngine.Debug.Assert(property.type == k_SkeletonBoneReferenceTypename);
            return true;
        }

        static bool TryGetSkeletonBoneReference(SerializedProperty property, Skeleton defaultSkeleton, out SkeletonBoneReference reference)
        {
            reference = default;
            if (property.hasMultipleDifferentValues)
                return false;

            var skeleton = GetSkeletonFromSerializedProperty(property, defaultSkeleton);
            if (skeleton == null)
                return false;

            var path = GetPathFromSerializedProperty(property);
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

        static void TogglePicker(Rect position, int id, SerializedProperty property, Skeleton defaultSkeleton, SkeletonBoneReference reference, bool showSkeletonSelection, string searchFilter = null)
        {
            // We just set our control to have focus, so this is the editorWindow we're part of
            // We need this window to be able to send a command back to it
            var currentEditorWindow = EditorWindow.focusedWindow;

            // Increase the undo group so we can collapse or cancel everything we do in the picker,
            // while still being able to undo/redo inside the picker
            s_StartPickerGroup = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();

            // We assign the reference back to ourselves, in case it was modified before the picker was called
            // This ensures the field accurately shows the current state.
            // NOTE: this is part of the undo group of the picker.
            if (AssignBoneReferenceTo(reference, property, defaultSkeleton))
                GUI.changed = true;

            SkeletonBonePickerWindow.TogglePicker(position, reference, searchFilter ?? string.Empty, currentEditorWindow, showSkeletonSelection, id);
            GUIUtility.ExitGUI();
        }

        static void TogglePicker(Rect position, int id, SerializedProperty property, Skeleton defaultSkeleton, string searchFilter = null)
        {
            bool found = TryGetSkeletonBoneReference(property, defaultSkeleton, out var reference);
            if (!found)
                reference = default;

            bool showSkeletonSelection = property.type != k_TransformBindingIDTypename;

            // We cannot pick a bone when we don't have a Skeleton and cannot select one either
            if (found || showSkeletonSelection)
            {
                // Force the skeleton to the defaultSkeleton, when set
                // This is for when we're in a bad state, and we'd try
                // to open the picker with the wrong bone
                if (defaultSkeleton != null)
                {
                    showSkeletonSelection = false;
                    reference.Skeleton = defaultSkeleton;
                }
                TogglePicker(position, id, property, defaultSkeleton, reference, showSkeletonSelection, searchFilter);
            }
        }

        static bool IsPropertyValid(SerializedProperty property, Skeleton defaultSkeleton)
        {
            if (property == null)
                return false;
            if (property.type == k_SkeletonBoneReferenceTypename)
                return true;
            if (property.type == k_TransformBindingIDTypename)
                return defaultSkeleton != null;
            return false;
        }

        static int s_StartPickerGroup;

        static void DoBoneField(Rect position, int id, SerializedProperty property, Skeleton defaultSkeleton, GUIStyle style, bool showFullPath = false)
        {
            bool isValid = IsPropertyValid(property, defaultSkeleton);
            var prevEnabled = GUI.enabled;
            GUI.enabled = prevEnabled && isValid;
            try
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
                        var path = GetPathFromSerializedProperty(property)?.stringValue;
                        if (!string.IsNullOrEmpty(path) && (path != TransformBindingID.Invalid.Path) &&
                            TryGetSkeletonBoneReference(property, defaultSkeleton, out var skeletonBoneReference) &&
                            // We check if the skeletonBoneReference is valid by checking if it would be assignable to ourselves
                            // This ensures the reference is valid itself, but also it matches our expectations (same skeleton as defaultSkeleton etc.)
                            // This avoids edge cases like dragging out a malformed SkeletonBoneReference that is by itself correct, but not in our context
                            // which could cause surprises for the user.
                            IsAssignableTo(skeletonBoneReference, property, defaultSkeleton))
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

                        // Try to get SkeletonBoneReference
                        var payLoad = DragAndDrop.GetGenericData(k_SkeletonBoneReferenceArray) as SkeletonBoneReference[];
                        if (payLoad != null && payLoad.Length != 0)
                        {
                            if (!IsAssignableTo(payLoad[0], property, defaultSkeleton)) { DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; break; }

                            // Do not allow dragging out references that are invalid
                            if (!payLoad[0].IsValid()) { DragAndDrop.visualMode = DragAndDropVisualMode.Rejected; break; }

                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                            DragAndDrop.activeControlID = id;
                            evt.Use();
                            break;
                        }

                        // Otherwise try to see if we're dropping a Skeleton
                        var skeleton = (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) ? null : DragAndDrop.objectReferences[0] as Skeleton;

                        // Can we accept this particular skeleton?
                        if (CanDropSkeleton(skeleton, property, defaultSkeleton))
                        {
                            if (skeleton != null)
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                                DragAndDrop.activeControlID = id;
                                evt.Use();
                                break;
                            }
                        }

                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        break;
                    }
                    case EventType.DragPerform:
                    {
                        if (!mouseOver)
                            break;
                        DragAndDrop.activeControlID = 0;

                        // Try to get SkeletonBoneReference
                        var payLoad = DragAndDrop.GetGenericData(k_SkeletonBoneReferenceArray) as SkeletonBoneReference[];
                        if (payLoad != null && payLoad.Length != 0)
                        {
                            DragAndDrop.AcceptDrag();

                            // Do not accept references that are invalid
                            if (!payLoad[0].IsValid())
                                break;

                            GUIUtility.keyboardControl = id;
                            EditorGUIUtility.editingTextField = false;
                            // Set the new reference
                            if (AssignBoneReferenceTo(payLoad[0], property, defaultSkeleton))
                                GUI.changed = true;
                            evt.Use();
                            break;
                        }

                        // Otherwise try to see if we're dropping a Skeleton
                        var skeleton = (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) ? null : DragAndDrop.objectReferences[0] as Skeleton;

                        // Can we accept this particular skeleton?
                        if (CanDropSkeleton(skeleton, property, defaultSkeleton))
                        {
                            if (skeleton != null)
                            {
                                DragAndDrop.AcceptDrag();

                                // Select our field
                                GUIUtility.keyboardControl = id;
                                EditorGUIUtility.editingTextField = false;
                                GUI.changed = true;

                                // Get the current Skeleton / Bone-path, but set the skeleton to the dropped skeleton
                                // This ensures that the picker shows the bones for the dropped skeleton
                                TryGetSkeletonBoneReference(property, defaultSkeleton, out var reference);
                                reference.Skeleton = skeleton;

                                evt.Use();
                                TogglePicker(position, id, property, defaultSkeleton, reference, showSkeletonSelection: false);
                                break;
                            }
                        }

                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
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
                            // Single click on field: ping skeleton (if any)
                            if (Event.current.clickCount == 1)
                            {
                                if (!anyModifiersPressed &&
                                    TryGetSkeletonBoneReference(property, defaultSkeleton, out var skeletonBoneReference))
                                    EditorGUIUtility.PingObject(skeletonBoneReference.Skeleton);
                                evt.Use();
                            }
                            // Double click on field: select skeleton, select bone in it and try to frame it to make it visible
                            else if (Event.current.clickCount == 2)
                            {
                                if (!anyModifiersPressed &&
                                    TryGetSkeletonBoneReference(property, defaultSkeleton, out var skeletonBoneReference))
                                    SkeletonEditor.SelectAndFrame(skeletonBoneReference);
                                evt.Use();
                            }
                        }
                        else
                        {
                            evt.Use();

                            // We didn't click on the field, but we clicked on the picker button instead
                            TogglePicker(position, id, property, defaultSkeleton);
                        }
                        break;
                    }
                    case EventType.ExecuteCommand:
                    {
                        string commandName = evt.commandName;
                        if (commandName == SkeletonBonePickerWindow.WindowUpdatedCommand && SkeletonBonePickerWindow.GetControlID() == id && GUIUtility.keyboardControl == id)
                        {
                            // If the value given by the bone picker window is valid, we set it
                            if (!SkeletonBonePickerWindow.GetValue(id, out SkeletonBoneReference reference))
                                break;
                            if (AssignBoneReferenceTo(reference, property, defaultSkeleton))
                                GUI.changed = true;
                            evt.Use();
                            break;
                        }
                        else if (commandName == SkeletonBonePickerWindow.WindowCancelledCommand && SkeletonBonePickerWindow.GetControlID() == id && GUIUtility.keyboardControl == id)
                        {
                            // Cancelled picker, revert to value before the picker was opened
                            Undo.RevertAllDownToGroup(s_StartPickerGroup);
                            GUIUtility.ExitGUI();
                        }
                        else if (commandName == SkeletonBonePickerWindow.WindowClosedCommand && SkeletonBonePickerWindow.GetControlID() == id && GUIUtility.keyboardControl == id)
                        {
                            // Collapse all undo/redo operations between when the picker was opened, and when it was closed, to a single undo operation
                            Undo.CollapseUndoOperations(s_StartPickerGroup);
                        }
                        break;
                    }
                    case EventType.KeyDown:
                    {
                        if (GUIUtility.keyboardControl != id)
                            break;

                        // Clear the reference when pressing backspace or delete
                        if (evt.keyCode == KeyCode.Backspace || (evt.keyCode == KeyCode.Delete && (evt.modifiers & EventModifiers.Shift) == 0))
                        {
                            ClearBoneReference(property);
                            GUI.changed = true;
                            evt.Use();
                        }
                        else
                        // When we press enter/return we open up the picker
                        if (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return)
                        {
                            evt.Use();
                            TogglePicker(position, id, property, defaultSkeleton);
                        }
                        /*
                        // TODO: figure out a way to make the search text field not select all the text on focus, but instead just place cursor at end
                        // When we press a letter, open the picker and already type in that letter in the search field
                        else
                        {
                            if (char.IsLetterOrDigit(evt.character))
                            {
                                evt.Use();
                                TogglePicker(position, id, property, defaultSkeleton, searchFilter: $"{evt.character}");
                            }
                        }*/
                        break;
                    }
                    case EventType.Repaint:
                    {
                        // Render title w/ icon
                        var objectNameContent = GetObjectGUIContent(property, defaultSkeleton, showFullPath, isValid);
                        var labelContent = position;
                        if (style == Styles.objectField && objectNameContent.image == AnimationIcons.WarnIcon)
                        {
                            // Ensure we show the warning icon because its important
                            style.Draw(labelContent, warningGUIContent, id, DragAndDrop.activeControlID == id, mouseOver);
                            objectNameContent.image = null;
                            var textStyle = Styles.objectFieldText;
                            textStyle.Draw(labelContent, objectNameContent, id, DragAndDrop.activeControlID == id, mouseOver);
                            objectNameContent.image = AnimationIcons.WarnIcon;
                        }
                        else
                            style.Draw(labelContent, objectNameContent, id, DragAndDrop.activeControlID == id, mouseOver);

                        // Render picker button
                        Rect buttonRect = Styles.objectFieldButton.margin.Remove(GetPickerButtonRect(position));
                        Styles.objectFieldButton.Draw(buttonRect, GUIContent.none, id, DragAndDrop.activeControlID == id, mouseOver);
                        break;
                    }
                }
            }
            finally
            {
                GUI.enabled = prevEnabled;
            }
        }

        static readonly int k_TransformFieldHash = "k_TransformFieldHash".GetHashCode();

        // These BoneTransformField pickers only show valid transforms in the context of a RigAuthoring component, and the skeleton index the transform will be assigned to

        public static void TransformBoneField(Rect position, RigAuthoring rigAuthoring, TransformBindingID bindingID, List<RigIndexToBone> boneMappings, int index, bool fixupChildren = false) { TransformBoneField(position, rigAuthoring, bindingID, boneMappings, index, (GUIContent)null, Styles.objectField, fixupChildren); }
        public static void TransformBoneField(Rect position, RigAuthoring rigAuthoring, TransformBindingID bindingID, List<RigIndexToBone> boneMappings, int index, GUIContent label, bool fixupChildren = false) { TransformBoneField(position, rigAuthoring, bindingID, boneMappings, index, label, Styles.objectField, fixupChildren); }
        public static void TransformBoneField(Rect position, RigAuthoring rigAuthoring, TransformBindingID bindingID, List<RigIndexToBone> boneMappings, int index, GUIContent label, GUIStyle style, bool fixupChildren = false)
        {
            var indent = EditorGUI.indentLevel;
            try
            {
                int id = GUIUtility.GetControlID(k_TransformFieldHash, FocusType.Keyboard, position);
                if (label != null)
                    position = EditorGUI.PrefixLabel(position, id, label);
                EditorGUI.indentLevel = 0;
                DoTransformBoneField(position, id, rigAuthoring, bindingID, boneMappings, index, style, fixupChildren);
            }
            finally
            {
                EditorGUI.indentLevel = indent;
            }
        }

        static readonly GUIContent k_NullRoot           = EditorGUIUtility.TrTextContent("<b>No Root</b>", AnimationIcons.WarnIcon);
        static readonly GUIContent k_MissingRoot        = EditorGUIUtility.TrTextContent("<b>Missing Root</b>", AnimationIcons.WarnIcon);
        static readonly GUIContent k_EmptyTransform     = EditorGUIUtility.TrTextContent($"None ({ObjectNames.NicifyVariableName(nameof(Transform))})");
        static readonly GUIContent k_MissingTransform   = EditorGUIUtility.TrTextContent($"<b>Missing</b> ({ObjectNames.NicifyVariableName(nameof(Transform))})", AnimationIcons.WarnIcon);

        const string kInvalidHierarchyTooltip       = "The transform is not a child or sibling transform of the transform assigned to the parent skeleton bone.";
        const string kNotDescendantOfRootTooltip    = "The transform is not a child transform, or a descendant, of the root.";
        const string kDuplicatedTransformTooltip    = "The same transform is assigned to multiple bones, which is not allowed.";

        static bool IsTransformFieldEnabled(RigAuthoring rigAuthoring, TransformBindingID bindingID, Transform root)
        {
            if (rigAuthoring == null || root == null || bindingID == TransformBindingID.Invalid)
                return false;

            if (sAreDragging)
                return rigAuthoring.CanTransformBeSet(bindingID, root, sDragAndDropTransform);

            return true;
        }

        static GUIContent GetObjectGUIContent(RigAuthoring rigAuthoring, TransformBindingID bindingID, Transform root, Transform transform)
        {
            if (root == null)
            {
                if (ReferenceEquals(root, null))
                    return k_NullRoot;
                return k_MissingRoot;
            }

            if (transform == null)
            {
                if (ReferenceEquals(transform, null))
                    return k_EmptyTransform;

                return k_MissingTransform;
            }

            Texture2D icon = AnimationIcons.TransformIcon;
            var boneName = transform.name;
            var tooltip = string.Empty;
            if (root != transform && !transform.IsChildOf(root))
            {
                icon = AnimationIcons.WarnIcon;
                tooltip = kNotDescendantOfRootTooltip;
            }
            else if (!rigAuthoring.IsHierarchyValidForTransform(bindingID, root, transform))
            {
                icon = AnimationIcons.WarnIcon;
                tooltip = kInvalidHierarchyTooltip;
            }
            else
            {
                var foundBindingID = rigAuthoring.GetBindingForTransform(transform);
                if (foundBindingID != bindingID)
                {
                    icon = AnimationIcons.WarnIcon;
                    tooltip = kDuplicatedTransformTooltip;
                }
            }

            tempGUIContent.image = icon;
            tempGUIContent.text = boneName;
            tempGUIContent.tooltip = tooltip;
            return tempGUIContent;
        }

        static void TogglePicker(Rect position, int id, RigAuthoring rigAuthoring, TransformBindingID bindingID, Transform value, string searchFilter = null)
        {
            // We just set our control to have focus, so this is the editorWindow we're part of
            // We need this window to be able to send a command back to it
            var currentEditorWindow = EditorWindow.focusedWindow;

            // Increase the undo group so we can collapse or cancel everything we do in the picker,
            // while still being able to undo/redo inside the picker
            s_StartPickerGroup = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();

            TransformBonePickerWindow.TogglePicker(position, rigAuthoring, bindingID, value, searchFilter ?? string.Empty, currentEditorWindow, id);
            GUIUtility.ExitGUI();
        }

        static bool sAreDragging = false; // Can be dragging something which is not a transform, so cannot rely on "null" value to tell us that we're dragging or not
        static Transform sDragAndDropTransform = null;

        // TODO: find a way to efficiently map between transforms and binding ids without needing to pass boneMappings/index along to TransformField
        static void DoTransformBoneField(Rect position, int id, RigAuthoring rigAuthoring, TransformBindingID bindingID, List<RigIndexToBone> boneMappings, int index, GUIStyle style, bool fixupChildren = false)
        {
            int boneMappingIndex = boneMappings.FindIndex(m => m.Index == index);
            Transform transform = boneMappingIndex != -1 ? boneMappings[boneMappingIndex].Bone : null;
            var rootTransform = (rigAuthoring.TargetSkeletonRoot != null) ? rigAuthoring.TargetSkeletonRoot : rigAuthoring.transform;
            bool isValid = IsTransformFieldEnabled(rigAuthoring, bindingID, rootTransform);
            var evt = Event.current;
            var eventType = evt.GetTypeForControl(id);
            var prevEnabled = GUI.enabled;
            GUI.enabled = prevEnabled && isValid;
            try
            {
                var mouseOver = position.Contains(Event.current.mousePosition);

                switch (eventType)
                {
                    case EventType.MouseDrag:
                    {
                        if (!mouseOver)
                            break;
                        if (EditorGUI.showMixedValue)
                            break;

                        // Is the transform valid?
                        if (transform != null)
                        {
                            DragAndDrop.StartDrag("Dragging Transform");
                            DragAndDrop.objectReferences = new UnityEngine.Object[] { transform };
                            evt.Use();
                        }
                        break;
                    }
                    case EventType.DragUpdated:
                    {
                        if (!mouseOver)
                            break;

                        // Try to see if we're dropping a Transform
                        var dropTransformObject = (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) ? null : DragAndDrop.objectReferences[0];
                        var dropTransform = (dropTransformObject is GameObject) ? ((GameObject)dropTransformObject).transform :
                            dropTransformObject as Transform;

                        sAreDragging = true;
                        sDragAndDropTransform = dropTransform;

                        // Can we accept this particular skeleton?
                        if (rigAuthoring.CanTransformBeSet(bindingID, rootTransform, dropTransform))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                            DragAndDrop.activeControlID = id;
                            evt.Use();
                            break;
                        }

                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        break;
                    }
                    case EventType.DragPerform:
                    {
                        sAreDragging = false;
                        sDragAndDropTransform = null;
                        if (!mouseOver)
                            break;

                        // Try to see if we're dropping a Transform
                        var dropTransformObject = (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0) ? null : DragAndDrop.objectReferences[0];
                        var dropTransform = (dropTransformObject is GameObject) ? ((GameObject)dropTransformObject).transform :
                            dropTransformObject as Transform;

                        // Can we accept this particular skeleton?
                        if (rigAuthoring.CanTransformBeSet(bindingID, rootTransform, dropTransform))
                        {
                            DragAndDrop.AcceptDrag();
                            DragAndDrop.activeControlID = 0;

                            // Select our field
                            GUIUtility.keyboardControl = id;
                            EditorGUIUtility.editingTextField = false;
                            GUI.changed = true;

                            Undo.RecordObject(rigAuthoring, "Edit Bone Mapping");
                            rigAuthoring.OverrideTransformBinding(bindingID, dropTransform);
                            if (fixupChildren)
                                rigAuthoring.AutoSetChildOrSiblingTransforms(bindingID, dropTransform);
                            GUI.changed = true;

                            evt.Use();
                            break;
                        }

                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        break;
                    }
                    case EventType.DragExited:
                    case EventType.MouseMove:
                    case EventType.MouseUp:
                    {
                        sAreDragging = false;
                        sDragAndDropTransform = null;
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
                            // Single click on field: ping transform (if valid)
                            if (Event.current.clickCount == 1)
                            {
                                if (!anyModifiersPressed && isValid)
                                    EditorGUIUtility.PingObject(transform);
                                evt.Use();
                            }
                            // Double click on field: select transform
                            else if (Event.current.clickCount == 2)
                            {
                                if (!anyModifiersPressed && isValid)
                                    Selection.activeObject = transform;
                                evt.Use();
                            }
                        }
                        else
                        {
                            evt.Use();

                            // We didn't click on the field, but we clicked on the picker button instead
                            // TODO: implement picker
                            TogglePicker(position, id, rigAuthoring, bindingID, transform);
                        }
                        break;
                    }

                    case EventType.ExecuteCommand:
                    {
                        string commandName = evt.commandName;
                        if (commandName == TransformBonePickerWindow.WindowUpdatedCommand && TransformBonePickerWindow.GetControlID() == id && GUIUtility.keyboardControl == id)
                        {
                            // If the value given by the bone picker window is valid, we set it
                            if (!TransformBonePickerWindow.GetValue(id, out Transform value))
                                break;

                            Undo.RecordObject(rigAuthoring, "Edit Bone Mapping");
                            rigAuthoring.OverrideTransformBinding(bindingID, value);
                            transform = value;
                            if (fixupChildren)
                                rigAuthoring.AutoSetChildOrSiblingTransforms(bindingID, value);
                            GUI.changed = true;
                            evt.Use();
                            break;
                        }
                        else if (commandName == TransformBonePickerWindow.WindowCancelledCommand && TransformBonePickerWindow.GetControlID() == id && GUIUtility.keyboardControl == id)
                        {
                            // Cancelled picker, revert to value before the picker was opened
                            Undo.RevertAllDownToGroup(s_StartPickerGroup);
                            GUIUtility.ExitGUI();
                        }
                        else if (commandName == TransformBonePickerWindow.WindowClosedCommand && TransformBonePickerWindow.GetControlID() == id && GUIUtility.keyboardControl == id)
                        {
                            // Collapse all undo/redo operations between when the picker was opened, and when it was closed, to a single undo operation
                            Undo.CollapseUndoOperations(s_StartPickerGroup);
                        }
                        break;
                    }
                    case EventType.KeyDown:
                    {
                        if (GUIUtility.keyboardControl != id)
                            break;

                        // Clear the transform when pressing backspace or delete
                        if (evt.keyCode == KeyCode.Backspace || evt.keyCode == KeyCode.Delete)
                        {
                            if ((evt.modifiers & EventModifiers.Shift) == 0)
                            {
                                Undo.RecordObject(rigAuthoring, "Edit Bone Mapping");
                                rigAuthoring.OverrideTransformBinding(bindingID, null);
                                if ((evt.modifiers & EventModifiers.Alt) != 0)
                                    rigAuthoring.SetChildrenManualOverrideToNull(bindingID);
                                GUI.changed = true;
                            }
                            else
                            {
                                Undo.RecordObject(rigAuthoring, "Edit Bone Mapping");
                                rigAuthoring.ClearManualOverride(bindingID);
                                if ((evt.modifiers & EventModifiers.Alt) != 0)
                                    rigAuthoring.ClearChildrenManualOverride(bindingID);
                                GUI.changed = true;
                            }
                            evt.Use();
                        }
                        else
                        // When we press enter/return we open up the picker
                        if (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return)
                        {
                            evt.Use();
                            TogglePicker(position, id, rigAuthoring, bindingID, transform);
                        }
                        /*
                        // TODO: figure out a way to make the search text field not select all the text on focus, but instead just place cursor at end
                        // When we press a letter, open the picker and already type in that letter in the search field
                        else
                        {
                            if (char.IsLetterOrDigit(evt.character))
                            {
                                evt.Use();
                                TogglePicker(position, id, rigAuthoring, bindingID, transform, searchFilter: $"{evt.character}");
                            }
                        }*/
                        break;
                    }
                    case EventType.Repaint:
                    {
                        // Render title w/ icon
                        var objectNameContent = GetObjectGUIContent(rigAuthoring, bindingID, rootTransform, transform);
                        var labelContent = position;
                        var oldIconSize = EditorGUIUtility.GetIconSize();
                        EditorGUIUtility.SetIconSize(Styles.InlineIconSize);
                        if (style == Styles.objectField && objectNameContent.image == AnimationIcons.WarnIcon)
                        {
                            // Ensure we show the warning icon because it's important
                            style.Draw(labelContent, warningGUIContent, id, DragAndDrop.activeControlID == id, mouseOver);
                            objectNameContent.image = null;
                            var textStyle = Styles.objectFieldText;
                            textStyle.Draw(labelContent, objectNameContent, id, DragAndDrop.activeControlID == id, mouseOver);
                            objectNameContent.image = AnimationIcons.WarnIcon;
                        }
                        else
                            style.Draw(labelContent, objectNameContent, id, DragAndDrop.activeControlID == id, mouseOver);
                        EditorGUIUtility.SetIconSize(oldIconSize);

                        // Render picker button
                        Rect buttonRect = Styles.objectFieldButton.margin.Remove(GetPickerButtonRect(position));
                        Styles.objectFieldButton.Draw(buttonRect, GUIContent.none, id, DragAndDrop.activeControlID == id, mouseOver);
                        break;
                    }
                }
            }
            finally
            {
                GUI.enabled = prevEnabled;
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
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, options);
            AnimationGUI.BoneField(r, property, label);
        }

        public static void BoneField(SerializedProperty property, Skeleton defaultSkeleton, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, options);
            AnimationGUI.BoneField(r, property, defaultSkeleton, label);
        }
    }
}
