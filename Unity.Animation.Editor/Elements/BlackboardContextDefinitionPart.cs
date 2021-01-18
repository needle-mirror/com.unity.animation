using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Hybrid;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class BlackboardContextDefinitionPart : BaseGraphElementPart
    {
        public static readonly string ussClassName = "ge-blackboard-section";
        public static readonly string headerUssClassName = ussClassName.WithUssElement("header");
        public static readonly string ussRowClassName = "ge-blackboard-row";
        public static readonly string titleRowUssClassName = ussRowClassName.WithUssElement("title");

        public static BlackboardContextDefinitionPart Create(
            string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
        {
            if (model is BlackboardGraphModel)
            {
                return new BlackboardContextDefinitionPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        VisualElement m_Root;
        Label m_TitleLabel;

        protected BlackboardContextDefinitionPart(string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);

            m_TitleLabel = new Label { name = "title-label" };
            m_TitleLabel.text = "Context";
            m_TitleLabel.AddToClassList(headerUssClassName);

            m_Root.Add(m_TitleLabel);
            m_Root.Add(BuildContextFieldEditor());

            parent.Add(m_Root);
        }

        private VisualElement BuildContextFieldEditor()
        {
            if (m_Model?.GraphModel == null)
                return default;
            var stencil = m_Model.GraphModel.Stencil as IAuthoringContextProvider;
            var boundObj = m_OwnerElement.Store.State.WindowState.CurrentGraph.BoundObject;
            return
                new BlackboardObjectField(
                stencil.Context.ContextType.Name,
                stencil.Context.GameObjectContextType,
                boundObj == null ? null : boundObj.GetComponent<AnimationGraph>().Context,
                (obj) =>
                {
                    if (boundObj != null)
                    {
                        var animGraph = boundObj.GetComponent<AnimationGraph>();
                        animGraph.Context = obj as Component;
                        EditorUtility.SetDirty(animGraph);
                        EditorUtility.SetDirty(boundObj);
                    }
                },
                boundObj != null);
        }

        protected override void UpdatePartFromModel()
        {
        }
    }
}
