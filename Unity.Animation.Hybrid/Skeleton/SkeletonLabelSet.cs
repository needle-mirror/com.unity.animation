using System.Collections.Generic;
using UnityEngine;

namespace Unity.Animation.Authoring
{
    /// <summary>
    /// The skeleton label set is an asset that assembles property labels for use in Skeleton.
    /// </summary>
    //[CreateAssetMenu(fileName = "SkeletonLabelSet", menuName = "Animation/Create Skeleton Label Set", order = 1)]
    class SkeletonLabelSet : ScriptableObject
    {
        public List<TransformLabel> TransformLabels = new List<TransformLabel>();
        public List<GenericPropertyLabel> GenericPropertyLabels = new List<GenericPropertyLabel>();

        public static SkeletonLabelSet Create(string name)
        {
            var skeletonLabelSet = ScriptableObject.CreateInstance<SkeletonLabelSet>();
            skeletonLabelSet.name = name;

            return skeletonLabelSet;
        }

        /// <summary>
        /// Clears all labels.
        /// </summary>
        public void Clear()
        {
            TransformLabels.Clear();
            GenericPropertyLabels.Clear();
        }
    }
}
