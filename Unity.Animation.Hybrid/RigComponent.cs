using System;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    [System.Serializable]
    public class TranslationChannel : System.Object
    {
        public string Id;
        public Vector3 DefaultValue;
    }

    [System.Serializable]
    public class RotationChannel : System.Object
    {
        public string Id;
        public Quaternion DefaultValue;
    }

    [System.Serializable]
    public class ScaleChannel : System.Object
    {
        public string Id;
        public Vector3 DefaultValue;
    }

    [System.Serializable]
    public class FloatChannel : System.Object
    {
        public string Id;
        public float DefaultValue;
    }

    [System.Serializable]
    public class IntChannel : System.Object
    {
        public string Id;
        public int DefaultValue;
    }

    public class RigComponent : MonoBehaviour
    {
        public Transform[] Bones = Array.Empty<Transform>();

        public TranslationChannel[] TranslationChannels = Array.Empty<TranslationChannel>();
        public RotationChannel[] RotationChannels = Array.Empty<RotationChannel>();
        public ScaleChannel[] ScaleChannels = Array.Empty<ScaleChannel>();
        public FloatChannel[] FloatChannels = Array.Empty<FloatChannel>();
        public IntChannel[] IntChannels = Array.Empty<IntChannel>();
    }
}
