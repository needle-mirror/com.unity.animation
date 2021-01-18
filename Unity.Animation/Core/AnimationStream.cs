using System.Runtime.CompilerServices;
using System.Diagnostics;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
    unsafe internal struct Ptr<T> where T : unmanaged
    {
        internal T* m_Ptr;

        public Ptr(T* ptr)
        {
            m_Ptr = ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int index)
        {
            ValidateIsNotNull();
            return ref UnsafeUtility.AsRef<T>(m_Ptr + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, in T value)
        {
            ValidateIsNotNull();
            UnsafeUtility.AsRef<T>(m_Ptr + index) = value;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsNotNull()
        {
            if (m_Ptr == null)
                throw new System.NullReferenceException("AnimationStream: null pointer.");
        }
    }

    /// <summary>
    /// An AnimationStream is a data proxy to a Rig's AnimatedData. It can be used to read from or write to individual rig channels.
    /// </summary>
    [DebuggerTypeProxy(typeof(AnimationStreamDebugView))]
    [BurstCompatible]
    public partial struct AnimationStream
    {
        /// <summary>
        /// The Rig used to create this AnimationStream.
        /// </summary>
        public BlobAssetReference<RigDefinition> Rig;

        Ptr<float3>      m_LocalTranslationData;
        Ptr<float3>      m_LocalScaleData;
        Ptr<float>       m_FloatData;
        Ptr<int>         m_IntData;
        Ptr<quaternion4> m_LocalRotationData;

        /// <summary>
        /// Channel Pass masks keep track of which channels have been modified in a pass (ProcessDefaultAnimationGraph vs ProcessLateAnimationGraph).
        /// </summary>
        internal UnsafeBitArray   m_ChannelPassMasks;

        /// <summary>
        /// Channel Frame masks keep track of which channels have been modified in a frame.
        /// </summary>
        internal UnsafeBitArray   m_ChannelFrameMasks;

        int m_IsReadOnly;

        public static readonly AnimationStream Null = default;

        internal bool IsReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_IsReadOnly != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_IsReadOnly = value ? 1 : 0;
        }

        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig == default;
        }

        /// <summary>
        /// Constructs a new AnimationStream given a rig and its AnimatedData.
        /// </summary>
        unsafe public static AnimationStream Create(BlobAssetReference<RigDefinition> rig, NativeArray<AnimatedData> buffer)
        {
            if (rig == default || buffer.Length == 0 || buffer.Length != rig.Value.Bindings.StreamSize)
                return Null;

            return Create(rig, buffer.GetUnsafePtr());
        }

        unsafe public static AnimationStream CreateReadOnly(BlobAssetReference<RigDefinition> rig, NativeArray<AnimatedData> buffer)
        {
            if (rig == default || buffer.Length == 0 || buffer.Length != rig.Value.Bindings.StreamSize)
                return Null;

            return Create(rig, buffer.GetUnsafeReadOnlyPtr(), true);
        }

        /// <summary>
        /// Constructs a new AnimationStream given a rig and its AnimatedData.
        /// </summary>
        unsafe internal static AnimationStream Create(BlobAssetReference<RigDefinition> rig, void* ptr, bool isReadOnly = false)
        {
            ref var bindings = ref rig.Value.Bindings;
            float* floatPtr = (float*)ptr;

            var maskSizeInBytes = Core.AlignUp(bindings.BindingCount, 64) / 8;
            var maskSizeIn4Bytes = Core.AlignUp(bindings.BindingCount, 64) / 32;

            return new AnimationStream()
            {
                Rig = rig,
                m_LocalTranslationData = new Ptr<float3>((float3*)(floatPtr + bindings.TranslationSamplesOffset)),
                m_LocalScaleData = new Ptr<float3>((float3*)(floatPtr + bindings.ScaleSamplesOffset)),
                m_FloatData = new Ptr<float>((floatPtr + bindings.FloatSamplesOffset)),
                m_IntData = new Ptr<int>((int*)(floatPtr + bindings.IntSamplesOffset)),
                m_LocalRotationData = new Ptr<quaternion4>((quaternion4*)(floatPtr + bindings.RotationSamplesOffset)),
                m_ChannelPassMasks = new UnsafeBitArray((void*)(floatPtr + bindings.ChannelMaskOffset), maskSizeInBytes),
                m_ChannelFrameMasks = new UnsafeBitArray((void*)(floatPtr + bindings.ChannelMaskOffset + maskSizeIn4Bytes), maskSizeInBytes),
                IsReadOnly = isReadOnly
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetLocalToParentTranslation(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForTranslation(index);

            return m_LocalTranslationData.Get(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion GetLocalToParentRotation(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForRotation(index);

            int idx = index & 0x3; // equivalent to % 4
            ref quaternion4 q4 = ref m_LocalRotationData.Get(index >> 2);
            return math.quaternion(q4.x[idx], q4.y[idx], q4.z[idx], q4.w[idx]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetLocalToParentScale(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForScale(index);

            return m_LocalScaleData.Get(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetFloat(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForFloat(index);

            return m_FloatData.Get(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetInt(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForInt(index);

            return m_IntData.Get(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform GetLocalToParentMatrix(int index) =>
            mathex.AffineTransform(GetLocalToParentTranslation(index), GetLocalToParentRotation(index), GetLocalToParentScale(index));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AffineTransform GetLocalToParentInverseMatrix(int index) =>
            mathex.inverse(GetLocalToParentTRS(index));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetLocalToParentTR(int index, out float3 translation, out quaternion rotation)
        {
            translation = GetLocalToParentTranslation(index);
            rotation = GetLocalToParentRotation(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetLocalToParentTRS(int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            translation = GetLocalToParentTranslation(index);
            rotation = GetLocalToParentRotation(index);
            scale = GetLocalToParentScale(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TRS GetLocalToParentTRS(int index) =>
            new TRS(GetLocalToParentTranslation(index), GetLocalToParentRotation(index), GetLocalToParentScale(index));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentTranslation(int index, in float3 translation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(translation);
            ValidateIndexBoundsForTranslation(index);

            m_LocalTranslationData.Set(index, translation);
            m_ChannelPassMasks.Set(Rig.Value.Bindings.TranslationBindingIndex + index, true);
            m_ChannelFrameMasks.Set(Rig.Value.Bindings.TranslationBindingIndex + index, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentRotation(int index, in quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(rotation);
            ValidateIndexBoundsForRotation(index);

            int idx = index & 0x3; // equivalent to % 4
            ref quaternion4 q4 = ref m_LocalRotationData.Get(index >> 2);
            q4.x[idx] = rotation.value.x;
            q4.y[idx] = rotation.value.y;
            q4.z[idx] = rotation.value.z;
            q4.w[idx] = rotation.value.w;
            m_ChannelPassMasks.Set(Rig.Value.Bindings.RotationBindingIndex + index, true);
            m_ChannelFrameMasks.Set(Rig.Value.Bindings.RotationBindingIndex + index, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentScale(int index, in float3 scale)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(scale);
            ValidateIndexBoundsForScale(index);

            m_LocalScaleData.Set(index, scale);
            m_ChannelPassMasks.Set(Rig.Value.Bindings.ScaleBindingIndex + index, true);
            m_ChannelFrameMasks.Set(Rig.Value.Bindings.ScaleBindingIndex + index, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFloat(int index, in float value)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIsFinite(value);
            ValidateIndexBoundsForFloat(index);

            m_FloatData.Set(index, value);
            m_ChannelPassMasks.Set(Rig.Value.Bindings.FloatBindingIndex + index, true);
            m_ChannelFrameMasks.Set(Rig.Value.Bindings.FloatBindingIndex + index, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetInt(int index, in int value)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateIndexBoundsForInt(index);

            m_IntData.Set(index, value);
            m_ChannelPassMasks.Set(Rig.Value.Bindings.IntBindingIndex + index, true);
            m_ChannelFrameMasks.Set(Rig.Value.Bindings.IntBindingIndex + index, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentTR(int index, in float3 translation, in quaternion rotation)
        {
            SetLocalToParentTranslation(index, translation);
            SetLocalToParentRotation(index, rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentTRS(int index, in float3 translation, in quaternion rotation, in float3 scale)
        {
            SetLocalToParentTranslation(index, translation);
            SetLocalToParentRotation(index, rotation);
            SetLocalToParentScale(index, scale);
        }

        public float3 GetLocalToRootTranslation(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            float3 rootT = GetLocalToParentTranslation(index);

            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                rootT = mathex.mul(GetLocalToParentTRS(parentIdx), rootT);
                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }

            return rootT;
        }

        public void SetLocalToRootTranslation(int index, in float3 translation)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            float3 localT = translation;
            if (index > 0)
            {
                AffineTransform parentRootInvMat = GetLocalToRootInverseMatrix(Rig.Value.Skeleton.ParentIndexes[index]);
                localT = mathex.mul(parentRootInvMat, localT);
            }

            SetLocalToParentTranslation(index, localT);
        }

        public quaternion GetLocalToRootRotation(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            quaternion rootR = GetLocalToParentRotation(index);

            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                rootR = mathex.scaleMulQuat(GetLocalToParentScale(parentIdx), rootR);
                rootR = mathex.mul(GetLocalToParentRotation(parentIdx), rootR);
                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }

            return rootR;
        }

        public void SetLocalToRootRotation(int index, in quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            quaternion localR = rotation;
            if (index > 0)
            {
                var parentRootInvR = math.conjugate(GetLocalToRootRotation(Rig.Value.Skeleton.ParentIndexes[index]));
                localR = mathex.mul(parentRootInvR, localR);
            }

            SetLocalToParentRotation(index, localR);
        }

        public float3 GetLocalToRootScale(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            ComputeLocalToRootRotationAndRSMatrix(index, out quaternion rootR, out float3x3 rootRS);
            return ExtractScale(rootR, rootRS);
        }

        public void SetLocalToRootScale(int index, in float3 scale)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            float3 localS = scale;
            if (index > 0)
            {
                ComputeLocalToRootRotationAndRSMatrix(Rig.Value.Skeleton.ParentIndexes[index], out quaternion parentRootR, out float3x3 parentRootRS);

                quaternion localR = GetLocalToParentRotation(index);
                float3x3 localRMat = math.float3x3(localR);
                float3x3 invRootRMat = math.float3x3(math.conjugate(mathex.mul(parentRootR, localR)));

                float3x3 parentRootS = math.mul(invRootRMat, math.mul(parentRootRS, localRMat));
                localS = mathex.rcpsafe(math.float3(parentRootS.c0.x, parentRootS.c1.y, parentRootS.c2.z)) * localS;
            }

            SetLocalToParentScale(index, localS);
        }

        public void GetLocalToRootTR(int index, out float3 translation, out quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            translation = GetLocalToParentTranslation(index);
            rotation = GetLocalToParentRotation(index);

            TRS parentTRS;
            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                parentTRS = GetLocalToParentTRS(parentIdx);
                translation = mathex.mul(parentTRS, translation);
                rotation = mathex.scaleMulQuat(parentTRS.s, rotation);
                rotation = mathex.mul(parentTRS.r, rotation);
                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }
        }

        public void SetLocalToRootTR(int index, in float3 translation, in quaternion rotation)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            float3 localT = translation;
            quaternion localR = rotation;
            if (index > 0)
            {
                ComputeLocalToRootInverseRotationAndMatrix(Rig.Value.Skeleton.ParentIndexes[index], out quaternion parentRootInvR, out AffineTransform parentRootInvTx);
                localT = mathex.mul(parentRootInvTx, localT);
                localR = mathex.mul(parentRootInvR, localR);
            }

            SetLocalToParentTR(index, localT, localR);
        }

        public void GetLocalToRootTRS(int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            TRS parentTRS = GetLocalToParentTRS(index);
            translation = parentTRS.t;
            rotation = parentTRS.r;

            float3x3 rootRS = math.float3x3(parentTRS.r);
            rootRS = mathex.mulScale(rootRS, parentTRS.s);

            float3x3 parentRS;
            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                parentTRS = GetLocalToParentTRS(parentIdx);
                parentRS = math.float3x3(parentTRS.r);
                parentRS = mathex.mulScale(parentRS, parentTRS.s);
                rootRS = math.mul(parentRS, rootRS);

                translation = mathex.mul(parentTRS, translation);
                rotation = mathex.scaleMulQuat(parentTRS.s, rotation);
                rotation = mathex.mul(parentTRS.r, rotation);

                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }

            scale = ExtractScale(rotation, rootRS);
        }

        public void SetLocalToRootTRS(int index, in float3 translation, in quaternion rotation, in float3 scale)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            float3 localT = translation;
            quaternion localR = rotation;
            float3 localS = scale;

            if (index > 0)
            {
                ComputeLocalToRootInverseRotationAndMatrix(Rig.Value.Skeleton.ParentIndexes[index], out quaternion parentRootInvR, out AffineTransform parentRootInvTx);
                localT = mathex.mul(parentRootInvTx, localT);
                localR = mathex.mul(parentRootInvR, localR);

                float3x3 invRootR = math.float3x3(math.conjugate(rotation));
                float3x3 localRMat = math.float3x3(localR);
                float3x3 parentRootS = math.mul(invRootR, math.mul(mathex.inverse(parentRootInvTx.rs), localRMat));
                localS = mathex.rcpsafe(math.float3(parentRootS.c0.x, parentRootS.c1.y, parentRootS.c2.z)) * localS;
            }

            SetLocalToParentTRS(index, localT, localR, localS);
        }

        public AffineTransform GetLocalToRootMatrix(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            AffineTransform rootTx = GetLocalToParentMatrix(index);

            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                rootTx = mathex.mul(GetLocalToParentMatrix(parentIdx), rootTx);
                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }

            return rootTx;
        }

        public AffineTransform GetLocalToRootInverseMatrix(int index)
        {
            ValidateIsNotNull();
            ValidateIndexBoundsForSkeleton(index);

            AffineTransform rootInvTx = GetLocalToParentInverseMatrix(index);

            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                rootInvTx = mathex.mul(rootInvTx, GetLocalToParentInverseMatrix(parentIdx));
                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }

            return rootInvTx;
        }

        internal void ComputeLocalToRootInverseRotationAndMatrix(int index, out quaternion invRootR, out AffineTransform invRootTx)
        {
            quaternion rootR = GetLocalToParentRotation(index);
            invRootTx = GetLocalToParentInverseMatrix(index);

            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                invRootTx = mathex.mul(invRootTx, GetLocalToParentInverseMatrix(parentIdx));

                rootR = mathex.scaleMulQuat(GetLocalToParentScale(parentIdx), rootR);
                rootR = mathex.mul(GetLocalToParentRotation(parentIdx), rootR);

                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }

            invRootR = math.conjugate(rootR);
        }

        internal void ComputeLocalToRootRotationAndRSMatrix(int index, out quaternion rootR, out float3x3 rootRS)
        {
            rootR  = GetLocalToParentRotation(index);
            rootRS = math.float3x3(rootR);
            rootRS = mathex.mulScale(rootRS, GetLocalToParentScale(index));

            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            quaternion parentR;
            float3 parentS;
            float3x3 parentRS;
            while (parentIdx >= 0)
            {
                parentR = GetLocalToParentRotation(parentIdx);
                parentS = GetLocalToParentScale(parentIdx);

                parentRS = math.float3x3(parentR);
                parentRS = mathex.mulScale(parentRS, parentS);
                rootRS = math.mul(parentRS, rootRS);

                rootR = mathex.scaleMulQuat(parentS, rootR);
                rootR = mathex.mul(parentR, rootR);

                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal float3 ExtractScale(in quaternion r, in float3x3 rs)
        {
            float3x3 scaleMat = math.mul(math.float3x3(math.conjugate(r)), rs);
            return math.float3(scaleMat.c0.x, scaleMat.c1.y, scaleMat.c2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static AnimationStream FromDefaultValues(BlobAssetReference<RigDefinition> rig) =>
            rig == default ? Null : Create(rig, rig.Value.DefaultValues.GetUnsafePtr(), true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void* GetUnsafePtr() => (void*)m_LocalTranslationData.m_Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float4* GetInterpolatedDataChunkUnsafePtr() => (float4*)m_LocalTranslationData.m_Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe int4* GetDiscreteDataChunkUnsafePtr() => (int4*)m_IntData.m_Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe quaternion4* GetRotationDataChunkUnsafePtr() => m_LocalRotationData.m_Ptr;

        public int TranslationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Value.Bindings.TranslationBindings.Length;
        }

        public int RotationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Value.Bindings.RotationBindings.Length;
        }

        public int RotationDataChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Value.Bindings.RotationDataChunkCount;
        }

        public int ScaleCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Value.Bindings.ScaleBindings.Length;
        }

        public int FloatCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Value.Bindings.FloatBindings.Length;
        }

        public int IntCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Value.Bindings.IntBindings.Length;
        }

        public int DiscreteDataChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Value.Bindings.DiscreteDataChunkCount;
        }

        public int InterpolatedDataChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Value.Bindings.InterpolatedDataChunkCount;
        }

        /// <summary>
        /// Pass channel masks, always cleared at the beginning of the frame in <see cref="InitializeAnimation"/> an in <see cref="AnimationGraphSystemBase"/> update( <see cref="ProcessDefaultAnimationGraph"/> and <see cref="ProcessLateAnimationGraph"/>).
        /// </summary>
        public ChannelMask PassMask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { ValidateIsNotNull();  return new ChannelMask(m_ChannelPassMasks.Ptr, Rig, IsReadOnly); } }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { unsafe { ValidateIsNotNull(); ValidateIsWritable(); m_ChannelPassMasks.CopyFrom(ref value.m_Masks); } }
        }

        /// <summary>
        /// Frame channel masks, always cleared at the beginning of the frame in <see cref="InitializeAnimation"/>.
        /// </summary>
        public ChannelMask FrameMask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { ValidateIsNotNull(); return new ChannelMask(m_ChannelFrameMasks.Ptr, Rig, IsReadOnly); } }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { unsafe { ValidateIsNotNull(); ValidateIsWritable(); m_ChannelFrameMasks.CopyFrom(ref value.m_Masks); } }
        }

        /// <summary>
        /// Clear all channel masks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearMasks()
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            m_ChannelPassMasks.Clear();
            m_ChannelFrameMasks.Clear();
        }

        /// <summary>
        /// Set all channel masks to specified value
        /// </summary>
        /// <param name="value">Value of masks to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMasks(bool value)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            m_ChannelPassMasks.SetBits(value);
            m_ChannelFrameMasks.SetBits(value);
        }

        /// <summary>
        /// Copy all channel masks from a source AnimationStream.
        /// </summary>
        /// <param name="src">Source AnimationStream.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyMasksFrom(ref AnimationStream src)
        {
            src.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(ref src);
            m_ChannelPassMasks.CopyFrom(ref src.m_ChannelPassMasks);
            m_ChannelFrameMasks.CopyFrom(ref src.m_ChannelFrameMasks);
        }

        /// <summary>
        /// OR all channel masks with other AnimationStream channel masks.
        /// </summary>
        /// <param name="other">Other AnimationStream to OR channel masks with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OrMasks(ref AnimationStream other)
        {
            other.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(ref other);
            m_ChannelPassMasks.OrBits64(ref other.m_ChannelPassMasks);
            m_ChannelFrameMasks.OrBits64(ref other.m_ChannelFrameMasks);
        }

        /// <summary>
        /// OR all lhs and rhs AnimationStream channel masks and store result.
        /// Channel masks from lhs and rhs are not modified.
        /// </summary>
        /// <param name="lhs">Input AnimationStream</param>
        /// <param name="rhs">Input AnimationStream</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OrMasks(ref AnimationStream lhs, ref AnimationStream rhs)
        {
            lhs.ValidateIsNotNull();
            rhs.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(ref lhs);
            ValidateCompatibility(ref rhs);
            m_ChannelPassMasks.OrBits64(ref lhs.m_ChannelPassMasks, ref rhs.m_ChannelPassMasks);
            m_ChannelFrameMasks.OrBits64(ref lhs.m_ChannelFrameMasks, ref rhs.m_ChannelFrameMasks);
        }

        /// <summary>
        /// AND all channel masks with other AnimationStream channel masks.
        /// </summary>
        /// <param name="other">Other AnimationStream to AND channel masks with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AndMasks(ref AnimationStream other)
        {
            other.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(ref other);
            m_ChannelPassMasks.AndBits64(ref other.m_ChannelPassMasks);
            m_ChannelFrameMasks.AndBits64(ref other.m_ChannelFrameMasks);
        }

        /// <summary>
        /// AND all lhs and rhs AnimationStream channel masks and store result.
        /// Channel masks from lhs and rhs are not modified.
        /// </summary>
        /// <param name="lhs">Input AnimationStream</param>
        /// <param name="rhs">Input AnimationStream</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AndMasks(ref AnimationStream lhs, ref AnimationStream rhs)
        {
            lhs.ValidateIsNotNull();
            rhs.ValidateIsNotNull();
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(ref lhs);
            ValidateCompatibility(ref rhs);
            m_ChannelPassMasks.AndBits64(ref lhs.m_ChannelPassMasks, ref rhs.m_ChannelPassMasks);
            m_ChannelFrameMasks.AndBits64(ref lhs.m_ChannelFrameMasks, ref rhs.m_ChannelFrameMasks);
        }

        /// <summary>
        /// Copies all the data from the specified AnimationStream into this AnimationStream. This includes all the animation channel data and channel masks.
        /// </summary>
        public unsafe void CopyFrom(ref AnimationStream other)
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ValidateCompatibility(ref other);
            UnsafeUtility.MemCpy(GetUnsafePtr(), other.GetUnsafePtr(), UnsafeUtility.SizeOf<AnimatedData>() * other.Rig.Value.Bindings.StreamSize);
        }

        /// <summary>
        /// Reset the AnimationStream to 0. This includes all animation channel data and channel masks.
        /// </summary>
        public unsafe void ResetToZero()
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            UnsafeUtility.MemClear(GetUnsafePtr(), UnsafeUtility.SizeOf<AnimatedData>() * Rig.Value.Bindings.StreamSize);
        }

        /// <summary>
        /// Reset the AnimationStream to default values of the Rig. This only includes the animation channel data. Channel masks are not changed by this operation.
        /// </summary>
        public unsafe void ResetToDefaultValues()
        {
            ValidateIsNotNull();
            ValidateIsWritable();
            ref var rig = ref Rig.Value;
            UnsafeUtility.MemCpy(GetUnsafePtr(), rig.DefaultValues.GetUnsafePtr(), UnsafeUtility.SizeOf<AnimatedData>() * rig.Bindings.ChannelSize);
        }
    }

    sealed class AnimationStreamDebugView
    {
        AnimationStream m_Stream;

        public AnimationStreamDebugView(AnimationStream stream)
        {
            m_Stream = stream;
        }

        public int TranslationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.TranslationCount;
        }

        public float3[] Translations
        {
            get
            {
                float3[] values = new float3[m_Stream.TranslationCount];
                for (int i = 0; i < m_Stream.TranslationCount; i++)
                    values[i] = m_Stream.GetLocalToParentTranslation(i);

                return values;
            }
        }

        public int RotationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.RotationCount;
        }

        public quaternion[] Rotations
        {
            get
            {
                quaternion[] values = new quaternion[m_Stream.RotationCount];
                for (int i = 0; i < m_Stream.RotationCount; i++)
                    values[i] = m_Stream.GetLocalToParentRotation(i);

                return values;
            }
        }

        public int ScaleCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.ScaleCount;
        }

        public float3[] Scales
        {
            get
            {
                float3[] values = new float3[m_Stream.ScaleCount];
                for (int i = 0; i < m_Stream.ScaleCount; i++)
                    values[i] = m_Stream.GetLocalToParentScale(i);

                return values;
            }
        }

        public int FloatCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.FloatCount;
        }

        public float[] Floats
        {
            get
            {
                float[] values = new float[m_Stream.FloatCount];
                for (int i = 0; i < m_Stream.FloatCount; i++)
                    values[i] = m_Stream.GetFloat(i);

                return values;
            }
        }

        public int IntCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.IntCount;
        }

        public int[] Ints
        {
            get
            {
                int[] values = new int[m_Stream.IntCount];
                for (int i = 0; i < m_Stream.IntCount; i++)
                    values[i] = m_Stream.GetInt(i);

                return values;
            }
        }

        public UnsafeBitArray ChannelPassMask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.m_ChannelPassMasks;
        }

        public UnsafeBitArray ChannelFrameMask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.m_ChannelFrameMasks;
        }
    }
}
