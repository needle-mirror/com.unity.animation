using Unity.Mathematics;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using System.IO;
using System;
using Unity.Entities;
using UnityEngine;

namespace Unity.Animation
{
    [StructLayout(LayoutKind.Explicit), Serializable]
    public struct GraphVariant : IEquatable<GraphVariant>
    {
        [Serializable]
        public enum ValueType : byte
        {
            Unknown,
            Bool,
            Int,
            UInt,
            Short,
            UShort,
            ULong,
            Long,
            Float,
            Float2,
            Float3,
            Float4,
            Quaternion,
            Entity,
            Hash128,
            Int4,
        }

        [FieldOffset(0)] public ValueType Type;
        [FieldOffset(sizeof(ValueType))] private bool _bool;
        [FieldOffset(sizeof(ValueType))] private int _int;
        [FieldOffset(sizeof(ValueType))] private long _long;
        [FieldOffset(sizeof(ValueType))] private float _float;
        [FieldOffset(sizeof(ValueType))] private float2 _float2;
        [FieldOffset(sizeof(ValueType))] private float3 _float3;
        [FieldOffset(sizeof(ValueType))] private float4 _float4;
        [FieldOffset(sizeof(ValueType))] private quaternion _quaternion;
        [FieldOffset(sizeof(ValueType))] private Entity _entity;
        [FieldOffset(sizeof(ValueType))] private Unity.Entities.Hash128 _hash128;
        [FieldOffset(sizeof(ValueType)), SerializeField] private int4 _int4;

        public bool Bool
        {
            get
            {
                if (Type == ValueType.Bool)
                    return _bool;
                throw new InvalidDataException($"Invalid Type {Type}");
            }
            set { Type = ValueType.Bool; _bool = value; }
        }

        public int Int
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Bool:
                        return _bool ? 1 : 0;
                    case ValueType.Float:
                        return (int)_float;
                    case ValueType.UShort:
                        return (ushort)_int;
                    case ValueType.Short:
                    case ValueType.Int:
                        return _int;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set { Type = ValueType.Int; _int = value; }
        }

