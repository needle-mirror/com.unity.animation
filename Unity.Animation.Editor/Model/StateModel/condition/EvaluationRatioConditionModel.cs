using System;
using System.Text;
using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class EvaluationRatioConditionModel : BaseConditionModel
    {
        public static float DefaultWidth = 300f;
        public static float DefaultHeight = 30f;

        [SerializeField]
        public float Ratio;

        public override string ToString(int indentLevel = 0)
        {
            string indentLevelStr = GetIndentationString(indentLevel);
            StringBuilder sb = new StringBuilder();
            sb.Append(indentLevelStr);
            sb.Append($"Evaluation ratio > {Ratio}");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
