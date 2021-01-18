using System;
using System.Collections.Generic;
using Unity.Animation.Hybrid;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Animation.Authoring.Editor
{
    internal class TransformBoneSelectionTreeView : TreeView
    {
        public TransformBoneSelectionTreeView(TreeViewState state)
            : base(state)
        {
            this.useScrollView = true;
            this.showBorder = true;
        }

        public event Action<Transform> SelectionHasChanged;
        public event Action DoubleClicked;

        [SerializeField] List<Transform> transforms = new List<Transform>();

        [field: SerializeField] public RigAuthoring         RigAuthoring            { get; set; }
        [field: SerializeField] public TransformBindingID   DestinationBindingID    { get; set; }
        [field: SerializeField] public bool                 IsEmpty                 { get; private set; }

        protected override bool CanMultiSelect(TreeViewItem item) { return false; }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            if (RigAuthoring == null)
                return;
            if (selectedIds.Count == 0 || transforms == null || selectedIds[0] < 1 || selectedIds[0] > transforms.Count)
            {
                SelectionHasChanged?.Invoke(null);
            }
            else
                SelectionHasChanged?.Invoke(transforms[selectedIds[0] - 1]);
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            DoubleClicked?.Invoke();
        }

        static readonly List<int> sIntegerList = new List<int>();

        public void Select(Transform transform)
        {
            sIntegerList.Clear();
            if (transform == null)
            {
                this.SetSelection(sIntegerList);
                return;
            }
            if (!m_TransformToTreeView.TryGetValue(transform, out var treeView))
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

        readonly Dictionary<Transform, TreeViewItem> m_TransformToTreeView = new Dictionary<Transform, TreeViewItem>();
        static readonly List<TreeViewItem> s_TreeViewItems = new List<TreeViewItem>();

        protected override TreeViewItem BuildRoot()
        {
            var rootTreeViewItem = new TreeViewItem { id = -1, depth = -1 };
            var emptyTreeViewItem = new TreeViewItem(0, -1, "(none)");
            rootTreeViewItem.AddChild(emptyTreeViewItem);

            s_TreeViewItems.Clear();
            if (RigAuthoring != null)
            {
                RigAuthoring.GetAllowedTransformsForBinding(DestinationBindingID, transforms);

                // First pass - create the bones.
                m_TransformToTreeView.Clear();
                for (int i = 0; i < transforms.Count; ++i)
                {
                    var treeViewItem = new TreeViewItem(i + 1, -1, transforms[i].name);
                    s_TreeViewItems.Add(treeViewItem);
                    m_TransformToTreeView[transforms[i]] = treeViewItem;
                }

                // Build parent bones.
                for (int i = 0; i < s_TreeViewItems.Count; ++i)
                {
                    var channelIndex = s_TreeViewItems[i].id - 1;
                    var transform = transforms[channelIndex];
                    if (m_TransformToTreeView.TryGetValue(transform.parent, out var parentTreeView))
                        parentTreeView.AddChild(s_TreeViewItems[i]);
                    else
                        rootTreeViewItem.AddChild(s_TreeViewItems[i]);
                }
            }

            SetupDepthsFromParentsAndChildren(rootTreeViewItem);

            return rootTreeViewItem;
        }
    }
}
