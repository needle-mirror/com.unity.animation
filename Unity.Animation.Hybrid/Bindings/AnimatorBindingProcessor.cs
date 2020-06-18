#if UNITY_EDITOR

using UnityEditor;

namespace Unity.Animation.Hybrid
{
    [ComponentBindingProcessor(typeof(UnityEngine.Animator))]
    class AnimatorBindingProcessor : ComponentBindingProcessor<UnityEngine.Animator>
    {
        protected override ChannelBindType Process(EditorCurveBinding binding)
        {
            return ChannelBindType.Float;
        }
    }
}

#endif
