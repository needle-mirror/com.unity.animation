using System.Collections.Generic;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Editor
{
    class AnimationOnBoarding : AnimationOnboardingProvider
    {
        public VisualElement CreateOnboardingElement(Store store)
        {
            return new Elements.OnboardingElement(store);
        }

        public bool GetGraphAndObjectFromSelection(GtfoWindow window, Object selectedObject, out string assetPath, out GameObject boundObject)
        {
            assetPath = null;
            boundObject = null;

            if (selectedObject is IGraphAssetModel graphAssetModel)
            {
                // don't change the current object if it's the same graph
                if (graphAssetModel == window.Store.State.GraphModel?.AssetModel)
                {
                    var currentOpenedGraph = window.Store.State.WindowState.CurrentGraph;
                    assetPath = currentOpenedGraph.GraphAssetModelPath;
                    boundObject = currentOpenedGraph.BoundObject;
                    return true;
                }
            }

            var gameObject = selectedObject as GameObject;
            var authoring = (gameObject != null) ? gameObject.GetComponent<AnimationGraph>() : null;

            if (authoring == null)
                return false;

            var path = AssetDatabase.GetAssetPath(authoring.Graph);
            if (path == null)
                return false;

            assetPath = path;
            boundObject = selectedObject as GameObject;

            return true;
        }
    }

    namespace Elements
    {
        class OnboardingElement : VisualElement
        {
            readonly Store m_Store;
            List<Button> m_NewButtons = new List<Button>();

            public OnboardingElement(Store store)
            {
                m_Store = store;

                AddToClassList("onboarding-block");

                Add(new Label("Select an existing graph or create a new one"));

                foreach (var c in ContextService.AvailableContexts)
                {
                    if (typeof(BaseGraphStencil).IsAssignableFrom(c.StencilType))
                    {
                        var template = new CreateGraphTemplate(c.StencilType);

                        Add(new Button(
                            () => { template.PromptToCreateGraph(m_Store, false); })
                            { text = "Create " + template.GraphTypeName });
                        Add(new Label("or"));
                        var button = new Button(
                            () => { template.PromptToCreateGraph(m_Store, true); })
                        { text = $"New {template.GraphTypeName} on current selection" };
                        m_NewButtons.Add(button);
                        Add(button);
                        continue;
                    }

                    Add(new Label("or"));

                    if (typeof(StateMachineStencil).IsAssignableFrom(c.StencilType))
                    {
                        var smTemplate = new CreateStateMachineTemplate(c.StencilType);
                        Add(new Button(
                            () => { smTemplate.PromptToCreateStateMachine(m_Store, false); })
                            { text = "Create " + smTemplate.GraphTypeName });

                        Add(new Label("or"));

                        var button = new Button(
                            () => { smTemplate.PromptToCreateStateMachine(m_Store, true); })
                        { text = $"New {smTemplate.GraphTypeName} on current selection" };

                        m_NewButtons.Add(button);
                        Add(button);
                    }
                }

                RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
                RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            }

            void OnEnterPanel(AttachToPanelEvent e)
            {
                Selection.selectionChanged += OnSelectionChanged;
                OnSelectionChanged();
            }

            void OnLeavePanel(DetachFromPanelEvent e)
            {
                // ReSharper disable once DelegateSubtraction
                Selection.selectionChanged -= OnSelectionChanged;
            }

            void OnSelectionChanged()
            {
                foreach (var b in m_NewButtons)
                    b.SetEnabled(Selection.gameObjects.Length > 0);
            }
        }
    }
}
