using System;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class StateTagConditionView : BaseConditionView
    {
        public StateTagConditionView(StateTagConditionViewModel viewModel)
            : base(viewModel)
        {
            AddToClassList("condition-editor-state-tag-fragment");
            Label comingFromLabel = new Label() { name = "label", text = "Coming from state " };
            comingFromLabel.AddToClassList("condition-editor-state-tag-fragment__label");
            Add(comingFromLabel);
            TextField stateTagRef = new TextField() { name = "state-tag-reference", value = viewModel.StateTagConditionModel.StateTagReference };
            stateTagRef.RegisterValueChangedCallback(evt =>
            {
                Undo.RegisterCompleteObjectUndo((UnityEngine.Object)viewModel.AssetModel, "Set Value");
                viewModel.StateTagConditionModel.StateTagReference = evt.newValue;
                EditorUtility.SetDirty(viewModel.AssetModel as UnityEngine.Object);
            });
            stateTagRef.AddToClassList("condition-editor-state-tag-fragment__reference");
            Add(stateTagRef);
        }

        public override Tuple<float, float> GetAutoArrangeDesiredSize()
        {
            return new Tuple<float, float>(StateTagConditionModel.DefaultWidth, StateTagConditionModel.DefaultHeight);
        }
    }
}
