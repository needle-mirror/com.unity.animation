using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace Unity.Animation.Editor
{
    internal class ClickableNodePart : BaseGraphElementPart
    {
        public static ClickableNodePart Create(string name, IGraphElementModel model, IGraphElement graphElement, string parentClassName, Action clickableAction)
        {
            return new ClickableNodePart(name, model, graphElement, parentClassName, clickableAction);
        }

        public ClickableNodePart(string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName, Action clickableAction)
            : base(name, model, ownerElement, parentClassName)
        {
            ClickableAction = clickableAction;
        }

        protected Action ClickableAction { get; set; }
        protected Clickable Clickable { get; set; }
        public override VisualElement Root { get; }

        protected override void BuildPartUI(VisualElement parent)
        {
            Clickable = new Clickable(ClickableAction);
            Clickable.activators.Clear();
            Clickable.activators.Add(
                new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            parent.AddManipulator(Clickable);
        }

        protected override void UpdatePartFromModel()
        {
        }
    }
}
