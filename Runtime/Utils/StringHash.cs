using System;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animation
{
    [Serializable]
    public struct StringHash : IEquatable<StringHash>
    {
        public uint Id;

        public StringHash(string str)
        {
            Id = Hash(str);
        }

        static public implicit operator StringHash(string str) => new StringHash(str);

        static public implicit operator StringHash(uint id) => new StringHash {Id = id};

        static public bool IsNullOrEmpty(StringHash strHash) => strHash.Id == 0;

        static public bool operator == (StringHash lhs, StringHash rhs) => lhs.Id == rhs.Id;

        static public bool operator != (StringHash lhs, StringHash rhs) => lhs.Id != rhs.Id;

        public bool Equals(StringHash other) => Id == other.Id;

        public override int GetHashCode() => (int)Id;

        public override bool Equals(object other)
        {
            if (other == null || !(other is StringHash))
                return false;

            return Id == ((StringHash)other).Id;
        }

        unsafe static internal uint Hash(string str)
        {
            uint hash = 0;
            if (str == null)
                return hash;

            var arr = (str == string.Empty) ? new char[] { '\0' } : str.ToCharArray();
            fixed (void* ptr = &arr[0])
            {
                hash = math.hash(ptr, UnsafeUtility.SizeOf<char>() * arr.Length);
            }

            return hash;
        }
    }
}
