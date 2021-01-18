using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.Overdrive;
using IVariableDeclarationModel = UnityEditor.GraphToolsFoundation.Overdrive.IVariableDeclarationModel;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    [RegisterReducer]
    internal class GraphRegisterReducer : ContextService.IReducerRegister
    {
        public void RegisterReducers(Store store)
        {
            store.RegisterReducer<SetNumberOfPortGroupInstanceAction>(SetNumberOfPortGroupInstanceAction.DefaultReducer);
        }
    }


    internal abstract class BaseGraphStencil : BaseStencil, IIndexableSearcherDatabaseProvider
    {
        static IEnumerable<Type> s_DataFlowGraphNodeTypes;

        LuceneSearcherDatabase s_StateMachineGraphElementsSearcherDatabase;
        LuceneSearcherDatabase s_StandAloneGraphElementsSearcherDatabase;
        DFGSearchFilterProvider s_DataFlowSearchFilter; // TODO : rename filter
        List<SearcherDatabase> s_DataPortTypesSearcherDatabases;
        List<SearcherDatabase> s_MessagePortTypesSearcherDatabases;
        List<SearcherDatabase> s_AllPortTypesSearcherDatabases;

        public override IBuilder Builder => Editor.Builder.Instance;

        LuceneSearcherDatabase m_PortSearcherDatabase;

        static List<Type> s_DataPortTypes;
        static List<Type> s_MessagePortTypes;
        static List<Type> s_AllPortTypes;

        DragNDropHandler m_DragNDropHandler;
        public override IExternalDragNDropHandler DragNDropHandler => m_DragNDropHandler ?? (m_DragNDropHandler = new DragNDropHandler());

        internal IEnumerable<Type> GetDataFlowGraphNodeTypes()
        {
            return s_DataFlowGraphNodeTypes ?? (s_DataFlowGraphNodeTypes = DFGService.GetAvailableTypes(sorted: true));
        }

        internal IEnumerable<Type> GetDataPortTypes()
        {
            return s_DataPortTypes ?? (s_DataPortTypes = DFGService.GetAvailablePortDataTypes(DFGService.PortUsage.Data));
        }

        internal IEnumerable<Type> GetMessagePortTypes()
        {
            return s_MessagePortTypes ?? (s_MessagePortTypes = DFGService.GetAvailablePortDataTypes(DFGService.PortUsage.Message));
        }

        internal List<Type> GetAllPortTypes()
        {
            if (s_AllPortTypes != null)
                return s_AllPortTypes;

            s_AllPortTypes = new List<Type>();
            s_AllPortTypes.AddRange(GetDataPortTypes());
            s_AllPortTypes.AddRange(GetMessagePortTypes());
            s_AllPortTypes = s_AllPortTypes.Distinct().ToList(); //need to make sure to remove items present twice in the list

            return s_AllPortTypes;
        }

        public List<SearcherDatabase> GetMessagePortsSearcherDatabases()
        {
            return s_MessagePortTypesSearcherDatabases ?? (s_MessagePortTypesSearcherDatabases = new List<SearcherDatabase> { TypeSearcherDatabase.FromTypes(this, GetMessagePortTypes())});
        }

        public List<SearcherDatabase> GetDataPortsSearcherDatabases()
        {
            return s_DataPortTypesSearcherDatabases ?? (s_DataPortTypesSearcherDatabases = new List<SearcherDatabase> { TypeSearcherDatabase.FromTypes(this, GetDataPortTypes())});
        }

        public List<SearcherDatabase> GetAllPortsSearcherDatabases()
        {
            return s_AllPortTypesSearcherDatabases ?? (s_AllPortTypesSearcherDatabases = new List<SearcherDatabase> { TypeSearcherDatabase.FromTypes(this, GetAllPortTypes())});
        }

        public override bool MoveNodeDependenciesByDefault => false;

        public static void CreateAnimationGraph<TStencil>(string assetName)
            where TStencil : BaseGraphStencil
        {
            GraphTemplateHelpers.CreateGraphAsset<BaseGraphAssetModel>(new CreateGraphTemplate(typeof(TStencil)));
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return this;
        }

        public override ITranslator CreateTranslator()
        {
            return new Translator(this);
        }

        public override IVariableNodeModel CreateVariableModelForDeclaration(IGraphModel graphModel, IVariableDeclarationModel declarationModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null)
        {
            if (declarationModel is InputComponentFieldVariableDeclarationModel inputComponentDecl)
            {
                if ((graphModel as BaseModel).TryGetComponentBinding(inputComponentDecl.Identifier, out var binding))
                {
                    return graphModel.CreateNode<InputComponentFieldVariableModel>(
                        declarationModel.DisplayTitle, position, guid,
                        v =>
                        {
                            v.DeclarationModel = declarationModel;
                            v.ComponentName = binding.Name;
                        },
                        spawnFlags);
                }
            }

            throw new ArgumentException("Invalid variable declaration model");
        }

        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            if (originalModel is InputComponentFieldVariableModel variableModel)
            {
                if (!(graph as BaseGraphModel).TryGetComponentBinding(
                    (variableModel.DeclarationModel as InputComponentFieldVariableDeclarationModel).Identifier, out var input))
                    return false;
            }
            else if (originalModel is VariableNodeModel && originalModel.GraphModel != graph)
            {
                return false;
            }

            return true;
        }

        public List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            if (((BaseGraphModel)graphModel).IsStandAloneGraph)
            {
                if (s_StandAloneGraphElementsSearcherDatabase == null)
                {
                    var db =
                        new GraphElementSearcherDatabase(this, graphModel)
                            .AddStandAloneCompatibleGraphNodes(this);

                    s_StandAloneGraphElementsSearcherDatabase = db.Build();
                }
                return new List<SearcherDatabaseBase> {s_StandAloneGraphElementsSearcherDatabase};
            }
            else
            {
                if (s_StateMachineGraphElementsSearcherDatabase == null)
                {
                    var db =
                        new GraphElementSearcherDatabase(this, graphModel)
                            .AddStateMachineCompatibleGraphNodes(this);
                    s_StateMachineGraphElementsSearcherDatabase = db.Build();
                }
                return new List<SearcherDatabaseBase> {s_StateMachineGraphElementsSearcherDatabase};
            }
        }

        public List<SearcherDatabaseBase> GetReferenceItemsSearcherDatabases(IGraphModel graphModel)
        {
            return GetGraphElementsSearcherDatabases(graphModel);
        }

        public List<SearcherDatabase> GetVariableTypesSearcherDatabases()
        {
            return new List<SearcherDatabase>
            {
                TypeSearcherDatabase.FromTypes(this, GetAllPortTypes())
            };
        }

        public List<SearcherDatabaseBase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return new List<SearcherDatabaseBase> { new GraphElementSearcherDatabase(this, graphModel).Build() };
        }

        public List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IPortModel portModel) => s_DynamicSearcherDatabases;

        public List<LuceneSearcherDatabase> GetTypeMembersSearcherDatabases(TypeHandle typeHandle, IGraphModel graphModel)
        {
            return new List<LuceneSearcherDatabase> { new GraphElementSearcherDatabase(this, graphModel).Build() };
        }

        static List<SearcherDatabaseBase> s_DynamicSearcherDatabases = new List<SearcherDatabaseBase>();

        public override ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return
                s_DataFlowSearchFilter ?? (s_DataFlowSearchFilter =
                        new DFGSearchFilterProvider(this));
        }

        public override ISearcherAdapter GetSearcherAdapter(IGraphModel graphModel, string title, IPortModel contextPortModel = null)
        {
            return new GraphSearcherAdapter(graphModel, title);
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            var type = typeHandle.Resolve();

            var constantNodeModelType = Context.GetDomainConstantEditorType(type);
            if (constantNodeModelType != null)
                return constantNodeModelType;

            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        public bool Index<T>(GraphNodeModelSearcherItem item, IGraphElementModel model, ref T indexer) where T : struct, IDocumentIndexer
        {
            if (model is BaseNodeModel nodeModel)
            {
                foreach (var p in nodeModel.GetInputMessagePorts())
                    indexer.IndexField(SearcherFilterExtension.k_MessageInputField, p.DataTypeHandle.Resolve().AssemblyQualifiedName.GetHashCode());

                foreach (var p in nodeModel.GetOutputMessagePorts())
                    indexer.IndexField(SearcherFilterExtension.k_MessageOutputField, p.DataTypeHandle.Resolve().AssemblyQualifiedName.GetHashCode());

                foreach (var p in nodeModel.GetInputDataPorts())
                    indexer.IndexField(SearcherFilterExtension.k_DataInputField, p.DataTypeHandle.Resolve().AssemblyQualifiedName.GetHashCode());

                foreach (var p in nodeModel.GetOutputDataPorts())
                    indexer.IndexField(SearcherFilterExtension.k_DataOutputField, p.DataTypeHandle.Resolve().AssemblyQualifiedName.GetHashCode());
                return true;
            }

            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class ContextSearchAttribute : Attribute
    {
        internal Type StencilType { get; }
        internal string Path { get; }
        internal ContextAvailability ContextAvailability { get; }

        internal ContextSearchAttribute(Type stencilType, ContextAvailability context, string path)
        {
            StencilType = stencilType; //Necessary?
            Path = path;
            ContextAvailability = context;
        }
    }

    internal enum ContextAvailability
    {
        StandAloneGraph,
        StateMachineGraph
    }
}
