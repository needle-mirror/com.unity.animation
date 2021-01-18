using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class BlackboardValueConditionView : BaseConditionView
    {
        public BlackboardValueConditionView(BlackboardValueConditionViewModel viewModel)
            : base(viewModel)
        {
            AddToClassList("condition-editor-blackboard-value-fragment");
            Type fieldType = viewModel.BlackboardValueConditionModel.BlackboardValueReference.FieldId.ResolveType();
            bool isInt = fieldType == typeof(int);
            bool isFloat = fieldType == typeof(float);
            bool isBool = fieldType == typeof(bool);

            string valueNamespace = string.Empty;
            var stateMachineModel = viewModel.AssetModel.GraphModel as StateMachineModel;
            if (stateMachineModel != null)
            {
                if (stateMachineModel.TryGetComponentBinding(viewModel.BlackboardValueConditionModel.BlackboardValueReference.ComponentBindingId, out var binding))
                {
                    valueNamespace = $"{binding.Name}.";
                }
            }
            Label blackboardValueRef = new Label() { name = "blackboard-value-reference", text = $"{valueNamespace}{viewModel.BlackboardValueConditionModel.BlackboardValueReference.FieldId.Resolve()}" };
            blackboardValueRef.AddToClassList("condition-editor-blackboard-value-fragment__reference");
            Add(blackboardValueRef);
            if (isInt || isFloat)
            {
                EnumField comparisonOp = new EnumField(BlackboardValueConditionModel.ComparisonType.Equal) { name = "operation", value = viewModel.BlackboardValueConditionModel.Comparison };
                comparisonOp.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)viewModel.AssetModel, "Set Value");
                    viewModel.BlackboardValueConditionModel.Comparison = (BlackboardValueConditionModel.ComparisonType)evt.newValue;
                    EditorUtility.SetDirty(viewModel.AssetModel as UnityEngine.Object);
                });
                comparisonOp.AddToClassList("condition-editor-blackboard-value-fragment__comparison-operation");
                Add(comparisonOp);
            }
            else if (isBool)
            {
                Label comparisonOp = new Label() { name = "operation", text = "is " };
                comparisonOp.AddToClassList("condition-editor-blackboard-value-fragment__comparison-operation");
                Add(comparisonOp);
            }
            if (isInt)
            {
                IntegerField value = new IntegerField() { name = "value", value = viewModel.BlackboardValueConditionModel.CompareValue.Int };
                value.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)viewModel.AssetModel, "Set Value");
                    viewModel.BlackboardValueConditionModel.CompareValue = evt.newValue;
                    EditorUtility.SetDirty(viewModel.AssetModel as UnityEngine.Object);
                });
                value.AddToClassList("condition-editor-blackboard-value-fragment__value");
                Add(value);
            }
            else if (isFloat)
            {
                FloatField value = new FloatField() { name = "value", value = viewModel.BlackboardValueConditionModel.CompareValue.Float };
                value.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)viewModel.AssetModel, "Set Value");
                    viewModel.BlackboardValueConditionModel.CompareValue = evt.newValue;
                    EditorUtility.SetDirty(viewModel.AssetModel as UnityEngine.Object);
                });
                value.AddToClassList("condition-editor-blackboard-value-fragment__value");
                Add(value);
            }
            else if (isBool)
            {
                Toggle value = new Toggle() { name = "value", value = viewModel.BlackboardValueConditionModel.CompareValue.Bool };
                value.RegisterValueChangedCallback(evt =>
                {
                    Undo.RegisterCompleteObjectUndo((UnityEngine.Object)viewModel.AssetModel, "Set Value");
                    viewModel.BlackboardValueConditionModel.CompareValue = evt.newValue;
                    EditorUtility.SetDirty(viewModel.AssetModel as UnityEngine.Object);
                });
                value.AddToClassList("condition-editor-blackboard-value-fragment__value");
                Add(value);
            }
        }

        public override Tuple<float, float> GetAutoArrangeDesiredSize()
        {
            return new Tuple<float, float>(BlackboardValueConditionModel.DefaultWidth, BlackboardValueConditionModel.DefaultHeight);
        }
    }
}
