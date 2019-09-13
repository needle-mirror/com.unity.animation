using UnityEngine;

namespace Unity.Animation.Hybrid
{
    [DisallowMultipleComponent]
    public class BoneRendererAuthoring : MonoBehaviour
    {
        public bool RenderBones = true;
        public BoneRendererUtils.BoneShape BoneShape;
        public Color Color = Color.white;
        public float Size = 1.0f;

        public RigComponent RigComponent;
        [Tooltip("The bones to render. Bones without a connection in the joints' list are not rendered for now.")]
        public Transform[] Transforms;
    }
}
