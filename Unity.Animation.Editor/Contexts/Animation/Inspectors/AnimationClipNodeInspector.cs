using Unity.Properties.UI;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    class AnimationClipNodeInspector : Inspector<AnimationClipNodeModel>
    {
        Toggle m_LoopToggle;

        public override VisualElement Build()
        {
            var root = new VisualElement();
            var overrideToggle = new Toggle("Override") { value = Target.OverrideLoop };
            overrideToggle.bindingPath = nameof(AnimationClipNodeModel.OverrideLoop);
            root.Add(overrideToggle);
            m_LoopToggle = new Toggle("Loop") { value = Target.Loop };
            m_LoopToggle.style.paddingLeft = 10;
            m_LoopToggle.SetEnabled(Target.OverrideLoop);
            m_LoopToggle.bindingPath = nameof(AnimationClipNodeModel.Loop);
            root.Add(m_LoopToggle);
            return root;
        }

        public override void Update()
        {
            m_LoopToggle.SetEnabled(Target.OverrideLoop);
        }
    }
}
