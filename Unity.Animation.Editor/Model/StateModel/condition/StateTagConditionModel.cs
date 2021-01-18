using System;
using System.Text;
using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class StateTagConditionModel : BaseConditionModel
    {
        public static float DefaultWidth = 300f;
        public static float DefaultHeight = 30f;

        [SerializeField]
        public string StateTagReference;

        public override string ToString(int indentLevel = 0)
        {
            string indentLevelStr = GetIndentationString(indentLevel);
            StringBuilder sb = new StringBuilder();
            sb.Append(indentLevelStr);
            sb.Append("Is coming from {StateTagReference} state");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
