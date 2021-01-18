using System;
using System.Text;
using UnityEngine;

namespace Unity.Animation.Model
{
    //to remember : if we modify the ComponentType, we might have to recompile all assets
    [Serializable]
    internal struct BlackboardValueReference
    {
        public ComponentBindingIdentifier ComponentBindingId;
        public FieldHandle FieldId;
    }

    [Serializable]
    internal class BlackboardValueConditionModel : BaseConditionModel
    {
        public static float DefaultWidth = 300f;
        public static float DefaultHeight = 30f;


        public enum ComparisonType
        {
            Equal,
            NotEqual,
            Smaller,
            SmallerOrEqual,
            Greater,
            GreaterOrEqual
        }

        public bool IsNumericalType()
        {
            //if false, can't support < >
            return true;
        }

        [SerializeField]
        public ComparisonType Comparison;

        public string ComparisonAsString
        {
            get
            {
                switch (Comparison)
                {
                    case ComparisonType.Equal:
                        return "==";
                    case ComparisonType.NotEqual:
                        return "!=";
                    case ComparisonType.Smaller:
                        return "<";
                    case ComparisonType.SmallerOrEqual:
                        return "<=";
                    case ComparisonType.Greater:
                        return ">";
                    case ComparisonType.GreaterOrEqual:
                        return ">=";
                }

                return string.Empty;
            }
        }

        [SerializeField]
        public BlackboardValueReference BlackboardValueReference;

        [SerializeField]
        public GraphVariant CompareValue;

        [SerializeField]
        public BlackboardValueReference CompareBlackboardValueReference;

        [SerializeField]
        public bool IsCompareBlackboardValueReference;

        public override string ToString(int indentLevel = 0)
        {
            string indentLevelStr = GetIndentationString(indentLevel);
            StringBuilder sb = new StringBuilder();
            sb.Append(indentLevelStr);
//            if (!BlackboardValueEntityReference.IsEmpty())
//                sb.Append($"{GameplayPropertyReferenceEntityRef}.");
//            sb.Append($"{GameplayPropertyReference} {ComparisonAsString} ");
//            if (CompareGameplayPropertyReferenceValueEntityRef != string.Empty && IsCompareGameplayPropertyReferenceValue)
//                sb.Append($"{CompareGameplayPropertyReferenceValueEntityRef}");
//            sb.Append(IsCompareGameplayPropertyReferenceValue ? $"{CompareGameplayPropertyReferenceValue}" : $"{CompareValue}");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
