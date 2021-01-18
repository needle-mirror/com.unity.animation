using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Model;


namespace Unity.Animation.Editor
{
    internal class PropertyHolder : VisualElement
    {
        public PropertyHolder()
            : base()
        {
            AddToClassList("compositor-property-holder");
        }

        bool m_Visible = true;

        public bool Visible
        {
            get => m_Visible;
            set
            {
                if (m_Visible == value)
                    return;
                m_Visible = value;
                if (m_Visible)
                    RemoveFromClassList("compositor-property-holder--hidden");
                else
                    AddToClassList("compositor-property-holder--hidden");
            }
        }

        public delegate VisualElement ControlCreationCallback();

        public delegate List<VisualElement> MultipleControlCreationCallback();

        public PropertyHolder WithLabel(string labelText)
        {
            var label = new Label() { text = labelText };
            label.AddToClassList("compositor-property-holder__label");
            Add(label);
            return this;
        }

        public PropertyHolder WithControl(ControlCreationCallback newControl)
        {
            var control = newControl();
            control.AddToClassList("compositor-property-holder__child-extensible");
            Add(control);
            return this;
        }

        public PropertyHolder WithMultipleControls(MultipleControlCreationCallback newControls)
        {
            var controls = newControls();
            foreach (var control in controls)
            {
                control.AddToClassList("compositor-property-holder__child");
                Add(control);
            }
            return this;
        }
    }

    internal class ConditionEditor : EditorWindow
    {
        const string k_AssetPath = "Packages/com.unity.animation/Unity.Animation.Editor/";

        [MenuItem("Window/Animation/Condition Editor", priority = MenuUtility.WindowPriority)]
        public static void ShowConditionEditor()
        {
            ConditionEditor wnd = GetWindow<ConditionEditor>();
            wnd.titleContent = new GUIContent("Condition Editor");
        }

        protected VisualElement surfaceElement { get; private set; }
        protected VisualElement contentElement { get; private set; }
        protected VisualElement propertiesElement { get; private set; }
        protected VisualElement groupOrElement { get; private set; }
        protected VisualElement groupAndElement { get; private set; }
        protected VisualElement blackboardValueElement { get; private set; }
        protected VisualElement markupElement { get; private set; }
        protected VisualElement elapsedTimeElement { get; private set; }
        protected VisualElement stateTagElement { get; private set; }
        protected VisualElement evalRatioElement { get; private set; }
        protected VisualElement endOfAnimElement { get; private set; }

        ShortcutHandler m_ShortcutHandler;

        public void OnEnable()
        {
            VisualElement root = rootVisualElement;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_AssetPath + "ConditionEditor/ConditionEditor.uxml");
            VisualElement labelFromUXML = visualTree.CloneTree();
            root.Add(labelFromUXML);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_AssetPath + "ConditionEditor/ConditionEditor.uss");
            root.styleSheets.Add(styleSheet);

            contentElement = root.Q("content");
            surfaceElement = root.Q("surface");
            propertiesElement = root.Q("properties");
            groupOrElement = root.Q("groupOrElement");
            groupOrElement.AddToClassList("conditionPreset");
            groupAndElement = root.Q("groupAndElement");
            groupAndElement.AddToClassList("conditionPreset");
            blackboardValueElement = root.Q("blackboardValueElement");
            blackboardValueElement.AddToClassList("conditionPreset");
            markupElement = root.Q("markupElement");
            markupElement.AddToClassList("conditionPreset");
            elapsedTimeElement = root.Q("elapsedElement");
            elapsedTimeElement.AddToClassList("conditionPreset");
            stateTagElement = root.Q("stateTagElement");
            stateTagElement.AddToClassList("conditionPreset");
            evalRatioElement = root.Q("evalRatioElement");
            evalRatioElement.AddToClassList("conditionPreset");
            endOfAnimElement = root.Q("endOfAnimElement");
            endOfAnimElement.AddToClassList("conditionPreset");
            root.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            root.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            root.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            root.RegisterCallback<WheelEvent>(OnMouseWheelEvent);
            root.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            root.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            root.RegisterCallback<DragUpdatedEvent>(OnDragUpdateEvent);

            Dictionary<Event, ShortcutDelegate> dictionaryShortcuts = GetShortcutDictionary();

            m_ShortcutHandler = new ShortcutHandler(GetShortcutDictionary());

            rootVisualElement.parent.AddManipulator(m_ShortcutHandler);

            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            EditorApplication.delayCall += SetInitialTransition;
            GlobalTransitionHolder.Instance.OnCurrentTransitionChangedCallback += OnTransitionChanged;
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void SetInitialTransition()
        {
            if (GlobalTransitionHolder.Instance.CurrentTransition != null)
            {
                SetTransition(GlobalTransitionHolder.Instance.CurrentTransition, GlobalTransitionHolder.Instance.GraphAssetModel);
            }
        }

