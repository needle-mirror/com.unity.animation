using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.DataFlowGraph;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.Profiling;
using State = UnityEditor.GraphToolsFoundation.Overdrive.State;
using Object = UnityEngine.Object;
using Unity.Animation.Hybrid;
using Unity.Animation.Model;

namespace Unity.Animation.Editor.Tests
{
    class TestState : UnityEditor.GraphToolsFoundation.Overdrive.State
    {
        public bool CheckIntegrity { get; set; } = true;

        public TestState(GUID graphViewEditorWindowGUID, Preferences preferences)
            : base(graphViewEditorWindowGUID, preferences) {}

        public override void PostDispatchAction(BaseAction action)
        {
            base.PostDispatchAction(action);

            if (CheckIntegrity && GraphModel != null)
                Assert.IsTrue(GraphModel.CheckIntegrity(Verbosity.Errors));
        }
    }

    internal interface IDummyContextHandler : ITaskPort<IDummyContextHandler>
    {}

    internal struct DummyComponent : Unity.Entities.IComponentData
    {
    }

    internal class DummyOutput : OutputNodeModel
    {
        public override INodeIRBuilder Builder => new DummyNodeIRBuilder(this);

        public override string NodeName => throw new NotImplementedException();

        private class DummyNodeIRBuilder : NodeIRBuilder
        {
            public DummyNodeIRBuilder(DummyOutput model)
                : base(model)
            {}

            public override void Build(IR ir, IBuildContext context)
            {
                throw new NotImplementedException();
            }
        }
    }

    internal class DummyContext : AuthoringContext<DummyOutput>
    {
        public override Type DefaultDataType => typeof(float);
        public override Type PassThroughForDefaultDataType => typeof(DummyPassThroughNode);
        public override Type ContextType => typeof(DummyComponent);
        public override Type GameObjectContextType => typeof(DummyComponent);
        public override Type ContextHandlerType => typeof(IDummyContextHandler);
    }

    internal class DummyGraphStencil : BaseGraphStencil
    {
        public override IAuthoringContext Context => new DummyContext();
    }

    internal class BaseGraphFixture<TStencil> : BaseFixture<BaseGraphAssetModel, BaseGraphModel, TStencil>
        where TStencil : BaseStencil
    {
        protected override void RegisterReducers()
        {
            base.RegisterReducers();
            var graphRegister = new GraphRegisterReducer();
            graphRegister.RegisterReducers(m_Store);
        }
    }

    internal class BaseGraphFixture : BaseGraphFixture<DummyGraphStencil>
    {
    }

    internal abstract class BaseFixture<TAsset, TModel, TStencil>
        where TAsset : BaseAssetModel
        where TModel : BaseModel
        where TStencil : BaseStencil
    {
        protected Store m_Store;
        protected const string k_BlackboardPath = "Assets/blackboardtest.asset";
        protected const string k_GraphPath = "Assets/";

        internal TAsset GraphAsset => (TAsset)m_Store.State.GraphModel.AssetModel;
        internal TModel GraphModel => (TModel)m_Store.State.GraphModel;
        internal TStencil Stencil => (TStencil)GraphModel.Stencil;

        protected virtual bool CreateGraphOnStartup => true;
        protected virtual string[] TestAssemblies { get; } = { "Unity.Animation.Editor.Tests" };
        protected virtual bool WriteOnDisk => false;
        protected Type CreatedGraphType => typeof(TModel);
        protected Type CreatedStencilType => typeof(TStencil);
        protected virtual bool CreateBoundObjectOnStartup => false;
        protected Type CreatedAssetType => typeof(TAsset);

        protected IEdgeModel CreateEdge(BaseNodeModel srcNode, BaseNodeModel dstNode)
        {
            return GraphModel.CreateEdge(
                dstNode.Ports.FirstOrDefault(),
                srcNode.Ports.FirstOrDefault());
        }

        protected DFGNodeModel CreateNode(BaseModel graphModel, Type nodeType)
        {
            var node = graphModel.CreateNode<DFGNodeModel>(
                nodeType.Name,
                preDefineSetup: n => n.NodeType = nodeType
            );
            node.DefineNode();
            return node;
        }

        protected DFGNodeModel CreateNode(Type nodeType)
        {
            return CreateNode(GraphModel, nodeType);
        }

        protected InputComponentFieldVariableModel CreateInputFieldVariableModel(string bindingName, Type bindingType, string fieldName)
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            Assert.IsNotNull(binding);
            var decl = GraphModel.VariableDeclarations.OfType<InputComponentFieldVariableDeclarationModel>()
                .SingleOrDefault(v => v.Identifier == binding.Identifier && v.FieldHandle.Resolve() == fieldName);
            Assert.IsNotNull(decl);
            return CreateInputComponentVariableNodeModel(decl);
        }

