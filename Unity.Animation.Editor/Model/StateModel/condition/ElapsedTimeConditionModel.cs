using System;
using System.Text;
using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class ElapsedTimeConditionModel : BaseConditionModel
    {
        public static float DefaultWidth = 300f;
        public static float DefaultHeight = 30f;

        [SerializeField]
        public float TimeElapsed;

        public override string ToString(int indentLevel = 0)
        {
            string indentLevelStr = GetIndentationString(indentLevel);
            StringBuilder sb = new StringBuilder();
            sb.Append(indentLevelStr);
            sb.Append($"Time elapsed > {TimeElapsed}");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