        void OnDragUpdateEvent(DragUpdatedEvent evt)
        {
            var groupConditionTarget =
                GetSelfOrParentOfType<GroupConditionView>(evt.target as VisualElement);
            if (groupConditionTarget == null)
                return;

            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectableGraphElement>;
            foreach (var s in selection)
            {
                var blackboardField = s as BlackboardField;
                // TODO FB : Use new blackboard values
                if (blackboardField != null &&
                    blackboardField.Model is InputComponentFieldVariableDeclarationModel)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    evt.StopPropagation();

                    UpdateHighlightedGroup(groupConditionTarget, evt.mousePosition);

                    return;
                }
            }
        }

        void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            m_HighlightedGroup?.ClearInsertionTarget();
        }

        void OnDragPerformEvent(DragPerformEvent evt)
        {
            Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Add Blackboard value condition fragment");

            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectableGraphElement>;
            List<InputComponentFieldVariableDeclarationModel> listVariables = new List<InputComponentFieldVariableDeclarationModel>();
            foreach (var item in selection)
            {
                // TODO FB : Use new blackboard values
                if (item is BlackboardField blackboardField &&
                    blackboardField.Model is InputComponentFieldVariableDeclarationModel fieldVariableModel)
                {
                    listVariables.Add(fieldVariableModel);
                }
            }

            for (int i = listVariables.Count - 1; i >= 0; --i)
            {
                BlackboardValueConditionModel newCondition = (BlackboardValueConditionModel)CreateConditionModelFromPreset(blackboardValueElement);
                newCondition.BlackboardValueReference.FieldId = listVariables[i].FieldHandle;
                newCondition.BlackboardValueReference.ComponentBindingId = listVariables[i].Identifier;

                var blackboardValueType = newCondition.BlackboardValueReference.FieldId.ResolveType();
                if (blackboardValueType == typeof(float))
                {
                    newCondition.CompareValue = new GraphVariant(){ Float = 0.0f };
                }
                else if (blackboardValueType == typeof(int))
                {
                    newCondition.CompareValue = new GraphVariant(){ Int = 0 };
                }
                else if (blackboardValueType == typeof(bool))
                {
                    newCondition.CompareValue = new GraphVariant(){ Bool = true };
                }

                var groupModel = m_HighlightedGroup.GroupViewModel.GroupConditionModel;
                groupModel.InsertCondition(newCondition, m_HighlightedGroup.InsertionIndex);
            }
            m_HighlightedGroup?.ClearInsertionTarget();
            EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
            RefreshView();
        }

        protected void OnDisable()
        {
            if (rootVisualElement != null && m_ShortcutHandler != null)
            {
                rootVisualElement.parent.RemoveManipulator(m_ShortcutHandler);
            }
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            GlobalTransitionHolder.Instance.OnCurrentTransitionChangedCallback -= OnTransitionChanged;
        }

        void UndoRedoPerformed()
        {
            if (m_AssetModel != null && m_CurrentTransition != null)
            {
                var stateMachine = (StateMachineAsset)m_AssetModel;
                ITransitionPropertiesModel transition = stateMachine.GraphModel.EdgeModels.OfType<StateToStateTransitionModel>().FirstOrDefault(x => x.TransitionProperties.TransitionId.GUID == m_CurrentTransition.TransitionProperties.TransitionId.GUID);
                m_CurrentTransition = transition;
            }
            RefreshView();
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            rootVisualElement.parent.AddManipulator(m_ShortcutHandler);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            rootVisualElement.parent.RemoveManipulator(m_ShortcutHandler);
        }

        protected virtual Dictionary<Event, ShortcutDelegate> GetShortcutDictionary()
        {
            return new Dictionary<Event, ShortcutDelegate>
            {
                { Event.KeyboardEvent("delete"), OnDeleteSelected },
                { Event.KeyboardEvent("F"), OnFrame }
            };
        }

        void OnTransitionChanged(GlobalTransitionHolder.TransitionInfo transition)
        {
            if (transition.Transition != null && transition.Transition != m_CurrentTransition)
            {
                SetTransition(transition.Transition, transition.GraphAssetModel);
            }
        }

        void RefreshView()
        {
            ClearCurrentTransition();
            BuildConditionViewModels();
            BuildTransitionProperties();
        }

        ITransitionPropertiesModel m_CurrentTransition;
        IGraphAssetModel m_AssetModel;
        Dictionary<BaseConditionViewModel, BaseConditionView> m_ConditionViewModels = new Dictionary<BaseConditionViewModel, BaseConditionView>();
        GroupConditionView m_RootElement;

        void SetTransition(ITransitionPropertiesModel transition, IGraphAssetModel asset)
        {
            if (transition != m_CurrentTransition)
            {
                ClearCurrentTransition();
            }
            m_CurrentTransition = transition;
            m_AssetModel = asset;
            BuildConditionViewModels();
            BuildTransitionProperties();
            OnFrame();
        }

        void BuildTransitionProperties()
        {
            if (m_CurrentTransition == null)
                return;
            BuildBaseProperties();

            if (m_CurrentTransition.TransitionProperties is StateTransitionProperties)
            {
                BuildStateTransitionProperties();
            }

            if (m_CurrentTransition.TransitionProperties is BaseTransitionSelectorProperties)
            {
                BuildSelectorProperties();
            }
        }

        PropertyHolder m_OverrideSyncTargetRatioHolder;
        PropertyHolder m_OverrideSyncTagTypeHolder;
        PropertyHolder m_OverrideSyncEntryPointHolder;

        void BuildSelectorProperties()
        {
            var selector = (BaseTransitionSelectorProperties)m_CurrentTransition.TransitionProperties;
            var propertyHolder = new PropertyHolder().WithLabel("Override Transition Duration").WithMultipleControls(() =>
            {
                var list = new List<VisualElement>();
                var overrideToggle = new Toggle() { value = selector.OverrideTransitionDuration };
                var transitionDuration = new FloatField() { value = selector.OverriddenTransitionDuration };
                transitionDuration.style.marginLeft = 3;
                transitionDuration.AddToClassList("compositor-property-holder__child-extensible");
                if (!selector.OverrideTransitionDuration)
                    transitionDuration.AddToClassList("compositor-property-holder--hidden");

                list.Add(overrideToggle);
                list.Add(transitionDuration);

                overrideToggle.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverrideTransitionDuration = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                    if (selector.OverrideTransitionDuration)
                        transitionDuration.RemoveFromClassList("compositor-property-holder--hidden");
                    else
                        transitionDuration.AddToClassList("compositor-property-holder--hidden");
                });
                transitionDuration.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverriddenTransitionDuration = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return list;
            });
            propertiesElement.Add(propertyHolder);

            propertyHolder = new PropertyHolder().WithLabel("Override Advance Source").WithMultipleControls(() =>
            {
                var list = new List<VisualElement>();
                var overrideToggle = new Toggle() { value = selector.OverrideAdvanceSourceDuringTransition };
                var advanceSource = new Toggle() { value = selector.OverriddenAdvanceSourceDuringTransition };
                advanceSource.style.marginLeft = 3;
                if (!selector.OverrideAdvanceSourceDuringTransition)
                    advanceSource.AddToClassList("compositor-property-holder--hidden");

                list.Add(overrideToggle);
                list.Add(advanceSource);

                overrideToggle.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverrideAdvanceSourceDuringTransition = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                    if (selector.OverrideAdvanceSourceDuringTransition)
                        advanceSource.RemoveFromClassList("compositor-property-holder--hidden");
                    else
                        advanceSource.AddToClassList("compositor-property-holder--hidden");
                });
                advanceSource.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverriddenAdvanceSourceDuringTransition = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return list;
            });
            propertiesElement.Add(propertyHolder);

            propertyHolder = new PropertyHolder().WithLabel("Override Synchronization type").WithMultipleControls(() =>
            {
                var list = new List<VisualElement>();
                var overrideToggle = new Toggle() { value = selector.OverrideTransitionSynchronization };
                var syncType = new EnumField(StateTransitionProperties.TransitionSynchronization.None){name = "SyncMode", value = selector.OverriddenTransitionSynchronization};
                syncType.style.marginLeft = 3;
                if (!selector.OverrideTransitionSynchronization)
                    syncType.AddToClassList("compositor-property-holder--hidden");

                list.Add(overrideToggle);
                list.Add(syncType);

                overrideToggle.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverrideTransitionSynchronization = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                    if (selector.OverrideTransitionSynchronization)
                        syncType.RemoveFromClassList("compositor-property-holder--hidden");
                    else
                        syncType.AddToClassList("compositor-property-holder--hidden");
                });
                syncType.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverriddenTransitionSynchronization = (StateTransitionProperties.TransitionSynchronization)evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                    UpdateOverrideSyncPropertyVisibility();
                });
                return list;
            });
            propertiesElement.Add(propertyHolder);

            m_OverrideSyncTargetRatioHolder = new PropertyHolder().WithLabel("Override Target Ratio").WithMultipleControls(() =>
            {
                var list = new List<VisualElement>();
                var overrideToggle = new Toggle() { value = selector.OverrideSyncTargetRatio };
                var syncTargetRatio = new FloatField() { value = selector.OverriddenSyncTargetRatio };
                syncTargetRatio.style.marginLeft = 3;
                syncTargetRatio.AddToClassList("compositor-property-holder__child-extensible");
                if (!selector.OverrideSyncTargetRatio)
                    syncTargetRatio.AddToClassList("compositor-property-holder--hidden");

                list.Add(overrideToggle);
                list.Add(syncTargetRatio);

                overrideToggle.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverrideSyncTargetRatio = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                    if (selector.OverrideSyncTargetRatio)
                        syncTargetRatio.RemoveFromClassList("compositor-property-holder--hidden");
                    else
                        syncTargetRatio.AddToClassList("compositor-property-holder--hidden");
                });
                syncTargetRatio.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverriddenSyncTargetRatio = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return list;
            });
            propertiesElement.Add(m_OverrideSyncTargetRatioHolder);

            m_OverrideSyncTagTypeHolder = new PropertyHolder().WithLabel("Override Tag Type").WithMultipleControls(() =>
            {
                var list = new List<VisualElement>();
                var overrideToggle = new Toggle() { value = selector.OverrideSyncTagType };
                var syncTagType = new TextField() { value = selector.OverriddenSyncTagType };
                syncTagType.style.marginLeft = 3;
                syncTagType.AddToClassList("compositor-property-holder__child-extensible");
                if (!selector.OverrideSyncTagType)
                    syncTagType.AddToClassList("compositor-property-holder--hidden");

                list.Add(overrideToggle);
                list.Add(syncTagType);

                overrideToggle.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverrideSyncTagType = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                    if (selector.OverrideSyncTagType)
                        syncTagType.RemoveFromClassList("compositor-property-holder--hidden");
                    else
                        syncTagType.AddToClassList("compositor-property-holder--hidden");
                });
                syncTagType.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverriddenSyncTagType = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return list;
            });
            propertiesElement.Add(m_OverrideSyncTagTypeHolder);

            m_OverrideSyncEntryPointHolder = new PropertyHolder().WithLabel("Override Entry Point").WithMultipleControls(() =>
            {
                var list = new List<VisualElement>();
                var overrideToggle = new Toggle() { value = selector.OverrideSyncEntryPoint };
                var syncEntryPoint = new IntegerField() { value = selector.OverriddenSyncEntryPoint };
                syncEntryPoint.style.marginLeft = 3;
                syncEntryPoint.AddToClassList("compositor-property-holder__child-extensible");
                if (!selector.OverrideSyncEntryPoint)
                    syncEntryPoint.AddToClassList("compositor-property-holder--hidden");

                list.Add(overrideToggle);
                list.Add(syncEntryPoint);

                overrideToggle.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverrideSyncEntryPoint = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                    if (selector.OverrideSyncEntryPoint)
                        syncEntryPoint.RemoveFromClassList("compositor-property-holder--hidden");
                    else
                        syncEntryPoint.AddToClassList("compositor-property-holder--hidden");
                });
                syncEntryPoint.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    selector.OverriddenSyncEntryPoint = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return list;
            });
            propertiesElement.Add(m_OverrideSyncEntryPointHolder);
            UpdateOverrideSyncPropertyVisibility();
        }

        void UpdateOverrideSyncPropertyVisibility()
        {
            var selector = (BaseTransitionSelectorProperties)m_CurrentTransition.TransitionProperties;
            m_OverrideSyncEntryPointHolder.Visible = selector.OverriddenTransitionSynchronization == StateTransitionProperties.TransitionSynchronization.EntryPoint;
            m_OverrideSyncTagTypeHolder.Visible = selector.OverriddenTransitionSynchronization == StateTransitionProperties.TransitionSynchronization.Tag;
            m_OverrideSyncTargetRatioHolder.Visible = selector.OverriddenTransitionSynchronization == StateTransitionProperties.TransitionSynchronization.Ratio;
        }

        PropertyHolder m_SyncTargetRatioHolder;
        PropertyHolder m_SyncTagTypeHolder;
        PropertyHolder m_SyncEntryPointHolder;

        void BuildStateTransitionProperties()
        {
            var transition = (StateTransitionProperties)m_CurrentTransition.TransitionProperties;

            var propertyHolder = new PropertyHolder().WithLabel("Transition Duration").WithControl(() =>
            {
                var transitionDuration = new FloatField() { name = "TransitionDuration", value = transition.TransitionDuration };
                transitionDuration.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    transition.TransitionDuration = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return transitionDuration;
            });
            propertiesElement.Add(propertyHolder);

            propertyHolder = new PropertyHolder().WithLabel("Advance Source During Transition").WithControl(() =>
            {
                var advanceSource = new Toggle() { name = "AdvanceSource", value = transition.AdvanceSourceDuringTransition };
                advanceSource.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    transition.AdvanceSourceDuringTransition = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return advanceSource;
            });
            propertiesElement.Add(propertyHolder);

            propertyHolder = new PropertyHolder().WithLabel("Synchronization type").WithControl(() =>
            {
                var syncType = new EnumField(StateTransitionProperties.TransitionSynchronization.None){name = "SyncMode", value = transition.SynchronizationMode};
                syncType.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    transition.SynchronizationMode = (StateTransitionProperties.TransitionSynchronization)evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                    UpdateSyncPropertyVisibility();
                });
                return syncType;
            });
            propertiesElement.Add(propertyHolder);

            m_SyncTargetRatioHolder = new PropertyHolder().WithLabel("Target Ratio").WithControl(() =>
            {
                var targetRatio = new FloatField() { name = "TargetRatio", value = transition.SyncTargetRatio };
                targetRatio.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    transition.SyncTargetRatio = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return targetRatio;
            });
            propertiesElement.Add(m_SyncTargetRatioHolder);

            m_SyncTagTypeHolder = new PropertyHolder().WithLabel("Tag Type").WithControl(() =>
            {
                var tagType = new TextField() { name = "TagType", value = transition.SyncTagType };
                tagType.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    transition.SyncTagType = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return tagType;
            });
            propertiesElement.Add(m_SyncTagTypeHolder);


            m_SyncEntryPointHolder = new PropertyHolder().WithLabel("Entry Point").WithControl(() =>
            {
                var entryPoint = new IntegerField() { name = "EntryPoint", value = transition.SyncEntryPoint };
                entryPoint.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    transition.SyncTargetRatio = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return entryPoint;
            });
            propertiesElement.Add(m_SyncEntryPointHolder);

            UpdateSyncPropertyVisibility();
        }

        void UpdateSyncPropertyVisibility()
        {
            var transition = (StateTransitionProperties)m_CurrentTransition.TransitionProperties;

            m_SyncEntryPointHolder.Visible = transition.SynchronizationMode == StateTransitionProperties.TransitionSynchronization.EntryPoint;
            m_SyncTagTypeHolder.Visible = transition.SynchronizationMode == StateTransitionProperties.TransitionSynchronization.Tag;
            m_SyncTargetRatioHolder.Visible = transition.SynchronizationMode == StateTransitionProperties.TransitionSynchronization.Ratio;
        }

        void BuildBaseProperties()
        {
            var propertyHolder = new PropertyHolder().WithLabel("Enable").WithControl(() =>
            {
                var toggle = new Toggle() { name = "Enable", value = m_CurrentTransition.TransitionProperties.Enable };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Set Value");
                    m_CurrentTransition.TransitionProperties.Enable = evt.newValue;
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                });
                return toggle;
            });
            propertiesElement.Add(propertyHolder);
        }

        void ClearCurrentTransition()
        {
            ClearConditionViewModels();
            for (int i = propertiesElement.childCount; i > 0; --i)
                propertiesElement.RemoveAt(i - 1);
        }

        void BuildConditionViewModels()
        {
            if (m_CurrentTransition == null)
                return;
            BaseConditionViewModel root = ConditionViewModelFactory.GetViewModelFromModel(m_CurrentTransition.TransitionProperties.Condition, m_AssetModel);
            root.Parent = null;
            m_RootElement = (GroupConditionView)ConditionViewFactory.CreateViewFromViewModel(root);
            surfaceElement.Add(m_RootElement);
            m_ConditionViewModels.Add(root, m_RootElement);
            BuildChildConditionsViewModel(m_RootElement);

            AutoArrangeElements();
        }

        void BuildChildConditionsViewModel(BaseConditionView element)
        {
            var groupElement = element as GroupConditionView;
            if (groupElement == null)
                return;
            foreach (var condition in groupElement.GroupViewModel.GroupConditionModel.ListSubConditions)
            {
                BaseConditionViewModel childConditionVM = ConditionViewModelFactory.GetViewModelFromModel(condition, m_AssetModel);
                childConditionVM.Parent = groupElement.GroupViewModel;
                BaseConditionView childElement = ConditionViewFactory.CreateViewFromViewModel(childConditionVM);
                childElement.style.position = Position.Absolute;
                groupElement.Insert(childElement);
                m_ConditionViewModels.Add(childConditionVM, childElement);
                BuildChildConditionsViewModel(childElement);
            }
        }

        void RemoveConditionElement(BaseConditionView conditionElement)
        {
            var parentViewModel = conditionElement.ViewModel.Parent;
            if (parentViewModel == null)
            {
                surfaceElement.Remove(conditionElement);
            }
            else if (m_ConditionViewModels.TryGetValue(parentViewModel, out BaseConditionView parentCondition))
            {
                GroupConditionView parentGroup = (GroupConditionView)parentCondition;
                parentGroup.Remove(conditionElement);
            }
            else
            {
                return;
            }

            if (conditionElement == m_RootElement)
                m_RootElement = null;

            m_ConditionViewModels.Remove(conditionElement.ViewModel);
        }

        void ClearConditionViewModels()
        {
            var listConditionToRemove = m_ConditionViewModels.Values.ToList();
            foreach (var condition in listConditionToRemove)
            {
                RemoveConditionElement(condition);
            }

            m_ConditionViewModels.Clear();
            m_RootElement = null;
        }

        BaseConditionModel CreateConditionModelFromPreset(VisualElement draggedPreset)
        {
            if (draggedPreset == null)
                return null;

            if (draggedPreset == groupAndElement || draggedPreset == groupOrElement)
            {
                return new GroupConditionModel()
                {
                    GroupOperation = draggedPreset == groupAndElement ? GroupConditionModel.Operation.And : GroupConditionModel.Operation.Or,
                    width = GroupConditionModel.DefaultWidth,
                    height = GroupConditionModel.DefaultHeight
                };
            }
            if (draggedPreset == blackboardValueElement)
            {
                return new BlackboardValueConditionModel()
                {
                    width = BlackboardValueConditionModel.DefaultWidth,
                    height = BlackboardValueConditionModel.DefaultHeight
                };
            }
            if (draggedPreset == markupElement)
            {
                return new MarkupConditionModel()
                {
                    width = MarkupConditionModel.DefaultWidth,
                    height = MarkupConditionModel.DefaultHeight
                };
            }
            if (draggedPreset == elapsedTimeElement)
            {
                return new ElapsedTimeConditionModel()
                {
                    width = ElapsedTimeConditionModel.DefaultWidth,
                    height = ElapsedTimeConditionModel.DefaultHeight
                };
            }
            if (draggedPreset == stateTagElement)
            {
                return new StateTagConditionModel()
                {
                    width = StateTagConditionModel.DefaultWidth,
                    height = StateTagConditionModel.DefaultHeight
                };
            }
            if (draggedPreset == evalRatioElement)
            {
                return new EvaluationRatioConditionModel()
                {
                    width = EvaluationRatioConditionModel.DefaultWidth,
                    height = EvaluationRatioConditionModel.DefaultHeight
                };
            }
            if (draggedPreset == endOfAnimElement)
            {
                return new EndOfDominantAnimationConditionModel()
                {
                    width = EndOfDominantAnimationConditionModel.DefaultWidth,
                    height = EndOfDominantAnimationConditionModel.DefaultHeight
                };
            }

            return null;
        }

        void AutoArrangeElements()
        {
            if (m_RootElement == null)
                return;
            var rootSize = m_RootElement.GetAutoArrangeDesiredSize();
            m_RootElement.Arrange(0f, 0f, rootSize.Item1, rootSize.Item2);
        }

        static float k_FrameMargin = 50.0f;
        EventPropagation OnFrame(KeyDownEvent evt = null)
        {
            if (m_RootElement == null)
                return EventPropagation.Continue;
            var bbHeight = m_RootElement.GroupViewModel.height + 2 * k_FrameMargin;
            var bbWidth = m_RootElement.GroupViewModel.width + 2 * k_FrameMargin;
            var surfaceHeight = contentElement.layout.height;
            var surfaceWidth = contentElement.layout.width;

            var yZoom = surfaceHeight / bbHeight;
            var xZoom = surfaceWidth / bbWidth;

            var necessaryZoom = Math.Min(xZoom, yZoom);
            surfaceElement.transform.scale = new Vector3(necessaryZoom, necessaryZoom, 1.0f);
            surfaceElement.transform.position = new Vector3(
                surfaceWidth / 2.0f - (m_RootElement.GroupViewModel.posX + bbWidth / 2.0f - k_FrameMargin) * necessaryZoom,
                surfaceHeight / 2.0f - (m_RootElement.GroupViewModel.posY + bbHeight / 2.0f - k_FrameMargin) * necessaryZoom,
                0.0f);
            return EventPropagation.Stop;
        }

        List<KeyValuePair<BaseConditionViewModel, BaseConditionView>> GetSelectedConditions()
        {
            var listSelectedItems = new List<KeyValuePair<BaseConditionViewModel, BaseConditionView>>();
            foreach (var condition in m_ConditionViewModels)
            {
                if (condition.Value.IsSelected && condition.Key.Parent != null)
                {
                    listSelectedItems.Add(condition);
                }
            }

            return listSelectedItems;
        }

        EventPropagation OnDeleteSelected(KeyDownEvent evt = null)
        {
            var listSelectedItems = GetSelectedConditions();

            if (listSelectedItems.Count > 0)
                Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Delete elements");
            bool needsUpdate = false;
            foreach (var item in listSelectedItems)
            {
                var parentGroup = (GroupConditionModel)item.Key.Model.Parent;
                parentGroup.RemoveCondition(item.Key.Model);
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                RefreshView();
            }
            return EventPropagation.Stop;
        }

        void AddToSelection(BaseConditionView baseConditionView)
        {
            baseConditionView.IsSelected = true;
        }

        void ToggleSelection(BaseConditionView baseConditionView)
        {
            baseConditionView.IsSelected = !baseConditionView.IsSelected;
        }

        void ClearSelection()
        {
            foreach (var condition in m_ConditionViewModels)
            {
                condition.Value.IsSelected = false;
            }
        }

        static T GetSelfOrParentOfType<T>(VisualElement elem) where T : VisualElement
        {
            while (elem != null)
            {
                if (elem is T retVal)
                    return retVal;
                if (elem.parent == null)
                    break;
                elem = elem.parent;
            }

            return null;
        }

        enum ToolState
        {
            None,
            Panning,
            DraggingPreset,
            ConditionInteraction,
            MovingConditions
        }

        VisualElement m_draggedPresetElement = null;
        BaseConditionView m_conditionInteraction = null;
        GroupConditionView m_HighlightedGroup;

        Vector2 m_originalPositionOnMouseDown;

        ToolState m_tool = ToolState.None;

        static float k_IncrementZoom = 0.05f;
        void OnMouseWheelEvent(WheelEvent evt)
        {
            Vector2 currentPos = surfaceElement.WorldToLocal(evt.mousePosition);

            var surfaceScale = surfaceElement.transform.scale.x;
            var deltaVal = evt.delta.y > 0 ? -k_IncrementZoom : k_IncrementZoom;
            if (surfaceScale + deltaVal > float.Epsilon)
            {
                surfaceScale += deltaVal;
            }
            surfaceElement.transform.scale = new Vector3(surfaceScale, surfaceScale,  1.0f);

            Vector2 zoomedPos = surfaceElement.WorldToLocal(evt.mousePosition);
            var surfacePan = surfaceElement.transform.position;

            surfaceElement.transform.position = new Vector3(surfacePan.x + (zoomedPos.x - currentPos.x) * surfaceScale, surfacePan.y + (zoomedPos.y - currentPos.y) * surfaceScale, 0.0f);
            evt.StopPropagation();
        }

        void OnMouseDownEvent(MouseDownEvent evt)
        {
            VisualElement ve = evt.target as VisualElement;
            if (ve == null)
                return;
            if (evt.button == (int)MouseButton.MiddleMouse)
            {
                m_tool = ToolState.Panning;
                return;
            }

            if (evt.button == (int)MouseButton.RightMouse)
                return;

            if (ve.ClassListContains("conditionPreset"))
            {
                m_tool = ToolState.DraggingPreset;
                m_draggedPresetElement = ve;
                return;
            }

            var veAsBase = GetSelfOrParentOfType<BaseConditionView>(ve);
            if (veAsBase != null)
            {
                m_tool = ToolState.ConditionInteraction;

                m_originalPositionOnMouseDown = evt.mousePosition;
                m_conditionInteraction = veAsBase;
            }

            if (evt.clickCount == 2 && m_conditionInteraction is GroupConditionView groupView)
            {
                groupView.GroupViewModel.GroupConditionModel.GroupOperation = groupView.GroupViewModel.IsAndGroup ? GroupConditionModel.Operation.Or : GroupConditionModel.Operation.And;
                groupView.UpdateOperationFromModel();
            }
        }

        static float k_ConditionMoveThreshold = 5.0f;
        void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            if (m_tool == ToolState.Panning)
            {
                surfaceElement.transform.position = new Vector3(surfaceElement.transform.position.x + evt.mouseDelta.x, surfaceElement.transform.position.y + evt.mouseDelta.y, surfaceElement.transform.position.z);
            }
            else if (m_tool == ToolState.DraggingPreset || m_tool == ToolState.MovingConditions)
            {
                var groupConditionTarget = GetSelfOrParentOfType<GroupConditionView>(evt.target as VisualElement);

                if (m_tool == ToolState.MovingConditions && groupConditionTarget == m_conditionInteraction)
                    groupConditionTarget = null;
                UpdateHighlightedGroup(groupConditionTarget, evt.mousePosition);
            }
            else if (m_tool == ToolState.ConditionInteraction)
            {
                Vector2 mouseDelta = evt.mousePosition - m_originalPositionOnMouseDown;
                if (Math.Abs(mouseDelta.x) > k_ConditionMoveThreshold || Math.Abs(mouseDelta.y) > k_ConditionMoveThreshold)
                {
                    m_tool = ToolState.MovingConditions;
                }
            }
        }

        void UpdateHighlightedGroup(GroupConditionView groupConditionTarget, Vector2 evtMousePosition)
        {
            if (m_HighlightedGroup != groupConditionTarget)
            {
                if (m_HighlightedGroup != null)
                {
                    m_HighlightedGroup.RemoveFromClassList("condition-editor-group-fragment--highlighted");
                    m_HighlightedGroup.ClearInsertionTarget();
                    m_HighlightedGroup = null;
                }
                if (groupConditionTarget != null)
                {
                    groupConditionTarget.AddToClassList("condition-editor-group-fragment--highlighted");
                    m_HighlightedGroup = groupConditionTarget;
                }
            }

            if (m_HighlightedGroup != null)
            {
                var posOnSurface = m_HighlightedGroup.WorldToSurfaceLocal(evtMousePosition);
                m_HighlightedGroup.SetInsertionTarget(posOnSurface);
            }
        }

        void OnMouseUpEvent(MouseUpEvent evt)
        {
            bool needsUpdate = false;
            if (m_tool == ToolState.DraggingPreset)
            {
                if (m_draggedPresetElement != null && m_HighlightedGroup != null)
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Add Condition");
                    var newCondition = CreateConditionModelFromPreset(m_draggedPresetElement);
                    var groupModel = m_HighlightedGroup.GroupViewModel.GroupConditionModel;
                    groupModel.InsertCondition(newCondition, m_HighlightedGroup.InsertionIndex);
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                    needsUpdate = true;
                }
            }
            else if (m_tool == ToolState.MovingConditions)
            {
                if (m_conditionInteraction != null && m_HighlightedGroup != null)
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)m_AssetModel, "Move Condition");
                    var movedConditions = new List<KeyValuePair<BaseConditionViewModel, BaseConditionView>>();
                    if (m_conditionInteraction.IsSelected)
                    {
                        movedConditions = GetSelectedConditions();
                    }
                    else
                    {
                        movedConditions.Add(new KeyValuePair<BaseConditionViewModel, BaseConditionView>(m_conditionInteraction.ViewModel, m_conditionInteraction));
                    }

                    var groupModel = m_HighlightedGroup.GroupViewModel.GroupConditionModel;
                    groupModel.MoveConditions(movedConditions.Select(x => x.Key.Model).ToList(), m_HighlightedGroup.InsertionIndex);
                    EditorUtility.SetDirty(m_AssetModel as UnityEngine.Object);
                    needsUpdate = true;
                }
            }
            else if (m_tool == ToolState.ConditionInteraction)
            {
                if (evt.altKey)
                {
                    if (m_conditionInteraction != null)
                    {
                        if (m_conditionInteraction.IsDebugValid)
                        {
                            m_conditionInteraction.IsDebugValid = false;
                            m_conditionInteraction.IsDebugInvalid = true;
                        }
                        else if (m_conditionInteraction.IsDebugInvalid)
                        {
                            m_conditionInteraction.IsDebugValid = false;
                            m_conditionInteraction.IsDebugInvalid = false;
                        }
                        else
                        {
                            m_conditionInteraction.IsDebugValid = true;
                            m_conditionInteraction.IsDebugInvalid = false;
                        }
                    }
                }
                else
                {
                    if (!evt.ctrlKey)
                    {
                        ClearSelection();
                    }

                    if (m_conditionInteraction != null)
                    {
                        if (evt.ctrlKey)
                            ToggleSelection(m_conditionInteraction);
                        else
                            AddToSelection(m_conditionInteraction);
                    }
                }
            }

            m_tool = ToolState.None;
            if (m_HighlightedGroup != null)
            {
                m_HighlightedGroup.ClearInsertionTarget();
                m_HighlightedGroup.RemoveFromClassList("condition-editor-group-fragment--highlighted");
                m_HighlightedGroup = null;
            }
            m_draggedPresetElement = null;
            m_conditionInteraction = null;

            if (needsUpdate)
            {
                RefreshView();
            }
        }
    }
}
