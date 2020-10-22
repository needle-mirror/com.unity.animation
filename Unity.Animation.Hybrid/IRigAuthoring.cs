using UnityEngine;

namespace Unity.Animation.Hybrid
{
    /// <summary>
    /// Interfaces that describe a rig authoring component generating
    /// a RigDefinition at conversion.
    /// </summary>
    internal interface IRigAuthoring
    {
        Transform[] Bones { get; }
    }
}
