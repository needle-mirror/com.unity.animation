using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class AnimationGraphWindow : GtfoWindow
    {
        new GraphView GraphView => m_GraphView as GraphView;

        public AnimationGraphWindow()
        {
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            titleContent = new GUIContent("Animation Graph");
        }

        protected override UnityEditor.GraphToolsFoundation.Overdrive.BlankPage CreateBlankPage()
        {
            return new BlankPage(Store);
        }

        protected override UnityEditor.GraphToolsFoundation.Overdrive.MainToolbar CreateMainToolbar()
        {
            return new MainToolbar(Store, GraphView);
        }

        protected override ErrorToolbar CreateErrorToolbar()
        {
            return new ErrorToolbar(Store, GraphView);
        }

        protected override GtfoGraphView CreateGraphView()
        {
            return new GraphView(this, Store);
        }

        protected override bool CanHandleAssetType(GraphAssetModel asset)
        {
            return (asset is BaseGraphAssetModel || asset is StateMachineAsset);
        }

        protected override Dictionary<Event, ShortcutDelegate> GetShortcutDictionary()
        {
            return new Dictionary<Event, ShortcutDelegate>
            {
                { Event.KeyboardEvent("F5"), _ =>
              {
                  Store.MarkStateDirty();
                  return EventPropagation.Continue;
              }},
                { Event.KeyboardEvent("backspace"), OnBackspaceKeyDown },
                { Event.KeyboardEvent("space"), OnSpaceKeyDown },
                { Event.KeyboardEvent("Q"), _ => GraphView.AlignSelection(false) },
                { Event.KeyboardEvent("#Q"), _ => GraphView.AlignSelection(true) },
            };
        }

        EventPropagation OnSpaceKeyDown(KeyDownEvent e)
        {
            if (Store.State.GraphModel is BaseGraphModel)
                m_GraphView.DisplaySmartSearch(e.originalMousePosition);

            return EventPropagation.Stop;
        }

        EventPropagation OnBackspaceKeyDown(KeyDownEvent e)
        {
            return GraphView.RemoveSelection();
        }

        protected override UnityEditor.GraphToolsFoundation.Overdrive.State CreateInitialState()
        {
            return new Unity.Animation.Editor.State(GUID);
        }

        protected override void RegisterReducers()
        {
            RegisterReducers(Store);
        }

        public static void RegisterReducers(Store store)
        {
            StoreHelper.RegisterDefaultReducers(store);
            BuildReducers.Register(store);
            ComponentReducers.Register(store);
            foreach (var type in ContextService.RegisterReducerCallbacks)
            {
                var instance = (ContextService.IReducerRegister)Activator.CreateInstance(type);
                instance.RegisterReducers(store);
            }
            //DragAndDropReducers.Register(store);
        }

        protected override IEnumerable<Type> RecompilationTriggerActions =>
            base.RecompilationTriggerActions.Concat(new[]
            {
                typeof(SetNumberOfPortGroupInstanceAction),
                typeof(CreateInputComponentAction),
                typeof(RemoveComponentAction),
                typeof(UpdateComponentNameAction)
            });
    }
}
