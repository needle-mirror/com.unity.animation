using System;
using System.IO;
using Unity.DataFlowGraph;
using Unity.Mathematics;

namespace Unity.Animation
{
    internal static class GraphVariantConverterNodeFactory
    {
        internal static Type GetGraphVariantConverterNodeType(Type propertyType)
        {
            if (propertyType == typeof(bool))
                return typeof(ToBoolVariantConverterNode);
            if (propertyType == typeof(int))
                return typeof(ToIntVariantConverterNode);
            if (propertyType == typeof(uint))
                return typeof(ToUIntVariantConverterNode);
            if (propertyType == typeof(short))
                return typeof(ToShortVariantConverterNode);
            if (propertyType == typeof(ushort))
                return typeof(ToUShortVariantConverterNode);
            if (propertyType == typeof(long))
                return typeof(ToLongVariantConverterNode);
            if (propertyType == typeof(ulong))
                return typeof(ToULongVariantConverterNode);
            if (propertyType == typeof(float))
                return typeof(ToFloatVariantConverterNode);
            if (propertyType == typeof(float2))
                return typeof(ToFloat2VariantConverterNode);
            if (propertyType == typeof(float3))
                return typeof(ToFloat3VariantConverterNode);
            if (propertyType == typeof(float4))
                return typeof(ToFloat4VariantConverterNode);
            if (propertyType == typeof(quaternion))
                return typeof(ToQuaternionVariantConverterNode);
            if (propertyType == typeof(Entities.Hash128))
                return typeof(ToHash128VariantConverterNode);
            if (propertyType == typeof(int4))
                return typeof(ToInt4VariantConverterNode);

            throw new InvalidDataException();
        }
    }

    internal class ToBoolVariantConverterNode : GraphVariantConverterNode<ToBoolVariantConverterNode, bool, GraphVariantToBoolConverter> {}
    internal class ToIntVariantConverterNode : GraphVariantConverterNode<ToIntVariantConverterNode, int, GraphVariantToIntConverter> {}
    internal class ToUIntVariantConverterNode : GraphVariantConverterNode<ToUIntVariantConverterNode, uint, GraphVariantToUIntConverter> {}
    internal class ToShortVariantConverterNode : GraphVariantConverterNode<ToShortVariantConverterNode, short, GraphVariantToShortConverter> {}
    internal class ToUShortVariantConverterNode : GraphVariantConverterNode<ToUShortVariantConverterNode, ushort, GraphVariantToUShortConverter> {}
    internal class ToLongVariantConverterNode : GraphVariantConverterNode<ToLongVariantConverterNode, long, GraphVariantToLongConverter> {}
    internal class ToULongVariantConverterNode : GraphVariantConverterNode<ToULongVariantConverterNode, ulong, GraphVariantToULongConverter> {}
    internal class ToFloatVariantConverterNode : GraphVariantConverterNode<ToFloatVariantConverterNode, float, GraphVariantToFloatConverter> {}
    internal class ToFloat2VariantConverterNode : GraphVariantConverterNode<ToFloat2VariantConverterNode, float2, GraphVariantToFloat2Converter> {}
    internal class ToFloat3VariantConverterNode : GraphVariantConverterNode<ToFloat3VariantConverterNode, float3, GraphVariantToFloat3Converter> {}
    internal class ToFloat4VariantConverterNode : GraphVariantConverterNode<ToFloat4VariantConverterNode, float4, GraphVariantToFloat4Converter> {}
    internal class ToQuaternionVariantConverterNode : GraphVariantConverterNode<ToQuaternionVariantConverterNode, quaternion, GraphVariantToQuaternionConverter> {}
    internal class ToHash128VariantConverterNode : GraphVariantConverterNode<ToHash128VariantConverterNode, Entities.Hash128, GraphVariantToHash128Converter> {}
    internal class ToInt4VariantConverterNode : GraphVariantConverterNode<ToInt4VariantConverterNode, int4, GraphVariantToInt4Converter> {}

    internal abstract class GraphVariantConverterNode<TFinalNodeDefinition, T, TConverter> : SimulationNodeDefinition<GraphVariantConverterNode<TFinalNodeDefinition, T, TConverter>.SimPorts>
        where TFinalNodeDefinition : NodeDefinition
        where T : struct
        where TConverter : struct, IFromGraphVariantConverter<T>
    {
        internal struct SimPorts : ISimulationPortDefinition
        {
#pragma warning disable 649  // Assigned through internal DataFlowGraph reflection
            public MessageInput<TFinalNodeDefinition, GraphVariant> In;
            public MessageOutput<TFinalNodeDefinition, T> Out;
#pragma warning restore 649
        }

        public struct NodeData : INodeData,
                                 IMsgHandler<GraphVariant>
        {
            public void HandleMessage(MessageContext ctx, in GraphVariant msg)
            {
                var converter = new TConverter();
                ctx.EmitMessage(SimulationPorts.Out, converter.ConvertFrom(msg));
            }
        }
    }
}
