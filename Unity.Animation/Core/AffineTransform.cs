using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animation
{
    [BurstCompatible]
    public struct AffineTransform : IEquatable<AffineTransform>, IFormattable
    {
        public float3x3 rs;
        public float3   t;

        public static readonly AffineTransform identity = new AffineTransform(float3.zero, float3x3.identity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float3 t, float3x3 rs)
        {
            this.rs = rs;
            this.t = t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float3 t, quaternion r)
        {
            this.rs = math.float3x3(r);
            this.t = t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float3 t, quaternion r, float3 s)
        {
            this.rs = mathex.mulScale(math.float3x3(r), s);
            this.t  = t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal AffineTransform(TRS x)
        {
            rs = mathex.mulScale(math.float3x3(x.r), x.s);
            t  = x.t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(RigidTransform rigid)
        {
            rs = math.float3x3(rigid.rot);
            t = rigid.pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float3x3 m)
        {
            rs = m;
            t = float3.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float3x4 m)
        {
            rs = math.float3x3(m.c0, m.c1, m.c2);
            t  = m.c3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform(float4x4 m)
        {
            rs = math.float3x3(m.c0.xyz, m.c1.xyz, m.c2.xyz);
            t = m.c3.xyz;
        }

        public static implicit operator float3x4(AffineTransform m) =>
            new float3x4(m.rs.c0, m.rs.c1, m.rs.c2, m.t);

        public static implicit operator float4x4(AffineTransform m) =>
            new float4x4(new float4(m.rs.c0, 0f), new float4(m.rs.c1, 0f), new float4(m.rs.c2, 0f), new float4(m.t, 1f));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AffineTransform x) => rs.Equals(x.rs) && t.Equals(x.t);

        [NotBurstCompatible]
        public override bool Equals(object x) => Equals((AffineTransform)x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => (int)mathex.hash(this);

        [NotBurstCompatible]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return string.Format("AffineTransform(({0}f, {1}f, {2}f,  {3}f, {4}f, {5}f,  {6}f, {7}f, {8}f), ({9}f, {10}f, {11}f))",
                rs.c0.x, rs.c1.x, rs.c2.x, rs.c0.y, rs.c1.y, rs.c2.y, rs.c0.z, rs.c1.z, rs.c2.z, t.x, t.y, t.z
            );
        }

        [NotBurstCompatible]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format("AffineTransform(({0}f, {1}f, {2}f,  {3}f, {4}f, {5}f,  {6}f, {7}f, {8}f), ({9}f, {10}f, {11}f))",
                rs.c0.x.ToString(format, formatProvider), rs.c1.x.ToString(format, formatProvider), rs.c2.x.ToString(format, formatProvider),
                rs.c0.y.ToString(format, formatProvider), rs.c1.y.ToString(format, formatProvider), rs.c2.y.ToString(format, formatProvider),
                rs.c0.z.ToString(format, formatProvider), rs.c1.z.ToString(format, formatProvider), rs.c2.z.ToString(format, formatProvider),
                t.x.ToString(format, formatProvider), t.y.ToString(format, formatProvider), t.z.ToString(format, formatProvider)
            );
        }
    }

    [BurstCompatible]
    public static partial class mathex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float3 translation, quaternion rotation) =>
            new AffineTransform(translation, rotation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float3 translation, float3x3 rs) =>
            new AffineTransform(translation, rs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float4x4 m) =>
            new AffineTransform(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float3x4 m) =>
            new AffineTransform(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform AffineTransform(float3 translation, quaternion rotation, float3 scale) =>
            new AffineTransform(translation, rotation, scale);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AffineTransform AffineTransform(TRS trs) =>
            new AffineTransform(trs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 float4x4(AffineTransform transform) =>
            new float4x4(new float4(transform.rs.c0, 0f), new float4(transform.rs.c1, 0f), new float4(transform.rs.c2, 0f), new float4(transform.t, 1f));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x4 float3x4(AffineTransform transform) =>
            new float3x4(transform.rs.c0, transform.rs.c1, transform.rs.c2, transform.t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mul(AffineTransform a, float3 v) =>
            a.t + math.mul(a.rs, v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform mul(AffineTransform a, AffineTransform b) =>
            new AffineTransform(mul(a, b.t), math.mul(a.rs, b.rs));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform mul(float3x3 rs, AffineTransform b) =>
            new AffineTransform(math.mul(rs, b.t), math.mul(rs, b.rs));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform mul(AffineTransform a, float3x3 rs) =>
            new AffineTransform(a.t, math.mul(rs, a.rs));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AffineTransform inverse(AffineTransform a)
        {
            AffineTransform inv;
            inv.rs = inverse(a.rs);
            inv.t  = math.mul(inv.rs, -a.t);
            return inv;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint hash(AffineTransform a) =>
            math.hash(a.rs) + 0xC5C5394Bu * math.hash(a.t);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 hashwide(AffineTransform a) =>
            math.hashwide(a.rs).xyzz + 0xC5C5394Bu * math.hashwide(a.t).xyzz;
    }
}
