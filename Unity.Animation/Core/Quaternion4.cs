using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animation
{
    // SOA 4-wide quaternion
    [Serializable]
    [BurstCompatible]
    public partial struct quaternion4 : IEquatable<quaternion4>, IFormattable
    {
        public float4 x;
        public float4 y;
        public float4 z;
        public float4 w;

        public static readonly quaternion4 identity = new quaternion4(float4.zero, float4.zero, float4.zero, math.float4(1f));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion4(float4 x, float4 y, float4 z, float4 w) { this.x = x; this.y = y; this.z = z; this.w = w; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion4(quaternion q0, quaternion q1, quaternion q2, quaternion q3)
        {
            var m = mathex.transpose(math.float4x4(q0.value, q1.value, q2.value, q3.value));
            x = m.c0; y = m.c1; z = m.c2; w = m.c3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion4(float4x4 m)
        {
            x = m.c0; y = m.c1; z = m.c2; w = m.c3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 operator+(quaternion4 lhs, quaternion4 rhs) { return new quaternion4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 operator*(quaternion4 lhs, float rhs) { return new quaternion4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 operator*(quaternion4 lhs, float4 rhs) { return new quaternion4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(quaternion4 q) { return x.Equals(q.x) && y.Equals(q.y) && z.Equals(q.z) && w.Equals(q.w); }

        [NotBurstCompatible]
        public override bool Equals(object o) { return Equals((quaternion4)o); }

        /// <summary>Returns a hash code for the quaternion4.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return (int)mathex.hash(this); }

        [NotBurstCompatible]
        /// <summary>Returns a string representation of the quaternion4.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return string.Format("quaternion4({0}f, {1}f, {2}f, {3}f,  {4}f, {5}f, {6}f, {7}f,  {8}f, {9}f, {10}f, {11}f,  {12}f, {13}f, {14}f, {15}f)", x.x, y.x, z.x, w.x, x.y, y.y, z.y, w.y, x.z, y.z, z.z, w.z, x.w, y.w, z.w, w.w);
        }

        [NotBurstCompatible]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("quaternion4({0}f, {1}f, {2}f, {3}f,  {4}f, {5}f, {6}f, {7}f,  {8}f, {9}f, {10}f, {11}f,  {12}f, {13}f, {14}f, {15}f)", x.x.ToString(format, formatProvider), y.x.ToString(format, formatProvider), z.x.ToString(format, formatProvider), w.x.ToString(format, formatProvider), x.y.ToString(format, formatProvider), y.y.ToString(format, formatProvider), z.y.ToString(format, formatProvider), w.y.ToString(format, formatProvider), x.z.ToString(format, formatProvider), y.z.ToString(format, formatProvider), z.z.ToString(format, formatProvider), w.z.ToString(format, formatProvider), x.w.ToString(format, formatProvider), y.w.ToString(format, formatProvider), z.w.ToString(format, formatProvider), w.w.ToString(format, formatProvider));
        }
    }

    [BurstCompatible]
    public static partial class mathex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 quaternion4(float4 x, float4 y, float4 z, float4 w) { return new quaternion4(x, y, z, w); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 quaternion4(quaternion q1, quaternion q2, quaternion q3, quaternion q4) { return new quaternion4(q1, q2, q3, q4); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 quaternion4(float4x4 m) { return new quaternion4(m); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 float4x4(quaternion4 q) { return new float4x4(q.x, q.y, q.z, q.w); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 select(quaternion4 a, quaternion4 b, bool4 c)
        {
            return quaternion4(math.select(a.x, b.x, c), math.select(a.y, b.y, c), math.select(a.z, b.z, c), math.select(a.w, b.w, c));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static quaternion4 chgsign(quaternion4 q, float4 sign)
        {
            uint4 sign_bits = math.asuint(sign) & 0x80000000;
            return quaternion4(
                math.asfloat(math.asuint(q.x) ^ sign_bits),
                math.asfloat(math.asuint(q.y) ^ sign_bits),
                math.asfloat(math.asuint(q.z) ^ sign_bits),
                math.asfloat(math.asuint(q.w) ^ sign_bits)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 dot(quaternion4 q1, quaternion4 q2) =>
            q1.x * q2.x + q1.y * q2.y + q1.z * q2.z + q1.w * q2.w;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 normalize(quaternion4 q) =>
            q * math.rsqrt(dot(q, q));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 normalizesafe(quaternion4 q)
        {
            float4 lensq = dot(q, q);
            return select(Animation.quaternion4.identity, q * math.rsqrt(lensq), lensq > math.FLT_MIN_NORMAL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 lerp(quaternion4 q1, quaternion4 q2, float4 blend)
        {
            q2 = chgsign(q2, dot(q1, q2));
            return normalize(
                quaternion4(
                    math.lerp(q1.x, q2.x, blend),
                    math.lerp(q1.y, q2.y, blend),
                    math.lerp(q1.z, q2.z, blend),
                    math.lerp(q1.w, q2.w, blend)
                )
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 add(quaternion4 q1, quaternion4 q2) =>
            q1 + chgsign(q2, dot(q1, q2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 quatWeight(quaternion4 q, float4 weight)
        {
            q.x *= weight; q.y *= weight; q.z *= weight;
            return normalizesafe(q);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 mul(quaternion4 lhs, quaternion4 rhs)
        {
            var a = transpose(float4x4(lhs));
            var b = transpose(float4x4(rhs));
            a.c0 = mul(math.quaternion(a.c0), math.quaternion(b.c0)).value;
            a.c1 = mul(math.quaternion(a.c1), math.quaternion(b.c1)).value;
            a.c2 = mul(math.quaternion(a.c2), math.quaternion(b.c2)).value;
            a.c3 = mul(math.quaternion(a.c3), math.quaternion(b.c3)).value;
            return quaternion4(transpose(a));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion4 conjugate(quaternion4 q)
        {
            return quaternion4(q.x * -1f, q.y * -1f, q.z * -1f, q.w);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint hash(quaternion4 q)
        {
            return math.csum(
                math.asuint(q.x) * math.uint4(0xC4B1493Fu, 0xBA0966D3u, 0xAFBEE253u, 0x5B419C01u) +
                math.asuint(q.y) * math.uint4(0x515D90F5u, 0xEC9F68F3u, 0xF9EA92D5u, 0xC2FAFCB9u) +
                math.asuint(q.z) * math.uint4(0x616E9CA1u, 0xC5C5394Bu, 0xCAE78587u, 0x7A1541C9u) +
                math.asuint(q.w) * math.uint4(0xF83BD927u, 0x6A243BCBu, 0x509B84C9u, 0x91D13847u)) + 0x52F7230Fu;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 hashwide(quaternion4 q)
        {
            return (math.asuint(q.x) * math.uint4(0xCF286E83u, 0xE121E6ADu, 0xC9CA1249u, 0x69B60C81u) +
                math.asuint(q.y) * math.uint4(0xE0EB6C25u, 0xF648BEABu, 0x6BDB2B07u, 0xEF63C699u) +
                math.asuint(q.z) * math.uint4(0x9001903Fu, 0xA895B9CDu, 0x9D23B201u, 0x4B01D3E1u) +
                math.asuint(q.w) * math.uint4(0x7461CA0Du, 0x79725379u, 0xD6258E5Bu, 0xEE390C97u)) + 0x9C8A2F05u;
        }
    }
}