        protected InputComponentFieldVariableModel CreateInputComponentVariableNodeModel(InputComponentFieldVariableDeclarationModel model)
        {
            return GraphModel.CreateNode<InputComponentFieldVariableModel>(
                "",
                default,
                null,
                n => n.DeclarationModel = model,
                SpawnFlags.Default);
        }

        static Preferences CreatePreferences()
        {
            var prefs = Preferences.CreatePreferences();
            return prefs;
        }

        [SetUp]
        public virtual void SetUp()
        {
            if (TestAssemblies.Any())
            {
                SetupTestAssemblies();
            }
            Profiler.BeginSample("Compositor Tests SetUp");

            if (WriteOnDisk)
                AssetDatabase.DeleteAsset(k_BlackboardPath);
            m_Store = new Store(new TestState(default, CreatePreferences()));

            RegisterReducers();

            if (CreateGraphOnStartup)
            {
                CreateNewGraphModel("Test");
            }
            Profiler.EndSample();
        }

        protected virtual void RegisterReducers()
        {
            StoreHelper.RegisterDefaultReducers(m_Store);
            ComponentReducers.Register(m_Store);
            BuildReducers.Register(m_Store);
        }

        protected List<string> CreatedAssetsPath = new List<string>();
        protected GameObject CreatedBoundObject;

        protected void LoadGraphAsset(string name)
        {
            m_Store.Dispatch(new LoadGraphAssetAction(name));
        }

