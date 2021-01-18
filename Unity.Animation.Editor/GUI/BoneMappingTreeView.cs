using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Animation.Hybrid;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Animation.Authoring.Editor
{
    [Serializable]
    class BoneMappingTreeViewState : TreeViewState
    {
        [SerializeField] public float m_CurrentTransformBoneWidth = -1;
    }

    class BoneMappingTreeView : TreeView
    {
        MultiColumnHeaderState headerState;

        bool defaultWidthInitialized = false;

        public new BoneMappingTreeViewState state
        {
            get { return base.state as BoneMappingTreeViewState; }
        }

        public BoneMappingTreeView(BoneMappingTreeViewState state)
            : base(state)
        {
            this.headerState = new MultiColumnHeaderState(new MultiColumnHeaderState.Column[]
            {
                new MultiColumnHeaderState.Column()
                {
                    headerContent = Content.s_SourceHeader,
                    headerTextAlignment = TextAlignment.Left,
                    width = 300,
                    minWidth = 60,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = false
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent = Content.s_TargetHeader,
                    headerTextAlignment = TextAlignment.Left,
                    width = 200,
                    minWidth = 60,
                    autoResize = true,
                    allowToggleVisibility = false,
                    canSort = false
                }
            });
            this.multiColumnHeader = new MultiColumnHeader(headerState)
            {
                canSort = false
            };
            this.useScrollView = false;
            this.showBorder = true;
            this.columnIndexForTreeFoldouts = 0;
            defaultWidthInitialized = false;
        }

        [field: SerializeField] public bool     EditMode            { get; set; }
        [field: SerializeField] public RigAuthoring RigAuthoring    { get; set; }

        public Skeleton Skeleton
        {
            get => RigAuthoring.Skeleton;
        }

        [field: SerializeField] public bool     IsEmpty             { get; private set; }
        [field: SerializeField] internal bool   HaveMoreThan1Child  { get; private set; }

        protected override bool CanMultiSelect(TreeViewItem item) { return !EditMode; }

        static readonly Dictionary<TransformBindingID, TreeViewItem> s_Parents = new Dictionary<TransformBindingID, TreeViewItem>();
        static readonly List<TreeViewItem> s_TreeViewItems = new List<TreeViewItem>();

        HashSet<int> activeIndices = new HashSet<int>();
        Dictionary<int, TransformBindingID> bindingIndices = new Dictionary<int, TransformBindingID>();

        List<RigIndexToBone> boneMappings = new List<RigIndexToBone>();

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem rootTreeViewItem = null;

            boneMappings.Clear();
            if (Skeleton != null)
            {
                ((IRigAuthoring)RigAuthoring).GetBones(boneMappings);

                var activeTransformChannels = new List<TransformChannel>();
                Skeleton.GetAllTransforms(activeTransformChannels);

                // First pass - create the bones.
                s_TreeViewItems.Clear();
                bindingIndices.Clear();
                s_Parents.Clear();
                activeIndices.Clear();
                for (int i = 0; i < activeTransformChannels.Count; ++i)
                {
                    var transformBindingID  = activeTransformChannels[i].ID;
                    var treeViewID          = transformBindingID.ID.GetHashCode();
                    var treeViewItem        = new TreeViewItem(treeViewID, -1, transformBindingID.Name);
                    bindingIndices[treeViewID] = transformBindingID;
                    activeIndices.Add(treeViewID);
                    s_TreeViewItems.Add(treeViewItem);
                    s_Parents[transformBindingID] = treeViewItem;
                }
                s_TreeViewItems.Sort();

                var rootBoneID = Skeleton.Root;
                if (rootBoneID == TransformBindingID.Invalid)
                    rootBoneID = TransformBindingID.Root;

                // Build parent bones.
                for (int i = 0; i < s_TreeViewItems.Count; ++i)
                {
                    var treeViewID          = s_TreeViewItems[i].id;
                    if (!bindingIndices.TryGetValue(treeViewID, out var transformBindingID) ||
                        transformBindingID == TransformBindingID.Invalid)
                        continue;
                    if (s_Parents.TryGetValue(transformBindingID.GetParent(), out var parentTreeView))
                        parentTreeView.AddChild(s_TreeViewItems[i]);
                    if (transformBindingID == rootBoneID)
                        rootTreeViewItem = s_TreeViewItems[i];
                }
            }

            if (rootTreeViewItem == null)
            {
                rootTreeViewItem = new TreeViewItem { id = -1, depth = -1 };
                IsEmpty = true;
                HaveMoreThan1Child = false;
            }
            else
            {
                IsEmpty = false;
                HaveMoreThan1Child = !IsEmpty & rootTreeViewItem.children != null && (rootTreeViewItem.children.Count > 1 || (rootTreeViewItem.children[0].children != null && rootTreeViewItem.children[0].children.Count > 0));
                var newRootTreeViewItem = new TreeViewItem { id = -1, depth = -1 };
                newRootTreeViewItem.AddChild(rootTreeViewItem);
                if (string.IsNullOrEmpty(rootTreeViewItem.displayName))
                    rootTreeViewItem.displayName = "<root>";
                rootTreeViewItem = newRootTreeViewItem;
            }

            if (IsEmpty)
            {
                var dummy = new TreeViewItem { id = -1, depth = 0 };
                rootTreeViewItem.AddChild(dummy);
            }
            SetupDepthsFromParentsAndChildren(rootTreeViewItem);

            return rootTreeViewItem;
        }

        protected void SourceRowGUI(Rect cellRect, ref RowGUIArgs args)
        {
            args.rowRect = cellRect;
            base.RowGUI(args);
        }

        static class Styles
        {
            public static readonly Vector2 InlineIconSize = new Vector2(12f, 12f);

            static GUIStyle m_TargetLabelField;
            public static GUIStyle TargetLabelField => (m_TargetLabelField != null) ? m_TargetLabelField : m_TargetLabelField = new GUIStyle(EditorStyles.label) { richText = true };

            static GUIStyle s_EdgeStyle;
            public static GUIStyle EdgeStyle
            {
                get
                {
                    if (s_EdgeStyle != null)
                        return s_EdgeStyle;

                    var color = EditorStyles.label.normal.textColor;
                    color.a *= 0.3f;

                    var colorTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false, true);
                    colorTexture.hideFlags = HideFlags.HideAndDontSave;
                    colorTexture.alphaIsTransparency = true;
                    var colors = new Color32[4 * 4];
                    for (int i = 0; i < colors.Length; i++) colors[i] = color;
                    colorTexture.SetPixels32(colors);
                    colorTexture.Apply();

                    s_EdgeStyle = new GUIStyle(GUIStyle.none)
                    {
                        normal = new GUIStyleState { background = colorTexture },
                        fixedWidth = 1,
                        clipping = TextClipping.Clip
                    };
                    return s_EdgeStyle;
                }
            }
        }

        static class Content
        {
            public const string kEmptyTransformText             = "<b>None</b> (Transform)";
            public const string kMissingTransformText           = "<b>Missing</b> (Transform)";

            public const string kBoneFoundAutomaticallyTooltip  = "This transform has been automatically found in the transform hierarchy based on the Skeleton.";
            public const string kHasBoneOverrideTooltip         = "This transform has been overriden with a user specified transform.";
            public const string kInvalidHierarchyTooltip        = "The transform is not a child or sibling transform of the transform assigned to the parent skeleton bone.";
            public const string kNotDescendantOfRootTooltip     = "The transform is not a child transform, or a descendant, of the root.";
            public const string kDuplicatedTransformTooltip     = "The same transform is assigned to multiple bones, which is not allowed.";
            const string kNullBoneOverrideTooltip               = "This transform has been overriden with an empty transform.";
            const string kMissingBoneOverrideTooltip            = "This transform has been overriden with a transform that's missing.";
            const string kClearBoneOverrideTooltip              = "This transform has been overriden with a user specified transform.\n\nClick to remove overridden transform and revert to an automatically found transform";
            const string kMatchedTransformTooltip               = "This transform has been automatically matched with a skeleton bone";
            const string kUnableToFindBoneTooltip               = "Failed to automatically find skeleton bone in transform hierarchy";

            public static readonly GUIContent s_UnableToFindBone        = EditorGUIUtility.TrTextContent(kEmptyTransformText, kUnableToFindBoneTooltip, AnimationIcons.WarnIcon);
            public static readonly GUIContent s_NullOverrideBone        = EditorGUIUtility.TrTextContent(kEmptyTransformText, kNullBoneOverrideTooltip, AnimationIcons.WarnIcon);
            public static readonly GUIContent s_MissingBone             = EditorGUIUtility.TrTextContent(kMissingTransformText, kMissingBoneOverrideTooltip, AnimationIcons.WarnIcon);

            public static readonly GUIContent s_WarningContent          = EditorGUIUtility.TrTextContent(string.Empty, kUnableToFindBoneTooltip, EditorGUIUtility.IconContent("console.warnicon").image);
            public static readonly GUIContent s_MatchedTransformContent = EditorGUIUtility.TrTextContent(string.Empty, kMatchedTransformTooltip, EditorGUIUtility.IconContent("Linked").image);
            public static readonly GUIContent s_ClearOverrideContent    = EditorGUIUtility.TrTextContent(string.Empty, kClearBoneOverrideTooltip, EditorGUIUtility.IconContent("UnLinked").image);

            public readonly static GUIContent s_SourceHeader            = EditorGUIUtility.TrTextContent("Source", AnimationIcons.SkeletonIcon);
            public readonly static GUIContent s_TargetHeader            = EditorGUIUtility.TrTextContent("Target", AnimationIcons.TransformIcon);

            public readonly static GUIContent OverrideAddedOverlay      = new GUIContent(AnimationIcons.OverrideAddedOverlay);
            public readonly static GUIContent OverrideRemovedOverlay    = new GUIContent(AnimationIcons.OverrideRemovedOverlay);

            static GUIContent tempContent = new GUIContent();
            public static GUIContent TempContent(string text, Texture2D icon = null, string tooltip = null)
            {
                tempContent.text = text;
                tempContent.image = icon;
                tempContent.tooltip = tooltip;
                return tempContent;
            }

            public static GUIContent TempContent(Texture2D icon, string tooltip = null)
            {
                return TempContent(string.Empty, icon, tooltip);
            }
        }


        void ValidateColumnWidths(Rect rect)
        {
            if (headerState.columns[kTransformBoneColumn].width < kMinColumnWidth)
            {
                headerState.columns[kTransformBoneColumn].width = kMinColumnWidth;
                headerState.columns[kSkeletonBoneColumn].width = rect.width - headerState.columns[kTransformBoneColumn].width;
            }
            if (headerState.columns[kSkeletonBoneColumn].width < kMinColumnWidth)
            {
                headerState.columns[kSkeletonBoneColumn].width = kMinColumnWidth;
                headerState.columns[kTransformBoneColumn].width = rect.width - headerState.columns[kSkeletonBoneColumn].width;
            }
            if (state.m_CurrentTransformBoneWidth != headerState.columns[kTransformBoneColumn].width)
            {
                state.m_CurrentTransformBoneWidth = headerState.columns[kTransformBoneColumn].width;
            }
        }

        const float kDefaultColumnWidth = 200f;
        const float kMinColumnWidth     = 32;

        const int   kSkeletonBoneColumn = 0;
        const int   kTransformBoneColumn = 1;
        const float kSizerPadding = 3;
        readonly int kSizerHash = "Sizer".GetHashCode();
        int kSizerID;
        float prevWidth = 0;
        float currSizePosition = 0;

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (!defaultWidthInitialized)
                {
                    if (state.m_CurrentTransformBoneWidth < 0)
                        state.m_CurrentTransformBoneWidth = kDefaultColumnWidth;
                    headerState.columns[kTransformBoneColumn].width = state.m_CurrentTransformBoneWidth;
                    headerState.columns[kSkeletonBoneColumn].width = rect.width - state.m_CurrentTransformBoneWidth;
                    defaultWidthInitialized = true;
                }
                else if (prevWidth == rect.width)
                {
                    headerState.columns[kTransformBoneColumn].width = rect.width - headerState.columns[kSkeletonBoneColumn].width;
                    ValidateColumnWidths(rect);
                }
                prevWidth = rect.width;
            }

            kSizerID = GUIUtility.GetControlID(kSizerHash, FocusType.Passive);

            var seperatorRect = rect;
            seperatorRect.width = 1;
            seperatorRect.xMin -= kSizerPadding;
            seperatorRect.xMax += kSizerPadding;
            seperatorRect.yMin += 28;
            seperatorRect.x = headerState.columns[kSkeletonBoneColumn].width + 15;


            var evt = Event.current;
            var sizerEvent = evt.GetTypeForControl(kSizerID);
            switch (sizerEvent)
            {
                case EventType.MouseDown:
                {
                    if (evt.button == 0 && GUIUtility.hotControl == 0 && seperatorRect.Contains(evt.mousePosition))
                    {
                        currSizePosition = headerState.columns[kSkeletonBoneColumn].width;
                        GUIUtility.hotControl = kSizerID;
                        evt.Use();
                    }
                    break;
                }
                case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl != kSizerID)
                        break;

                    currSizePosition += evt.delta.x;
                    headerState.columns[kSkeletonBoneColumn].width = currSizePosition;
                    headerState.columns[kTransformBoneColumn].width = rect.width - currSizePosition;
                    ValidateColumnWidths(rect);
                    evt.Use();
                    break;
                }
                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl != kSizerID)
                        break;

                    GUIUtility.hotControl = 0;
                    evt.Use();
                    break;
                }
            }
            EditorGUIUtility.AddCursorRect(seperatorRect, MouseCursor.ResizeHorizontal);

            base.OnGUI(rect);


            if (sizerEvent == EventType.Repaint)
            {
                seperatorRect.xMin += kSizerPadding;
                seperatorRect.xMax -= kSizerPadding;
                Styles.EdgeStyle.Draw(seperatorRect, GUIContent.none, kSizerID);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            if (!bindingIndices.TryGetValue(id, out var transformBindingID) ||
                transformBindingID == TransformBindingID.Invalid)
                return;

            int index = Skeleton.QueryTransformIndex(transformBindingID);
            if (index == -1)
                return;

            int boneMappingIndex = boneMappings.FindIndex(m => m.Index == index);
            Transform bone = boneMappingIndex != -1 ? boneMappings[boneMappingIndex].Bone : null;
            if (bone != null)
                EditorGUIUtility.PingObject(bone);
        }

        const float kResetButtonWidth = 20;
        const float kResetButtonOffset = kResetButtonWidth + 4;

        protected void ResetButtonGUI(Rect cellRect, int index, TransformBindingID bindingID)
        {
            int boneMappingIndex = boneMappings.FindIndex(m => m.Index == index);
            Transform bone = boneMappingIndex != -1 ? boneMappings[boneMappingIndex].Bone : null;
            if (RigAuthoring.HasManualOverride(bindingID))
            {
                if (GUI.Button(cellRect, Content.s_ClearOverrideContent, EditorStyles.miniButtonLeft))
                {
                    Undo.RecordObject(RigAuthoring, "Edit Bone Mapping");
                    RigAuthoring.ClearManualOverride(bindingID);
                    Reload();
                }
            }
            else
            {
                var enabled = GUI.enabled;
                GUI.enabled = (bone == null) && enabled;
                var content = (bone == null) ? Content.s_WarningContent : Content.s_MatchedTransformContent;
                GUI.Label(cellRect, content);
                GUI.enabled = enabled;
            }
        }

        // TODO: turn into checkbox
        const bool fixupChildren = true;

        protected void TargetRowGUI(Rect cellRect, int index, TransformBindingID bindingID)
        {
            if (EditMode)
            {
                var objectRect = cellRect;
                EditorGUI.BeginChangeCheck();
                // TODO: find a way to efficiently map between transforms and binding ids without needing to pass boneMappings/index along to TransformField
                AnimationGUI.TransformBoneField(objectRect, RigAuthoring, bindingID, boneMappings, index, fixupChildren);
                if (EditorGUI.EndChangeCheck())
                    Reload();
            }
            else if (Event.current.type == EventType.Repaint)
            {
                int boneMappingIndex = boneMappings.FindIndex(m => m.Index == index);
                var bone = boneMappingIndex != -1 ? boneMappings[boneMappingIndex].Bone : null;
                var missingReference = !ReferenceEquals(bone, null) && bone.GetInstanceID() != 0;

                GUIContent labelContent;
                GUIContent labelOverlay = null;
                if (bone == null)
                {
                    if (missingReference)
                    {
                        labelContent = Content.s_MissingBone;
                        labelOverlay = Content.OverrideAddedOverlay;
                    }
                    else
                    {
                        if (RigAuthoring.HasManualOverride(bindingID))
                        {
                            labelContent = Content.s_NullOverrideBone;
                            labelOverlay = Content.OverrideRemovedOverlay;
                        }
                        else
                            labelContent = Content.s_UnableToFindBone;
                    }
                }
                else
                {
                    var icon = AnimationIcons.TransformIcon;
                    var tooltip = string.Empty;
                    if (RigAuthoring.HasManualOverride(bindingID))
                    {
                        var root = (RigAuthoring.TargetSkeletonRoot != null) ? RigAuthoring.TargetSkeletonRoot : RigAuthoring.transform;
                        if (root != bone && !bone.IsChildOf(root))
                        {
                            icon = AnimationIcons.WarnIcon;
                            tooltip = Content.kNotDescendantOfRootTooltip;
                        }
                        else if (!RigAuthoring.IsHierarchyValidForTransform(bindingID, root, bone))
                        {
                            icon = AnimationIcons.WarnIcon;
                            tooltip = Content.kInvalidHierarchyTooltip;
                        }
                        else if (RigAuthoring.GetBindingForTransform(bone) != bindingID)
                        {
                            icon = AnimationIcons.WarnIcon;
                            tooltip = Content.kDuplicatedTransformTooltip;
                        }
                        else
                        {
                            tooltip = Content.kHasBoneOverrideTooltip;
                            labelOverlay = Content.OverrideAddedOverlay;
                        }
                    }
                    else
                        tooltip = Content.kBoneFoundAutomaticallyTooltip;

                    labelContent = Content.TempContent(bone.name, icon, tooltip);
                }

                var oldIconSize = EditorGUIUtility.GetIconSize();
                EditorGUIUtility.SetIconSize(Styles.InlineIconSize);
                Styles.TargetLabelField.Draw(cellRect, labelContent, -1);
                if (labelOverlay != null)
                    Styles.TargetLabelField.Draw(cellRect, labelOverlay, -1);
                EditorGUIUtility.SetIconSize(oldIconSize);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var treeViewID = args.item.id;
            if (!bindingIndices.TryGetValue(treeViewID, out var transformBindingID) ||
                transformBindingID == TransformBindingID.Invalid)
                return;

            int index = Skeleton.QueryTransformIndex(transformBindingID);
            if (index == -1)
                return;

            var skeletonBoneRect    = args.GetCellRect(kSkeletonBoneColumn);
            var transformBoneRect   = args.GetCellRect(kTransformBoneColumn);
            if (EditMode)
            {
                var resetRect = transformBoneRect;
                resetRect.x -= kResetButtonOffset;
                resetRect.width = kResetButtonWidth;

                skeletonBoneRect.xMax = resetRect.xMin - 1;

                SourceRowGUI(skeletonBoneRect, ref args);
                ResetButtonGUI(resetRect, index, transformBindingID);
            }
            else
            {
                SourceRowGUI(skeletonBoneRect, ref args);
            }
            TargetRowGUI(transformBoneRect, index, transformBindingID);
        }
    }
}
