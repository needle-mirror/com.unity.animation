using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Animation.Authoring;

namespace Unity.Animation.Hybrid
{
    // This is a placeholder component to test out conversion
    // workflows for the Skeleton asset. This will at some
    // point be replaced by more fleshed out animation player that
    // make use of the Skeleton asset.
    public class RigAuthoring : MonoBehaviour, IRigAuthoring
    {
        public Authoring.Skeleton Skeleton;

        protected RigAuthoring()
        {
        }

        Transform[] IRigAuthoring.Bones
        {
            get
            {
                if (Skeleton == null)
                    return Array.Empty<Transform>();

                var activeTransformChannels = Skeleton.ActiveTransformChannels;
                var bones = new List<Transform>(activeTransformChannels.Count);

                Transform[] children = GetComponentsInChildren<Transform>();
                for (int i = 0; i < children.Length; ++i)
                {
                    var path = RigGenerator.ComputeRelativePath(children[i], transform);
                    if (Skeleton.Contains(new TransformBindingID { Path = path }))
                    {
                        bones.Add(children[i]);
                    }
                }

                return bones.ToArray();
            }
        }
    }
}
