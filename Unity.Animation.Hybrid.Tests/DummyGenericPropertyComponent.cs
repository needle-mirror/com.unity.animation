using UnityEngine;
using Unity.Mathematics;

#pragma warning disable 0649

namespace Unity.Animation.Tests
{
    class DummyGenericPropertyComponent : MonoBehaviour
    {
        public float MyFloat;
        public int MyInt;
        public quaternion MyQuat;

        public float2 MyFloat2 = float2.zero;
        public float3 MyFloat3 = float3.zero;

        public struct MyStruct
        {
            public float a;
            public float b;
            public float c;
            public float d;
            public float e;
            public float f;
            public float g;
            public float h;
        }

        public MyStruct MyBuffer;
    }
}
