using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animation
{
    [BurstCompatible]
    internal struct TRS
    {
        public quaternion r;
        public float3 t;
        public float3 s;

        internal static readonly TRS identity = new TRS(float3.zero, quaternion.identity, 1f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TRS(float3 t, quaternion r, float3 s)
        {
            this.t = t; this.r = r; this.s = s;
        }
    }

    [BurstCompatible]
    public static partial class mathex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 mul(TRS x, float3 v) =>
            x.t + math.mul(x.r, v * x.s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 inverseMul(TRS x, float3 v) =>
            math.mul(math.conjugate(x.r), v - x.t) * rcpsafe(x.s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AffineTransform inverse(TRS x)
        {
            AffineTransform a;
            a.rs = math.float3x3(math.conjugate(x.r));
            a.rs = scaleMul(rcpsafe(x.s), a.rs);
            a.t = math.mul(a.rs, -x.t);
            return a;
        }
    }
}
