using System.Linq;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;
using UnityEngine;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class BlackboardInputsPart : BaseGraphElementPart
    {
        public static readonly string ussClassName = "ge-blackboard-section";
        public static readonly string headerUssClassName = ussClassName.WithUssElement("header");
        public static readonly string ussRowClassName = "ge-blackboard-row";
        public static readonly string titleRowUssClassName = ussRowClassName.WithUssElement("title");
        public static readonly string addButtonUssClassName = ussClassName.WithUssElement("add");

        public static BlackboardInputsPart Create(
            string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
        {
            if (model is BlackboardGraphModel)
            {
                return new BlackboardInputsPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        VisualElement m_Root;
        VisualElement m_Inputs;
        Label m_TitleLabel;
        Button m_AddButton;
        VisualElement m_Header;

        protected BlackboardInputsPart(string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);

            m_Header = new VisualElement() { name = "section-header" };
            m_Header.AddToClassList(headerUssClassName);
            m_TitleLabel = new Label { name = "title-label" };
            m_TitleLabel.text = "Inputs";
            m_TitleLabel.AddToClassList(headerUssClassName);
            m_Header.Add(m_TitleLabel);

            m_AddButton = new Button(() =>
            {
                GenericMenu menu = new GenericMenu();
                (m_Model as IBlackboardGraphModel)?.PopulateCreateMenu("Inputs", menu, m_OwnerElement.Store);
                Vector2 menuPosition = new Vector2(m_AddButton.layout.xMin, m_AddButton.layout.yMax);
                menuPosition = m_AddButton.parent.LocalToWorld(menuPosition);
                menu.DropDown(new Rect(menuPosition, Vector2.zero));
            })
            { text = "+" };
            m_AddButton.AddToClassList(addButtonUssClassName);
            m_Header.Add(m_AddButton);

            m_Root.Add(m_Header);
            m_Inputs = new VisualElement();
            m_Root.Add(m_Inputs);

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            // TODO FB : Shouldn't clear everything
            m_Inputs.Clear();
            BuildInputs();
        }

        private VisualElement BuildInputs()
        {
            var graphModel = m_Model.GraphModel as BaseModel;
            var inputs = graphModel?.InputComponentBindings;
            if (inputs != null)
            {
                foreach (var input in inputs)
                {
                    if (input == null || input.Identifier.Type == null)
                        continue;

                    var inputRoot = new VisualElement();

                    var inputTitle = new BlackboardComponentLabel(input, m_OwnerElement.Store);
                    inputRoot.Add(inputTitle);

                    var variableDeclarations =
                        graphModel.GetComponentVariableDeclarations().Where(p => p.Identifier == input.Identifier);
                    foreach (var decl in variableDeclarations)
                    {
                        var field = new BlackboardField();
                        field.SetupBuildAndUpdate(decl, m_OwnerElement.Store, m_OwnerElement.GraphView);
                        inputRoot.Add(field);
                    }
                    m_Inputs.Add(inputRoot);
                }
            }
            return m_Inputs;
        }
    }
}
