using System;
using System.Collections.Generic;
using Unity.Animation.Hybrid;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Animation.Authoring.Editor
{
    internal class SkeletonBoneSelectionTreeView : TreeView
    {
        public SkeletonBoneSelectionTreeView(TreeViewState state)
            : base(state)
        {
            this.useScrollView = true;
            this.showBorder = true;
        }

        public event Action<SkeletonBoneReference> SelectionHasChanged;
        public event Action DoubleClicked;

        [SerializeField] List<TransformChannel> rootTransformChannels = new List<TransformChannel>();

        [field: SerializeField] public Skeleton Skeleton    { get; set; }
        [field: SerializeField] public bool     IsEmpty     { get; private set; }

        protected override bool CanMultiSelect(TreeViewItem item) { return false; }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            if (Skeleton == null)
                return;
            if (selectedIds.Count == 0 || rootTransformChannels == null || selectedIds[0] < 0 || selectedIds[0] >= rootTransformChannels.Count)
                SelectionHasChanged?.Invoke(new SkeletonBoneReference(Skeleton, TransformBindingID.Root));
            else
                SelectionHasChanged?.Invoke(new SkeletonBoneReference(Skeleton, rootTransformChannels[selectedIds[0]].ID));
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            DoubleClicked?.Invoke();
        }

        static readonly List<int> sIntegerList = new List<int>();

        public void Select(TransformBindingID bindingID)
        {
            sIntegerList.Clear();
            if (bindingID == TransformBindingID.Invalid)
            {
                this.SetSelection(sIntegerList);
                return;
            }
            if (!s_TransformBindingIDToTreeView.TryGetValue(bindingID, out var treeView))
            {
                this.SetSelection(sIntegerList);
                return;
            }
            var id = treeView.id;
            if (id != -1)
            {
                sIntegerList.Add(id);
                this.SetSelection(sIntegerList);
                this.FrameItem(id);
            }
        }

        readonly Dictionary<TransformBindingID, TreeViewItem> s_TransformBindingIDToTreeView = new Dictionary<TransformBindingID, TreeViewItem>();
        static readonly List<TreeViewItem> s_TreeViewItems = new List<TreeViewItem>();

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem rootTreeViewItem = null;

            if (Skeleton != null)
            {
                Skeleton.GetAllTransforms(rootTransformChannels, TransformChannelSearchMode.ActiveRootDescendants);

                // First pass - create the bones.
                s_TreeViewItems.Clear();
                s_TransformBindingIDToTreeView.Clear();
                for (int i = 0; i < rootTransformChannels.Count; ++i)
                {
                    var treeViewItem = new TreeViewItem(i, -1, rootTransformChannels[i].ID.Name);
                    s_TreeViewItems.Add(treeViewItem);
                    s_TransformBindingIDToTreeView[rootTransformChannels[i].ID] = treeViewItem;
                }

                s_TreeViewItems.Sort();

                var rootBoneID = Skeleton.Root;
                if (rootBoneID == TransformBindingID.Invalid)
                    rootBoneID = TransformBindingID.Root;
                // Build parent bones.
                for (int i = 0; i < s_TreeViewItems.Count; ++i)
                {
                    var channelIndex = s_TreeViewItems[i].id;
                    var ID = rootTransformChannels[channelIndex].ID;
                    if (s_TransformBindingIDToTreeView.TryGetValue(ID.GetParent(), out var parentTreeView))
                        parentTreeView.AddChild(s_TreeViewItems[i]);
                    if (ID == rootBoneID)
                        rootTreeViewItem = s_TreeViewItems[i];
                }
            }

            if (rootTreeViewItem == null)
            {
                rootTreeViewItem = new TreeViewItem { id = -1, depth = -1 };
                IsEmpty = true;
            }
            else
            {
                IsEmpty = rootTreeViewItem.children == null || rootTreeViewItem.children.Count == 0;
            }
            if (IsEmpty)
            {
                rootTreeViewItem.children = new List<TreeViewItem>() { new TreeViewItem(-1, -1, string.Empty) };
            }
            else
            {
                var newRootTreeViewItem = new TreeViewItem { id = -1, depth = -1 };
                newRootTreeViewItem.AddChild(rootTreeViewItem);
                rootTreeViewItem.displayName = "<root>";
                rootTreeViewItem = newRootTreeViewItem;;
            }

            SetupDepthsFromParentsAndChildren(rootTreeViewItem);

            return rootTreeViewItem;
        }
    }
}
