using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    [RegisterReducer]
    internal class StateMachineRegisterReducer : ContextService.IReducerRegister
    {
        public void RegisterReducers(Store store)
        {
            store.RegisterReducer<CreateStateToStateTransitionAction>(CreateStateToStateTransitionAction.DefaultReducer);
            store.RegisterReducer<CreateTargetStateTransitionAction>(CreateTargetStateTransitionAction.DefaultReducer);
            store.RegisterReducer<CreateStateAction>(CreateStateAction.DefaultReducer);
            store.RegisterReducer<MoveTransitionAnchorAction>(MoveTransitionAnchorAction.DefaultReducer);
        }
    }

    internal abstract class StateMachineStencil : BaseStencil, ISearcherDatabaseProvider
    {
        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return this;
        }

        public override bool MoveNodeDependenciesByDefault => false;

        public override IBuilder Builder => Editor.Builder.Instance;

        public override ITranslator CreateTranslator()
        {
            return new Translator(this);
        }

        LuceneSearcherDatabase s_GraphElementsSearcherDatabase;

        public List<SearcherDatabase> GetReferenceItemsSearcherDatabases()
        {
            return new List<SearcherDatabase>();
        }

        public  List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            if (s_GraphElementsSearcherDatabase == null)
            {
                var db = new GraphElementSearcherDatabase(this, graphModel);
//                    .AddStateMachineStates<CompositorGraphAsset, GraphStateModel>()
//                    .AddStateMachineStates<StateMachineAsset, StateMachineStateModel>();

                s_GraphElementsSearcherDatabase = db.Build();
            }
            return new List<SearcherDatabaseBase> {s_GraphElementsSearcherDatabase};
        }

        public List<SearcherDatabase> GetTypesSearcherDatabases()
        {
            return new List<SearcherDatabase>();
        }

        public List<SearcherDatabaseBase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return new List<SearcherDatabaseBase>();
        }

        public List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return new List<SearcherDatabaseBase>();
        }

        public List<SearcherDatabase> GetVariableTypesSearcherDatabases()
        {
            return new List<SearcherDatabase>();
        }

        public abstract void CreateAssetFromStateModel(BaseStateModel stateModel, IGraphAssetModel assetModel);
    }
}
