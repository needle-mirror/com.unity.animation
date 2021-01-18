using Unity.Properties.UI;
using UnityEngine.UIElements;

namespace Unity.Animation.Editor
{
    class AnimationOutputNodeInspector : Inspector<AnimationOutputNodeModel>
    {
        public override VisualElement Build()
        {
            var toggle = new Toggle("Loop") { value = Target.Loop };
            toggle.bindingPath = nameof(AnimationOutputNodeModel.Loop);
            return toggle;
        }
    }
}
