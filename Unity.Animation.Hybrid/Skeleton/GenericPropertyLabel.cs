using UnityEngine;

namespace Unity.Animation.Authoring
{
    class GenericPropertyLabel : PropertyLabelBase
    {
        public GenericPropertyType ValueType;

        protected GenericPropertyLabel()
        {}

        public static GenericPropertyLabel Create(string name)
        {
            var label = CreateInstance<GenericPropertyLabel>();
            label.name = name;

            return label;
        }
    }
}
