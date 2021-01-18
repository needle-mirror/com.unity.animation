using System;
using UnityEngine;

namespace Unity.Animation
{
    [Serializable]
    internal struct PortTargetGUID : IEquatable<PortTargetGUID>
    {
        [SerializeField]
        internal ulong NodeIDPart1;
        [SerializeField]
        internal ulong NodeIDPart2;
        [SerializeField]
        internal string PortUniqueName;

        public override int GetHashCode()
        {
            return NodeIDPart1.GetHashCode() + NodeIDPart2.GetHashCode() + PortUniqueName.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is PortTargetGUID otherID)
                return Equals(otherID);
            return false;
        }

        public bool Equals(PortTargetGUID other)
        {
            return NodeIDPart1 == other.NodeIDPart1 && NodeIDPart2 == other.NodeIDPart2 &&
                PortUniqueName == other.PortUniqueName;
        }

        public static bool operator==(PortTargetGUID lhs, PortTargetGUID rhs)
        {
            if (ReferenceEquals(lhs, null))
            {
                if (ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator!=(PortTargetGUID lhs, PortTargetGUID rhs)
        {
            return !(lhs == rhs);
        }
    }

    [Serializable]
    public struct PortID : IEquatable<PortID>
    {
        static public PortID Invalid = new PortID(UInt16.MaxValue);
        public ushort ID;
        public ushort Index;

        public bool IsPortArray() => Index != UInt16.MaxValue;
        public bool IsValid() => ID != UInt16.MaxValue;

        public PortID(ushort id, ushort index = UInt16.MaxValue)
        {
            ID = id;
            Index = index;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode() + Index;
        }

        public override bool Equals(object other)
        {
            if (other is PortID otherID)
                return Equals(otherID);
            return false;
        }

        public bool Equals(PortID other)
        {
            if (!IsValid() && !other.IsValid())
                return true;
            return ID == other.ID && Index == other.Index;
        }

        public static implicit operator PortID(ushort id)
        {
            return new PortID(id);
        }

        public static implicit operator ushort(PortID id)
        {
            return id.ID;
        }

        public static bool operator==(PortID lhs, PortID rhs)
        {
            if (ReferenceEquals(lhs, null))
            {
                if (ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator!=(PortID lhs, PortID rhs)
        {
            return !(lhs == rhs);
        }
    }

    [Serializable]
    public struct NodeID : IEquatable<NodeID>
    {
        public static readonly NodeID Invalid = new NodeID(-1);
        public int Value;

        public bool IsValid() => (NodeID)Value != Invalid;

        public NodeID(int value)
        {
            Value = value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is NodeID otherID)
                return Equals(otherID);
            return false;
        }

        public bool Equals(NodeID other)
        {
            return Value == other.Value;
        }

        public static explicit operator NodeID(int value)
        {
            return new NodeID(value);
        }

        public static explicit operator int(NodeID id)
        {
            return id.Value;
        }

        public static bool operator==(NodeID lhs, NodeID rhs)
        {
            if (ReferenceEquals(lhs, null))
            {
                if (ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator!=(NodeID lhs, NodeID rhs)
        {
            return !(lhs == rhs);
        }
    }

    [Serializable]
    internal struct PortReference
    {
        public NodeID ID;
        public PortID Port;
    }
}
