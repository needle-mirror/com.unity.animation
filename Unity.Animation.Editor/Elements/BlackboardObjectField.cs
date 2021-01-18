using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Animation.Editor
{
    internal class BlackboardObjectField : VisualElement
    {
        public BlackboardObjectField(string text, Type objectType, Object obj, Action<Object> valueChanged, bool enabled = true)
        {
            var field = new ObjectField();
            field.objectType = objectType;

            //Mimic UIElement property fields style
            AddToClassList("unity-property-field");
            field.value = obj;
            field.SetEnabled(enabled);
            field.RegisterValueChangedCallback(evt => valueChanged(evt.newValue));
            Add(field);
        }
    }
}
