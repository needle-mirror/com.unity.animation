using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.DataFlowGraph.Attributes;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Searcher;
using ITypeMetadata = UnityEditor.GraphToolsFoundation.Overdrive.ITypeMetadata;
using StringExtensions = UnityEditor.GraphToolsFoundation.Overdrive.StringExtensions;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal static class SearcherFilterExtension
    {
        public const string k_MessageInputField = "message_input";
        public const string k_DataInputField = "data_input";
        public const string k_MessageOutputField = "message_output";
        public const string k_DataOutputField = "data_output";
        internal static SearcherFilter WithCompositorNodes(this SearcherFilter filter)
        {
            // filter.RegisterNode(data => typeof(CompositorBaseNodeModel).IsAssignableFrom(data.Type));
            return filter;
        }

        internal static SearcherFilter WithDataFlowGraphNodes(this SearcherFilter filter, BaseGraphStencil stencil)
        {
            var dataFlowGraphTypes = stencil.GetDataFlowGraphNodeTypes();

            //TODO GTFOConvert - keep as reference until lucene filter approach is validated
            /*
            filter.Register(data =>
            {
                if (data is NodeSearcherItemData nodeItem)
                {
                    return nodeItem.Type != null && dataFlowGraphTypes.Contains(nodeItem.Type);
                }

                return false;
            });
            filter.Register(data =>
            {
                if (data is NodeSearcherItemData nodeItem)
                {
                    return nodeItem.Type == typeof(CompositorSubGraphNodeModel);
                }

                return false;
            });
            */

            return filter;
        }

        internal static SearcherFilter WithDataFlowGraphNodesWithInputPort(this SearcherFilter filter, BaseGraphStencil stencil, DFGService.PortUsage usage, Type portType)
        {
            return filter.WithFieldQuery(usage == DFGService.PortUsage.Data ? k_DataInputField : k_MessageInputField, portType.AssemblyQualifiedName.GetHashCode());
        }

        internal static SearcherFilter WithDataFlowGraphNodesWithOutputPort(this SearcherFilter filter, BaseGraphStencil stencil, DFGService.PortUsage usage, Type portType)
        {
            return filter.WithFieldQuery(usage == DFGService.PortUsage.Data ? k_DataOutputField : k_MessageOutputField, portType.AssemblyQualifiedName.GetHashCode());
        }

        internal static SearcherFilter WithDataPortTypes(this SearcherFilter filter)
        {
            return filter
                .WithFieldQuery(k_DataInputField, null)
                .WithFieldQuery(k_DataOutputField, null);
        }

        internal static SearcherFilter WithMessagePortTypes(this SearcherFilter filter)
        {
            return filter
                .WithFieldQuery(k_MessageInputField, null)
                .WithFieldQuery(k_MessageOutputField, null);
        }
    }

    internal static class SearcherDatabaseExtensions
    {
        public static GraphElementSearcherDatabase AddNodesWithSearcherItemAttribute(this GraphElementSearcherDatabase db, ContextAvailability contextAvailability)
        {
            var types = TypeCache.GetTypesWithAttribute<SearcherItemAttribute>();
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes<ContextSearchAttribute>().ToList();
                if (!attributes.Any())
                    continue;

                foreach (var attribute in attributes)
                {
                    if (!attribute.StencilType.IsInstanceOfType(db.Stencil))
                        continue;

                    var name = attribute.Path.Split('/').Last();
                    var path = attribute.Path.Remove(attribute.Path.LastIndexOf('/') + 1);

                    if (attribute.ContextAvailability == contextAvailability)
                    {
                        var node = new GraphNodeModelSearcherItem(
                            new NodeSearcherItemData(type),
                            data => data.CreateNode(type, name),
                            name
                        );

                        db.Items.AddAtPath(node, path);
                        break;
                    }
                }
            }

            return db;
        }

        internal static GraphElementSearcherDatabase AddStandAloneCompatibleGraphNodes(
            this GraphElementSearcherDatabase db,
            BaseGraphStencil stencil)
        {
            return db.AddNodesWithSearcherItemAttribute(ContextAvailability.StandAloneGraph);
        }

        internal static GraphElementSearcherDatabase AddStateMachineCompatibleGraphNodes(
            this GraphElementSearcherDatabase db,
            BaseGraphStencil stencil)
        {
            return db.AddNodesWithSearcherItemAttribute(ContextAvailability.StateMachineGraph);
        }

        internal static GraphElementSearcherDatabase AddDataFlowGraphNodes(
            this GraphElementSearcherDatabase db,
            IEnumerable<Type> types,
            Stencil stencil)
        {
            foreach (Type t in types)
            {
                var nodeName = DFGService.FormatNodeName(t.Name);
                var node = new GraphNodeModelSearcherItem(
                    new NodeSearcherItemData(t),
                    data => data.CreateNode<DFGNodeModel>(
                        t.Name,
                        n => n.NodeType = t
                    ),
                    nodeName);
                var nodeDefinition = t.GetCustomAttributes(
                    typeof(NodeDefinitionAttribute), false).FirstOrDefault() as NodeDefinitionAttribute;
                if (nodeDefinition != null && nodeDefinition.IsHidden)
                    continue;
                db.Items.AddAtPath(node,
                    nodeDefinition != null && !string.IsNullOrEmpty(nodeDefinition.Category) ? FormatNodeSearcherPath(nodeDefinition.Category) :
                    string.IsNullOrEmpty(t.Namespace) ?
                    StringExtensions.Nicify(t.Assembly.GetName().Name.Replace('.', '/')) :
                    StringExtensions.Nicify(t.Namespace.Replace('.', '/')));
            }

            return db;
        }

        internal static GraphElementSearcherDatabase AddCreateSubGraphNode<TGraphAssetModel, TNodeModel>(
            this GraphElementSearcherDatabase db, string name, Type stencilType)
            where TGraphAssetModel : IGraphAssetModel
            where TNodeModel : SubNodeModel<TGraphAssetModel>
        {
            var node = new GraphNodeModelSearcherItem(
                new NodeSearcherItemData(typeof(TNodeModel)),
                data => data.CreateNode<TNodeModel>(name,
                    model =>
                    {
                        model.StencilType = stencilType;
                    }),
                $"Create new {name}");
            db.Items.AddAtPath(node, "Sub Graphs");
            return db;
        }

        internal static GraphElementSearcherDatabase AddCompositorSubNodes<TGraphAssetModel, TNodeModel>(
            this GraphElementSearcherDatabase db)
            where TGraphAssetModel : BaseGraphAssetModel
            where TNodeModel : SubNodeModel<TGraphAssetModel>
        {
            var assets = AssetDatabase.FindAssets($"t:{typeof(TGraphAssetModel).Name}");
            foreach (var assetGUID in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetGUID);
                var name = Path.GetFileNameWithoutExtension(path);
                var node = new GraphNodeModelSearcherItem(
                    new NodeSearcherItemData(typeof(TNodeModel)),
                    data => data.CreateNode<TNodeModel>(
                        name,
                        model =>
                        {
                            model.GraphAsset = AssetDatabase.LoadAssetAtPath<TGraphAssetModel>(path);
                        }
                    ),
                    name);
                db.Items.AddAtPath(node, "Sub Graphs");
            }

            return db;
        }

        static string BuildPath(string parentName, ITypeMetadata meta, Stencil stencil)
        {
            string declaringTypePath = String.Empty;
            var declaringType = meta.TypeHandle.Resolve().DeclaringType;

            if (declaringType != null)
            {
                declaringTypePath = "/" + TypeExtensions.FriendlyName(declaringType) + "/";
            }

            var formattedNamespace = meta.Namespace.Replace(".", "/");
            if (!string.IsNullOrEmpty(formattedNamespace))
                return parentName + "/" + formattedNamespace + declaringTypePath;
            return !string.IsNullOrEmpty(parentName) && !string.IsNullOrEmpty(declaringTypePath) ?
                parentName + "/" + declaringTypePath : String.Empty;
        }

        internal static string FormatNodeSearcherPath(string nodePath)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                nodePath, "([ ]{0,})([/])([ ]{0,})", "/",
                System.Text.RegularExpressions.RegexOptions.Compiled);
        }
    }
}
