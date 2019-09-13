using System.Runtime.CompilerServices;
using System.Diagnostics;

using UnityEngine.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace Unity.Animation
{
    public interface IAnimationStreamDescriptor
    {
        float3 GetLocalTranslation(int index);
        quaternion GetLocalRotation(int index);
        float3 GetLocalScale(int index);
        float GetFloat(int index);
        int GetInt(int index);

        void SetLocalTranslation(int index, float3 translation);
        void SetLocalRotation(int index, quaternion rotation);
        void SetLocalScale(int index, float3 scale);
        void SetFloat(int index, float value);
        void SetInt(int index, int value);

        unsafe float3* GetLocalTranslationUnsafePtr();
        unsafe quaternion* GetLocalRotationUnsafePtr();
        unsafe float3* GetLocalScaleUnsafePtr();
        unsafe float* GetFloatUnsafePtr();
        unsafe int* GetIntUnsafePtr();

        int TranslationCount { get; }
        int RotationCount { get; }
        int ScaleCount { get; }
        int FloatCount { get; }
        int IntCount { get; }
    }

    unsafe internal struct OffsetPtr<T> where T : unmanaged
    {
        internal T* m_Ptr;
        internal int m_Length;

        public OffsetPtr(T* ptr, int length)
        {
            m_Ptr = ptr;
            m_Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(int index)
        {
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_Ptr == null)
                throw new System.NullReferenceException("Invalid offset pointer");
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, m_Length));
        #endif

            return *(m_Ptr + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, T value)
        {
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_Ptr == null)
                throw new System.NullReferenceException("Invalid offset pointer");
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, m_Length));
        #endif

            *(m_Ptr + index) = value;
        }
    }

    [DebuggerTypeProxy(typeof(AnimationStreamOffsetPtrDescriptorDebugView))]
    public struct AnimationStreamOffsetPtrDescriptor : IAnimationStreamDescriptor
    {
        internal OffsetPtr<float3> m_LocalTranslationData;
        internal OffsetPtr<quaternion> m_LocalRotationData;
        internal OffsetPtr<float3> m_LocalScaleData;
        internal OffsetPtr<float> m_FloatData;
        internal OffsetPtr<int> m_IntData;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetLocalTranslation(int index) => m_LocalTranslationData.Get(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion GetLocalRotation(int index) => m_LocalRotationData.Get(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetLocalScale(int index) => m_LocalScaleData.Get(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetFloat(int index) => m_FloatData.Get(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetInt(int index) => (int)m_IntData.Get(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalTranslation(int index, float3 translation)
        {
            ValidateIsFinite(translation);
            m_LocalTranslationData.Set(index, translation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalRotation(int index, quaternion rotation)
        {
            ValidateIsFinite(rotation);
            m_LocalRotationData.Set(index, rotation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalScale(int index, float3 scale)
        {
            ValidateIsFinite(scale);
            m_LocalScaleData.Set(index, scale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFloat(int index, float value)
        {
            ValidateIsFinite(value);
            m_FloatData.Set(index, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetInt(int index, int value) => m_IntData.Set(index, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float3* GetLocalTranslationUnsafePtr() => m_LocalTranslationData.m_Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe quaternion* GetLocalRotationUnsafePtr() => m_LocalRotationData.m_Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float3* GetLocalScaleUnsafePtr() => m_LocalScaleData.m_Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float* GetFloatUnsafePtr() => m_FloatData.m_Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe int* GetIntUnsafePtr() => m_IntData.m_Ptr;

        public int TranslationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_LocalTranslationData.m_Length;
        }

        public int RotationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_LocalRotationData.m_Length;
        }

        public int ScaleCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_LocalScaleData.m_Length;
        }

        public int FloatCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_FloatData.m_Length;
        }

        public int IntCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_IntData.m_Length;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsFinite(float value)
        {
            if (!math.isfinite(value))
                throw new System.ArithmeticException();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsFinite(float3 value)
        {
            if (!math.all(math.isfinite(value)))
                throw new System.ArithmeticException();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void ValidateIsFinite(quaternion value)
        {
            if (!math.all(math.isfinite(value.value)))
                throw new System.ArithmeticException();
        }
    }

    public struct AnimationStream<T> where T : struct, IAnimationStreamDescriptor
    {
        public BlobAssetReference<RigDefinition> Rig;
        internal T m_StreamDescriptor;

        public static AnimationStream<T> Null => new AnimationStream<T>();

        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Rig == default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetLocalToParentTranslation(int index) => m_StreamDescriptor.GetLocalTranslation(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public quaternion GetLocalToParentRotation(int index) => m_StreamDescriptor.GetLocalRotation(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetLocalToParentScale(int index) => m_StreamDescriptor.GetLocalScale(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetFloat(int index) => m_StreamDescriptor.GetFloat(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetInt(int index) => m_StreamDescriptor.GetInt(index);

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
        public void SetLocalToParentTranslation(int index, float3 translation) => m_StreamDescriptor.SetLocalTranslation(index, translation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentRotation(int index, quaternion rotation) => m_StreamDescriptor.SetLocalRotation(index, rotation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentScale(int index, float3 scale) => m_StreamDescriptor.SetLocalScale(index, scale);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFloat(int index, float value) => m_StreamDescriptor.SetFloat(index, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetInt(int index, int value) => m_StreamDescriptor.SetInt(index, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLocalToParentTRS(int index, float3 translation, quaternion rotation, float3 scale)
        {
            SetLocalToParentTranslation(index, translation);
            SetLocalToParentRotation(index, rotation);
            SetLocalToParentScale(index, scale);
        }

        public float3 GetLocalToRigTranslation(int index)
        {
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
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

        public void SetLocalToRigTranslation(int index, float3 translation)
        {
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
        #endif
            if (index > 0)
            {
                translation = math.transform(
                    math.inverse(GetLocalToRigMatrix(Rig.Value.Skeleton.ParentIndexes[index])),
                    translation
                    );
            }

            SetLocalToParentTranslation(index, translation);
        }

        public quaternion GetLocalToRigRotation(int index)
        {
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
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
                rigR = math.mul(GetLocalToParentRotation(parentIdx), rigR);
                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }

            return rigR;
        }

        internal void InverseTransformRotationRecursive(int index, ref quaternion rotation)
        {
            if (index > 0)
                InverseTransformRotationRecursive(Rig.Value.Skeleton.ParentIndexes[index], ref rotation);

            rotation = math.mul(math.conjugate(GetLocalToParentRotation(index)), rotation);
            rotation = mathex.scaleMulQuat(GetLocalToParentScale(index), rotation);
        }

        public void SetLocalToRigRotation(int index, quaternion rotation)
        {
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
        #endif
            if (index > 0)
                InverseTransformRotationRecursive(Rig.Value.Skeleton.ParentIndexes[index], ref rotation);

            SetLocalToParentRotation(index, rotation);
        }

        public float4x4 GetLocalToRigMatrix(int index)
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

        public void GetLocalToRigTR(int index, out float3 translation, out quaternion rotation)
        {
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
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
                rotation = math.mul(GetLocalToParentRotation(parentIdx), rotation);
                parentIdx = Rig.Value.Skeleton.ParentIndexes[parentIdx];
            }
        }

        public void SetLocalToRigTR(int index, float3 translation, quaternion rotation)
        {
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (IsNull)
                throw new System.NullReferenceException("Invalid rig definition");
            if ((uint)index >= (uint)Rig.Value.Skeleton.BoneCount)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, Rig.Value.Skeleton.BoneCount));
        #endif
            if (index > 0)
            {
                GetLocalToRigTR(Rig.Value.Skeleton.ParentIndexes[index], out float3 pT, out quaternion pR);
                float4x4 tx = math.mul(
                    math.inverse(new float4x4(pR, pT)),
                    new float4x4(rotation, translation)
                    );

                SetLocalToParentTranslation(index, new float3(tx.c3.x, tx.c3.y, tx.c3.z));
                SetLocalToParentRotation(index, new quaternion(tx));
            }
            else
            {
                SetLocalToParentTranslation(index, translation);
                SetLocalToParentRotation(index, rotation);
            }
        }

        unsafe internal static AnimationStream<AnimationStreamOffsetPtrDescriptor> Create(BlobAssetReference<RigDefinition> rig, float* dataPtr)
        {
            ref var bindings = ref rig.Value.Bindings;
            return new AnimationStream<AnimationStreamOffsetPtrDescriptor>()
            {
                Rig = rig,
                m_StreamDescriptor = new AnimationStreamOffsetPtrDescriptor()
                {
                    m_LocalTranslationData = new OffsetPtr<float3>((float3*)(dataPtr + bindings.TranslationSamplesOffset), bindings.TranslationBindings.Length),
                    m_LocalRotationData = new OffsetPtr<quaternion>((quaternion*)(dataPtr + bindings.RotationSamplesOffset), bindings.RotationBindings.Length),
                    m_LocalScaleData = new OffsetPtr<float3>((float3*)(dataPtr + bindings.ScaleSamplesOffset), bindings.ScaleBindings.Length),
                    m_FloatData = new OffsetPtr<float>(dataPtr + bindings.FloatSamplesOffset, bindings.FloatBindings.Length),
                    m_IntData = new OffsetPtr<int>((int* )dataPtr + bindings.IntSamplesOffset, bindings.IntBindings.Length)
                }
            };
        }

        unsafe internal static AnimationStream<AnimationStreamOffsetPtrDescriptor> Create(BlobAssetReference<RigDefinition> rig, float3* translationPtr, quaternion* rotationPtr, float3* scalePtr, float* floatPtr, int* intPtr)
        {
            ref var bindings = ref rig.Value.Bindings;
            return new AnimationStream<AnimationStreamOffsetPtrDescriptor>()
            {
                Rig = rig,
                m_StreamDescriptor = new AnimationStreamOffsetPtrDescriptor()
                {
                    m_LocalTranslationData = new OffsetPtr<float3>(translationPtr, bindings.TranslationBindings.Length),
                    m_LocalRotationData = new OffsetPtr<quaternion>(rotationPtr, bindings.RotationBindings.Length),
                    m_LocalScaleData = new OffsetPtr<float3>(scalePtr, bindings.ScaleBindings.Length),
                    m_FloatData = new OffsetPtr<float>(floatPtr, bindings.FloatBindings.Length),
                    m_IntData = new OffsetPtr<int>(intPtr, bindings.IntBindings.Length)
                }
            };
        }
    }

    public static class AnimationStreamProvider
    {
        // ANSME: Burst needs this indirection to work otherwise it can't
        // seem to compile if both implementations live in this helper object
        unsafe public static AnimationStream<AnimationStreamOffsetPtrDescriptor> Create(BlobAssetReference<RigDefinition> rig, NativeArray<float> buffer)
        {
            if (rig == default || buffer.Length == 0 || buffer.Length != rig.Value.Bindings.CurveCount)
                return AnimationStream<AnimationStreamOffsetPtrDescriptor>.Null;

            return AnimationStream<AnimationStreamOffsetPtrDescriptor>.Create(rig, (float*)buffer.GetUnsafePtr());
        }

        unsafe public static AnimationStream<AnimationStreamOffsetPtrDescriptor> CreateReadOnly(BlobAssetReference<RigDefinition> rig, NativeArray<float> buffer)
        {
            if (rig == default || buffer.Length == 0 || buffer.Length != rig.Value.Bindings.CurveCount)
                return AnimationStream<AnimationStreamOffsetPtrDescriptor>.Null;

            return AnimationStream<AnimationStreamOffsetPtrDescriptor>.Create(rig, (float*)buffer.GetUnsafeReadOnlyPtr());
        }

        unsafe public static AnimationStream<AnimationStreamOffsetPtrDescriptor> Create(
            BlobAssetReference<RigDefinition> rig,
            DynamicBuffer<AnimatedLocalTranslation> localTranslations,
            DynamicBuffer<AnimatedLocalRotation> localRotations,
            DynamicBuffer<AnimatedLocalScale> localScales,
            DynamicBuffer<AnimatedFloat> floats,
            DynamicBuffer<AnimatedInt> ints
            )
        {
            if (rig == default)
                return AnimationStream<AnimationStreamOffsetPtrDescriptor>.Null;

            Assert.AreEqual(rig.Value.Bindings.TranslationBindings.Length, localTranslations.Length);
            Assert.AreEqual(rig.Value.Bindings.RotationBindings.Length, localRotations.Length);
            Assert.AreEqual(rig.Value.Bindings.ScaleBindings.Length, localScales.Length);
            Assert.AreEqual(rig.Value.Bindings.FloatBindings.Length, floats.Length);
            Assert.AreEqual(rig.Value.Bindings.IntBindings.Length, ints.Length);

            return AnimationStream<AnimationStreamOffsetPtrDescriptor>.Create(
                rig,
                (float3*)localTranslations.Reinterpret<float3>().AsNativeArray().GetUnsafePtr(),
                (quaternion*)localRotations.Reinterpret<quaternion>().AsNativeArray().GetUnsafePtr(),
                (float3*)localScales.Reinterpret<float3>().AsNativeArray().GetUnsafePtr(),
                (float*)floats.Reinterpret<float>().AsNativeArray().GetUnsafePtr(),
                (int*)ints.Reinterpret<int>().AsNativeArray().GetUnsafePtr()
                );
        }

        unsafe public static AnimationStream<AnimationStreamOffsetPtrDescriptor> CreateReadOnly(
            BlobAssetReference<RigDefinition> rig,
            DynamicBuffer<AnimatedLocalTranslation> localTranslations,
            DynamicBuffer<AnimatedLocalRotation> localRotations,
            DynamicBuffer<AnimatedLocalScale> localScales,
            DynamicBuffer<AnimatedFloat> floats,
            DynamicBuffer<AnimatedInt> ints
            )
        {
            if (rig == default)
                return AnimationStream<AnimationStreamOffsetPtrDescriptor>.Null;

            Assert.AreEqual(rig.Value.Bindings.TranslationBindings.Length, localTranslations.Length);
            Assert.AreEqual(rig.Value.Bindings.RotationBindings.Length, localRotations.Length);
            Assert.AreEqual(rig.Value.Bindings.ScaleBindings.Length, localScales.Length);
            Assert.AreEqual(rig.Value.Bindings.FloatBindings.Length, floats.Length);
            Assert.AreEqual(rig.Value.Bindings.IntBindings.Length, ints.Length);

            return AnimationStream<AnimationStreamOffsetPtrDescriptor>.Create(
                rig,
                (float3*)localTranslations.Reinterpret<float3>().AsNativeArray().GetUnsafeReadOnlyPtr(),
                (quaternion*)localRotations.Reinterpret<quaternion>().AsNativeArray().GetUnsafeReadOnlyPtr(),
                (float3*)localScales.Reinterpret<float3>().AsNativeArray().GetUnsafeReadOnlyPtr(),
                (float*)floats.Reinterpret<float>().AsNativeArray().GetUnsafeReadOnlyPtr(),
                (int*)ints.Reinterpret<int>().AsNativeArray().GetUnsafeReadOnlyPtr()
                );
        }

        unsafe public static AnimationStream<AnimationStreamOffsetPtrDescriptor> CreateReadOnly(
            BlobAssetReference<RigDefinition> rig,
            ref BlobArray<float3> localTranslations,
            ref BlobArray<quaternion> localRotations,
            ref BlobArray<float3> localScales,
            ref BlobArray<float> floats,
            ref BlobArray<int> ints
            )
        {
            if (rig == default)
                return AnimationStream<AnimationStreamOffsetPtrDescriptor>.Null;

            Assert.AreEqual(rig.Value.Bindings.TranslationBindings.Length, localTranslations.Length);
            Assert.AreEqual(rig.Value.Bindings.RotationBindings.Length, localRotations.Length);
            Assert.AreEqual(rig.Value.Bindings.ScaleBindings.Length, localScales.Length);
            Assert.AreEqual(rig.Value.Bindings.FloatBindings.Length, floats.Length);
            Assert.AreEqual(rig.Value.Bindings.IntBindings.Length, ints.Length);

            return AnimationStream<AnimationStreamOffsetPtrDescriptor>.Create(
                rig,
                (float3*)localTranslations.GetUnsafePtr(),
                (quaternion*)localRotations.GetUnsafePtr(),
                (float3*)localScales.GetUnsafePtr(),
                (float*)floats.GetUnsafePtr(),
                (int*)ints.GetUnsafePtr()
                );
        }

    };

    public static class AnimationStreamUtils
    {
        unsafe public static void MemCpy<TDescriptor0, TDescriptor1>(ref AnimationStream<TDescriptor0> dst, ref AnimationStream<TDescriptor1> src)
            where TDescriptor0 : struct, IAnimationStreamDescriptor
            where TDescriptor1 : struct, IAnimationStreamDescriptor
        {
            Assert.IsFalse(dst.IsNull || src.IsNull);

            ref var dstDesc = ref dst.m_StreamDescriptor;
            ref var srcDesc = ref src.m_StreamDescriptor;
            Assert.AreEqual(dstDesc.TranslationCount, srcDesc.TranslationCount);
            Assert.AreEqual(dstDesc.RotationCount, srcDesc.RotationCount);
            Assert.AreEqual(dstDesc.ScaleCount, srcDesc.ScaleCount);
            Assert.AreEqual(dstDesc.FloatCount, srcDesc.FloatCount);
            Assert.AreEqual(dstDesc.IntCount, srcDesc.IntCount);

            UnsafeUtility.MemCpy(dstDesc.GetLocalTranslationUnsafePtr(), srcDesc.GetLocalTranslationUnsafePtr(), UnsafeUtility.SizeOf<float3>() * srcDesc.TranslationCount);
            UnsafeUtility.MemCpy(dstDesc.GetLocalRotationUnsafePtr(), srcDesc.GetLocalRotationUnsafePtr(), UnsafeUtility.SizeOf<quaternion>() * srcDesc.RotationCount);
            UnsafeUtility.MemCpy(dstDesc.GetLocalScaleUnsafePtr(), srcDesc.GetLocalScaleUnsafePtr(), UnsafeUtility.SizeOf<float3>() * srcDesc.ScaleCount);
            UnsafeUtility.MemCpy(dstDesc.GetFloatUnsafePtr(), srcDesc.GetFloatUnsafePtr(), UnsafeUtility.SizeOf<float>() * srcDesc.FloatCount);
            UnsafeUtility.MemCpy(dstDesc.GetIntUnsafePtr(), srcDesc.GetIntUnsafePtr(), UnsafeUtility.SizeOf<int>() * srcDesc.IntCount);
        }

        unsafe public static void SetDefaultValues<TDescriptor>(ref AnimationStream<TDescriptor> stream)
            where TDescriptor : struct, IAnimationStreamDescriptor
        {
            Assert.IsFalse(stream.IsNull);

            ref var defaultValues = ref stream.Rig.Value.DefaultValues;
            ref var desc = ref stream.m_StreamDescriptor;
            UnsafeUtility.MemCpy(desc.GetLocalTranslationUnsafePtr(), defaultValues.LocalTranslations.GetUnsafePtr(), UnsafeUtility.SizeOf<float3>() * desc.TranslationCount);
            UnsafeUtility.MemCpy(desc.GetLocalRotationUnsafePtr(), defaultValues.LocalRotations.GetUnsafePtr(), UnsafeUtility.SizeOf<quaternion>() * desc.RotationCount);
            UnsafeUtility.MemCpy(desc.GetLocalScaleUnsafePtr(), defaultValues.LocalScales.GetUnsafePtr(), UnsafeUtility.SizeOf<float3>() * desc.ScaleCount);
            UnsafeUtility.MemCpy(desc.GetFloatUnsafePtr(), defaultValues.Floats.GetUnsafePtr(), UnsafeUtility.SizeOf<float>() * desc.FloatCount);
            UnsafeUtility.MemCpy(desc.GetIntUnsafePtr(), defaultValues.Integers.GetUnsafePtr(), UnsafeUtility.SizeOf<int>() * desc.IntCount);
        }

        unsafe public static void MemClear<TDescriptor>(ref AnimationStream<TDescriptor> stream)
            where TDescriptor : struct, IAnimationStreamDescriptor
        {
            Assert.IsFalse(stream.IsNull);

            ref var desc = ref stream.m_StreamDescriptor;
            UnsafeUtility.MemClear(desc.GetLocalTranslationUnsafePtr(), UnsafeUtility.SizeOf<float3>() * desc.TranslationCount);
            UnsafeUtility.MemClear(desc.GetLocalRotationUnsafePtr(), UnsafeUtility.SizeOf<quaternion>() * desc.RotationCount);
            UnsafeUtility.MemClear(desc.GetLocalScaleUnsafePtr(), UnsafeUtility.SizeOf<float3>() * desc.ScaleCount);
            UnsafeUtility.MemClear(desc.GetFloatUnsafePtr(), UnsafeUtility.SizeOf<float>() * desc.FloatCount);
            UnsafeUtility.MemClear(desc.GetIntUnsafePtr(), UnsafeUtility.SizeOf<int>() * desc.IntCount);
        }
    }
    sealed class AnimationStreamOffsetPtrDescriptorDebugView
    {
        AnimationStreamOffsetPtrDescriptor m_Stream;

        public AnimationStreamOffsetPtrDescriptorDebugView(AnimationStreamOffsetPtrDescriptor stream)
        {
            m_Stream = stream;
        }

        public int TranslationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.m_LocalTranslationData.m_Length;
        }

        public float3[] Translations
        {
            get
            {
                float3[] values = new float3[m_Stream.m_LocalTranslationData.m_Length];
                for(int i=0;i<m_Stream.m_LocalTranslationData.m_Length;i++)
                    values[i] = m_Stream.m_LocalTranslationData.Get(i);

                return values;
            }
        }

        public int RotationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.m_LocalRotationData.m_Length;
        }

        public quaternion[] Rotations
        {
            get
            {
                quaternion[] values = new quaternion[m_Stream.m_LocalRotationData.m_Length];
                for(int i=0;i<m_Stream.m_LocalRotationData.m_Length;i++)
                    values[i] = m_Stream.m_LocalRotationData.Get(i);

                return values;
            }
        }

        public int ScaleCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.m_LocalScaleData.m_Length;
        }

        public float3[] Scales
        {
            get
            {
                float3[] values = new float3[m_Stream.m_LocalScaleData.m_Length];
                for(int i=0;i<m_Stream.m_LocalScaleData.m_Length;i++)
                    values[i] = m_Stream.m_LocalScaleData.Get(i);

                return values;
            }
        }

        public int FloatCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.m_FloatData.m_Length;
        }

        public float[] Floats
        {
            get
            {
                float[] values = new float[m_Stream.m_FloatData.m_Length];
                for(int i=0;i<m_Stream.m_FloatData.m_Length;i++)
                    values[i] = m_Stream.m_FloatData.Get(i);

                return values;
            }
        }

        public int IntCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.m_IntData.m_Length;
        }
        public int[] Ints
        {
            get
            {
                int[] values = new int[m_Stream.m_IntData.m_Length];
                for(int i=0;i<m_Stream.m_IntData.m_Length;i++)
                    values[i] = m_Stream.m_IntData.Get(i);

                return values;
            }
        }
    }
}
