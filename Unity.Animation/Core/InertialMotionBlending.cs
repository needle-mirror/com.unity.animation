using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Animation
{
    [BurstCompatible]
    static public partial class mathex
    {
        /// <summary>
        /// Unwind angle (in radians) to be between [-PI, PI] range. Only works if the angle is already in the [-3PI, 3PI] range.
        /// </summary>
        /// <param name="a">angle in radians, must be in the [-3PI, 3PI] range</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float unwind_once(float a)
        {
            if (a < -math.PI)
            {
                a += k_2PI;
            }

            if (a > math.PI)
            {
                a -= k_2PI;
            }

            return a;
        }
    }

    [BurstCompatible]
    static public partial class Core
    {
        internal static void InertializeVector(
            float3 secondLastVector,
            float3 lastVector,
            float3 currentVector,
            float deltaTime,
            float duration,
            out float3 direction,
            out InertialBlendingCoefficients coefficients
        )
        {
            var translation = lastVector - currentVector;
            var x0 = math.length(translation);
            direction = math.normalizesafe(translation, float3.zero);

            var previousTranslation = secondLastVector - currentVector;
            var previousMag = math.dot(previousTranslation, direction);
            var v0 = (x0 - previousMag) / deltaTime;
            coefficients = new InertialBlendingCoefficients(x0, v0, duration);
        }

        internal static void InertializeQuaternion(
            quaternion secondLastQuaternion,
            quaternion lastQuaternion,
            quaternion currentQuaternion,
            float deltaTime,
            float duration,
            out float3 axis,
            out InertialBlendingCoefficients coefficients
        )
        {
            var currentQuaternionInverse = math.inverse(currentQuaternion);
            quaternion rotation = math.normalize(mathex.mul(lastQuaternion, currentQuaternionInverse));
            axis = mathex.axis(rotation);
            float angle = mathex.angle(rotation);

            // Ensure that rotations are the shortest possible
            if (angle > math.PI)
            {
                angle = 2f * math.PI - angle;
                axis = -axis;
            }

            quaternion previousRotation = mathex.mul(secondLastQuaternion, currentQuaternionInverse);
            float previousAngle = mathex.unwind_once(2f * math.atan2(math.dot(previousRotation.value.xyz, axis), previousRotation.value.w));
            var v0 = mathex.unwind_once(angle - previousAngle) / deltaTime;
            coefficients = new InertialBlendingCoefficients(angle, v0, duration);
        }

        internal static void InertializeFloat(
            float secondLastFloat,
            float lastFloat,
            float currentFloat,
            float deltaTime,
            float duration,
            out InertialBlendingCoefficients coefficients
        )
        {
            var x0 = lastFloat - currentFloat;
            var v0 = (lastFloat - secondLastFloat) / deltaTime;
            coefficients = new InertialBlendingCoefficients(x0, v0, duration);
        }

        /// <summary>
        /// Computes coefficients for inertial blending. One coefficient set <see cref="InertialBlendingCoefficients"/>
        /// will be computed for each :
        /// <list type="bullet">
        /// <item>vector3 channel (translation and scale)</item>
        /// <item>quaternion channel (rotation)</item>
        /// <item>float channel</item>
        /// </list>
        /// In order to use these coefficients, you can call the <see cref="InertialBlend"/> method.
        /// </summary>
        /// <param name="currentInput">The current input pose (the pose at transition time), it should be a pose of the destination clip</param>
        /// <param name="lastOutput">The pose that was calculated by the inertial motion blending algorithm the last frame.</param>
        /// <param name="secondLastOutput">The pose that was calculated by the inertial motion blending algorithm the second last frame.</param>
        /// <param name="deltaTime">The time between the last frame and the second last frame. It is entirely possible to approximate it with Time.deltaTime.</param>
        /// <param name="duration">The duration the blend should take, in seconds. Due to the way the algorithm works, the blend might be of a shorter duration, especially if the given duration is long.</param>
        /// <param name="outCoefficients">
        /// An output parameter, must be of length translationCount + rotationCount + scaleCount + floatCount,
        /// the coefficients of the blend will be written to this in the following order: translations, rotations, scales, floats
        /// </param>
        /// <param name="outDirections">
        /// An output parameter, must be of length translationCount + rotationCount + scaleCount.
        /// The directions of the coefficients will be written in the following order: translations, rotations, scales
        /// </param>
        public static void ComputeInertialBlendingCoefficients(
            ref AnimationStream currentInput,
            ref AnimationStream lastOutput,
            ref AnimationStream secondLastOutput,
            float deltaTime,
            float duration,
            NativeArray<InertialBlendingCoefficients> outCoefficients,
            NativeArray<float3> outDirections
        )
        {
            currentInput.ValidateIsNotNull();
            currentInput.ValidateRigEquality(ref lastOutput);
            currentInput.ValidateRigEquality(ref secondLastOutput);

            Core.ValidateBufferLengthsAreEqual(currentInput.TranslationCount + currentInput.RotationCount + currentInput.ScaleCount + currentInput.FloatCount, outCoefficients.Length);
            Core.ValidateBufferLengthsAreEqual(currentInput.TranslationCount + currentInput.RotationCount + currentInput.ScaleCount, outDirections.Length);
            Core.ValidateGreater(deltaTime, 0.0f);
            Core.ValidateGreaterOrEqual(duration, 0f);

            int translationOffset = InertialBlendingCoefficients.GetTranslationsOffset(currentInput.Rig);
            for (int i = 0; i < currentInput.TranslationCount; i++)
            {
                InertializeVector(
                    secondLastOutput.GetLocalToParentTranslation(i),
                    lastOutput.GetLocalToParentTranslation(i),
                    currentInput.GetLocalToParentTranslation(i),
                    deltaTime,
                    duration,
                    out var direction,
                    out var coefficients);

                var idx = i + translationOffset;
                outCoefficients[idx] = coefficients;
                outDirections[idx] = direction;
            }

            int rotationOffset = InertialBlendingCoefficients.GetRotationsOffset(currentInput.Rig);
            for (int i = 0; i < currentInput.RotationCount; i++)
            {
                InertializeQuaternion(
                    secondLastOutput.GetLocalToParentRotation(i),
                    lastOutput.GetLocalToParentRotation(i),
                    currentInput.GetLocalToParentRotation(i),
                    deltaTime,
                    duration,
                    out var axis,
                    out var coefficients);

                var idx = i + rotationOffset;
                outCoefficients[idx] = coefficients;
                outDirections[idx] = axis;
            }

            int scaleOffset = InertialBlendingCoefficients.GetScalesOffset(currentInput.Rig);
            for (int i = 0; i < currentInput.ScaleCount; i++)
            {
                InertializeVector(
                    secondLastOutput.GetLocalToParentScale(i),
                    lastOutput.GetLocalToParentScale(i),
                    currentInput.GetLocalToParentScale(i),
                    deltaTime,
                    duration,
                    out var direction,
                    out var coefficients);

                var idx = i + scaleOffset;
                outCoefficients[idx] = coefficients;
                outDirections[idx] = direction;
            }

            int floatOffset = InertialBlendingCoefficients.GetFloatsOffset(currentInput.Rig);
            for (int i = 0; i < currentInput.FloatCount; i++)
            {
                InertializeFloat(secondLastOutput.GetFloat(i), lastOutput.GetFloat(i), currentInput.GetFloat(i), deltaTime, duration, out var coefficients);
                outCoefficients[i + floatOffset] = coefficients;
            }
        }

        /// <summary>
        /// Calculates the result of a transition using inertial motion blending.
        /// </summary>
        /// <param name="currentInput">The current input pose before the blend is applied</param>
        /// <param name="outputPose">The output pose (blended)</param>
        /// <param name="interpolationFactors">Factors controlling the interpolation. You can use the <see cref="ComputeInertialBlendingCoefficients"/> method to compute these.</param>
        /// <param name="interpolationDirections">Directions controlling the interpolation. You can use the <see cref="ComputeInertialBlendingCoefficients"/> method to compute these.</param>
        /// <param name="duration">The total duration of the blend in seconds. It should correspond to the duration given to the <see cref="ComputeInertialBlendingCoefficients"/> method.</param>
        /// <param name="remainingTime">The remaining time of the blend, in seconds. Could be calculated like this : <c>remainingTime = blendStartTime + duration - Time.elapsedTime</c></param>
        public static void InertialBlend(
            ref AnimationStream currentInput,
            ref AnimationStream outputPose,
            NativeArray<InertialBlendingCoefficients> interpolationFactors,
            NativeArray<float3> interpolationDirections,
            float duration,
            float remainingTime
        )
        {
            currentInput.ValidateIsNotNull();
            currentInput.ValidateRigEquality(ref outputPose);

            Core.ValidateBufferLengthsAreEqual(currentInput.TranslationCount + currentInput.RotationCount + currentInput.ScaleCount + currentInput.FloatCount, interpolationFactors.Length);
            Core.ValidateBufferLengthsAreEqual(currentInput.TranslationCount + currentInput.RotationCount + currentInput.ScaleCount, interpolationDirections.Length);
            Core.ValidateGreaterOrEqual(duration, remainingTime);
            Core.ValidateGreaterOrEqual(remainingTime, 0f);

            var t = duration - remainingTime;
            var timePowers = InertialBlendingCoefficients.ComputeTimePowers(t);

            int translationOffset = InertialBlendingCoefficients.GetTranslationsOffset(currentInput.Rig);
            for (int i = 0; i < currentInput.TranslationCount; i++)
            {
                var idx = i + translationOffset;
                var factors = interpolationFactors[idx];
                var xInterpolated = factors.Interpolate(timePowers, t);
                var correction = interpolationDirections[idx] * xInterpolated;
                outputPose.SetLocalToParentTranslation(i, correction + currentInput.GetLocalToParentTranslation(i));
            }

            int rotationOffset = InertialBlendingCoefficients.GetRotationsOffset(currentInput.Rig);
            for (int i = 0; i < currentInput.RotationCount; i++)
            {
                var idx = i + rotationOffset;
                var factors = interpolationFactors[idx];
                var xInterpolated = factors.Interpolate(timePowers, t);
                var correction = quaternion.AxisAngle(interpolationDirections[idx], xInterpolated);
                outputPose.SetLocalToParentRotation(i, math.mul(correction, currentInput.GetLocalToParentRotation(i)));
            }

            int scaleOffset = InertialBlendingCoefficients.GetScalesOffset(currentInput.Rig);
            for (int i = 0; i < currentInput.ScaleCount; i++)
            {
                var idx = i + scaleOffset;
                var factors = interpolationFactors[idx];
                var xInterpolated = factors.Interpolate(timePowers, t);
                var correction = interpolationDirections[idx] * xInterpolated;
                outputPose.SetLocalToParentScale(i, correction + currentInput.GetLocalToParentScale(i));
            }

            int floatOffset = InertialBlendingCoefficients.GetFloatsOffset(currentInput.Rig);
            for (int i = 0; i < currentInput.FloatCount; i++)
            {
                var idx = i + floatOffset;
                var factors = interpolationFactors[idx];
                var correction = factors.Interpolate(timePowers, t);
                outputPose.SetFloat(i, correction + currentInput.GetFloat(i));
            }
            // Why do we not blend ints at all?
            // Ints make no sense to blend since they are discrete values. So all that we can choose is when to
            // switch from the source to the dest clip. There are 3 possibilities : at the beginning, somewhere in the middle and at the end.
            // The two last ones would require sampling the source clip during the transition, which breaks the performance guaranties
            // of inertial motion blending. All we are left with is to change int channels at the start of the blend.
            for (int i = 0; i < currentInput.IntCount; i++)
            {
                // TODO: Optimize with a memcopy
                outputPose.SetInt(i, currentInput.GetInt(i));
            }
        }
    }
}
