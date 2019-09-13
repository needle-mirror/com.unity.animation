using Unity.Entities;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    public class Skeleton : MonoBehaviour
    {
        public Transform[] Bones;

        [HideInInspector]
        public BlobAssetReference<RigDefinition> RigDefinitionPrefab;
    }
}
