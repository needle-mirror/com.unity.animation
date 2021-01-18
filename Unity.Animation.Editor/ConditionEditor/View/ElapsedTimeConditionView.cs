using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class ElapsedTimeConditionView : BaseConditionView
    {
        public ElapsedTimeConditionView(ElapsedTimeConditionViewModel viewModel)
            : base(viewModel)
        {
            AddToClassList("condition-editor-elapsed-time-fragment");
            Label greaterLabel = new Label() { name = "label", text = "Time in state  >" };
            greaterLabel.AddToClassList("condition-editor-elapsed-time-fragment__label");
            Add(greaterLabel);
            FloatField value = new FloatField() { name = "value" , value = viewModel.ElapsedTimeConditionModel.TimeElapsed};
            value.RegisterValueChangedCallback(evt =>
            {
                Undo.RegisterCompleteObjectUndo((UnityEngine.Object)viewModel.AssetModel, "Set Value");
                viewModel.ElapsedTimeConditionModel.TimeElapsed = evt.newValue;
                EditorUtility.SetDirty(viewModel.AssetModel as UnityEngine.Object);
            });
            value.AddToClassList("condition-editor-elapsed-time-fragment__value");
            Add(value);
        }

        public override Tuple<float, float> GetAutoArrangeDesiredSize()
        {
            return new Tuple<float, float>(ElapsedTimeConditionModel.DefaultWidth, ElapsedTimeConditionModel.DefaultHeight);
        }
    }
}
