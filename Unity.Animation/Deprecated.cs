using System;
using System.ComponentModel;
using Unity.Entities;

namespace Unity.Animation
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to ComputeBoneRenderingMatricesBase (RemovedAfter 2020-12-21) (UnityUpgradable) -> ComputeBoneRenderingMatricesBase", true)]
    public abstract class BoneRendererMatrixSystemBase : SystemBase
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Renamed to RenderBonesBase (RemovedAfter 2020-12-21) (UnityUpgradable) -> RenderBonesBase", true)]
    public abstract class BoneRendererRenderingSystemBase : SystemBase
    {
    }
}
