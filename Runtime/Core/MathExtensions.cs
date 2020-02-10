using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Unity.Animation
{
    public static partial class mathex
    {
        const float k_EpsilonNormalSqrt = 1e-15f;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion mul(quaternion q1, quaternion q2)
        {
            return chgsign((q1.value.ywzx * q2.value.xwyz - q1.value.wxyz * q2.value.zxzx - q1.value.zzww * q2.value.wzxy - q1.value.xyxy * q2.value.yyww).zwxy, math.float4(-1f, -1f, -1f, 1f));
        }

        /// <summary>Returns b if c is true, a otherwise.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion select(quaternion a, quaternion b, bool c) => c ? b : a;

        // returns the weighted quaternion. This is very useful for additive blending.
        // var result = mathex.mul(basevalue, quaternionWeight(layerValue, 0.5F))
        // q must be normalized
        // returns a normalized quaternion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion quatWeight(quaternion q, float weight) =>
            new quaternion(math.normalizesafe(new float4(q.value.xyz * weight, q.value.w)));

        public static RigidTransform rigidPow(RigidTransform x, uint pow)
        {
            var ret = RigidTransform.identity;

            while (pow > 0)
            {
                if ((pow & 1) == 1)
                    ret = math.mul(ret, x);

                x = math.mul(x, x);
                pow >>= 1;
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

        public static float angle(float3 from, float3 to)
        {
            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
            float denominator = math.sqrt(math.lengthsq(from) * math.lengthsq(to));
            if (denominator < k_EpsilonNormalSqrt)
                return 0f;

            return math.acos(math.clamp(math.dot(from, to) * math.rcp(denominator), -1f, 1f));
        }

        public static float3 projectOnPlane(float3 vector, float3 planeNormal)
        {
            float lenSq = math.lengthsq(planeNormal);
            if (lenSq < math.FLT_MIN_NORMAL)
                return vector;

            return math.mad(planeNormal, -(math.dot(vector, planeNormal) * math.rcp(lenSq)), vector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 transpose(float4x4 m)
        {
            float4 v1 = math.shuffle(m.c0, m.c1, math.ShuffleComponent.LeftX, math.ShuffleComponent.LeftY, math.ShuffleComponent.RightX, math.ShuffleComponent.RightY);
            float4 v3 = math.shuffle(m.c0, m.c1, math.ShuffleComponent.LeftZ, math.ShuffleComponent.LeftW, math.ShuffleComponent.RightZ, math.ShuffleComponent.RightW);
            float4 v2 = math.shuffle(m.c2, m.c3, math.ShuffleComponent.LeftX, math.ShuffleComponent.LeftY, math.ShuffleComponent.RightX, math.ShuffleComponent.RightY);
            float4 v4 = math.shuffle(m.c2, m.c3, math.ShuffleComponent.LeftZ, math.ShuffleComponent.LeftW, math.ShuffleComponent.RightZ, math.ShuffleComponent.RightW);
            return new float4x4(
                math.shuffle(v1, v2, math.ShuffleComponent.LeftX, math.ShuffleComponent.LeftZ, math.ShuffleComponent.RightX, math.ShuffleComponent.RightZ),
                math.shuffle(v1, v2, math.ShuffleComponent.LeftY, math.ShuffleComponent.LeftW, math.ShuffleComponent.RightY, math.ShuffleComponent.RightW),
                math.shuffle(v3, v4, math.ShuffleComponent.LeftX, math.ShuffleComponent.LeftZ, math.ShuffleComponent.RightX, math.ShuffleComponent.RightZ),
                math.shuffle(v3, v4, math.ShuffleComponent.LeftY, math.ShuffleComponent.LeftW, math.ShuffleComponent.RightY, math.ShuffleComponent.RightW)
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 eulerReorderBack(float3 euler, math.RotationOrder order)
        {
            switch (order)
            {
                case math.RotationOrder.XZY:
                    return euler.xzy;
                case math.RotationOrder.YZX:
                    return euler.zxy;
                case math.RotationOrder.YXZ:
                    return euler.yxz;
                case math.RotationOrder.ZXY:
                    return euler.yzx;
                case math.RotationOrder.ZYX:
                    return euler.zyx;
                case math.RotationOrder.XYZ:
                default:
                    return euler;
            }
        }

        public static float3 toEuler(quaternion q, math.RotationOrder order = math.RotationOrder.Default)
        {
            const float epsilon = 1e-6f;

            //prepare the data
            var qv = q.value;
            var d1 = qv * qv.wwww * new float4(2.0f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * new float4(2.0f); //xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = new float3(0.0f);

            const float CUTOFF = (1.0f - 2.0f * epsilon) * (1.0f - 2.0f * epsilon);

            switch (order)
            {
                case math.RotationOrder.ZYX:
                {
                    var y1 = d2.z + d1.y;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = -d2.x + d1.z;
                        var x2 = d3.x + d3.w - d3.y - d3.z;
                        var z1 = -d2.y + d1.x;
                        var z2 = d3.z + d3.w - d3.y - d3.x;
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                    }
                    else //zxz
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.z, d1.y, d2.y, d1.x);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), 0.0f);
                    }

                    break;
                }

                case math.RotationOrder.ZXY:
                {
                    var y1 = d2.y - d1.x;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = d2.x + d1.z;
                        var x2 = d3.y + d3.w - d3.x - d3.z;
                        var z1 = d2.z + d1.y;
                        var z2 = d3.z + d3.w - d3.x - d3.y;
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                    }
                    else //zxz
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.z, d1.y, d2.y, d1.x);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
                    }

                    break;
                }

                case math.RotationOrder.YXZ:
                {
                    var y1 = d2.y + d1.x;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = -d2.z + d1.y;
                        var x2 = d3.z + d3.w - d3.x - d3.y;
                        var z1 = -d2.x + d1.z;
                        var z2 = d3.y + d3.w - d3.z - d3.x;
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                    }
                    else //yzy
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.x, d1.z, d2.y, d1.x);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), 0.0f);
                    }

                    break;
                }

                case math.RotationOrder.YZX:
                {
                    var y1 = d2.x - d1.z;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = d2.z + d1.y;
                        var x2 = d3.x + d3.w - d3.z - d3.y;
                        var z1 = d2.y + d1.x;
                        var z2 = d3.y + d3.w - d3.x - d3.z;
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                    }
                    else //yxy
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.x, d1.z, d2.y, d1.x);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
                    }

                    break;
                }

                case math.RotationOrder.XZY:
                {
                    var y1 = d2.x + d1.z;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = -d2.y + d1.x;
                        var x2 = d3.y + d3.w - d3.z - d3.x;
                        var z1 = -d2.z + d1.y;
                        var z2 = d3.x + d3.w - d3.y - d3.z;
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                    }
                    else //xyx
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.x, d1.z, d2.z, d1.y);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), 0.0f);
                    }

                    break;
                }

                case math.RotationOrder.XYZ:
                {
                    var y1 = d2.z - d1.y;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = d2.y + d1.x;
                        var x2 = d3.z + d3.w - d3.y - d3.x;
                        var z1 = d2.x + d1.z;
                        var z2 = d3.x + d3.w - d3.y - d3.z;
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                    }
                    else //xzx
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.z, d1.y, d2.x, d1.z);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
                    }

                    break;
                }
            }

            return eulerReorderBack(euler, order);
        }
    }
}

