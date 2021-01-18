using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Animation.Model
{
    [Serializable]
    class PortGroup
    {
        public int Index;
        public int Size;
    }

    [Serializable]
    class PortGroupConstant : Constant<PortGroup>
    {
        protected override PortGroup FromObject(object value)
        {
            if (value != null && value.GetType() == typeof(PortGroup))
                return (PortGroup)value;
            if (m_Value != null && value != null && value.GetType() == typeof(int))
                m_Value.Size = (int)value;
            return m_Value;
        }
    }

    internal static partial class ConstantEditorExtensions
    {
        internal static VisualElement BuildPortGroup(this IConstantEditorBuilder builder, PortGroupConstant group)
        {
            return ConstantEditorHelper.BuildPortGroupEditor(builder, group.Value);
        }
    }

    internal static partial class ConstantEditorHelper
    {
        internal static void UpdatePortGroup(IChangeEvent evt, IPortModel portModel, PortGroup group)
        {
            var intEvt = evt as ChangeEvent<int>;
            if (intEvt == null || intEvt.newValue == intEvt.previousValue)
                return;
            group.Size = intEvt.newValue;
            (portModel.NodeModel as IPortGroup)?.SetPortGroupInstanceSize(group.Index, group.Size);
        }

        internal static VisualElement BuildPortGroupEditor(IConstantEditorBuilder builder, PortGroup group)
        {
            Action<IChangeEvent> valueChangeCallbackWrapper = (evt) =>
            {
                builder.OnValueChanged(evt);
                UpdatePortGroup(evt, builder.PortModel, group);
                builder.Store.State.MarkChanged(builder.PortModel.NodeModel);
            };

            return UnityEditor.GraphToolsFoundation.Overdrive.ConstantEditorExtensions.BuildInlineValueEditor(
                group.Size, new IntegerField(), valueChangeCallbackWrapper);
        }
    }
}
