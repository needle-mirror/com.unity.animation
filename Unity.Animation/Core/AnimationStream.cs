using System.Runtime.CompilerServices;
using System.Diagnostics;

using UnityEngine.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    unsafe internal struct Ptr<T> where T : unmanaged
    {
        internal T* m_Ptr;
        internal int m_Length;

        public Ptr(T* ptr, int length)
        {
            m_Ptr = ptr;
            m_Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int index)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (m_Ptr == null)
                throw new System.NullReferenceException("Invalid offset pointer");
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, m_Length));
#endif

            return ref UnsafeUtility.AsRef<T>(m_Ptr + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, in T value)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (m_Ptr == null)
                throw new System.NullReferenceException("Invalid offset pointer");
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, m_Length));
#endif

            UnsafeUtility.AsRef<T>(m_Ptr + index) = value;
        }
    }

    /// <summary>
    /// An AnimationStream is a data proxy to a Rig's AnimatedData. It can be used to read from or write to individual rig channels.
    /// </summary>
    [DebuggerTypeProxy(typeof(AnimationStreamDebugView))]
    public struct AnimationStream
    {
        /// <summary>
        /// The Rig used to create this AnimationStream.
        /// </summary>
        public BlobAssetReference<RigDefinition> Rig;

        internal Ptr<float3>      m_LocalTranslationData;
        internal Ptr<float3>      m_LocalScaleData;
        internal Ptr<float>       m_FloatData;
        internal Ptr<int>         m_IntData;
        internal Ptr<quaternion4> m_LocalRotationData;

        /// <summary>
        /// Channel masks keep track of which channels have been modified.
        /// </summary>
        internal UnsafeBitArray   m_ChannelMasks;

        public static AnimationStream Null => default;

        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig == default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetLocalToParentTranslation(int index) => m_LocalTranslationData.Get(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion GetLocalToParentRotation(int index)
        {
            int idx = index & 0x3; // equivalent to % 4
            ref quaternion4 q4 = ref m_LocalRotationData.Get(index >> 2);
            return math.quaternion(q4.x[idx], q4.y[idx], q4.z[idx], q4.w[idx]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetLocalToParentScale(int index) => m_LocalScaleData.Get(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetFloat(int index) => m_FloatData.Get(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetInt(int index) => m_IntData.Get(index);

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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            ValidateIsFinite(translation);
#endif
            m_LocalTranslationData.Set(index, translation);
            m_ChannelMasks.Set(Rig.Value.Bindings.TranslationBindingIndex + index, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentRotation(int index, in quaternion rotation)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            ValidateIsFinite(rotation);
#endif
            int idx = index & 0x3; // equivalent to % 4
            ref quaternion4 q4 = ref m_LocalRotationData.Get(index >> 2);
            q4.x[idx] = rotation.value.x;
            q4.y[idx] = rotation.value.y;
            q4.z[idx] = rotation.value.z;
            q4.w[idx] = rotation.value.w;
            m_ChannelMasks.Set(Rig.Value.Bindings.RotationBindingIndex + index, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentScale(int index, in float3 scale)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            ValidateIsFinite(scale);
#endif
            m_LocalScaleData.Set(index, scale);
            m_ChannelMasks.Set(Rig.Value.Bindings.ScaleBindingIndex + index, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFloat(int index, in float value)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            ValidateIsFinite(value);
#endif
            m_FloatData.Set(index, value);
            m_ChannelMasks.Set(Rig.Value.Bindings.FloatBindingIndex + index, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetInt(int index, in int value)
        {
            m_IntData.Set(index, value);
            m_ChannelMasks.Set(Rig.Value.Bindings.IntBindingIndex + index, true);
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
            ComputeLocalToRootRotationAndRSMatrix(index, out quaternion rootR, out float3x3 rootRS);
            return ExtractScale(rootR, rootRS);
        }

        public void SetLocalToRootScale(int index, in float3 scale)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif

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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            Assert.IsTrue(index > -1);
#endif
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
#if !UNITY_DISABLE_ANIMATION_CHECKS
            Assert.IsTrue(index > -1);
#endif
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

        /// <summary>
        /// Constructs a new AnimationStream given a rig and its AnimatedData.
        /// </summary>
        unsafe internal static AnimationStream Create(BlobAssetReference<RigDefinition> rig, void* ptr)
        {
            ref var bindings = ref rig.Value.Bindings;
            float* floatPtr = (float*)ptr;

            return new AnimationStream()
            {
                Rig = rig,
                m_LocalTranslationData = new Ptr<float3>((float3*)(floatPtr + bindings.TranslationSamplesOffset), bindings.TranslationBindings.Length),
                m_LocalScaleData = new Ptr<float3>((float3*)(floatPtr + bindings.ScaleSamplesOffset), bindings.ScaleBindings.Length),
                m_FloatData = new Ptr<float>((floatPtr + bindings.FloatSamplesOffset), bindings.FloatBindings.Length),
                m_IntData = new Ptr<int>((int*)(floatPtr + bindings.IntSamplesOffset), bindings.IntBindings.Length),
                m_LocalRotationData = new Ptr<quaternion4>((quaternion4*)(floatPtr + bindings.RotationSamplesOffset), bindings.RotationChunkCount),
                m_ChannelMasks = new UnsafeBitArray((void*)(floatPtr + bindings.ChannelMaskOffset), Core.AlignUp(bindings.BindingCount / sizeof(byte) * 8, 8)),
            };
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

            return Create(rig, buffer.GetUnsafeReadOnlyPtr());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static AnimationStream FromDefaultValues(BlobAssetReference<RigDefinition> rig)
        {
            return rig == default ? Null : Create(rig, rig.Value.DefaultValues.GetUnsafePtr());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void* GetUnsafePtr() => (void*)m_LocalTranslationData.m_Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float4* GetDataChunkUnsafePtr() => (float4*)m_LocalTranslationData.m_Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe quaternion4* GetRotationChunkUnsafePtr() => m_LocalRotationData.m_Ptr;

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

        public int RotationChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Value.Bindings.RotationChunkCount;
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

        public int DataChunkCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig.Value.Bindings.DataChunkCount;
        }

#if !UNITY_DISABLE_ANIMATION_CHECKS
        void ValidateIsFinite(in float value)
        {
            if (!math.isfinite(value))
                throw new System.ArithmeticException();
        }

        void ValidateIsFinite(in float3 value)
        {
            if (!math.all(math.isfinite(value)))
                throw new System.ArithmeticException();
        }

        void ValidateIsFinite(in quaternion value)
        {
            if (!math.all(math.isfinite(value.value)))
                throw new System.ArithmeticException();
        }

#endif

        /// <summary>
        /// Clear all channel masks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearChannelMasks() =>
            m_ChannelMasks.Clear();

        /// <summary>
        /// Set all channel masks to specified value
        /// </summary>
        /// <param name="value">Value of masks to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetChannelMasks(bool value) =>
            m_ChannelMasks.SetBits(value);

        /// <summary>
        /// Copy all channel masks from a source AnimationStream.
        /// </summary>
        /// <param name="src">Source AnimationStream.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyChannelMasksFrom(ref AnimationStream src) =>
            m_ChannelMasks.CopyFrom(ref src.m_ChannelMasks);

        /// <summary>
        /// OR all channel masks with other AnimationStream channel masks.
        /// </summary>
        /// <param name="other">Other AnimationStream to OR channel masks with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OrChannelMasks(ref AnimationStream other) =>
            m_ChannelMasks.OrBits64(ref other.m_ChannelMasks);

        /// <summary>
        /// OR all lhs and rhs AnimationStream channel masks and store result.
        /// Channel masks from lhs and rhs are not modified.
        /// </summary>
        /// <param name="lhs">Input AnimationStream</param>
        /// <param name="rhs">Input AnimationStream</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OrChannelMasks(ref AnimationStream lhs, ref AnimationStream rhs) =>
            m_ChannelMasks.OrBits64(ref lhs.m_ChannelMasks, ref rhs.m_ChannelMasks);

        /// <summary>
        /// AND all channel masks with other AnimationStream channel masks.
        /// </summary>
        /// <param name="other">Other AnimationStream to AND channel masks with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AndChannelMasks(ref AnimationStream other) =>
            m_ChannelMasks.AndBits64(ref other.m_ChannelMasks);

        /// <summary>
        /// AND all lhs and rhs AnimationStream channel masks and store result.
        /// Channel masks from lhs and rhs are not modified.
        /// </summary>
        /// <param name="lhs">Input AnimationStream</param>
        /// <param name="rhs">Input AnimationStream</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AndChannelMasks(ref AnimationStream lhs, ref AnimationStream rhs) =>
            m_ChannelMasks.AndBits64(ref lhs.m_ChannelMasks, ref rhs.m_ChannelMasks);

        /// <summary>
        /// Calculate number of set bits.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetChannelMaskBitCount() => m_ChannelMasks.CountBits(0, m_ChannelMasks.Length);

        /// <summary>
        /// Returns true if Any channels bit are set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAnyChannelMasks() => m_ChannelMasks.TestAny(0, m_ChannelMasks.Length);

        /// <summary>
        /// Returns true if All channels bit are set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAllChannelMasks() => m_ChannelMasks.TestAll(0, m_ChannelMasks.Length);

        /// <summary>
        /// Returns true if none of channels bit are set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasNoChannelMasks() => m_ChannelMasks.TestNone(0, m_ChannelMasks.Length);

        /// <summary>
        /// Return translation channel mask.
        /// </summary>
        /// <param name="index">Translation channel index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetTranslationChannelMask(int index)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)TranslationCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, TranslationCount));
#endif
            return m_ChannelMasks.IsSet(Rig.Value.Bindings.TranslationBindingIndex + index);
        }

        /// <summary>
        /// Return rotation channel mask.
        /// </summary>
        /// <param name="index">Rotation channel index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetRotationChannelMask(int index)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)RotationCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, RotationCount));
#endif
            return m_ChannelMasks.IsSet(Rig.Value.Bindings.RotationBindingIndex + index);
        }

        /// <summary>
        /// Return scale channel mask.
        /// </summary>
        /// <param name="index">Scale channel index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetScaleChannelMask(int index)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)ScaleCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, ScaleCount));
#endif
            return m_ChannelMasks.IsSet(Rig.Value.Bindings.ScaleBindingIndex + index);
        }

        /// <summary>
        /// Return float channel mask.
        /// </summary>
        /// <param name="index">Float channel index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetFloatChannelMask(int index)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)FloatCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, FloatCount));
#endif
            return m_ChannelMasks.IsSet(Rig.Value.Bindings.FloatBindingIndex + index);
        }

        /// <summary>
        /// Return int channel mask.
        /// </summary>
        /// <param name="index">Int channel index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetIntChannelMask(int index)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)IntCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, IntCount));
#endif
            return m_ChannelMasks.IsSet(Rig.Value.Bindings.IntBindingIndex + index);
        }
    }

    public static class AnimationStreamUtils
    {
        unsafe public static void MemCpy(ref AnimationStream dst, ref AnimationStream src)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            Assert.IsFalse(dst.IsNull || src.IsNull);

            Assert.AreEqual(dst.TranslationCount, src.TranslationCount);
            Assert.AreEqual(dst.RotationCount, src.RotationCount);
            Assert.AreEqual(dst.ScaleCount, src.ScaleCount);
            Assert.AreEqual(dst.FloatCount, src.FloatCount);
            Assert.AreEqual(dst.IntCount, src.IntCount);
#endif
            UnsafeUtility.MemCpy(dst.GetUnsafePtr(), src.GetUnsafePtr(), Unsafe.SizeOf<AnimatedData>() * src.Rig.Value.Bindings.StreamSize);
        }

        unsafe public static void SetDefaultValues(ref AnimationStream stream)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            Assert.IsFalse(stream.IsNull);
#endif
            ref var rig = ref stream.Rig.Value;
            UnsafeUtility.MemCpy(stream.GetUnsafePtr(), rig.DefaultValues.GetUnsafePtr(), Unsafe.SizeOf<AnimatedData>() * rig.Bindings.ChannelSize);
        }

        unsafe public static void MemClear(ref AnimationStream stream)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            Assert.IsFalse(stream.IsNull);
#endif
            UnsafeUtility.MemClear(stream.GetUnsafePtr(), Unsafe.SizeOf<AnimatedData>() * stream.Rig.Value.Bindings.StreamSize);
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
    }
}
