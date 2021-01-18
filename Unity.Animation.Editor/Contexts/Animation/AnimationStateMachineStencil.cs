using System;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using Object = UnityEngine.Object;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    [RegisterReducer]
    internal class AnimationStateMachineRegisterReducer : ContextService.IReducerRegister
    {
        public void RegisterReducers(Store store)
        {
            store.RegisterReducer<CreateStatesFromAnimationsAction>(CreateStatesFromAnimationsAction.DefaultReducer);
        }
    }

    [Context("State Machine")]
    internal class AnimationStateMachineStencil : StateMachineStencil
    {
        private static IAuthoringContext s_AuthoringContext;

        AnimationStateMachineDragNDropHandler m_SMDragNDropHandler;
        public override IExternalDragNDropHandler DragNDropHandler => m_SMDragNDropHandler ?? (m_SMDragNDropHandler = new AnimationStateMachineDragNDropHandler());

        public override IAuthoringContext Context =>
            s_AuthoringContext ?? (s_AuthoringContext = new AnimationAuthoringContext());

        [MenuItem("Assets/Create/DOTS/Animation/State Machine", priority = MenuUtility.CreateGraphAssetPriority)]
        public static void CreateStateMachine()
        {
            GraphTemplateHelpers.CreateGraphAsset<StateMachineAsset>(new CreateStateMachineTemplate(typeof(AnimationStateMachineStencil)));
        }

        public override void CreateAssetFromStateModel(BaseStateModel stateModel, IGraphAssetModel assetModel)
        {
            IGraphAssetModel newAsset = null;
            var assetPath = AssetDatabase.GetAssetPath((Object)assetModel);
            int lastSeparatorIndex = assetPath.LastIndexOf('/');
            if (lastSeparatorIndex == -1)
                assetPath = "Assets/";
            else
            {
                assetPath = assetPath.Substring(0, lastSeparatorIndex + 1);
            }
            if (stateModel is StateMachineStateModel)
            {
                assetPath += $"{stateModel.Title}.asset";
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                newAsset = GraphAssetCreationHelpers<StateMachineAsset>.CreateGraphAsset(
                    typeof(AnimationStateMachineStencil),
                    "StateMachine.asset",
                    assetPath
                );
            }
            else if (stateModel is GraphStateModel)
            {
                var stencilType = typeof(AnimationGraphStencil);
                assetPath += $"{stateModel.Title}.asset";
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                newAsset = GraphAssetCreationHelpers<BaseGraphAssetModel>.CreateGraphAsset(
                    stencilType, "AnimationGraph.asset", assetPath);
                ((BaseGraphModel)newAsset.GraphModel).IsStandAloneGraph = false;
                AssetActionHelper.InitTemplate(new CreateGraphTemplate(stencilType), newAsset.GraphModel);
            }

            if (newAsset != null)
            {
                stateModel.StateDefinitionAsset = (BaseAssetModel)newAsset;
                EditorUtility.SetDirty(assetModel as UnityEngine.Object);
            }
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }
    }
}
