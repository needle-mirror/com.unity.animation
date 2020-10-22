using UnityEngine;

namespace Unity.Animation.Authoring
{
    class TransformLabel : PropertyLabelBase
    {
        protected TransformLabel()
        {}

        public static TransformLabel Create(string name)
        {
            var label = ScriptableObject.CreateInstance<TransformLabel>();
            label.name = name;

            return label;
        }
    }
}
