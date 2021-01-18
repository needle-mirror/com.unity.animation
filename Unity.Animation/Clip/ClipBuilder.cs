using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;


namespace Unity.Animation
{
    /// <summary>
    /// Intermediate structure to build a DOTS Clip. Animated curves can be imported from an UnityEngine AnimationClip
    /// or built manually. All animated curve stored inside ClipBuilder are sampled at the same fixed sample rate and have the same
    /// number of samples (as a consequence, they all have the same duration).
    /// </summary>
    [BurstCompatible]
    public struct ClipBuilder : IDisposable
    {
        /// <summary>
        /// Duration of all animation curves stored inside ClipBuilder, in seconds.
        /// </summary>
        public float Duration { get; internal set; }

        /// <summary>
        /// Fixed sample rate of all animation curves stored inside ClipBuilder, in frames/second.
        /// That means all animation curves are made of consecutive discrete samples, which are assumed to be spaced in
        /// time of <code>1 / SampleRate</code> seconds.
        /// </summary>
        public float SampleRate { get; internal set; }

        /// <summary>
        /// Number of frames of all animation curves stored inside ClipBuilder. An animation frame is assumed to
        /// be an atomic animated segment of <code>1 / SampleRate</code> seconds. Therefore, each animation curve
        /// is made of a strictly positive integer number of animation frames.
        ///
        /// As a consequence, <code>Duration</code> is expected to be roughly equal to <code>FrameCount / SampleRate</code>.
        /// </summary>
        public int FrameCount => (int)math.ceil(Duration * SampleRate);

        /// <summary>
        /// Number of samples per animated curve inside the ClipBuilder. Samples are actually the curve keyframes that will
        /// be linearly interpolated to sample the curve at any time. Samples are spaced in time of <code>1 / SampleRate</code> seconds,
        /// the duration of a frame, meaning each frame is surrounded by 2 samples. As a consequence <code>SampleCount</code> is equal
        /// to <code>FrameCount + 1</code>.
        /// </summary>
        public int SampleCount => FrameCount + 1;

        internal float m_LastFrameError => FrameCount - Duration * SampleRate;

        internal NativeHashMap<StringHash, UnsafeList<float3>> m_TranslationCurves;
        internal NativeHashMap<StringHash, UnsafeList<quaternion>> m_QuaternionCurves;
        internal NativeHashMap<StringHash, UnsafeList<float3>> m_ScaleCurves;
        internal NativeHashMap<StringHash, UnsafeList<float>> m_FloatCurves;
        internal NativeHashMap<StringHash, UnsafeList<float>> m_IntCurves;
        internal NativeList<SynchronizationTag> m_SynchronizationTags;

        Allocator m_Allocator;
        bool m_IsDisposed;
        const int k_MapCapacity = 32;

        /// <summary>
        /// Create a ClipBuilder.
        /// </summary>
        /// <param name="duration">Duration of the clip in seconds.</param>
        /// <param name="sampleRate">Sample rate of the clip in frames per second.</param>
        /// <param name="allocator">Allocator policy for the curves inside the ClipBuilder.</param>
        /// <exception cref="InvalidOperationException">Both duration and sampleRate must be greater than 0.</exception>
        public ClipBuilder(float duration, float sampleRate, Allocator allocator)
        {
            Core.ValidateGreater(duration, 0.0f);
            Core.ValidateGreater(sampleRate, 0.0f);

            Duration = duration;
            SampleRate = sampleRate;

            m_TranslationCurves = new NativeHashMap<StringHash, UnsafeList<float3>>(k_MapCapacity, allocator);
            m_QuaternionCurves = new NativeHashMap<StringHash, UnsafeList<quaternion>>(k_MapCapacity, allocator);
            m_ScaleCurves = new NativeHashMap<StringHash, UnsafeList<float3>>(k_MapCapacity, allocator);
            m_FloatCurves = new NativeHashMap<StringHash, UnsafeList<float>>(k_MapCapacity, allocator);
            m_IntCurves = new NativeHashMap<StringHash, UnsafeList<float>>(k_MapCapacity, allocator);
            m_SynchronizationTags = new NativeList<SynchronizationTag>(allocator);

            m_Allocator = allocator;
            m_IsDisposed = false;
        }

