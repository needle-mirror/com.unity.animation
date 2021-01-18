using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
    [BurstCompatible]
    public struct AnimationCurveEvaluator
    {
        /// <summary>
        /// Gets the value of the curve at a time.
        /// </summary>
        /// <param name="time">The time in seconds at which to evaluate the curve.</param>
        /// <param name="animCurve">The curve to evaluate.</param>
        /// <returns>The value of the curve at the specified time. If evaluating at a time
        /// before the the left-most keyframe's time, returns its value. If evaluating at a time
        /// greater than the right-most keyframe's time, returns its value.</returns>
        public static float Evaluate(float time, ref AnimationCurve animCurve)
        {
            return Evaluate(time, ref animCurve.CurveBlob.Value, ref animCurve.Cache);
        }

        /// <summary>
        /// Gets the value of the curve at a time.
        /// </summary>
        /// <param name="time">The time in seconds at which to evaluate the curve.</param>
        /// <param name="curveBlob">The reference to a blob asset of the AnimationCurve's blob to evaluate.</param>
        /// <returns>The value of the curve at the specified time. If evaluating at a time
        /// before the the left-most keyframe's time, returns its value. If evaluating at a time
        /// greater than the right-most keyframe's time, returns its value.</returns>
        public static float Evaluate(float time, BlobAssetReference<AnimationCurveBlob> curveBlob)
        {
            var cache = new AnimationCurveCache();
            return Evaluate(time, ref curveBlob.Value, ref cache);
        }

        /// <summary>
        /// Gets the value of the curve at a time.
        /// </summary>
        /// <param name="time">The time in seconds at which to evaluate the curve.</param>
        /// <param name="curveBlob">The curve to evaluate.</param>
        /// <param name="cache">The cache of the last interval that was accessed for evaluation</param>
        /// <returns>The value of the curve at the specified time. If evaluating at a time
        /// before the the left-most keyframe's time, returns its value. If evaluating at a time
        /// greater than the right-most keyframe's time, returns its value.
        /// As long as the curve is evaluated within the cached interval, the cache is reused instead of going
        /// through the curve. If we evaluate outside of the interval, the cache is updated.
        /// </returns>
        public static float Evaluate(float time, ref AnimationCurveBlob curveBlob, ref AnimationCurveCache cache)
        {
            ref var curveTimes = ref curveBlob.KeyframesTime;

            // Wrap time
            time = math.clamp(time, curveTimes[0], curveTimes[curveTimes.Length - 1]);

            FindSurroundingKeyframes(time, ref curveTimes, ref cache, out float leftTime, out float rightTime);

            if (curveBlob.Type == AnimationCurveType.Hermite)
            {
                ref var leftKeyframe = ref curveBlob.GetHermiteKeyframe(cache.LhsIndex);
                ref var rightKeyframe = ref curveBlob.GetHermiteKeyframe(cache.RhsIndex);

                // Handle stepped curve.
                if (math.isinf(leftKeyframe.OutTangent) || math.isinf(rightKeyframe.InTangent))
                {
                    return leftKeyframe.Value;
                }
                return HermiteInterpolate(time, leftTime, ref leftKeyframe, rightTime, ref rightKeyframe);
            }
            else if (curveBlob.Type == AnimationCurveType.Bezier)
            {
                ref var leftKeyframe = ref curveBlob.GetBezierKeyframe(cache.LhsIndex);
                ref var rightKeyframe = ref curveBlob.GetBezierKeyframe(cache.RhsIndex);

                // Handle stepped curve.
                if (math.isinf(leftKeyframe.OutTangent) || math.isinf(rightKeyframe.InTangent))
                {
                    return leftKeyframe.Value;
                }
                // Note: In this case float equality check is fine since we are not checking the result of a computation, but
                // a specific value that was assigned.
                if (leftKeyframe.OutWeight == KeyframeData.DEFAULT_WEIGHT && rightKeyframe.InWeight == KeyframeData.DEFAULT_WEIGHT)
                {
                    return HermiteInterpolate(time, leftTime, ref leftKeyframe, rightTime, ref rightKeyframe);
                }
                return BezierInterpolate(time, leftTime, ref leftKeyframe, rightTime, ref rightKeyframe);
            }
            else
            {
                curveBlob.ThrowInvalidCurveType();
                return 0;
            }
        }

        static void FindSurroundingKeyframes(float time, ref BlobArray<float> timeCurve, ref AnimationCurveCache cache,
            out float leftTime, out float rightTime)
        {
            var lhsTime = timeCurve[cache.LhsIndex];
            var rhsTime = timeCurve[cache.RhsIndex];

            // Check if we are in the cached interval.
            if (cache.LhsIndex != cache.RhsIndex && time >= lhsTime && time <= rhsTime)
            {
                leftTime = lhsTime;
                rightTime = rhsTime;
                return;
            }

            // Fall back to using dichotomic search.
            var length = timeCurve.Length;
            int half;
            int middle;
            int first = 0;

            while (length > 0)
            {
                half = length >> 1;
                middle = first + half;

                if (time < timeCurve[middle])
                {
                    length = half;
                }
                else
                {
                    first = middle + 1;
                    length = length - half - 1;
                }
            }

            // If not within range, we pick the last element twice.
            cache.LhsIndex = first - 1;
            cache.RhsIndex = math.min(timeCurve.Length - 1, first);

            leftTime = timeCurve[cache.LhsIndex];
            rightTime = timeCurve[cache.RhsIndex];
        }

        static float HermiteInterpolate(float time,
            float leftTime, ref KeyframeData leftKeyframe,
            float rightTime, ref KeyframeData rightKeyframe)
        {
            float dx = rightTime - leftTime;
            float m0;
            float m1;
            float t;
            if (dx != 0.0f)
            {
                t = (time - leftTime) / dx;
                m0 = leftKeyframe.OutTangent * dx;
                m1 = rightKeyframe.InTangent * dx;
            }
            else
            {
                t = 0.0f;
                m0 = 0;
                m1 = 0;
            }

            return HermiteInterpolate(t, leftKeyframe.Value, m0, m1, rightKeyframe.Value);
        }

        static float HermiteInterpolate(float t, float p0, float m0, float m1, float p1)
        {
            // Unrolled the equations to avoid precision issue.
            // (2 * t^3 -3 * t^2 +1) * p0 + (t^3 - 2 * t^2 + t) * m0 + (-2 * t^3 + 3 * t^2) * p1 + (t^3 - t^2) * m1

            var a = 2.0f * p0 + m0 - 2.0f * p1 + m1;
            var b = -3.0f * p0 - 2.0f * m0 + 3.0f * p1 - m1;
            var c = m0;
            var d = p0;

            return t * (t * (a * t + b) + c) + d;
        }

        static float BezierInterpolate(float curveT, float leftTime, ref KeyframeData lhs, float rightTime, ref KeyframeData rhs)
        {
            float lhsOutWeight = lhs.OutWeight;
            float rhsInWeight = rhs.InWeight;

            float dx = rightTime - leftTime;
            if (dx == 0.0F)
                return lhs.Value;

            return BezierInterpolate((curveT - leftTime) / dx, lhs.Value, lhs.OutTangent * dx, lhsOutWeight, rhs.Value, rhs.InTangent * dx, rhsInWeight);
        }

        static float FAST_CBRT_POSITIVE(float x)
        {
            return math.exp(math.log(x) / 3.0f);
        }

        static float FAST_CBRT(float x)
        {
            return (((x) < 0) ? -math.exp(math.log(-(x)) / 3.0f) : math.exp(math.log(x) / 3.0f));
        }

        static float BezierExtractU(float t, float w1, float w2)
        {
            float a = 3.0F * w1 - 3.0F * w2 + 1.0F;
            float b = -6.0F * w1 + 3.0F * w2;
            float c = 3.0F * w1;
            float d = -t;

            if (math.abs(a) > 1e-3f)
            {
                float p = -b / (3.0F * a);
                float p2 = p * p;
                float p3 = p2 * p;

                float q = p3 + (b * c - 3.0F * a * d) / (6.0F * a * a);
                float q2 = q * q;

                float r = c / (3.0F * a);
                float rmp2 = r - p2;

                float s = q2 + rmp2 * rmp2 * rmp2;

                if (s < 0.0F)
                {
                    float ssi = math.sqrt(-s);
                    float r_1 = math.sqrt(-s + q2);
                    float phi = math.atan2(ssi, q);

                    float r_3 = FAST_CBRT_POSITIVE(r_1);
                    float phi_3 = phi / 3.0F;

                    // Extract cubic roots.
                    float u1 = 2.0F * r_3 * math.cos(phi_3) + p;
                    float u2 = 2.0F * r_3 * math.cos(phi_3 + 2.0F * (float)math.PI / 3.0f) + p;
                    float u3 = 2.0F * r_3 * math.cos(phi_3 - 2.0F * (float)math.PI / 3.0f) + p;

                    if (u1 >= 0.0F && u1 <= 1.0F)
                        return u1;
                    else if (u2 >= 0.0F && u2 <= 1.0F)
                        return u2;
                    else if (u3 >= 0.0F && u3 <= 1.0F)
                        return u3;

                    // Aiming at solving numerical imprecisions when u is outside [0,1].
                    return (t < 0.5F) ? 0.0F : 1.0F;
                }
                else
                {
                    float ss = math.sqrt(s);
                    float u = FAST_CBRT(q + ss) + FAST_CBRT(q - ss) + p;

                    if (u >= 0.0F && u <= 1.0F)
                        return u;

                    // Aiming at solving numerical imprecisions when u is outside [0,1].
                    return (t < 0.5F) ? 0.0F : 1.0F;
                }
            }

            if (math.abs(b) > 1e-3f)
            {
                float s = c * c - 4.0F * b * d;
                float ss = math.sqrt(s);

                float u1 = (-c - ss) / (2.0F * b);
                float u2 = (-c + ss) / (2.0F * b);

                if (u1 >= 0.0F && u1 <= 1.0F)
                    return u1;
                else if (u2 >= 0.0F && u2 <= 1.0F)
                    return u2;

                // Aiming at solving numerical imprecisions when u is outside [0,1].
                return (t < 0.5F) ? 0.0F : 1.0F;
            }

            if (math.abs(c) > 1e-3f)
            {
                return (-d / c);
            }

            return 0.0F;
        }

        static float BezierInterpolate(float t, float v1, float m1, float w1, float v2, float m2, float w2)
        {
            float u = BezierExtractU(t, w1, 1.0F - w2);
            return BezierInterpolate(u, v1, w1 * m1 + v1, v2 - w2 * m2, v2);
        }

        static float BezierInterpolate(float t, float p0, float p1, float p2, float p3)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            float omt = 1.0F - t;
            float omt2 = omt * omt;
            float omt3 = omt2 * omt;

            return omt3 * p0 + 3.0F * t * omt2 * p1 + 3.0F * t2 * omt * p2 + t3 * p3;
        }
    }
}
