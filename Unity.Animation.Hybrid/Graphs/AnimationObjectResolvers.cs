using System;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    internal class MotionIDResolver : IObjectResolver
    {
        public Type Type => typeof(MotionID);

        public GraphVariant ResolveValue(UnityEngine.Object objectReference, Component context)
        {
            if (context is RigComponent)
            {
                var rig = context as RigComponent;

                for (var boneIter = 0; boneIter < rig.Bones.Length; boneIter++)
                {
                    if (objectReference == rig.Bones[boneIter])
                    {
                        StringHash hash = RigGenerator.ComputeRelativePath(rig.Bones[boneIter], rig.transform);
                        return hash.Id;
                    }
                }
            }
            throw new ArgumentException("MotionIDResolver : Invalid arguments, objectReference should be a Transform and context should be a RigComponent");
        }
    }
}
