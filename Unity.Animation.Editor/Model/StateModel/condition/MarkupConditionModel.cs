using System;
using System.Text;
using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class MarkupConditionModel : BaseConditionModel
    {
        public static float DefaultWidth = 300f;
        public static float DefaultHeight = 30f;

        [SerializeField]
        public string MarkupReference;

        [SerializeField]
        public bool IsSet = true;

        [SerializeField]
        public string MarkupEntityRef;

        public override string ToString(int indentLevel = 0)
        {
            string indentLevelStr = GetIndentationString(indentLevel);
            StringBuilder sb = new StringBuilder();
            sb.Append(indentLevelStr);
            if (MarkupEntityRef != string.Empty)
                sb.Append($"{MarkupEntityRef}.");
            sb.Append($"{MarkupReference} ");
            sb.Append(IsSet ? "Is set" : "Is not set");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