        public uint UInt
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Bool:
                        return _bool ? 1U : 0U;
                    case ValueType.UShort:
                    case ValueType.UInt:
                        return (uint)_int;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set { Type = ValueType.UInt; _int = (int)value; }
        }

        public long Long
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Bool:
                        return _bool ? 1 : 0;
                    case ValueType.Float:
                        return (long)_float;
                    case ValueType.UShort:
                    case ValueType.UInt:
                        return (uint)_int;
                    case ValueType.Short:
                    case ValueType.Int:
                        return _int;
                    case ValueType.Long:
                        return _long;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set { Type = ValueType.Long; _long = value; }
        }

        public ulong ULong
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Bool:
                        return _bool ? 1UL : 0UL;
                    case ValueType.UShort:
                    case ValueType.UInt:
                    case ValueType.ULong:
                        return (ulong)_long;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set { Type = ValueType.ULong; _long = (long)value; }
        }

        public short Short
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Bool:
                        return _bool ? (short)1 : (short)0;
                    case ValueType.Short:
                        return (short)_int;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set { Type = ValueType.Short; _int = (int)value; }
        }

        public ushort UShort
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Bool:
                        return _bool ? (ushort)1 : (ushort)0;
                    case ValueType.UShort:
                        return (ushort)_int;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set { Type = ValueType.UShort; _int = (int)value; }
        }

        public float Float
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Bool:
                        return _bool ? 1 : 0;
                    case ValueType.Float:
                        return _float;
                    case ValueType.UShort:
                        return (uint)_int;
                    case ValueType.Short:
                        return _int;
                    case ValueType.Float2:
                        return _float2.x;
                    case ValueType.Float3:
                        return _float3.x;
                    case ValueType.Float4:
                        return _float4.x;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set { Type = ValueType.Float; _float = value; }
        }

        public float2 Float2
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Float:
                        return new float2(_float, _float);
                    case ValueType.Float2:
                        return _float2;
                    case ValueType.Float3:
                        return _float3.xy;
                    case ValueType.Float4:
                        return _float4.xy;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set
            {
                Type = ValueType.Float2;
                _float2 = value;
            }
        }

        public float3 Float3
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Float:
                        return new float3(_float, 0, 0);
                    case ValueType.Float2:
                        return new float3(_float2.xy, 0);
                    case ValueType.Float3:
                        return _float3;
                    case ValueType.Float4:
                        return _float4.xyz;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set
            {
                Type = ValueType.Float3;
                _float3 = value;
            }
        }

        public float4 Float4
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Float:
                        return new float4(_float, 0, 0, 0);
                    case ValueType.Float2:
                        return new float4(_float2.xy, 0, 0);
                    case ValueType.Float3:
                        return new float4(_float3, 0);
                    case ValueType.Float4:
                        return _float4;
                    case ValueType.Quaternion:
                        return _quaternion.value;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set
            {
                Type = ValueType.Float4;
                _float4 = value;
            }
        }

        public quaternion Quaternion
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Float4:
                        return new quaternion(_float4);
                    case ValueType.Quaternion:
                        return _quaternion;
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set
            { Type = ValueType.Quaternion; _quaternion = value; }
        }
        public Entity Entity { get { Assert.AreEqual(Type, ValueType.Entity); return _entity; } set { Type = ValueType.Entity; _entity = value; } }

        public Unity.Entities.Hash128 Hash128
        {
            get
            {
                if (Type == ValueType.Hash128)
                    return _hash128;
                throw new InvalidDataException($"Invalid Type {Type}");
            }
            set { Type = ValueType.Hash128; _hash128 = value; }
        }

        public int4 Int4
        {
            get
            {
                switch (Type)
                {
                    case ValueType.Int4:
                        return new int4(_int4);
                    default:
                        throw new InvalidDataException($"Invalid Type {Type}");
                }
            }
            set
            {
                Type = ValueType.Int4;
                _int4 = value;
            }
        }

        public static implicit operator GraphVariant(bool f)
        {
            return new GraphVariant { Bool = f };
        }

        public static implicit operator GraphVariant(uint f)
        {
            return new GraphVariant { UInt = f };
        }

        public static implicit operator GraphVariant(int f)
        {
            return new GraphVariant { Int = f };
        }

        public static implicit operator GraphVariant(ushort f)
        {
            return new GraphVariant { UShort = f };
        }

        public static implicit operator GraphVariant(short f)
        {
            return new GraphVariant { Short = f };
        }

        public static implicit operator GraphVariant(long f)
        {
            return new GraphVariant { Long = f };
        }

        public static implicit operator GraphVariant(ulong f)
        {
            return new GraphVariant { ULong = f };
        }

        public static implicit operator GraphVariant(float f)
        {
            return new GraphVariant { Float = f };
        }

        public static implicit operator GraphVariant(float2 f)
        {
            return new GraphVariant { Float2 = f };
        }

        public static implicit operator GraphVariant(float3 f)
        {
            return new GraphVariant { Float3 = f };
        }

        public static implicit operator GraphVariant(float4 f)
        {
            return new GraphVariant { Float4 = f };
        }

        public static implicit operator GraphVariant(quaternion f)
        {
            return new GraphVariant { Quaternion = f };
        }

        public static implicit operator GraphVariant(Entity f)
        {
            return new GraphVariant { Entity = f };
        }

        public static implicit operator GraphVariant(Unity.Entities.Hash128 f)
        {
            return new GraphVariant { Hash128 = f };
        }

        public static implicit operator GraphVariant(int4 f)
        {
            return new GraphVariant { Int4 = f };
        }

        public static GraphVariant FromObject(object o)
        {
            if (o != null)
            {
                var type = o.GetType();
                if (type == typeof(bool))
                    return (bool)o;
                if (type == typeof(long))
                    return (long)o;
                if (type == typeof(ulong))
                    return (ulong)o;
                if (type == typeof(uint))
                    return (uint)o;
                if (type == typeof(ushort))
                    return (ushort)o;
                if (type == typeof(int))
                    return (int)o;
                if (type == typeof(short))
                    return (short)o;
                if (type == typeof(float))
                    return (float)o;
                if (type == typeof(float2))
                    return (float2)o;
                if (type == typeof(float3))
                    return (float3)o;
                if (type == typeof(float4))
                    return (float4)o;
                if (type == typeof(quaternion))
                    return (quaternion)o;
                if (type == typeof(Unity.Entities.Hash128))
                    return (Unity.Entities.Hash128)o;
                if (type == typeof(int4))
                    return (int4)o;
                throw new InvalidDataException($"Invalid Object of type {o.GetType()}");
            }
            throw new InvalidDataException($"Invalid Object");
        }

        public override string ToString()
        {
            switch (Type)
            {
                case ValueType.Unknown:
                    return ValueType.Unknown.ToString();
                case ValueType.Bool:
                    return Bool.ToString();
                case ValueType.ULong:
                    return ULong.ToString();
                case ValueType.Long:
                    return Long.ToString();
                case ValueType.UInt:
                    return UInt.ToString();
                case ValueType.Int:
                    return Int.ToString();
                case ValueType.UShort:
                    return UShort.ToString();
                case ValueType.Short:
                    return Short.ToString();
                case ValueType.Float:
                    return Float.ToString();
                case ValueType.Float2:
                    return Float2.ToString();
                case ValueType.Float3:
                    return Float3.ToString();
                case ValueType.Float4:
                    return Float4.ToString();
                case ValueType.Quaternion:
                    return Quaternion.ToString();
                case ValueType.Entity:
                    return Entity.ToString();
                case ValueType.Hash128:
                    return Hash128.ToString();
                case ValueType.Int4:
                    return Int4.ToString();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Equals(GraphVariant other)
        {
            if (Type != other.Type)
                return false;

            switch (Type)
            {
                case ValueType.Unknown:
                    return false;
                case ValueType.Bool:
                    return Bool == other.Bool;
                case ValueType.ULong:
                    return ULong == other.ULong;
                case ValueType.Long:
                    return Long == other.Long;
                case ValueType.UInt:
                    return UInt == other.UInt;
                case ValueType.Int:
                    return Int == other.Int;
                case ValueType.UShort:
                    return UShort == other.UShort;
                case ValueType.Short:
                    return Short == other.Short;
                case ValueType.Float:
                    return Float == other.Float;
                case ValueType.Float2:
                    return Float2.Equals(other.Float2);
                case ValueType.Float3:
                    return Float3.Equals(other.Float3);
                case ValueType.Float4:
                    return Float4.Equals(other.Float4);
                case ValueType.Quaternion:
                    return Quaternion.Equals(other.Quaternion);
                case ValueType.Entity:
                    return Entity == other.Entity;
                case ValueType.Hash128:
                    return Hash128.Equals(other.Hash128);
                case ValueType.Int4:
                    return Int4.Equals(other.Int4);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool Equals(object obj)
        {
            return obj is GraphVariant other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Tuple.Create((int)Type, _int4).GetHashCode();
        }

        public static ValueType ValueTypeFromType(Type type)
        {
            if (type == typeof(bool))
                return ValueType.Bool;
            if (type == typeof(uint))
                return ValueType.UInt;
            if (type == typeof(int))
                return ValueType.Int;
            if (type == typeof(long))
                return ValueType.Long;
            if (type == typeof(ulong))
                return ValueType.ULong;
            if (type == typeof(ushort))
                return ValueType.UShort;
            if (type == typeof(short))
                return ValueType.Short;
            if (type == typeof(float))
                return ValueType.Float;
            if (type == typeof(float2))
                return ValueType.Float2;
            if (type == typeof(float3))
                return ValueType.Float3;
            if (type == typeof(float4))
                return ValueType.Float4;
            if (type == typeof(quaternion))
                return ValueType.Quaternion;
            if (type == typeof(Entity))
                return ValueType.Entity;
            if (type == typeof(Unity.Entities.Hash128))
                return ValueType.Hash128;
            if (type == typeof(int4))
                return ValueType.Int4;
            return ValueType.Unknown;
        }

        public static Type TypeFromValueType(ValueType type)
        {
            if (type == ValueType.Bool)
                return typeof(bool);
            if (type == ValueType.Short)
                return typeof(short);
            if (type == ValueType.UShort)
                return typeof(ushort);
            if (type == ValueType.Int)
                return typeof(int);
            if (type == ValueType.UInt)
                return typeof(uint);
            if (type == ValueType.ULong)
                return typeof(ulong);
            if (type == ValueType.Long)
                return typeof(long);
            if (type == ValueType.Float)
                return typeof(float);
            if (type == ValueType.Float2)
                return typeof(float2);
            if (type == ValueType.Float3)
                return typeof(float3);
            if (type == ValueType.Float4)
                return typeof(float4);
            if (type == ValueType.Quaternion)
                return typeof(quaternion);
            if (type == ValueType.Entity)
                return typeof(Entity);
            if (type == ValueType.Hash128)
                return typeof(Unity.Entities.Hash128);
            if (type == ValueType.Int4)
                return typeof(int4);
            return null;
        }
    }
}
