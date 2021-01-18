using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Animation
{
    /// <summary>
    /// The data associated at a point in time in a bezier curve. If the weights of the tangents are
    /// <see cref="DEFAULT_WEIGHT"/>, then the tangent is equivalent to a hermit curve.
    /// </summary>
    [BurstCompatible]
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

        /// <summary>
        /// The weight of the incoming tangent for this key.
        /// It affects the slope of the curve from the previous key to this one.
        /// If the value is <see cref="DEFAULT_WEIGHT"/>, this has the same effect as a <see cref="HermiteKeyframeData"/>.
        /// </summary>
        public float InWeight;

        /// <summary>
        /// The weight of the outgoing tangent for this key.
        /// It affects the slope of the curve from this key to the next.
        /// If the value is <see cref="DEFAULT_WEIGHT"/>, this has the same effect as a <see cref="HermiteKeyframeData"/>.
        /// </summary>
        public float OutWeight;

        /// <summary>
        /// The default weight of the tangents. Used for tangents without a specified weight.
        /// </summary>
        public const float DEFAULT_WEIGHT = 1.0f / 3.0f;
    }

    /// <summary>
    /// Type of an animation curve. The Hermite type is a subset of the Bezier type, but requires less memory.
    /// </summary>
    public enum AnimationCurveType
    {
        Hermite,
        Bezier,
        // TODO: Maybe we should have a constant curve type where there is only a value (like what is needed for int channels / object properties)
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
    [BurstCompatible]
    public struct AnimationCurveBlob
    {
        /// <summary>
        /// Type of the animation curve. To set this at creation time, call either <see cref="AllocateBezierKeyframes"/> or
        /// <see cref="AllocateHermiteKeyframes"/>.
        /// </summary>
        public AnimationCurveType Type { get; private set; }

        /// <summary>
        /// The array of times at which data has been set.
        /// </summary>
        public BlobArray<float> KeyframesTime;

        /// <summary>
        /// The data associated with the times.
        /// </summary>
        public BlobArray<float> RawKeyframesData;

        static readonly int k_RelativeHermiteKeyframeSize = UnsafeUtility.SizeOf<HermiteKeyframeData>() / UnsafeUtility.SizeOf<float>();
        static readonly int k_RelativeBezierKeyframeSize = UnsafeUtility.SizeOf<KeyframeData>() / UnsafeUtility.SizeOf<float>();

        /// <summary>
        /// Gets the keyframe at the given index, assuming the curve is a hermit curve. It is the callers responsibility
        /// to check that the type of the curve is hermite (<c>curve.Type == AnimationCurveType.Hermite</c>).
        /// </summary>
        /// <param name="index">The index of the keyframe. Must be between <c>0</c> and <c>curve.KeyframesTime.Length</c></param>
        /// <returns>The keyframe at index <c>index</c>. <b>WARNING: Never dereference this keyframe, only access its members.</b></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe ref KeyframeData GetHermiteKeyframe(int index)
        {
            ValidateKeyframeGet(index, AnimationCurveType.Hermite);
            return ref UnsafeUtility.AsRef<KeyframeData>(((HermiteKeyframeData*)RawKeyframesData.GetUnsafePtr()) + index);
        }

        /// <summary>
        /// Gets the keyframe at the given index, assuming the curve is a bezier curve. It is the callers responsibility
        /// to check that the type of the curve is bezier (<c>curve.Type == AnimationCurveType.Bezier</c>).
        /// </summary>
        /// <param name="index">The index of the keyframe. Must be between <c>0</c> and <c>curve.KeyframesTime.Length</c></param>
        /// <returns>The keyframe at index <c>index</c></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe ref KeyframeData GetBezierKeyframe(int index)
        {
            ValidateKeyframeGet(index, AnimationCurveType.Bezier);
            return ref UnsafeUtility.AsRef<KeyframeData>(((KeyframeData*)RawKeyframesData.GetUnsafePtr()) + index);
        }

        /// <summary>
        /// Gets a keyframe from the curve. If the curve was a hermite curve, the keyframe weights get filled with the default
        /// value <see cref="KeyframeData.DEFAULT_WEIGHT"/>
        /// </summary>
        /// <param name="index">The index of the keyframe. Must be between <c>0</c> and <c>curve.KeyframesTime.Length</c></param>
        /// <returns>The keyframe at index <c>index</c></returns>
        public KeyframeData this[int index]
        {
            get
            {
                if (Type == AnimationCurveType.Bezier)
                    return GetBezierKeyframe(index);

                if (Type == AnimationCurveType.Hermite)
                {
                    ref var keyframe = ref GetHermiteKeyframe(index);
                    return new KeyframeData
                    {
                        Value = keyframe.Value,
                        InTangent = keyframe.InTangent,
                        OutTangent = keyframe.OutTangent,
                        InWeight = KeyframeData.DEFAULT_WEIGHT,
                        OutWeight = KeyframeData.DEFAULT_WEIGHT
                    };
                }

                ThrowInvalidCurveType();
                return default;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateKeyframeGet(int index, AnimationCurveType expectedType)
        {
            if (Type != expectedType)
                throw new InvalidOperationException($"Tried to get {expectedType} curve data on a curve where the type is {Type}");
            if (index < 0 || index > KeyframesTime.Length)
                throw new IndexOutOfRangeException($"Index {index} is not in the range of the curve keyframes: [0, {KeyframesTime.Length})");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ThrowInvalidCurveType()
        {
            throw new InvalidOperationException($"Curve has a type that is not valid ({Type}).");
        }

        // TODO: How to make sure this cannot be used on a created animation curve?
        public Builder CreateBuilder(BlobBuilder builder, AnimationCurveType type, int keyframeLength)
        {
            return new Builder(ref this, builder, type, keyframeLength);
        }

        [BurstCompatible]
        struct HermiteKeyframeData
        {
            public float Value;
            public float InTangent;
            public float OutTangent;
        }

        public ref struct Builder
        {
            BlobBuilderArray<float> m_TimeBuilder;
            BlobBuilderArray<float> m_RawKeyframesBuilder;
            AnimationCurveType m_Type;

            /// <summary>
            /// Use <see cref="AnimationCurveBlob.CreateBuilder"/> instead of this constructor.
            /// </summary>
            internal Builder(ref AnimationCurveBlob curve, BlobBuilder builder, AnimationCurveType type, int keyframeCount)
            {
                int relativeKeyframeSize;
                switch (type)
                {
                    case AnimationCurveType.Bezier:
                        relativeKeyframeSize = k_RelativeBezierKeyframeSize;
                        break;
                    case AnimationCurveType.Hermite:
                        relativeKeyframeSize = k_RelativeHermiteKeyframeSize;
                        break;
                    default:
                        throw new ArgumentException($"The curve type {type} is not supported.");
                }

                m_TimeBuilder = builder.Allocate(ref curve.KeyframesTime, keyframeCount);
                m_RawKeyframesBuilder = builder.Allocate(ref curve.RawKeyframesData, keyframeCount * relativeKeyframeSize);
                curve.Type = type;
                m_Type = curve.Type;
            }

            /// <summary>
            /// Sets a keyframe in the animation curve. If the curve type is <see cref="AnimationCurveType.Hermite"/>, only accepts <see cref="KeyframeData.DEFAULT_WEIGHT"/> as weight.
            /// </summary>
            /// <param name="index">The index of the keyframe</param>
            /// <param name="time">The time at which the keyframe occurs</param>
            /// <param name="data">The data of the keyframe</param>
            /// <exception cref="ArgumentException">Will be thrown if the curve type is <see cref="AnimationCurveType.Hermite"/>, and the weights are not <see cref="KeyframeData.DEFAULT_WEIGHT"/></exception>
            /// <exception cref="IndexOutOfRangeException">If the index is out of range of the animation curve</exception>
            public void SetKeyframe(int index, float time, KeyframeData data)
            {
                ValidateIndex(index);
                m_TimeBuilder[index] = time;
                if (m_Type == AnimationCurveType.Hermite)
                {
                    ValidateDefaultWeights(data);
                    var keyframe = new HermiteKeyframeData
                    {
                        Value = data.Value,
                        InTangent = data.InTangent,
                        OutTangent = data.OutTangent,
                    };
                    unsafe
                    {
                        ((HermiteKeyframeData*)m_RawKeyframesBuilder.GetUnsafePtr())[index] = keyframe;
                    }
                }
                else if (m_Type == AnimationCurveType.Bezier)
                {
                    unsafe
                    {
                        ((KeyframeData*)m_RawKeyframesBuilder.GetUnsafePtr())[index] = data;
                    }
                }
            }

            /// <summary>
            /// Sets the parameters of a keyframe.
            /// </summary>
            /// <param name="index">The index of the keyframe</param>
            /// <param name="time">The time at which the keyframe occurs</param>
            /// <param name="value">The value of the keyframe</param>
            /// <param name="inTangent">The slope of the tangent going into the keyframe</param>
            /// <param name="outTangent">The slope of the tangent going out of the keyframe</param>
            /// <exception cref="IndexOutOfRangeException">If the index is out of range of the animation curve</exception>
            public void SetKeyframe(int index, float time, float value, float inTangent, float outTangent)
            {
                SetKeyframe(index, time, new KeyframeData
                {
                    Value = value,
                    InTangent = inTangent,
                    OutTangent = outTangent,
                    InWeight = KeyframeData.DEFAULT_WEIGHT,
                    OutWeight = KeyframeData.DEFAULT_WEIGHT,
                });
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void ValidateIndex(int index)
            {
                if (index < 0 || index > m_TimeBuilder.Length)
                    throw new IndexOutOfRangeException($"Index {index} is not in the range of the curve keyframes: [0, {m_TimeBuilder.Length})");
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void ValidateDefaultWeights(KeyframeData keyframe)
            {
                // Note: In this case comparing floats using equality is fine, we are verifying the result of an assignment, not
                // a computation.
                if (keyframe.OutWeight != KeyframeData.DEFAULT_WEIGHT || keyframe.InWeight != KeyframeData.DEFAULT_WEIGHT)
                {
                    throw new ArgumentException("You are trying to set a keyframe with weights on a hermite curve.");
                }
            }
        }
    }

    /// <summary>
    /// Stores the indices of the last two keyframes that were used for evaluation.
    /// Speeds up the performance when evaluating several times within the same interval.
    /// </summary>
    [BurstCompatible]
    public struct AnimationCurveCache
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
    [BurstCompatible]
    public struct AnimationCurve : IDisposable
    {
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
