using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class GlobalTransitionHolder
    {
        static Lazy<GlobalTransitionHolder> s_Instance = new Lazy<GlobalTransitionHolder>();
        internal static GlobalTransitionHolder Instance => s_Instance.Value;

        public struct TransitionInfo
        {
            public IGraphAssetModel GraphAssetModel;
            public ITransitionPropertiesModel Transition;
        }
        public event Action<TransitionInfo> OnCurrentTransitionChangedCallback;

        public IGraphAssetModel GraphAssetModel { get; private set; }
        public ITransitionPropertiesModel CurrentTransition { get; private set; }

        public void SetCurrentTransition(ITransitionPropertiesModel transition, IGraphAssetModel graphAssetModel)
        {
            if (CurrentTransition == transition)
                return;
            CurrentTransition = transition;
            GraphAssetModel = graphAssetModel;
            OnCurrentTransitionChangedCallback?.Invoke(new TransitionInfo(){GraphAssetModel = graphAssetModel, Transition = transition});
        }
    }
}
