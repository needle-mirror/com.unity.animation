#if UNITY_EDITOR

using UnityEditor;

namespace Unity.Animation.Hybrid
{
    [ComponentBindingProcessor(typeof(UnityEngine.Animation))]
    class AnimationBindingProcessor : ComponentBindingProcessor<UnityEngine.Animation>
    {
        protected override ChannelBindType Process(EditorCurveBinding binding)
        {
            return ChannelBindType.Integer;
        }
    }
}

#endif
