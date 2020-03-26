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

            return ref UnsafeUtilityEx.AsRef<T>(m_Ptr + index);
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

            UnsafeUtilityEx.AsRef<T>(m_Ptr + index) = value;
        }
    }

    [DebuggerTypeProxy(typeof(AnimationStreamDebugView))]
    public struct AnimationStream
    {
        public BlobAssetReference<RigDefinition> Rig;

        internal Ptr<float3>      m_LocalTranslationData;
        internal Ptr<float3>      m_LocalScaleData;
        internal Ptr<float>       m_FloatData;
        internal Ptr<int>         m_IntData;
        internal Ptr<quaternion4> m_LocalRotationData;

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
        public float4x4 GetLocalToParentMatrix(int index) => float4x4.TRS(GetLocalToParentTranslation(index), GetLocalToParentRotation(index), GetLocalToParentScale(index));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetLocalToParentTRS(int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            translation = GetLocalToParentTranslation(index);
            rotation = GetLocalToParentRotation(index);
            scale = GetLocalToParentScale(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentTranslation(int index, in float3 translation)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            ValidateIsFinite(translation);
#endif
            m_LocalTranslationData.Set(index, translation);
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentScale(int index, in float3 scale)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            ValidateIsFinite(scale);
#endif
            m_LocalScaleData.Set(index, scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFloat(int index, in float value)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            ValidateIsFinite(value);
#endif
            m_FloatData.Set(index, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetInt(int index, in int value) => m_IntData.Set(index, value);

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
            float3 rigT = GetLocalToParentTranslation(index);

            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                rigT = math.transform(GetLocalToParentMatrix(parentIdx), rigT);
                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }

            return rigT;
        }

        public void SetLocalToRootTranslation(int index, in float3 translation)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
            float3 tmp = translation;
            if (index > 0)
            {
                tmp = math.transform(
                    math.inverse(GetLocalToRootMatrix(Rig.Value.Skeleton.ParentIndexes[index])),
                    tmp
                    );
            }

            SetLocalToParentTranslation(index, tmp);
        }

        public quaternion GetLocalToRootRotation(int index)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
            quaternion rigR = GetLocalToParentRotation(index);

            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                rigR = mathex.scaleMulQuat(GetLocalToParentScale(parentIdx), rigR);
                rigR = mathex.mul(GetLocalToParentRotation(parentIdx), rigR);
                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }

            return rigR;
        }

        internal void InverseTransformRotationRecursive(int index, ref quaternion rotation)
        {
            if (index > 0)
                InverseTransformRotationRecursive(Rig.Value.Skeleton.ParentIndexes[index], ref rotation);

            rotation = mathex.mul(math.conjugate(GetLocalToParentRotation(index)), rotation);
            rotation = mathex.scaleMulQuat(GetLocalToParentScale(index), rotation);
        }

        public void SetLocalToRootRotation(int index, in quaternion rotation)
        {
#if !UNITY_DISABLE_ANIMATION_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
            quaternion tmp = rotation;
            if (index > 0)
                InverseTransformRotationRecursive(Rig.Value.Skeleton.ParentIndexes[index], ref tmp);

            SetLocalToParentRotation(index, tmp);
        }

        public float4x4 GetLocalToRootMatrix(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
#endif
            float4x4 rigTx = GetLocalToParentMatrix(index);

            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                rigTx = math.mul(GetLocalToParentMatrix(parentIdx), rigTx);
                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }

            return rigTx;
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

            int parentIdx = Rig.Value.Skeleton.ParentIndexes[index];
            while (parentIdx >= 0)
            {
                translation = math.transform(GetLocalToParentMatrix(parentIdx), translation);
                rotation = mathex.scaleMulQuat(GetLocalToParentScale(parentIdx), rotation);
                rotation = mathex.mul(GetLocalToParentRotation(parentIdx), rotation);
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
            if (index > 0)
            {
                GetLocalToRootTR(Rig.Value.Skeleton.ParentIndexes[index], out float3 pT, out quaternion pR);
                float4x4 tx = math.mul(
                    math.inverse(new float4x4(pR, pT)),
                    new float4x4(rotation, translation)
                    );

                SetLocalToParentTranslation(index, tx.c3.xyz);
                SetLocalToParentRotation(index, new quaternion(tx));
            }
            else
            {
                SetLocalToParentTranslation(index, translation);
                SetLocalToParentRotation(index, rotation);
            }
        }

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
                m_LocalRotationData = new Ptr<quaternion4>((quaternion4*)(floatPtr + bindings.RotationSamplesOffset), bindings.RotationChunkCount)
            };
        }

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
    }

    [System.Obsolete("AnimationStreamProvider is obsolete, use AnimationStream.Create, AnimationStream.CreateReadOnly or AnimationStream.FromDefaultValues instead (RemovedAfter 2020-03-29)", false)]
    public static class AnimationStreamProvider
    {
        // ANSME: Burst needs this indirection to work otherwise it can't
        // seem to compile if both implementations live in this helper object
        unsafe public static AnimationStream Create(BlobAssetReference<RigDefinition> rig, NativeArray<AnimatedData> buffer)
        {
            if (rig == default || buffer.Length == 0 || buffer.Length != rig.Value.Bindings.StreamSize)
                return AnimationStream.Null;

            return AnimationStream.Create(rig, buffer.GetUnsafePtr());
        }

        unsafe public static AnimationStream CreateReadOnly(BlobAssetReference<RigDefinition> rig, NativeArray<AnimatedData> buffer)
        {
            if (rig == default || buffer.Length == 0 || buffer.Length != rig.Value.Bindings.StreamSize)
                return AnimationStream.Null;

            return AnimationStream.Create(rig, buffer.GetUnsafeReadOnlyPtr());
        }
    };

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
            UnsafeUtility.MemCpy(stream.GetUnsafePtr(), rig.DefaultValues.GetUnsafePtr(), Unsafe.SizeOf<AnimatedData>() * rig.Bindings.StreamSize);
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
                for(int i=0;i<m_Stream.TranslationCount;i++)
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
                for(int i=0;i<m_Stream.RotationCount;i++)
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
                for(int i=0;i<m_Stream.ScaleCount;i++)
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
                for(int i = 0; i < m_Stream.FloatCount; i++)
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
