using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

namespace Unity.Animation.Editor
{
    static class EditorGUILayoutExtensions
    {
        public static GraphVariant VariantField(string name, GraphVariant variant)
        {
            var type = variant.Type;
            switch (type)
            {
                case GraphVariant.ValueType.Float:
                    variant.Float = EditorGUILayout.FloatField(name, variant.Float);
                    break;
                case GraphVariant.ValueType.Bool:
                    variant.Bool = EditorGUILayout.Toggle(name, variant.Bool);
                    break;
                case GraphVariant.ValueType.UShort:
                    variant.UShort = (ushort)EditorGUILayout.IntField(name, variant.UShort);
                    break;
                case GraphVariant.ValueType.Short:
                    variant.Short = (short)EditorGUILayout.IntField(name, variant.Short);
                    break;
                case GraphVariant.ValueType.UInt:
                    variant.UInt = (uint)EditorGUILayout.LongField(name, variant.UInt);
                    break;
                case GraphVariant.ValueType.Int:
                    variant.Int = EditorGUILayout.IntField(name, variant.Int);
                    break;
                case GraphVariant.ValueType.Long:
                    variant.Long = EditorGUILayout.LongField(name, variant.Long);
                    break;
                case GraphVariant.ValueType.Float2:
                {
                    var v = new Vector2(variant.Float2.x, variant.Float2.y);
                    v = EditorGUILayout.Vector2Field(name, v);
                    variant = new float2(v.x, v.y);
                }
                break;
                case GraphVariant.ValueType.Float3:
                {
                    var v = new Vector3(variant.Float3.x, variant.Float3.y, variant.Float3.z);
                    v = EditorGUILayout.Vector3Field(name, v);
                    variant = new float3(v.x, v.y, v.z);
                }
                break;
                case GraphVariant.ValueType.Float4:
                {
                    var v = new Vector4(variant.Float4.x, variant.Float4.y, variant.Float4.z, variant.Float4.w);
                    v = EditorGUILayout.Vector4Field(name, v);
                    variant = new float4(v.x, v.y, v.z, v.w);
                }
                break;
                case GraphVariant.ValueType.Hash128:
                {
                    var v = EditorGUILayout.TextField(name, variant.Hash128.ToString());
                    variant = new Entities.Hash128(v);
                }
                break;
                case GraphVariant.ValueType.Quaternion:
                {
                    var q = new Quaternion(
                        variant.Quaternion.value.x,
                        variant.Quaternion.value.y,
                        variant.Quaternion.value.z,
                        variant.Quaternion.value.w);

                    q = Quaternion.Euler(EditorGUILayout.Vector3Field(name, q.eulerAngles));
                    variant = new quaternion(q.x, q.y, q.z, q.w);
                }
                break;
                case GraphVariant.ValueType.Entity:
                    //variant = EditorGUILayout.ObjectField(name, variant.Object, typeof(GameObject), true);
                    break;
                case GraphVariant.ValueType.ULong:
                default:
                    break;
            }

            return variant;
        }
    }

    static class EditorGUIExtensions
    {
        public static GraphVariant VariantField(Rect rect, string name, GraphVariant variant)
        {
            switch (variant.Type)
            {
                case GraphVariant.ValueType.Float:
                    variant.Float = EditorGUI.FloatField(rect, name, variant.Float);
                    break;
                case GraphVariant.ValueType.Bool:
                    variant.Bool = EditorGUI.Toggle(rect, name, variant.Bool);
                    break;
                case GraphVariant.ValueType.UShort:
                    variant.UShort = (ushort)EditorGUI.IntField(rect, name, variant.UShort);
                    break;
                case GraphVariant.ValueType.Short:
                    variant.Short = (short)EditorGUI.IntField(rect, name, variant.Short);
                    break;
                case GraphVariant.ValueType.UInt:
                    variant.UInt = (uint)EditorGUI.LongField(rect, name, variant.UInt);
                    break;
                case GraphVariant.ValueType.Int:
                    variant.Int = EditorGUI.IntField(rect, name, variant.Int);
                    break;
                case GraphVariant.ValueType.Long:
                    variant.Long = EditorGUI.LongField(rect, name, variant.Long);
                    break;
                case GraphVariant.ValueType.Float2:
                {
                    var v = new Vector2(variant.Float2.x, variant.Float2.y);
                    v = EditorGUI.Vector2Field(rect, name, v);
                    variant = new float2(v.x, v.y);
                }
                break;
                case GraphVariant.ValueType.Float3:
                {
                    var v = new Vector3(variant.Float3.x, variant.Float3.y, variant.Float3.z);
                    v = EditorGUI.Vector3Field(rect, name, v);
                    variant = new float3(v.x, v.y, v.z);
                }
                break;
                case GraphVariant.ValueType.Float4:
                {
                    var v = new Vector4(variant.Float4.x, variant.Float4.y, variant.Float4.z, variant.Float4.w);
                    v = EditorGUI.Vector4Field(rect, name, v);
                    variant = new float4(v.x, v.y, v.z, v.w);
                }
                break;
                case GraphVariant.ValueType.Quaternion:
                {
                    var q = new Quaternion(
                        variant.Quaternion.value.x,
                        variant.Quaternion.value.y,
                        variant.Quaternion.value.z,
                        variant.Quaternion.value.w);

                    q = Quaternion.Euler(EditorGUI.Vector3Field(rect, name, q.eulerAngles));
                    variant = new quaternion(q.x, q.y, q.z, q.w);
                }
                break;
                case GraphVariant.ValueType.Hash128:
                {
                    var v = EditorGUI.TextField(rect, name, variant.Hash128.ToString());
                    variant = new Entities.Hash128(v);
                }
                break;
                case GraphVariant.ValueType.Entity:
                    //    variant = EditorGUI.ObjectField(name, variant.Object, typeof(GameObject), true);
                    break;
                case GraphVariant.ValueType.ULong:
                default:
                    break;
            }

            return variant;
        }
    }
}
