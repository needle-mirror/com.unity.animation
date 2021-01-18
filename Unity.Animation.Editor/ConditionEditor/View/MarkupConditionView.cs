using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class MarkupConditionView : BaseConditionView
    {
        public MarkupConditionView(MarkupConditionViewModel viewModel)
            : base(viewModel)
        {
            AddToClassList("condition-editor-markup-fragment");

            TextField markupRef = new TextField() { name = "markup-reference", value = viewModel.MarkupConditionModel.MarkupReference };
            markupRef.RegisterValueChangedCallback(evt =>
            {
                Undo.RegisterCompleteObjectUndo((UnityEngine.Object)viewModel.AssetModel, "Set Value");
                viewModel.MarkupConditionModel.MarkupReference = evt.newValue;
                EditorUtility.SetDirty(viewModel.AssetModel as UnityEngine.Object);
            });
            markupRef.AddToClassList("condition-editor-markup-fragment__reference");
            Add(markupRef);
            EnumField isSet = new EnumField(QueryType.IsSet) { name = "is-set", value = viewModel.MarkupConditionModel.IsSet ? QueryType.IsSet : QueryType.IsNotSet };
            isSet.RegisterValueChangedCallback(evt =>
            {
                Undo.RegisterCompleteObjectUndo((UnityEngine.Object)viewModel.AssetModel, "Set Value");
                viewModel.MarkupConditionModel.IsSet = (QueryType)evt.newValue == QueryType.IsSet;
                EditorUtility.SetDirty(viewModel.AssetModel as UnityEngine.Object);
            });
            isSet.AddToClassList("condition-editor-markup-fragment__is-set");
            Add(isSet);
        }

        public override Tuple<float, float> GetAutoArrangeDesiredSize()
        {
            return new Tuple<float, float>(MarkupConditionModel.DefaultWidth, MarkupConditionModel.DefaultHeight);
        }
    }
}
