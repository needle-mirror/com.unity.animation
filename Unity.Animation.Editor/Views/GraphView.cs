using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class GraphView : GtfoGraphView, IDropTarget
    {
        bool m_DragStarted;

        public override bool SupportsWindowedBlackboard => true;

        public GraphView(GraphViewEditorWindow window, Store store, string graphViewName = "Animation Graph")
            : base(window, store, graphViewName)
        {
            OnSelectionChangedCallback += OnSelectionChanged;
            SetupZoom(0.025f, 1.0f, 1.0f);
        }

        public void OnSelectionChanged(List<ISelectableGraphElement> obj)
        {
            var edge = obj.OfType<IGraphElement>().Select(e => e.Model).FirstOrDefault();
            var transition = edge as ITransitionPropertiesModel;
            GlobalTransitionHolder.Instance.SetCurrentTransition(transition, Store.State.GraphModel.AssetModel);
        }

        public EventPropagation RemoveSelection()
        {
            IInOutPortsNode[] selectedNodes = Selection.OfType<IGraphElement>()
                .Select(x => x.Model).OfType<IInOutPortsNode>().ToArray();

            IInOutPortsNode[] connectedNodes = selectedNodes.Where(x => x.InputsById.Values
                .Any(y => y.IsConnected()) && x.OutputsById.Values.Any(y => y.IsConnected()))
                .ToArray();

            bool canSelectionBeBypassed = connectedNodes.Any();
            if (canSelectionBeBypassed)
                Store.Dispatch(new BypassNodesAction(connectedNodes, selectedNodes.ToArray<INodeModel>()));
            else
                Store.Dispatch(new DeleteElementsAction(selectedNodes.Cast<IGraphElementModel>().ToArray()));

            return selectedNodes.Any() ? EventPropagation.Stop : EventPropagation.Continue;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var graphModel = Store.State.GraphModel;

            bool isStateMachine = graphModel is StateMachineModel;

            Vector2 currentPos = contentViewContainer.WorldToLocal(evt.mousePosition);

            if (isStateMachine)
            {
                if (evt.target == this)
                {
                    evt.menu.AppendAction("Add State Machine State", menuAction =>
                    {
                        GUID guid = GUID.Generate();
                        Store.Dispatch(new CreateStateAction(currentPos, CreateStateAction.StateType.StateMachineState, guid));
                        m_NodeToRename = guid;
                    });

                    evt.menu.AppendAction("Add Graph State", menuAction =>
                    {
                        GUID guid = GUID.Generate();
                        Store.Dispatch(new CreateStateAction(currentPos, CreateStateAction.StateType.GraphState, guid));
                        m_NodeToRename = guid;
                    });
                }

                if (evt.menu.MenuItems().Count != 0)
                    evt.menu.AppendSeparator();

                var models = Selection.OfType<GraphElement>().Select(e => e.Model).ToArray();

                evt.menu.AppendAction("Cut", (a) => { CutSelectionCallback(); },
                    CanCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendAction("Copy", (a) => { CopySelectionCallback(); },
                    CanCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendAction("Paste", (a) => { PasteCallback(); },
                    CanPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendSeparator();

                evt.menu.AppendAction("Duplicate", (a) => { DuplicateSelectionCallback(); },
                    CanDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendAction("Delete", _ =>
                {
                    Store.Dispatch(new DeleteElementsAction(models));
                }, CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
            else
            {
                base.BuildContextualMenu(evt);
            }
        }

        GUID? m_NodeToRename = null;
        bool m_RequestFrameAll = false;
        public override void UpdateUI(UIRebuildType rebuildType)
        {
            base.UpdateUI(rebuildType);

            if (m_RequestFrameAll)
            {
                schedule.Execute(FrameAll);
                m_RequestFrameAll = false;
            }

            if (m_NodeToRename == null)
                return;

            var graphModel = Store.State.GraphModel;
            var nodeToRename = graphModel.NodesByGuid[m_NodeToRename.Value];
            var nodeUI = nodeToRename?.GetUI<Node>(this);
            nodeUI?.EditTitle();
            m_NodeToRename = null;
        }

        static IDropTarget GetDropTarget(Vector2 pickPoint, VisualElement target)
        {
            var pickList = new List<VisualElement>();
            var pickElem = target.panel.PickAll(pickPoint, pickList);
            bool targetIsGraphView = target is GraphView;

            IDropTarget dropTarget = null;
            foreach (var pickItem in pickList)
            {
                if (!targetIsGraphView)
                    continue;

                dropTarget = pickItem as IDropTarget;
                if (dropTarget == null)
                    continue;

                // found one
                break;
            }

            return dropTarget ?? pickElem as IDropTarget;
        }

        protected override void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            if (DragAndDrop.objectReferences.Length > 0)
            {
                base.OnDragUpdatedEvent(e);
            }
            else
            {
                m_CurrentDragNDropHandler = null;

                bool containsBlackboardFields = Selection.OfType<BlackboardField>().Any();
                if (!containsBlackboardFields)
                    return;

                IDropTarget dropTarget = GetDropTarget(e.mousePosition, e.target as VisualElement);
                dropTarget?.DragUpdated(e, Selection, dropTarget, Blackboard);

                if (dropTarget != null && dropTarget.CanAcceptDrop(Selection))
                    DragAndDrop.visualMode = e.ctrlKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                else
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
            e.StopPropagation();
        }

        protected override void OnDragPerformEvent(DragPerformEvent e)
        {
            if (m_CurrentDragNDropHandler != null)
            {
                base.OnDragPerformEvent(e);
            }
            else
            {
                bool containsBlackboardFields = Selection.OfType<BlackboardField>().Any();
                if (!containsBlackboardFields)
                    return;

                IDropTarget dropTarget = GetDropTarget(e.mousePosition, e.target as VisualElement);
                dropTarget?.DragPerform(e, Selection, dropTarget, Blackboard);
            }
            e.StopPropagation();
        }

        protected override void OnDragExitedEvent(DragExitedEvent e)
        {
            base.OnDragExitedEvent(e);

            if (!Selection.OfType<BlackboardField>().Any())
                return;

            // TODO: How to differentiate between case where mouse has left a drop target window and a true drag operation abort?
            IDropTarget dropTarget = GetDropTarget(e.mousePosition, e.target as VisualElement);
            dropTarget?.DragExited();
            e.StopPropagation();
        }

        public override IEnumerable<(IVariableDeclarationModel, SerializableGUID, Vector2)> ExtractVariablesFromDroppedElements(
            IReadOnlyCollection<GraphElement> dropElements,
            Vector2 initialPosition)
        {
            var elementOffset = Vector2.zero;
            var variablesToCreate = new List<(IVariableDeclarationModel, SerializableGUID, Vector2)>();

            foreach (var dropElement in dropElements)
            {
                if (dropElement is BlackboardField blackboardField)
                {
                    Vector2 pos = contentViewContainer.WorldToLocal(initialPosition) + elementOffset;
                    elementOffset.y += (blackboardField).layout.height + DragDropSpacer;

                    if (!variablesToCreate.Any(x => ReferenceEquals(x.Item1, blackboardField.Model)))
                        variablesToCreate.Add((blackboardField.Model as IVariableDeclarationModel, GUID.Generate(), pos));
                }
            }
            return variablesToCreate;
        }

        public override bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection)
        {
            bool isStateMachine = (Store.State.GraphModel is StateMachineModel);
            return !isStateMachine && dragSelection.Any(x =>
                x is BlackboardField blackboardField && blackboardField.CanInstantiateInGraph());
        }

        public override bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource)
        {
            DragSetup(evt, dragSelection, dropTarget, dragSource);
            return true;
        }

        public override bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource)
        {
            var dragSelectionList = dragSelection.ToList();

            DragSetup(evt, dragSelectionList, dropTarget, dragSource);
            List<GraphElement> dropElements = dragSelectionList.OfType<GraphElement>().ToList();

            var variablesToCreate = ExtractVariablesFromDroppedElements(dropElements, evt.mousePosition);

            List<CollapsibleInOutNode> droppedNodes = dropElements.OfType<CollapsibleInOutNode>().ToList();

            if (droppedNodes.Any(e => !(e.NodeModel is IVariableNodeModel)) && variablesToCreate.Any())
            {
                // fail because in the current setup this would mean dispatching several actions
                throw new ArgumentException("Unhandled case, dropping blackboard/variables fields and nodes at the same time");
            }

            if (variablesToCreate.Any())
                (Store.State.GraphModel?.Stencil)?.OnDragAndDropVariableDeclarations(Store, variablesToCreate.ToList());

            RemoveFromClassList("dropping");
            m_DragStarted = false;

            return true;
        }

        public override bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget enteredTarget, ISelection dragSource)
        {
            return true;
        }

        public override bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget leftTarget, ISelection dragSource)
        {
            m_DragStarted = false;
            return true;
        }

        public override bool DragExited()
        {
            RemoveFromClassList("dropping");
            m_DragStarted = false;
            return true;
        }

        void DragSetup(IMouseEvent mouseEvent, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource)
        {
            if (m_DragStarted)
                return;

            AddToClassList("dropping");

            if (dragSource != dropTarget || !(dragSource is IDropTarget))
            {
                // Drop into target graph view
                if (dropTarget is GraphView dropGraphView)
                {
                    var mousePosition = dropGraphView.contentViewContainer.WorldToLocal(mouseEvent.mousePosition);

                    var elementOffset = Vector2.zero;
                    foreach (var dropElement in dragSelection.OfType<VisualElement>())
                    {
                        var actualDropElement = dropElement as GraphElement;

                        if (dragSource is Blackboard)
                        {
                            //Code is commented out until we uncomment DetachConvertedBlackboardField
                            if (dropElement is BlackboardField) //blackboardField)
                                actualDropElement = null; //DetachConvertedBlackboardField(blackboardField);
                        }
                        else
                        {
                            dropGraphView.AddElement((GraphElement)dropElement);
                        }

                        if (actualDropElement == null)
                            continue;

                        actualDropElement.style.position = Position.Absolute;

                        var newPos = new Rect(mousePosition.x, mousePosition.y, dropElement.layout.width, dropElement.layout.height);
                        actualDropElement.SetPosition(newPos);

                        actualDropElement.MarkDirtyRepaint();
                    }
                }
            }

            m_DragStarted = true;
        }

        public void RequestFrameAll()
        {
            m_RequestFrameAll = true;
        }
    }
}
