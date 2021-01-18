using System;
using Unity.Animation.Model;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Animation.Editor
{
    internal class EvaluationRatioConditionView : BaseConditionView
    {
        public EvaluationRatioConditionView(EvaluationRatioConditionViewModel viewModel)
            : base(viewModel)
        {
            AddToClassList("condition-editor-evaluation-ratio-fragment");
            Label greaterLabel = new Label() { name = "label", text = "State ratio  >" };
            greaterLabel.AddToClassList("condition-editor-evaluation-ratio-fragment__label");
            Add(greaterLabel);
            FloatField value = new FloatField() { name = "value" , value = viewModel.EvaluationRatioConditionModel.Ratio};
            value.RegisterValueChangedCallback(evt =>
            {
                Undo.RegisterCompleteObjectUndo((UnityEngine.Object)viewModel.AssetModel, "Set Value");
                viewModel.EvaluationRatioConditionModel.Ratio = evt.newValue;
                EditorUtility.SetDirty(viewModel.AssetModel as UnityEngine.Object);
            });
            value.AddToClassList("condition-editor-evaluation-ratio-fragment__value");
            Add(value);
        }

        public override Tuple<float, float> GetAutoArrangeDesiredSize()
        {
            return new Tuple<float, float>(EvaluationRatioConditionModel.DefaultWidth, EvaluationRatioConditionModel.DefaultHeight);
        }
    }
}
