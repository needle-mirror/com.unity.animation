using UnityEngine;

namespace Unity.Animation.Hybrid
{
    [DisallowMultipleComponent]
    public class BoneRendererComponent : MonoBehaviour
    {
        public bool RenderBones = true;
        public BoneRendererUtils.BoneShape BoneShape;
        public Color Color = Color.white;
        public float Size = 1.0f;

        // This is actually just to get a reference in the scene so that our shader is included in the build.
        // If you include the shader in the Resources folder you don't need this.
        public Material BoneMaterial;

        [System.Obsolete("A reference to RigComponent is not necessary anymore (RemovedAfter 2020-12-22).")]
        public RigComponent RigComponent;
        [Tooltip("The bones to render. Bones without a connection in the joints' list are not rendered for now.")]
        public Transform[] Transforms;
    }
}