        /// <summary>
        /// Returns true if the ClipBuilder is correctly initialized and ready for use, false otherwise.
        /// </summary>
        public bool IsCreated
        {
            get
            {
                return m_TranslationCurves.IsCreated
                    && m_QuaternionCurves.IsCreated
                    && m_ScaleCurves.IsCreated
                    && m_FloatCurves.IsCreated
                    && m_IntCurves.IsCreated
                    && m_SynchronizationTags.IsCreated;
            }
        }

        /// <summary>
        /// Dispose the internal curves and synchronization tags stored inside the ClipBuilder.
        /// Does nothing if the ClipBuilder was already disposed.
        /// </summary>
        public void Dispose()
        {
            if (m_IsDisposed) return;

            DisposeCurveMapAndElements(ref m_TranslationCurves);
            DisposeCurveMapAndElements(ref m_QuaternionCurves);
            DisposeCurveMapAndElements(ref m_ScaleCurves);
            DisposeCurveMapAndElements(ref m_FloatCurves);
            DisposeCurveMapAndElements(ref m_IntCurves);
            m_SynchronizationTags.Dispose();

            m_IsDisposed = true;
        }

        /// <summary>
        /// Add a new translation curve to the ClipBuilder. If a translation curve associated with <paramref name="propertyHash"/> already exists,
        /// it will be overridden by the curve <paramref name="samples"/> given as parameter.
        /// </summary>
        /// <param name="samples">Keyframes of the curve to be added, assumed to be sampled at ClipBuilder's <code>SampleRate</code>. Length
        /// must be equal to ClipBuilder <code>SampleCount</code>, <code>InvalidOperationException</code> exception will be thrown otherwise. Values from
        /// <paramref name="samples"/> will be copied by value inside ClipBuilder, so the array can safely disposed afterward if needed.</param>
        /// <param name="propertyHash"></param>
        public void AddTranslationCurve(NativeArray<float3> samples, StringHash propertyHash)
        {
            AddCurve(samples, propertyHash, ref m_TranslationCurves);
        }

        /// <summary>
        /// Add a new quaternion curve to the ClipBuilder. If a quaternion curve associated with <paramref name="propertyHash"/> already exists,
        /// it will be overridden by the curve <paramref name="samples"/> given as parameter.
        /// </summary>
        /// <param name="samples">Keyframes of the curve to be added, assumed to be sampled at ClipBuilder's <code>SampleRate</code>. Length
        /// must be equal to ClipBuilder <code>SampleCount</code>, <code>InvalidOperationException</code> exception will be thrown otherwise. Values from
        /// <paramref name="samples"/> will be copied by value inside ClipBuilder, so the array can safely disposed afterward if needed.</param>
        /// <param name="propertyHash"></param>
        public void AddQuaternionCurve(NativeArray<quaternion> samples, StringHash propertyHash)
        {
            AddCurve(samples, propertyHash, ref m_QuaternionCurves);
        }

        /// <summary>
        /// Add a new scale curve to the ClipBuilder. If a scale curve associated with <paramref name="propertyHash"/> already exists,
        /// it will be overridden by the curve <paramref name="samples"/> given as parameter.
        /// </summary>
        /// <param name="samples">Keyframes of the curve to be added, assumed to be sampled at ClipBuilder's <code>SampleRate</code>. Length
        /// must be equal to ClipBuilder <code>SampleCount</code>, <code>InvalidOperationException</code> exception will be thrown otherwise. Values from
        /// <paramref name="samples"/> will be copied by value inside ClipBuilder, so the array can safely disposed afterward if needed.</param>
        /// <param name="propertyHash"></param>
        public void AddScaleCurve(NativeArray<float3> samples, StringHash propertyHash)
        {
            AddCurve(samples, propertyHash, ref m_ScaleCurves);
        }

        /// <summary>
        /// Add a new float curve to the ClipBuilder. If a float curve associated with <paramref name="propertyHash"/> already exists,
        /// it will be overridden by the curve <paramref name="samples"/> given as parameter.
        /// </summary>
        /// <param name="samples">Keyframes of the curve to be added, assumed to be sampled at ClipBuilder's <code>SampleRate</code>. Length
        /// must be equal to ClipBuilder <code>SampleCount</code>, <code>InvalidOperationException</code> exception will be thrown otherwise. Values from
        /// <paramref name="samples"/> will be copied by value inside ClipBuilder, so the array can safely disposed afterward if needed.</param>
        /// <param name="propertyHash"></param>
        public void AddFloatCurve(NativeArray<float> samples, StringHash propertyHash)
        {
            AddCurve(samples, propertyHash, ref m_FloatCurves);
        }

