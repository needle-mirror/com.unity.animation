using UnityEngine;

namespace Unity.Animation.Hybrid
{
    /// <summary>
    /// Defines a SkinnedMeshRenderer rig channel declarator to inject blendshape weights to a rig definition.
    /// NOTE: Temporary solution until we have a nice way of declaring these at authoring time.
    /// </summary>
    internal class SkinnedMeshRendererRigChannelDeclarator : RigChannelDeclarator<SkinnedMeshRenderer>
    {
        static readonly string k_BlendShapeBindingPrefix = "blendShape.";

        protected override void DeclareRigChannels(RigChannelCollector collector, SkinnedMeshRenderer smr)
        {
            var sharedMesh = smr.sharedMesh;
            if (sharedMesh == null)
                throw new System.ArgumentNullException("SkinnedMeshRenderer contains a null SharedMesh.");

            int count = sharedMesh.blendShapeCount;
            if (count == 0)
                return;

            var relativePath = collector.ComputeRelativePath(smr.transform);
            for (int i = 0; i < count; ++i)
            {
                var id = new GenericBindingID
                {
                    AttributeName = k_BlendShapeBindingPrefix + sharedMesh.GetBlendShapeName(i),
                    ComponentType = typeof(SkinnedMeshRenderer),
                    Path = relativePath
                };

                collector.Add(new FloatChannel
                {
                    // TODO : Once we remove BindingHashDeprecationHelper, code should be replaced with Id = id.ID
                    Id = BindingHashDeprecationHelper.BuildPath(id.Path, id.AttributeName),
                    DefaultValue = smr.GetBlendShapeWeight(i)
                });
            }
        }
    }
}
