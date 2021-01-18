//#define DEBUG_STRINGHASH

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;


#if DEBUG_STRINGHASH
using System.Collections.Generic;
#endif

namespace Unity.Animation
{
#if DEBUG_STRINGHASH
    static class StringHashDebugCache
    {
        internal static Dictionary<StringHash, string> Hashes =
            new Dictionary<StringHash, string>();

        internal static void Add(StringHash hash, string str)
        {
            if (!Hashes.TryGetValue(hash, out var value))
                Hashes[hash] = str;
            else if (value != str)
                throw new Exception($"StringHash [{hash}] for '{str}' collides with existing hash of '{value}'");
        }
    }
#endif

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [BurstCompatible]
    [Serializable]
    public struct StringHash : IEquatable<StringHash>
    {
        public uint Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringHash(string str) =>
            Id = Hash(str);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public implicit operator StringHash(string str) =>
            new StringHash(str);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public implicit operator StringHash(uint id) =>
            new StringHash {Id = id};

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsNullOrEmpty(StringHash strHash) =>
            strHash.Id == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator==(StringHash lhs, StringHash rhs) =>
            lhs.Id == rhs.Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator!=(StringHash lhs, StringHash rhs) =>
            lhs.Id != rhs.Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StringHash other) =>
            Id == other.Id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() =>
            (int)Id;

        [NotBurstCompatible]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (other == null || !(other is StringHash))
                return false;

            return Id == ((StringHash)other).Id;
        }

        [NotBurstCompatible]
        unsafe static internal uint Hash(string str)
        {
            uint hash = 0;
            if (string.IsNullOrEmpty(str))
                return hash;

            var arr = str.ToCharArray();
            fixed(void* ptr = &arr[0])
            {
                hash = math.hash(ptr, UnsafeUtility.SizeOf<char>() * arr.Length);
            }

#if DEBUG_STRINGHASH
            StringHashDebugCache.Add(hash, str);
#endif

            return hash;
        }

        [NotBurstCompatible]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal string DebuggerDisplay
        {
            get
            {
                string debugDisplay = default;
#if DEBUG_STRINGHASH
                StringHashDebugCache.Hashes.TryGetValue(this, out var value);
                if (string.IsNullOrEmpty(value))
                    value = "null";
                debugDisplay = $"Id = {Id} : '{value}'";
#else
                debugDisplay = $"Id = {Id}";
#endif
                return debugDisplay;
            }
        }
    }
}
