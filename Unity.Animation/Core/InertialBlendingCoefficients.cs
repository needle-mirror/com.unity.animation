using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    [BurstCompatible]
    public struct InertialBlendingCoefficients
    {
        internal float4 m_ABCD;
        internal float  m_E;
        internal float  m_F;

        // TODO: Maybe remove, the reason it is here is that Duration = min(GlobalDuration, -5 * F/E)
        // so we could recalculate it on the fly...
        float m_Duration;

        // Used to compute the inertial blending coefficients when acceleration is zero
        // See comments in method for details
        static readonly float4x2 k_CoefficientsAccelerationZero = math.float4x2(
            -3f, -6f,
            8f, 15f,
            -6f, -10f,
            0f, 0f);

        // Used to compute the inertial blending coefficients when acceleration is non zero
        // See comments in method for details
        static readonly float4x2 k_CoefficientsAccelerationNonzero = math.float4x2(
            1f, 4f,
            -4f, -15f,
            6f, 20f,
            -4f, -10f);

        public InertialBlendingCoefficients(float x0, float v0, float duration)
        {
            // TODO: This sign flipping could be optimized for vectors and quaterions
            float sign = math.sign(x0);
            x0 = math.abs(x0);
            v0 *= sign;
            // Why -0f? we need to enforce that velocity is always negative so that later when
            // we calculate maxDuration, a velocity of 0 will result in a maxDuration of
            // -5 * x0 / -0f = +Infinity and NOT : -5 * x0 / 0 = -Infinity
            // so that math.min and the acceleration test work correctly
            v0 = math.select(v0, -0f, v0 >= 0);

            var maxDuration = -5f * x0 / v0;
            m_Duration = math.min(duration, maxDuration);
            m_Duration = math.select(m_Duration, duration, !math.isfinite(m_Duration));


            // The code below is optimized. Here is an explanation starting from the original code,
            // that is very similar to how the inertial motion blending slides do it:
            //
            // Original code:
            // a0 = (-8 * v0 * m_Duration - 20 * x0) / duration2;
            // if (a0 < 0) a0 = 0;
            // A = sign * -(a0 * duration2 + 6f * v0 * m_Duration + 12f * x0) / (2f * duration5);
            // B = sign * (3f * a0 * duration2 + 16f * v0 * m_Duration + 30f * x0) / (2f * duration4);
            // C = sign * -(3f * a0 * duration2 + 12f * v0 * m_Duration + 20f * x0) / (2f * duration3);
            // D = sign * a0 / 2f;
            //
            // The key to understand how this piece of code works is the following: you can substitute
            // a0 in the formulas to compute A, B, C and D, and the formulas then become very symmetrical.
            //
            // Here is the full calculation done for the first coefficient:
            // A = sign * -(a0 * duration2 + 6f * v0 * m_Duration + 12f * x0) / (2f * duration5);
            //   (we substitute a0 assuming it is not being clamped by the if statement)
            // A = sign * -(((-8 * v0 * m_Duration - 20 * x0) / duration2) * duration2 + 6f * v0 * m_Duration + 12f * x0) / (2f * duration5);
            //   (the "/ duration2 * duration2" is simplified)
            // A = sign * -((-8 * v0 * m_Duration - 20 * x0) + 6f * v0 * m_Duration + 12f * x0) / (2f * duration5);
            //   (now we can combine all "v0 * m_Duration" and "x0" terms)
            // A = sign * -(-2 * v0 * m_Duration - 8 * x0) / (2f * duration5);
            //   (we distribute the - factor and the 1/2 factor on both terms)
            // A = sign * (1 * v0 * m_Duration + 4 * x0) / duration5;
            //
            // Now, if a0 is clamped to 0 (in the if in the original code):
            // A = sign * -(a0 * duration2 + 6f * v0 * m_Duration + 12f * x0) / (2f * duration5);
            //   (we substitute the value of a0, "0")
            // A = sign * -(0 * duration2 + 6f * v0 * m_Duration + 12f * x0) / (2f * duration5);
            //   (the whole a0 term becomes null)
            // A = sign * -(6f * v0 * m_Duration + 12f * x0) / (2f * duration5);
            //   (like before, we distribute the -1 and 1/2 factors on both terms)
            // A = sign * (-3f * v0 * m_Duration - 6f * x0) / duration5;
            //
            // And there we have it. If we apply the same process to the A, B, C and D coefficients,
            // we get the following code:
            //
            // Code version 1 :
            // a0 = (-8 * v0 * m_Duration - 20 * x0) / duration2;
            // if (a0 < 0) { // This is the result of clamping acceleration to 0
            //     A = sign * (-3 * v0 * m_Duration +  -6 * x0) / duration5;
            //     B = sign * ( 8 * v0 * m_Duration +  15 * x0) / duration4;
            //     C = sign * (-6 * v0 * m_Duration + -10 * x0) / duration3;
            //     D = sign * ( 0 * v0 * m_Duration +   0 * x0) / duration2;
            // }
            // else { // This is the result of not clamping the acceleration
            //     A = sign * ( 1 * v0 * m_Duration +   4 * x0) / duration5;
            //     B = sign * (-4 * v0 * m_Duration + -15 * x0) / duration4;
            //     C = sign * ( 6 * v0 * m_Duration +  20 * x0) / duration3;
            //     D = sign * (-4 * v0 * m_Duration + -10 * x0) / duration2;
            // }
            //
            // Note: To make the code above more uniform, when acceleration is clamped to 0,
            // we use two coefficients of value 0, instead of directly assigning 0. The end
            // result is still the same, but is makes both branches of the if identical modulo
            // the coefficients.
            //
            // Between branches of the if in code version 1, the only thing that changes are the
            // coefficients. Luckily, we can extract all coefficients from the branches, exploiting
            // matrix multiplication. We can then rewrite the code as follows:
            //
            // float a0 = (-8 * v0 * m_Duration - 20 * x0) / duration2;
            // var coefficients = a0 < 0 ? k_CoefficientsAccelerationZero : k_CoefficientsAccelerationNonzero;
            // float2 numerator = math.float2(v0 * m_Duration, x0);
            // float4 denominator = math.float4(duration5, duration4, duration3, duration2);
            // m_ABCD = sign * math.mul(coefficients, numerator) / denominator;
            //
            // But can we make it faster? Yes. The key is to notice the ternary expression. We calculate
            // the acceleration only for the conditional in the expression. But we can rework the expression
            // in the following way:
            // a0 < 0
            //   (substitute a0 in the expression)
            // (-8 * v0 * m_Duration - 20 * x0) / duration2 < 0
            //   (since duration2 is always positive and non-zero, we can remove it)
            // -8 * v0 * m_Duration - 20 * x0 < 0
            //   (we can move -20 * x0 to the other side of the equation)
            // -8 * v0 * m_Duration < 20 * x0
            //   (we divide both sides by -4 * v0. Since v0 is always negative, we always divide by a positive
            //    number, so we don't need to change the inequality symbol. In addition, when v0 is -0f, the
            //    expression will evaluate correctly (2 * m_Duration < +Infinity))
            // 2 * m_Duration < - 5 * x0 / v0
            //   (now we can replace -5 * x0 / v0 by the maxDuration variable, since it is exactly the same value)
            // 2 * m_Duration < maxDuration
            //
            // Replacing the conditional expression in the ternary gives us the final optimized code, written below.

            var duration2 = m_Duration * m_Duration;
            var duration3 = m_Duration * duration2;
            var duration4 = m_Duration * duration3;
            var duration5 = m_Duration * duration4;

            var coefficients = 2f * m_Duration < maxDuration ? k_CoefficientsAccelerationZero : k_CoefficientsAccelerationNonzero;

            float2 numerator = math.float2(v0 * m_Duration, x0);
            float4 denominator = math.float4(duration5, duration4, duration3, duration2);
            m_ABCD = sign * math.mul(coefficients, numerator) / denominator;

            m_E = sign * v0;
            m_F = sign * x0;

            if (!math.all(math.isfinite(m_ABCD)))
            {
                m_ABCD = float4.zero;
                m_E = 0f;
                m_F = 0f;
                m_Duration = 0f;
            }
        }

        /// <param name="timePowers">Should contain (time^5, time^4, time^3, time^2), use ComputeTimePowers().</param>
        /// <param name="time">Current interpolation time</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Interpolate(float4 timePowers, float time)
        {
            var x = 0.0f;
            if (time < m_Duration)
            {
                x = math.dot(m_ABCD, timePowers) + m_E * time + m_F;
            }
            return x;
        }

        /// <summary>
        /// Compute time powers needed by Interpolate function.
        /// </summary>
        /// <param name="t">time</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 ComputeTimePowers(float t) =>
            math.float4(t * t * t * t * t, t * t * t * t, t * t * t, t * t);

        internal static int GetTranslationsOffset(BlobAssetReference<RigDefinition> rig) => 0;
        internal static int GetRotationsOffset(BlobAssetReference<RigDefinition> rig) =>
            rig.Value.Bindings.TranslationBindings.Length;
        internal static int GetScalesOffset(BlobAssetReference<RigDefinition> rig) =>
            rig.Value.Bindings.RotationBindings.Length + rig.Value.Bindings.RotationBindings.Length;
        internal static int GetFloatsOffset(BlobAssetReference<RigDefinition> rig) =>
            rig.Value.Bindings.ScaleBindings.Length + rig.Value.Bindings.RotationBindings.Length + rig.Value.Bindings.RotationBindings.Length;
    }
}
