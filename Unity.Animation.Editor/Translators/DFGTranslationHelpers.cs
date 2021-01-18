using System;
using System.Linq;
using System.Reflection;
using Unity.DataFlowGraph;

namespace Unity.Animation.Editor
{
    internal class DFGTranslationHelpers
    {
        private static NodeDefinition GetNodeDefinition<T>(NodeSet set)
            where T : NodeDefinition, new()
        {
            return set.GetDefinition<T>();
        }

        private static NodeDefinition GetNodeDefinitionForType(NodeSetAPI set, Type t)
        {
            var getPortDescriptionMethod = typeof(DFGTranslationHelpers).GetMethod(nameof(GetNodeDefinition), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(t);
            return (NodeDefinition)getPortDescriptionMethod.Invoke(null, new object[] {set});
        }

        private static PortDescription GetPortDescription(NodeSet nodeSet, Type nodeDefinitionType)
        {
            return GetNodeDefinitionForType(nodeSet, nodeDefinitionType).GetStaticPortDescription();
        }

        internal static PortID GetDataInputPortIDValue(NodeSet nodeSet, Type nodeDefinition, string portName, int index = -1)
        {
            PortDescription portDescription = GetPortDescription(nodeSet, nodeDefinition);
            var matchingPort = portDescription.Inputs.Select((port, i) => (port, i)).Where(x => (x.port.Category == PortDescription.Category.Data && x.port.Name == portName)).SingleOrDefault();
            if (matchingPort == default)
            {
                throw new System.InvalidOperationException($"No public data input port with name {portName} found in NodeDef {nodeDefinition.Name}. Verify that it exists and that it is not marked internal/private");
            }
            return new PortID((ushort)matchingPort.i, index == -1 ? UInt16.MaxValue : (ushort)index);
        }

        internal static PortID GetMessageInputPortIDValue(NodeSet nodeSet, Type nodeDefinition, string portName, int index = -1)
        {
            PortDescription portDescription = GetPortDescription(nodeSet, nodeDefinition);
            var matchingPort = portDescription.Inputs.Select((port, i) => (port, i)).Where(x => (x.port.Category == PortDescription.Category.Message && x.port.Name == portName)).SingleOrDefault();
            if (matchingPort == default)
            {
                throw new System.InvalidOperationException($"No public message input port with name {portName} found in NodeDef {nodeDefinition.Name}. Verify that it exists and that it is not marked internal/private");
            }
            return new PortID((ushort)matchingPort.i, index == -1 ? UInt16.MaxValue : (ushort)index);
        }

        internal static PortID GetDataOutputPortIDValue(NodeSet nodeSet, Type nodeDefinition, string portName, int index = -1)
        {
            PortDescription portDescription = GetPortDescription(nodeSet, nodeDefinition);
            var matchingPort = portDescription.Outputs.Select((port, i) => (port, i)).Where(x => (x.port.Category == PortDescription.Category.Data && x.port.Name == portName)).SingleOrDefault();
            if (matchingPort == default)
            {
                throw new System.InvalidOperationException($"No public data output port with name {portName} found in NodeDef {nodeDefinition.Name}. Verify that it exists and that it is not marked internal/private");
            }
            return new PortID((ushort)matchingPort.i, index == -1 ? UInt16.MaxValue : (ushort)index);
        }

        internal static PortID GetMessageOutputPortIDValue(NodeSet nodeSet, Type nodeDefinition, string portName, int index = -1)
        {
            PortDescription portDescription = GetPortDescription(nodeSet, nodeDefinition);
            var matchingPort = portDescription.Outputs.Select((port, i) => (port, i)).Where(x => (x.port.Category == PortDescription.Category.Message && x.port.Name == portName)).SingleOrDefault();
            if (matchingPort == default)
            {
                throw new System.InvalidOperationException($"No public message output port with name {portName} found in NodeDef {nodeDefinition.Name}. Verify that it exists and that it is not marked internal/private");
            }
            return new PortID((ushort)matchingPort.i, index == -1 ? UInt16.MaxValue : (ushort)index);
        }

        private static string GetInputPortForHandler<T>(NodeSet nodeSet, Type nodeDefinitionType)
            where T : class, ITaskPort<T>
        {
            NodeDefinition nodedef = GetNodeDefinitionForType(nodeSet, nodeDefinitionType);
            if (!(nodedef is T))
                throw new InvalidOperationException($"Type {nodeDefinitionType.Name} does not implement {typeof(T).Name}");

            PortDescription portDescription = nodedef.GetStaticPortDescription();
            var n = nodedef as T;
            InputPortID portID = n.GetPort(default);
            var matchingPort = portDescription.Inputs.Select((port, i) => (port, i)).Where(x => (x.port == portID)).SingleOrDefault();
            return portDescription.Inputs[matchingPort.i].Name;
        }

        /// Returns the index for the input port matching corresponding task
        internal static string GetInputPortNameForTask(NodeSet nodeSet, Type nodeDefinitionType, Type taskType)
        {
            if (!taskType.IsAssignableFrom(nodeDefinitionType))
                throw new InvalidOperationException($"Type {nodeDefinitionType.Name} does not implement {taskType.Name}");
            var getInputPortForHandler = typeof(DFGTranslationHelpers).GetMethod(nameof(GetInputPortForHandler), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(taskType);
            return (string)getInputPortForHandler.Invoke(null, new object[] {nodeSet, nodeDefinitionType});
        }

        internal static IRNodeDefinition CreateMessagePassThroughNodeOfType(IR ir, Type itemDataType)
        {
            if (!itemDataType.IsValueType)
                throw new ArgumentException($"Cannot create passthrough for type {itemDataType.Name} : is not a Value Type");

            Type passthroughNodeType = typeof(SimulationPhasePassThroughNode<>).MakeGenericType(itemDataType);
            var node = ir.CreateNode("GeneratedMessagePassThrough", passthroughNodeType);
            return node;
        }

        internal static IRNodeDefinition CreateDataPassThroughNodeOfType(IR ir, Type itemDataType, IAuthoringContext authoringContext)
        {
            if (!itemDataType.IsValueType)
                throw new ArgumentException($"Cannot create passthrough for type {itemDataType.Name} : is not a Value Type");
            Type passthroughNodeType;

            if (itemDataType == authoringContext.DefaultDataType)
            {
                passthroughNodeType = authoringContext.PassThroughForDefaultDataType;
            }
            else
            {
                passthroughNodeType = typeof(DataPhasePassThroughNode<>).MakeGenericType(itemDataType);
            }
            var node = ir.CreateNode("GeneratedDataPassThroughNode", passthroughNodeType);
            return node;
        }

        internal static IRNodeDefinition CreateGraphVariantConverterNodeOfType(IR ir, Type itemDataType)
        {
            Type converterType = GraphVariantConverterNodeFactory.GetGraphVariantConverterNodeType(itemDataType);
            var node = ir.CreateNode("GeneratedGraphVariantConverter", converterType);
            return node;
        }
    }
}
