using System.Collections.Generic;
using System.Linq;
using Unity.DataFlowGraph.Attributes;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class GraphSearcherAdapter : GraphNodeSearcherAdapter
    {
        Label m_Description;

        public GraphSearcherAdapter(IGraphModel graphModel, string title) : base(graphModel, title) {}

        public override void InitDetailsPanel(VisualElement detailsPanel)
        {
            base.InitDetailsPanel(detailsPanel);

            m_Description = new Label();
            m_Description.style.unityFontStyleAndWeight = FontStyle.Italic;
            m_DetailsPanel.Add(m_Description);
        }

        public override void OnSelectionChanged(IEnumerable<SearcherItem> items)
        {
            if (m_DetailsPanel == null)
                return;

            var itemsList = items.ToList();
            m_DetailsTitle.text = itemsList.First().Name;

            var graphView = SearcherService.GraphView;
            foreach (var graphElement in graphView.GraphElements.ToList())
            {
                graphView.RemoveElement(graphElement);
            }

            if (!m_DetailsPanel.Contains(graphView))
            {
                m_DetailsPanel.Add(graphView);

                var eventCatcher = new VisualElement();
                eventCatcher.RegisterCallback<MouseDownEvent>(e => e.StopImmediatePropagation());
                eventCatcher.RegisterCallback<MouseMoveEvent>(e => e.StopImmediatePropagation());
                m_DetailsPanel.Add(eventCatcher);
                eventCatcher.StretchToParentSize();
            }

            m_Description.text = string.Empty;

            var elements = CreateGraphElements(itemsList.First());
            foreach (var element in elements)
            {
                if (element is INodeModel || element is IStickyNoteModel)
                {
                    graphView.AddElement(GraphElementFactory.CreateUI<GraphElement>(graphView, graphView.Store, element));

                    // set graph description
                    if ((element as DFGNodeModel)?.NodeType.GetCustomAttributes(
                        typeof(NodeDefinitionAttribute), false).FirstOrDefault() is NodeDefinitionAttribute description
                        && !string.IsNullOrEmpty(description.NodeDescription))
                    {
                        m_Description.text = $"{description.NodeDescription}";
                    }
                }
            }
        }
    }
}