        /// <summary>
        /// Add a new int curve to the ClipBuilder. If an int curve associated with <paramref name="propertyHash"/> already exists,
        /// it will be overridden by the curve <paramref name="samples"/> given as parameter.
        /// </summary>
        /// <param name="samples">Keyframes of the curve to be added, assumed to be sampled at ClipBuilder's <code>SampleRate</code>. Length
        /// must be equal to ClipBuilder <code>SampleCount</code>, <code>InvalidOperationException</code> exception will be thrown otherwise. Values from
        /// <paramref name="samples"/> will be copied by value inside ClipBuilder, so the array can safely disposed afterward if needed.</param>
        /// <param name="propertyHash"></param>
        public void AddIntCurve(NativeArray<float> samples, StringHash propertyHash)
        {
            AddCurve(samples, propertyHash, ref m_IntCurves);
        }

        /// <summary>
        /// Dispose and remove translation curve associated with <paramref name="propertyHash"/>.
        /// Does nothing if curve is not found.
        /// </summary>
        /// <param name="propertyHash"></param>
        public void RemoveTranslationCurve(StringHash propertyHash)
        {
            RemoveCurve(propertyHash, ref m_TranslationCurves);
        }

        /// <summary>
        /// Dispose and remove quaternion curve associated with <paramref name="propertyHash"/>.
        /// Does nothing if curve is not found.
        /// </summary>
        /// <param name="propertyHash"></param>
        public void RemoveQuaternionCurve(StringHash propertyHash)
        {
            RemoveCurve(propertyHash, ref m_QuaternionCurves);
        }

        /// <summary>
        /// Dispose and remove scale curve associated with <paramref name="propertyHash"/>.
        /// Does nothing if curve is not found.
        /// </summary>
        /// <param name="propertyHash"></param>
        public void RemoveScaleCurve(StringHash propertyHash)
        {
            RemoveCurve(propertyHash, ref m_ScaleCurves);
        }

        /// <summary>
        /// Dispose and remove float curve associated with <paramref name="propertyHash"/>.
        /// Does nothing if curve is not found.
        /// </summary>
        /// <param name="propertyHash"></param>
        public void RemoveFloatCurve(StringHash propertyHash)
        {
            RemoveCurve(propertyHash, ref m_FloatCurves);
        }

        /// <summary>
        /// Dispose and remove int curve associated with <paramref name="propertyHash"/>.
        /// Does nothing if curve is not found.
        /// </summary>
        /// <param name="propertyHash"></param>
        public void RemoveIntCurve(StringHash propertyHash)
        {
            RemoveCurve(propertyHash, ref m_IntCurves);
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        internal void AddCurve<T>(UnsafeList<T> samples, StringHash propertyHash, ref NativeHashMap<StringHash, UnsafeList<T>> curveMap) where T : unmanaged
        {
            Core.ValidateAreEqual(SampleCount, samples.Length);

            if (curveMap.TryGetValue(propertyHash, out var curve))
            {
                curve.Dispose();
            }

            curveMap[propertyHash] = samples;
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        void AddCurve<T>(NativeArray<T> samples, StringHash propertyHash, ref NativeHashMap<StringHash, UnsafeList<T>> curveMap) where T : unmanaged
        {
            Core.ValidateAreEqual(SampleCount, samples.Length);

            UnsafeList<T> curve;
            if (!curveMap.TryGetValue(propertyHash, out curve))
            {
                curve = new UnsafeList<T>(samples.Length, m_Allocator);
            }
            curve.Length = 0;

            unsafe
            {
                curve.AddRange(samples.GetUnsafeReadOnlyPtr(), samples.Length);
            }

            curveMap[propertyHash] = curve;
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        void RemoveCurve<T>(StringHash propertyHash, ref NativeHashMap<StringHash, UnsafeList<T>> curveMap) where T : unmanaged
        {
            if (curveMap.TryGetValue(propertyHash, out var curve))
            {
                curve.Dispose();
                curveMap.Remove(propertyHash);
            }
        }

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        void DisposeCurveMapAndElements<T>(ref NativeHashMap<StringHash, UnsafeList<T>> curveMap) where T : unmanaged
        {
            foreach (var pair in curveMap)
            {
                pair.Value.Dispose();
            }

            curveMap.Dispose();
        }
    }
}
