using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation
{
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
            ref var curveTimes = ref animCurve.CurveBlob.Value.KeyframesTime;
            ref var curve = ref animCurve.CurveBlob.Value.KeyframesData;

            // Wrap time
            time = math.clamp(time, curveTimes[0], curveTimes[curve.Length - 1]);

            FindSurroundingKeyframes(time, ref curveTimes, ref animCurve.Cache, out float leftTime, out float rightTime); 
            return HermiteInterpolate(time, leftTime, curve[animCurve.Cache.LhsIndex], rightTime, curve[animCurve.Cache.RhsIndex]);
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
            var curve = new AnimationCurve();
            curve.SetAnimationCurveBlobAssetRef(curveBlob);

            return Evaluate(time, ref curve);
        }

        static void FindSurroundingKeyframes(float time, ref BlobArray<float> timeCurve, ref AnimationCurve.AnimationCurveCache cache, 
            out float leftTime, out float rightTime)
        {
            var lhsTime = timeCurve[cache.LhsIndex];
            var rhsTime = timeCurve[cache.RhsIndex];

            // Check if we are in the cached interval.
            if (cache.LhsIndex!= cache.RhsIndex && time >= lhsTime && time <= rhsTime)
            {
                leftTime = lhsTime;
                rightTime = lhsTime;
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
            float leftTime, in KeyframeData leftKeyframe,
            float rightTime, in KeyframeData rightKeyframe)
        {
            // Handle stepped curve.
            if (math.isinf(leftKeyframe.OutTangent) || math.isinf(rightKeyframe.InTangent))
            {
                return leftKeyframe.Value;
            }

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
    }
}
