using Unity.Mathematics;
using UnityEditor;

namespace Unity.Animation.Editor
{
    internal static class GraphVariantExtensions
    {
        static internal void ToSerializedProperty(this GraphVariant variant, SerializedProperty variantProperty)
        {
            variantProperty.FindPropertyRelative("Type").enumValueIndex = (int)variant.Type;
            var int4Property = variantProperty.FindPropertyRelative("_int4");
            var tmpVariant = variant;
            tmpVariant.Type = GraphVariant.ValueType.Int4;

            int4Property.FindPropertyRelative("x").intValue = tmpVariant.Int4.x;
            int4Property.FindPropertyRelative("y").intValue = tmpVariant.Int4.y;
            int4Property.FindPropertyRelative("z").intValue = tmpVariant.Int4.z;
            int4Property.FindPropertyRelative("w").intValue = tmpVariant.Int4.w;
        }

        static internal void FromSerializedProperty(this ref GraphVariant variant, SerializedProperty variantProperty)
        {
            var int4Property = variantProperty.FindPropertyRelative("_int4");
            var tmpValue = new int4();
            tmpValue.x = int4Property.FindPropertyRelative("x").intValue;
            tmpValue.y = int4Property.FindPropertyRelative("y").intValue;
            tmpValue.z = int4Property.FindPropertyRelative("z").intValue;
            tmpValue.w = int4Property.FindPropertyRelative("w").intValue;

            var type = variant.Type;
            variant.Type = GraphVariant.ValueType.Int4;
            variant.Int4 = tmpValue;
            variant.Type = type;
        }
    }
}