        protected void DeleteAllGraphAssets()
        {
            foreach (var path in CreatedAssetsPath)
            {
                AssetDatabase.DeleteAsset(path);
            }
            CreatedAssetsPath.Clear();
            if (CreatedBoundObject != null)
            {
                Object.DestroyImmediate(CreatedBoundObject);
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (TestAssemblies.Any())
            {
                TearDownTestAssemblies();
            }

            UnloadGraph();
            m_Store = null;
            Profiler.enabled = false;

            DeleteAllGraphAssets();
        }

        void UnloadGraph()
        {
            var previousState = m_Store.State;

            if (previousState.GraphModel != null)
                AssetWatcher.Instance.UnwatchGraphAssetAtPath(previousState.GraphModel.AssetModel?.GetPath());

            previousState.UnloadCurrentGraphAsset();
        }

        protected virtual void CreateNewGraphModel(string name)
        {
            var path = $"{k_GraphPath}{name}.asset";
            if (WriteOnDisk)
                CreatedAssetsPath.Add(path);

            if (CreateBoundObjectOnStartup)
            {
                CreatedBoundObject = new GameObject();
                CreatedBoundObject.AddComponent<AnimationGraph>();
            }

            var graphAsset = CreateGraphAsset(name, path);
            m_Store.Dispatch(new LoadGraphAssetAction(graphAsset));
            m_Store.State.WindowState.CurrentGraph = new OpenedGraph(graphAsset, CreatedBoundObject);
            AssumeIntegrity();
        }

        protected virtual IGraphAssetModel CreateGraphAsset(string name, string path)
        {
            return GraphAssetCreationHelpers<TAsset>.CreateInMemoryGraphAsset(CreatedStencilType, name, path);
        }

        protected void CompileGraphAndAddToBoundObject()
        {
            if (CreatedBoundObject == null)
                return;
            var animGraph = CreatedBoundObject.GetComponent<AnimationGraph>();
            var translator = Stencil.CreateTranslator();
            translator.Compile(GraphModel);
            animGraph.Graph = GraphAsset;
        }

        protected void SetupTestAssemblies()
        {
            var testAssemblies = new List<Assembly>();
            foreach (var a in TestAssemblies)
                testAssemblies.Add(Assembly.Load(a));
            DFGService.CachedAssemblies = testAssemblies;
            ContextService.CachedAssemblies = testAssemblies;
        }

        protected void TearDownTestAssemblies()
        {
            ContextService.CachedAssemblies = null;
            DFGService.CachedAssemblies = null;
        }

        protected void AssumeIntegrity()
        {
            if (GraphModel != null)
                Assume.That(GraphModel.CheckIntegrity(Verbosity.Errors));
        }

        protected IEnumerable<NodeModel> GetAllNodes()
        {
            return GraphModel.NodeModels.Cast<NodeModel>();
        }

        protected NodeModel GetNode(int index)
        {
            return GetAllNodes().ElementAt(index);
        }

        protected int GetNodeCount()
        {
            return GraphModel.NodeModels.Count;
        }

        protected IEnumerable<EdgeModel> GetAllEdges()
        {
            return GraphModel.EdgeModels.Cast<EdgeModel>();
        }

        protected EdgeModel GetEdge(int index)
        {
            return GetAllEdges().ElementAt(index);
        }

        protected int GetEdgeCount()
        {
            return GetAllEdges().Count();
        }

        protected IEnumerable<VariableDeclarationModel> GetAllVariableDeclarations()
        {
            return GraphModel.VariableDeclarations.Cast<VariableDeclarationModel>();
        }

        protected VariableDeclarationModel GetVariableDeclaration(int index)
        {
            return GetAllVariableDeclarations().ElementAt(index);
        }

        protected int GetVariableDeclarationCount()
        {
            return GetAllVariableDeclarations().Count();
        }

        protected IVariableDeclarationModel GetGraphVariableDeclaration(string fieldName)
        {
            return GraphModel.VariableDeclarations.Single(f => f.Title == fieldName);
        }

        protected IVariableDeclarationModel CreateGraphVariableDeclaration(string fieldName, Type type)
        {
            int prevCount = GraphModel.VariableDeclarations.Count();

            m_Store.Dispatch(new CreateGraphVariableDeclarationAction(fieldName, false, type.GenerateTypeHandle()));

            Assert.AreEqual(prevCount + 1, GraphModel.VariableDeclarations.Count());
            IVariableDeclarationModel decl = GetGraphVariableDeclaration(fieldName);
            Assume.That(decl, Is.Not.Null);
            Assume.That(decl.Title, Is.EqualTo(fieldName));
            return decl;
        }
    }

    internal class DummyPassThroughNode
        : SimulationKernelNodeDefinition<DummyPassThroughNode.SimDefs, DummyPassThroughNode.KernelDefs>,
        IDummyContextHandler
    {
        public InputPortID GetPort(NodeHandle handle)
        {
            return (InputPortID)SimulationPorts.Context;
        }

        public struct SimDefs : ISimulationPortDefinition
        {
#pragma warning disable 649
            public MessageInput<DummyPassThroughNode, int> Context;
#pragma warning restore 649
        }

        public struct NodeData : INodeData,
                                 IMsgHandler<int>
        {
            public void HandleMessage(MessageContext ctx, in int msg)
            {
                throw new NotImplementedException();
            }
        }

        public struct KernelDefs : IKernelPortDefinition
        {
#pragma warning disable 649
            public DataInput<DummyPassThroughNode, float> Input;
            public DataOutput<DummyPassThroughNode, float> Output;
#pragma warning restore 649
        }

        public struct KernelData : IKernelData
        {
        }

        public struct Kernel : IGraphKernel<KernelData, KernelDefs>
        {
            public void Execute(RenderContext context, in KernelData data, ref KernelDefs ports)
            {
                context.Resolve(ref ports.Output) = context.Resolve(ports.Input);
            }
        }
    }
}
