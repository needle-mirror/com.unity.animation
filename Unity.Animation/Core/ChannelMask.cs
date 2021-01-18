using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Animation
{
    /// <summary>
    /// Used by the <see cref="AnimationStream"/> to reference the masks within a stream.
    /// </summary>
    /// <remarks>Use this reference to manipulate the AnimationStream masks. Since this struct references a memory block
    /// in the ECS component store, it's life cycle is limited to a local scope.</remarks>
    public unsafe ref struct ChannelMask
    {
        internal UnsafeBitArray   m_Masks;
        internal BlobAssetReference<RigDefinition> m_Rig;
        internal int m_IsReadOnly;

        /// <summary>
        /// Returns number of translation channels.
        /// </summary>
        public int TranslationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Rig.Value.Bindings.TranslationBindings.Length;
        }

        /// <summary>
        /// Returns number of rotation channels.
        /// </summary>
        public int RotationCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Rig.Value.Bindings.RotationBindings.Length;
        }

        /// <summary>
        /// Returns number of scale channels.
        /// </summary>
        public int ScaleCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Rig.Value.Bindings.ScaleBindings.Length;
        }

        /// <summary>
        /// Returns number of float channels.
        /// </summary>
        public int FloatCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Rig.Value.Bindings.FloatBindings.Length;
        }

        /// <summary>
        /// Returns number of int channels.
        /// </summary>
        public int IntCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Rig.Value.Bindings.IntBindings.Length;
        }

        internal ChannelMask(void* ptr, BlobAssetReference<RigDefinition> rigDefinition, bool IsReadOnly, Allocator allocator = Allocator.None)
        {
            var maskSizeInBytes = Core.AlignUp(rigDefinition.Value.Bindings.BindingCount, 64) / 8;
            m_Masks = new UnsafeBitArray(ptr, maskSizeInBytes, allocator);
            m_Rig = rigDefinition;
            m_IsReadOnly = IsReadOnly ? 1 : 0;
        }

        /// <summary>
        /// Clear all channels to 0.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            ValidateIsWritable();
            m_Masks.Clear();
        }

        /// <summary>
        /// Set all channels to specified value
        /// </summary>
        /// <param name="value">Value of masks to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(bool value)
        {
            ValidateIsWritable();
            m_Masks.SetBits(value);
        }

        /// <summary>
        /// Set channels to desired boolean value.
        /// </summary>
        /// <param name="pos">Position in channel mask.</param>
        /// <param name="value">Value of bits to set.</param>
        /// <param name="numBits">Number of channels to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Set(int pos, bool value, int numBits)
        {
            ValidateIsWritable();
            m_Masks.SetBits(pos, value, numBits);
        }

        /// <summary>
        /// Returns true if channel at position is set.
        /// </summary>
        /// <param name="pos">Position in ChannelMask.</param>
        /// <returns>Returns true if bit is set.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(int pos)
        {
            return m_Masks.IsSet(pos);
        }

        /// <summary>
        /// Copy all channels from a source.
        /// </summary>
        /// <param name="src">Source AnimationStream.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(ref ChannelMask src)
        {
            ValidateIsWritable();
            m_Masks.CopyFrom(ref src.m_Masks);
        }

        /// <summary>
        /// OR all channels with other channelmask.
        /// </summary>
        /// <param name="other">Other ChannelMask to OR channel masks with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Or(ref ChannelMask other)
        {
            ValidateIsWritable();
            m_Masks.OrBits64(ref other.m_Masks);
        }

        /// <summary>
        /// OR all lhs and rhs channels and store result.
        /// ChannelMask from lhs and rhs are not modified.
        /// </summary>
        /// <param name="lhs">Input ChannelMask</param>
        /// <param name="rhs">Input ChannelMask</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Or(ref ChannelMask lhs, ref ChannelMask rhs)
        {
            ValidateIsWritable();
            m_Masks.OrBits64(ref lhs.m_Masks, ref rhs.m_Masks);
        }

        /// <summary>
        /// AND all channels with other channelmask.
        /// </summary>
        /// <param name="other">Other ChannelMask to AND channel masks with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void And(ref ChannelMask other)
        {
            ValidateIsWritable();
            m_Masks.AndBits64(ref other.m_Masks);
        }

        /// <summary>
        /// AND all lhs and rhs channels and store result.
        /// ChannelMask from lhs and rhs are not modified.
        /// </summary>
        /// <param name="lhs">Input ChannelMask</param>
        /// <param name="rhs">Input ChannelMask</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void And(ref ChannelMask lhs, ref ChannelMask rhs)
        {
            ValidateIsWritable();
            m_Masks.AndBits64(ref lhs.m_Masks, ref rhs.m_Masks);
        }

        /// <summary>
        /// Calculate number of set channels.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CountChannels()
        {
            return m_Masks.CountBits(0, m_Masks.Length);
        }

        /// <summary>
        /// Invert all channels.
        /// </summary>
        public void Invert()
        {
            ValidateIsWritable();
            m_Masks.InvertBits64();
        }

        /// <summary>
        /// Returns true if Any channels bit are set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAny()
        {
            return m_Masks.TestAny(0, m_Masks.Length);
        }

        /// <summary>
        /// Returns true if All channels bit are set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAll()
        {
            return m_Masks.TestAll(0, m_Masks.Length);
        }

        /// <summary>
        /// Returns true if none of channels bit are set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasNone()
        {
            return m_Masks.TestNone(0, m_Masks.Length);
        }

        /// <summary>
        /// Return translation channel mask.
        /// </summary>
        /// <param name="index">Translation channel index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTranslationSet(int index)
        {
            ValidateIndexBounds(index, TranslationCount);
            return m_Masks.IsSet(m_Rig.Value.Bindings.TranslationBindingIndex + index);
        }

        /// <summary>
        /// Return rotation channel mask.
        /// </summary>
        /// <param name="index">Rotation channel index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRotationSet(int index)
        {
            ValidateIndexBounds(index, RotationCount);
            return m_Masks.IsSet(m_Rig.Value.Bindings.RotationBindingIndex + index);
        }

        /// <summary>
        /// Return scale channel mask.
        /// </summary>
        /// <param name="index">Scale channel index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsScaleSet(int index)
        {
            ValidateIndexBounds(index, ScaleCount);
            return m_Masks.IsSet(m_Rig.Value.Bindings.ScaleBindingIndex + index);
        }

        /// <summary>
        /// Return float channel mask.
        /// </summary>
        /// <param name="index">Float channel index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFloatSet(int index)
        {
            ValidateIndexBounds(index, FloatCount);
            return m_Masks.IsSet(m_Rig.Value.Bindings.FloatBindingIndex + index);
        }

        /// <summary>
        /// Return int channel mask.
        /// </summary>
        /// <param name="index">Int channel index.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsIntSet(int index)
        {
            ValidateIndexBounds(index, IntCount);
            return m_Masks.IsSet(m_Rig.Value.Bindings.IntBindingIndex + index);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateIsWritable()
        {
            if (m_IsReadOnly != 0)
                throw new System.InvalidOperationException("ChannelMask is read only.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateIndexBounds(int index, int length)
        {
            if ((uint)index >= length)
                throw new System.IndexOutOfRangeException($"ChannelMask: index '{index}' is out of range of '{length}'.");
        }
    }
}
