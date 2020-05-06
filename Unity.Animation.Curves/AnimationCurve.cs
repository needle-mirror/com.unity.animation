using System;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Unity.Animation
{
    /// <summary>
    /// The data associated at a point in time in a curve.
    /// </summary>
    public struct KeyframeData
    {
        /// <summary>
        /// The value of the curve at this keyframe.
        /// </summary>
        public float Value;

        /// <summary>
        /// The incoming tangent for this key.
        /// It affects the slope of the curve from the previous key to this one.
        /// </summary>
        public float InTangent;

        /// <summary>
        /// The outgoing tangent for this key.
        /// It affects the slope of the curve from this key to the next.
        /// </summary>
        public float OutTangent;
    }

    /// <summary>
    /// The blob representation of an animation curve. It contains the keys defined in
    /// the animation curve, split in two separate blob arrays: one for the times, and one for
    /// the keyframes' data.
    /// </summary>
    /// <remarks>
    /// We split the times and data because we only need to go through the times when we
    /// search for the right interval for evaluation. Once the interval is found, we don't
    /// need the times anymore as we interpolate between the values of the two keyframes.
    ///
    /// The order in the arrays is preserved, so that for any index 'i', the data in
    /// `KeyframesData[i]` is the one set at the time `KeyframesTime[i]`.
    /// </remarks>
    public struct AnimationCurveBlob
    {
        /// <summary>
        /// The array of times at which data has been set.
        /// </summary>
        public BlobArray<float> KeyframesTime;

        /// <summary>
        /// The data associated with the times.
        /// </summary>
        public BlobArray<KeyframeData> KeyframesData;
    }

    /// <summary>
    /// Stores a collection of keyframes that can be evaluated over time.
    /// </summary>
    /// <remarks>
    /// In addition to a BlobAssetReference of an AnimationCurveBlob, the AnimationCurve
    /// keeps an internal cache of the last interval that was accessed for evaluation.
    /// As long as the curve is evaluated within this interval, the cache is reused
    /// instead of going through the curve. If we evaluate outside of the interval, the
    /// cache is updated.
    /// </remarks>
    public struct AnimationCurve : IDisposable
    {
        /// <summary>
        /// Stores the indices of the last two keyframes that were used for evaluation.
        /// Speeds up the performance when evaluating several times within the same interval.
        /// </summary>
        internal struct AnimationCurveCache
        {
            /// <summary>
            /// Index of the key on the left-hand side of the interval.
            /// </summary>
            public int LhsIndex;
            /// <summary>
            /// Index of the key on the right-hand side of the interval.
            /// </summary>
            public int RhsIndex;

            /// <summary>
            /// Resets the indices of the cache to the start of the curve.
            /// </summary>
            public void Reset()
            {
                LhsIndex = RhsIndex = 0;
            }
        }

        internal BlobAssetReference<AnimationCurveBlob> CurveBlob;
        internal AnimationCurveCache Cache;

        /// <summary>
        /// Returns true if the blob asset reference of the curve blob associated with this
        /// animation curve is created.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CurveBlob.IsCreated;
        }

        /// <summary>
        /// Sets the BlobAssetReference of the AnimationCurveBlob used in the AnimationCurve.
        /// This will reset the internal cache of the curve.
        /// </summary>
        /// <param name="curveBlob">
        /// The BlobAssetReference of the AnimationCurveBlob used to evaluate the curve.
        /// </param>
        public void SetAnimationCurveBlobAssetRef(BlobAssetReference<AnimationCurveBlob> curveBlob)
        {
            CurveBlob = curveBlob;
            Cache.Reset();
        }

        /// <summary>
        /// Gets the BlobAssetReference of the AnimationCurveBlob used in the AnimationCurve.
        /// </summary>
        /// <returns> The BlobAssetReference to the curve's AnimationCurveBlob. </returns>
        public BlobAssetReference<AnimationCurveBlob> GetAnimationCurveBlobAssetRef()
        {
            return CurveBlob;
        }

        /// <summary>
        /// Disposes of the BlobAssetReference of the AnimationCurveBlob of the curve.
        /// </summary>
        public void Dispose()
        {
            CurveBlob.Dispose();
        }
    }
}
