using System;
using System.ComponentModel;

namespace Unity.Animation.Hybrid
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to DefaultAnimationGraphReadTransformHandle (RemovedAfter 2020-12-21) (UnityUpgradable) -> DefaultAnimationGraphReadTransformHandle", true)]
    public class PreAnimationGraphReadTransformHandle : ReadExposeTransform<PreAnimationGraphSystem.ReadTransformHandle>
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to DefaultAnimationGraphWriteTransformHandle (RemovedAfter 2020-12-21) (UnityUpgradable) -> DefaultAnimationGraphWriteTransformHandle", true)]
    public class PreAnimationGraphWriteTransformHandle : WriteExposeTransform<PreAnimationGraphSystem.WriteTransformHandle>
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to LateAnimationGraphReadTransformHandle (RemovedAfter 2020-12-21) (UnityUpgradable) -> LateAnimationGraphReadTransformHandle", true)]
    public class PostAnimationGraphReadTransformHandle : ReadExposeTransform<PostAnimationGraphSystem.ReadTransformHandle>
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to LateAnimationGraphWriteTransformHandle (RemovedAfter 2020-12-21) (UnityUpgradable) -> LateAnimationGraphWriteTransformHandle", true)]
    public class PostAnimationGraphWriteTransformHandle : WriteExposeTransform<PostAnimationGraphSystem.WriteTransformHandle>
    {
    }
}
