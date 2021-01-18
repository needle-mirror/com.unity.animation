using System;
using Unity.Mathematics;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class Float3Constant : Constant<float3>
    {
    }

    [Serializable]
    internal class QuaternionConstant : Constant<quaternion>
    {
    }

    [Serializable]
    internal class Float44Constant : Constant<float4x4>
    {
    }

    [GraphElementsExtensionMethodsCache]
    internal static partial class ConstantEditorExtensions
    {
        //GTFOConvert all constant editors would need a custom view to display those
        // public static VisualElement BuildVector3Editor(this IConstantEditorBuilder builder, ConstantNodeModel<float3> v)
        // {
        //     return builder.MakeFloatVectorEditor(v, 3,
        //         (vec, i) => vec[i], (ref float3 data, int i, float value) => data[i] = value);
        // }
        //
        // public static VisualElement BuildVector4Editor(this IConstantEditorBuilder builder, ConstantNodeModel<quaternion> v)
        // {
        //     return builder.MakeFloatVectorEditor(v, 4,
        //         (vec, i) => vec.value[i], (ref quaternion data, int i, float value) => data.value[i] = value);
        // }
        //
        // static VisualElement BuildMatrixFromFloatVector(InlineFloatEditor vector, int numberElementPerRow)
        // {
        //     var newVisualElement = new VisualElement();
        //     newVisualElement.style.flexDirection = FlexDirection.Column;
        //     newVisualElement.name = "matrixContainer";
        //     int nbrChildren = vector.childCount;
        //     int currentRow = 0;
        //     int currentColumn = 0;
        //     List<VisualElement> childCopy = vector.Children().ToList();
        //     InlineFloatEditor currentInline = null;
        //     while (currentRow * numberElementPerRow + currentColumn < nbrChildren)
        //     {
        //         if (currentInline == null)
        //         {
        //             currentInline = new InlineFloatEditor();
        //             newVisualElement.Add(currentInline);
        //         }
        //         currentInline.Add(childCopy[currentRow * numberElementPerRow + currentColumn]);
        //         ++currentColumn;
        //         if (currentColumn >= numberElementPerRow)
        //         {
        //             currentColumn = 0;
        //             ++currentRow;
        //             currentInline = null;
        //         }
        //     }
        //
        //     return newVisualElement;
        // }
        //
        // public static VisualElement BuildFloat44Editor(this IConstantEditorBuilder builder, ConstantNodeModel<float4x4> m)
        // {
        //     var fieldNames = new string[16];
        //     var elems = builder.MakeFloatVectorEditor(m, fieldNames, (vec, i) => vec[i / 4][i % 4], (ref float4x4 data, int i, float value) => data[i / 4][i % 4] = value);
        //     return BuildMatrixFromFloatVector(elems, 4);
        // }
    }
}
