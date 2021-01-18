using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    [RegisterReducer]
    internal class AnimationGraphRegisterReducer : ContextService.IReducerRegister
    {
        public void RegisterReducers(Store store)
        {
            store.RegisterReducer<CreateClipsFromAnimationsAction>(CreateClipsFromAnimationsAction.DefaultReducer);
        }
    }

    [Context("Animation Graph")]
    internal class AnimationGraphStencil : BaseGraphStencil
    {
        private static IAuthoringContext s_AuthoringContext;

        public override IAuthoringContext Context =>
            s_AuthoringContext ?? (s_AuthoringContext = new AnimationAuthoringContext());

        AnimationGraphDragNDropHandler m_graphDragNDropHandler;
        public override IExternalDragNDropHandler DragNDropHandler => m_graphDragNDropHandler ?? (m_graphDragNDropHandler = new AnimationGraphDragNDropHandler());


        [MenuItem("Assets/Create/DOTS/Animation/Blend Graph", priority = MenuUtility.CreateGraphAssetPriority)]
        public static void CreateGraph()
        {
            CreateAnimationGraph<AnimationGraphStencil>("AnimationGraph.asset");
        }
    }
}
