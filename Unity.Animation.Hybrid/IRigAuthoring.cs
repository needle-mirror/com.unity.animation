using System.Collections.Generic;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    struct RigIndexToBone
    {
        public int Index;
        public Transform Bone;
    }

    /// <summary>
    /// Interfaces that describe a rig authoring component generating
    /// a RigDefinition at conversion.
    /// </summary>
    interface IRigAuthoring
    {
        void GetBones(List<RigIndexToBone> bones);
    }
}
