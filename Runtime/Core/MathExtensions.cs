using System.Runtime.CompilerServices;

namespace Unity.Animation
{
    using Mathematics;

    public static partial class mathex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 one() => new float3(1f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 right() => new float3(1f, 0f, 0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 mulScale(float3x3 m, float3 s) => new float3x3(m.c0 * s.x, m.c1 * s.y, m.c2 * s.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 mulScale(float3x3 m, float s) => new float3x3(m.c0 * s, m.c1 * s, m.c2 * s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 chgsign(float3 val, float3 sign) =>
            math.asfloat(math.asuint(val) ^ (math.asuint(sign) & 0x80000000));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 chgsign(float4 val, float4 sign) =>
            math.asfloat(math.asuint(val) ^ (math.asuint(sign) & 0x80000000));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion scaleMulQuat(float3 scale, quaternion q)
        {
            float3 s = chgsign(one(), scale);
            return chgsign(q.value, new float4(s.yxx * s.zzy, 0f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion lerp(quaternion p, quaternion q, float blend) =>
            math.normalize(p.value + blend * (chgsign(q.value, math.dot(p.value, q.value)) - p.value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion add(quaternion p, quaternion q) =>
            p.value + (chgsign(q.value, math.dot(p.value, q.value)));

        /// <summary>Returns b if c is true, a otherwise.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion select(quaternion a, quaternion b, bool c) => c ? b : a;

        // returns the weighted quaternion. This is very useful for additive blending.
        // var result = math.mul(basevalue, quaternionWeight(layerValue, 0.5F))
        // q must be normalized
        // returns a normalized quaternion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion quatWeight(quaternion q, float weight) =>
            new quaternion(math.normalizesafe(new float4(q.value.xyz * weight, q.value.w)));

        public static RigidTransform rigidPow(RigidTransform x, int pow)
        {
            var ret = RigidTransform.identity;

            for (int powIter = 0; powIter < pow; powIter++)
            {
                ret = math.mul(ret, x);
            }

            return ret;
        }

        public static RigidTransform select(RigidTransform a, RigidTransform b, bool c) =>
            new RigidTransform(math.select(a.rot.value, b.rot.value, c), math.select(a.pos, b.pos, c));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 cross(float4 p0, float4 p1) =>
            (p0 * p1.yzxw - p0.yzxw * p1).yzxw;

        // Creates a rotation which rotates from vector a to b.
        // Input vectors can be either normalized or not, function will return a normalized quaternion.
        // NOTE: Two parallel vectors pointing in opposite directions yield NANs as there is an infinity of solutions.
        public static quaternion fromTo(float3 a, float3 b)
        {
            float4 aa = new float4(a, 0f);
            float4 bb = new float4(b, 0f);

            float4 q = cross(aa, bb);
            q.w = math.dot(aa, bb) + math.sqrt(math.dot(aa, aa) * math.dot(bb, bb));

            return math.normalize(q);
        }
    }
}

