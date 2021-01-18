using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;
using Unity.Animation.Hybrid;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class BlackboardComponentLabel : VisualElement
    {
        public static readonly string ussClassName = "ge-blackboard-field";
        public static readonly string nameLabelUssClassName = ussClassName.WithUssElement("name-label");
        public static readonly string typeLabelUssClassName = ussClassName.WithUssElement("type-label");

        EditableLabel m_Name;

        ComponentBindingIdentifier m_Identifier;
        readonly Store m_Store;
        internal BlackboardComponentLabel(ComponentBinding component, Store store)
        {
            m_Identifier = component.Identifier;
            m_Store = store;

            AddToClassList(ussClassName);

            m_Name = new EditableLabel();
            m_Name.AddToClassList(nameLabelUssClassName);
            m_Name.SetValueWithoutNotify(component.Name);
            m_Name.RegisterCallback<ChangeEvent<string>>(OnRename);
            Add(m_Name);

            AuthoringComponentService.TryGetComponentByRuntimeType(component.Identifier.Type.Resolve(), out var componentInfo);

            var typeLabel = new Label() { name = "type-label" };
            typeLabel.AddToClassList(typeLabelUssClassName);
            typeLabel.text = componentInfo != null ? componentInfo.AuthoringType.Name : "unknown";
            Add(typeLabel);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        void OnRename(ChangeEvent<string> e)
        {
            m_Store.Dispatch(new UpdateComponentNameAction(m_Identifier, e.newValue));
        }

        void RemoveComponent()
        {
            m_Store.Dispatch(new RemoveComponentAction(m_Identifier));
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction("Delete", action => RemoveComponent());
        }
    }
}
