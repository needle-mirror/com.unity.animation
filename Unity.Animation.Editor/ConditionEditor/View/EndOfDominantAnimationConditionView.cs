using System;
using Unity.Animation.Model;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Animation.Editor
{
    internal class EndOfDominantAnimationConditionView : BaseConditionView
    {
        public EndOfDominantAnimationConditionView(EndOfDominantAnimationConditionViewModel viewModel)
            : base(viewModel)
        {
            AddToClassList("condition-editor-end-of-dominant-animation-fragment");
            Label label = new Label() { name = "label", text = "End Of Animation minus " };
            label.AddToClassList("condition-editor-end-of-dominant-animation-fragment__label");
            Add(label);
            FloatField value = new FloatField() { name = "value" , value = viewModel.EndOfDominantAnimationConditionModel.TimeBeforeEnd};
            value.RegisterValueChangedCallback(evt =>
            {
                Undo.RegisterCompleteObjectUndo((UnityEngine.Object)viewModel.AssetModel, "Set Value");
                viewModel.EndOfDominantAnimationConditionModel.TimeBeforeEnd = evt.newValue;
                EditorUtility.SetDirty(viewModel.AssetModel as UnityEngine.Object);
            });
            value.AddToClassList("condition-editor-end-of-dominant-animation-fragment__value");
            Add(value);
        }

        public override Tuple<float, float> GetAutoArrangeDesiredSize()
        {
            return new Tuple<float, float>(EndOfDominantAnimationConditionModel.DefaultWidth, EndOfDominantAnimationConditionModel.DefaultHeight);
        }
    }
}
