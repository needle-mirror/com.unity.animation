using System;
using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class BaseTransitionSelectorProperties : BaseTransitionProperties
    {
        [SerializeField]
        public bool OverrideTransitionDuration;

        [SerializeField]
        public float OverriddenTransitionDuration;

        [SerializeField]
        public bool OverrideTransitionSynchronization;

        [SerializeField]
        public StateTransitionProperties.TransitionSynchronization OverriddenTransitionSynchronization;

        [SerializeField]
        public bool OverrideSyncTargetRatio;

        [SerializeField]
        public float OverriddenSyncTargetRatio;

        [SerializeField]
        public bool OverrideSyncTagType;

        [SerializeField]
        public string OverriddenSyncTagType;

        [SerializeField]
        public bool OverrideSyncEntryPoint;

        [SerializeField]
        public int OverriddenSyncEntryPoint;

        [SerializeField]
        public bool OverrideAdvanceSourceDuringTransition;

        [SerializeField]
        public bool OverriddenAdvanceSourceDuringTransition; // @DEVNOTE do we want to support that
    }


    [Serializable]
    internal class OnEnterStateSelectorModel : BaseTransitionModel
    {
        [SerializeReference]
        protected BaseTransitionSelectorProperties  m_StoreTransitionProperties = new BaseTransitionSelectorProperties();

        public override BaseTransitionProperties TransitionProperties => m_StoreTransitionProperties;

        public override TransitionType TransitionType => TransitionType.OnEnterSelector;

        public override void DuplicateTransitionProperties(BaseTransitionModel sourceTransition)
        {
            m_StoreTransitionProperties = (BaseTransitionSelectorProperties)sourceTransition.TransitionProperties.Clone();
        }
    }
}
