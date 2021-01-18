using System.Collections.Generic;
using System.Linq;
using Unity.Animation.Hybrid;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Animation.Authoring.Editor
{
    internal class SkeletonTreeView : TreeView
    {
        static class Styles
        {
            public static readonly string IndexFormatString = L10n.Tr("(Index {0})");
            public static readonly GUIStyle Index =
                new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
        }

        public SkeletonTreeView(TreeViewState state)
            : base(state)
        {
            this.useScrollView = false;
            this.showBorder = true;
        }

        [field: SerializeField] public bool     EditMode            { get; set; }
        [field: SerializeField] public Skeleton Skeleton            { get; set; }
        [field: SerializeField] public bool     IsEmpty             { get; private set; }
        [field: SerializeField] internal bool   HaveMoreThan1Child  { get; private set; }

        protected override bool CanMultiSelect(TreeViewItem item) { return !EditMode; }

        static readonly Dictionary<TransformBindingID, TreeViewItem> s_Parents = new Dictionary<TransformBindingID, TreeViewItem>();
        static readonly List<TreeViewItem> s_TreeViewItems = new List<TreeViewItem>();

        readonly HashSet<int> activeIndices = new HashSet<int>();
        readonly Dictionary<int, TransformBindingID> indexToBinding = new Dictionary<int, TransformBindingID>();
        readonly Dictionary<TransformBindingID, TreeViewItem> bindingToTreeViewItem = new Dictionary<TransformBindingID, TreeViewItem>();
        readonly Dictionary<int, int> idToIndex = new Dictionary<int, int>();
        readonly Dictionary<int, float> idToPosition = new Dictionary<int, float>();

        public TransformBindingID GetTransformBindingID(int treeViewID)
        {
            if (indexToBinding.TryGetValue(treeViewID, out var transformBindingID))
                return transformBindingID;
            return TransformBindingID.Invalid;
        }

        public float GetItemPosition(TransformBindingID id)
        {
            if (!bindingToTreeViewItem.TryGetValue(id, out var treeViewItem))
                return -1;
            if (!idToPosition.TryGetValue(treeViewItem.id, out var position))
                return -1;
            return position;
        }

        void SetIndicesAndPositions(TreeViewItem parent)
        {
            float position = 0;
            SetIndicesAndPositions(parent, ref position, EditorGUIUtility.singleLineHeight);
        }

        void SetIndicesAndPositions(TreeViewItem parent, ref float position, float stepSize)
        {
            if (activeIndices.Contains(parent.id))
                idToIndex[parent.id] = Skeleton.QueryTransformIndex(indexToBinding[parent.id]);
            idToPosition[parent.id] = position;
            position += stepSize;
            var children = parent.children;
            if (children == null)
                return;
            for (int i = 0; i < children.Count; i++)
                SetIndicesAndPositions(children[i], ref position, stepSize);
        }

        static readonly List<TransformChannel> s_TransformChannels = new List<TransformChannel>();

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem rootTreeViewItem = null;

            idToIndex.Clear();
            if (Skeleton != null)
            {
                // First pass - create the bones.
                indexToBinding.Clear();
                bindingToTreeViewItem.Clear();
                s_Parents.Clear();
                activeIndices.Clear();
                s_TreeViewItems.Clear();
                if (EditMode)
                {
                    Skeleton.GetAllTransforms(s_TransformChannels, TransformChannelSearchMode.ActiveAndInactiveRootDescendants);
                    for (int i = 0; i < s_TransformChannels.Count; ++i)
                    {
                        var transformBindingID = s_TransformChannels[i].ID;
                        var treeViewID = transformBindingID.ID.GetHashCode();
                        var treeViewItem = new TreeViewItem(treeViewID, -1, transformBindingID.Name);
                        indexToBinding[treeViewID] = transformBindingID;
                        bindingToTreeViewItem[transformBindingID] = treeViewItem;
                        if (Skeleton.GetTransformChannelState(transformBindingID) == TransformChannelState.Active)
                            activeIndices.Add(treeViewID);
                        s_TreeViewItems.Add(treeViewItem);
                        s_Parents[transformBindingID] = treeViewItem;
                    }
                }
                else
                {
                    Skeleton.GetAllTransforms(s_TransformChannels, TransformChannelSearchMode.ActiveRootDescendants);
                    for (int i = 0; i < s_TransformChannels.Count; ++i)
                    {
                        var transformBindingID = s_TransformChannels[i].ID;
                        var treeViewID = transformBindingID.ID.GetHashCode();
                        var treeViewItem = new TreeViewItem(treeViewID, -1, transformBindingID.Name);
                        indexToBinding[treeViewID] = transformBindingID;
                        bindingToTreeViewItem[transformBindingID] = treeViewItem;
                        activeIndices.Add(treeViewID);
                        s_TreeViewItems.Add(treeViewItem);
                        s_Parents[transformBindingID] = treeViewItem;
                    }
                }
                s_TreeViewItems.Sort();

                var rootBoneID = Skeleton.Root;
                if (rootBoneID == TransformBindingID.Invalid)
                    rootBoneID = TransformBindingID.Root;

                // Build parent bones.
                for (int i = 0; i < s_TreeViewItems.Count; ++i)
                {
                    var treeViewID          = s_TreeViewItems[i].id;
                    if (!indexToBinding.TryGetValue(treeViewID, out var transformBindingID) ||
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
                SetIndicesAndPositions(rootTreeViewItem);
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

        public void SetSelection(params TransformBindingID[] ids)
        {
            var items = new List<int>();
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (!bindingToTreeViewItem.TryGetValue(id, out var treeViewItem))
                    continue;
                items.Add(treeViewItem.id);
            }
            SetSelection(items);
        }

        internal void FrameItem(TransformBindingID id)
        {
            if (!bindingToTreeViewItem.TryGetValue(id, out var treeViewItem))
                return;
            FrameItem(treeViewItem.id);
        }

        const float             kToggleWidth    = 16f;
        const float             kToggleSpacing  = 2f;
        static readonly string  kAddBone        = L10n.Tr("Add Bone");
        static readonly string  kRemoveBone     = L10n.Tr("Remove Bone");

        static Dictionary<int, string> s_IndexLabels = new Dictionary<int, string>();
        protected override void RowGUI(RowGUIArgs args)
        {
            var treeViewID = args.item.id;
            if (!indexToBinding.TryGetValue(treeViewID, out var transformBindingID) ||
                transformBindingID == TransformBindingID.Invalid)
                return;
            if (!Skeleton.Contains(transformBindingID, TransformChannelSearchMode.ActiveAndInactiveAll))
                return;

            if (EditMode)
            {
                extraSpaceBeforeIconAndLabel = kToggleWidth + kToggleSpacing;
                Rect toggleRect = args.rowRect;
                toggleRect.x += GetContentIndent(args.item);
                toggleRect.width = kToggleWidth;

                var evt = Event.current;
                if (evt.type == EventType.MouseDown && toggleRect.Contains(evt.mousePosition))
                    SelectionClick(args.item, false);

                EditorGUI.BeginChangeCheck();
                var included = Skeleton.GetTransformChannelState(transformBindingID) == TransformChannelState.Active;
                included = EditorGUI.Toggle(toggleRect, included);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Skeleton, included ? kAddBone : kRemoveBone);
                    if (included)
                    {
                        Skeleton.SetTransformChannelAncestorsToActive(transformBindingID);
                        if (evt.alt)
                            Skeleton.SetTransformChannelDescendantsAndAncestorsToActive(transformBindingID);
                    }
                    else
                        Skeleton.SetTransformChannelDescendantsToInactive(transformBindingID, includeSelf: true);
                    EditorUtility.SetDirty(Skeleton);
                    Reload();
                }
            }
            else
                extraSpaceBeforeIconAndLabel = 0;


            if (idToIndex.TryGetValue(treeViewID, out var boneIndex))
            {
                if (!s_IndexLabels.TryGetValue(boneIndex, out var indexLabel))
                    s_IndexLabels[boneIndex] = indexLabel = string.Format(Styles.IndexFormatString, boneIndex);
                using (new EditorGUI.DisabledScope(true))
                    GUI.Label(args.rowRect, indexLabel, Styles.Index);
            }
            base.RowGUI(args);
        }

        protected override bool CanStartDrag(CanStartDragArgs args) { return true; }

        List<SkeletonBoneReference> s_SortedObjectList = new List<SkeletonBoneReference>();
        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var sortedDraggedIDs = SortItemIDsInRowOrder(args.draggedItemIDs);
            s_SortedObjectList.Clear();
            if (s_SortedObjectList.Capacity < sortedDraggedIDs.Count)
                s_SortedObjectList.Capacity = sortedDraggedIDs.Count;
            foreach (var treeViewID in sortedDraggedIDs)
            {
                if (!indexToBinding.TryGetValue(treeViewID, out var transformBindingID) ||
                    transformBindingID == TransformBindingID.Invalid)
                    continue;
                s_SortedObjectList.Add(new SkeletonBoneReference(Skeleton, transformBindingID));
            }

            DragAndDrop.SetGenericData(AnimationGUI.k_SkeletonBoneReferenceArray, s_SortedObjectList.ToArray());
            DragAndDrop.StartDrag(s_SortedObjectList.Count > 1 ? "<Multiple>" : s_SortedObjectList[0].ID.Name);

            s_SortedObjectList.Clear(); // clear references
        }
    }
}
