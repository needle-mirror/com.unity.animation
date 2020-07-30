using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Animation.Hybrid;
using UnityEditor.IMGUI.Controls;

namespace Unity.Animation.Editor
{
    internal class RigBoneTreeView : TreeView
    {
        static class Styles
        {
            public static readonly string IndexFormatString = L10n.Tr("(Index {0})");
            public static readonly GUIStyle Index =
                new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
        }

        public RigBoneTreeView(TreeViewState state)
            : base(state)
        {
            this.useScrollView = false;
            this.showBorder = true;
        }

        protected override bool CanStartDrag(CanStartDragArgs args) { return true; }

        public bool         EditMode        { get; set; }
        public RigComponent RigComponent    { get; set; }
        public int          NodeCount       { get { return s_TransformIndices.Count; } }


        static GUIContent s_MeshRendererContent;
        static GUIContent meshRendererContent => s_MeshRendererContent ?? (s_MeshRendererContent = new GUIContent(string.Empty, AssetPreview.GetMiniTypeThumbnail(typeof(MeshRenderer))));
        static GUIContent s_SkinnedMeshRendererContent;
        static GUIContent skinnedMeshRendererContent => s_SkinnedMeshRendererContent ?? (s_SkinnedMeshRendererContent = new GUIContent(string.Empty, AssetPreview.GetMiniTypeThumbnail(typeof(SkinnedMeshRenderer))));

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1 };

            s_TransformIndices.Clear();
            int index = 0;
            if (RigComponent != null)
            {
                var rootTransform = RigComponent.RootBone;
                if (!EditMode &&
                    !RigComponent.IsBoneIncluded(rootTransform))
                {
                    AddChildrenRecursive(rootTransform, root, ref index);
                }
                else
                {
                    s_TransformIndices[rootTransform] = index;
                    index++;
                    var rootItem = new TreeViewItem(rootTransform.GetInstanceID(), -1, rootTransform.name);
                    root.AddChild(rootItem);
                    if (rootTransform.childCount > 0)
                        AddChildrenRecursive(rootTransform, rootItem, ref index);
                }
            }

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        Transform GetTransform(int instanceID)
        {
            return (Transform)EditorUtility.InstanceIDToObject(instanceID);
        }

        static readonly Dictionary<Transform, int> s_TransformIndices = new Dictionary<Transform, int>();
        void AddChildrenRecursive(Transform parentTransform, TreeViewItem item, ref int index)
        {
            int childCount = parentTransform.childCount;

            item.children = new List<TreeViewItem>(childCount);
            for (int i = 0; i < childCount; ++i)
            {
                var childTransform = parentTransform.GetChild(i);

                if (!EditMode &&
                    !RigComponent.IsBoneIncluded(childTransform))
                    continue;

                s_TransformIndices[childTransform] = index;
                index++;

                var childItem = new TreeViewItem(childTransform.GetInstanceID(), -1, childTransform.name);
                item.AddChild(childItem);
                if (childTransform.childCount > 0)
                    AddChildrenRecursive(childTransform, childItem, ref index);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            var transform = GetTransform(id);
            EditorGUIUtility.PingObject(transform);
        }

        const float             kToggleWidth    = 16f;
        const float             kToggleSpacing  = 2f;
        const float             kIconSpacing    = 20f;
        static readonly string  kAddBone        = L10n.Tr("Add Bone");
        static readonly string  kRemoveBone     = L10n.Tr("Remove Bone");

        static GUIContent GetIconForTransform(Transform transform)
        {
            if (transform.TryGetComponent(out SkinnedMeshRenderer _))
                return skinnedMeshRendererContent;
            else if (transform.TryGetComponent(out MeshRenderer _))
                return meshRendererContent;
            return null;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var transform = GetTransform(args.item.id);
            if (transform == null)
                return;

            float offset = 0;

            if (EditMode)
            {
                extraSpaceBeforeIconAndLabel = kToggleWidth + kToggleSpacing;
                Rect toggleRect = args.rowRect;
                toggleRect.x += GetContentIndent(args.item);
                toggleRect.width = kToggleWidth;
                offset = toggleRect.x + kToggleWidth;

                var evt = Event.current;
                if (evt.type == EventType.MouseDown && toggleRect.Contains(evt.mousePosition))
                    SelectionClick(args.item, false);

                EditorGUI.BeginChangeCheck();
                var included = RigComponent.IsBoneIncluded(transform);
                included = EditorGUI.Toggle(toggleRect, included);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(RigComponent, included ? kAddBone : kRemoveBone);
                    if (included)
                    {
                        RigComponent.IncludeBoneAndAncestors(transform);
                        if (evt.alt)
                            RigComponent.IncludeBoneAndDescendants(transform);
                    }
                    else
                        RigComponent.ExcludeBoneAndDescendants(transform);
                    EditorUtility.SetDirty(RigComponent);
                    Reload();
                }
            }
            else
                extraSpaceBeforeIconAndLabel = 0;

            var iconContent = GetIconForTransform(transform);
            if (iconContent != null)
            {
                if (offset == 0)
                    offset = GetContentIndent(args.item);
                Rect iconRect;
                iconRect = args.rowRect;
                iconRect.x += offset;
                iconRect.width = kIconSpacing;
                extraSpaceBeforeIconAndLabel += kIconSpacing;
                GUI.Label(iconRect, iconContent);
            }

            var boneIndex = RigComponent.FindTransformIndex(transform);
            if (boneIndex >= 0)
            {
                if (!s_IndexLabels.TryGetValue(boneIndex, out var indexLabel))
                    s_IndexLabels[boneIndex] = indexLabel = string.Format(Styles.IndexFormatString, boneIndex);
                using (new EditorGUI.DisabledScope(true))
                    GUI.Label(args.rowRect, indexLabel, Styles.Index);
            }
            base.RowGUI(args);
        }

        static Dictionary<int, string> s_IndexLabels = new Dictionary<int, string>();

        List<UnityEngine.Object> s_SortedObjectList = new List<UnityEngine.Object>();
        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var sortedDraggedIDs = SortItemIDsInRowOrder(args.draggedItemIDs);
            s_SortedObjectList.Clear();
            if (s_SortedObjectList.Capacity < sortedDraggedIDs.Count)
                s_SortedObjectList.Capacity = sortedDraggedIDs.Count;
            foreach (var id in sortedDraggedIDs)
            {
                var obj = GetTransform(id);
                if (obj != null)
                    s_SortedObjectList.Add(obj);
            }

            DragAndDrop.objectReferences = s_SortedObjectList.ToArray();
            DragAndDrop.StartDrag(s_SortedObjectList.Count > 1 ? "<Multiple>" : s_SortedObjectList[0].name);

            s_SortedObjectList.Clear(); // clear references
        }
    }
}
