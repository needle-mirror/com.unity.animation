using System;
using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class GlobalTransitionModel : BaseTransitionModel
    {
        [SerializeReference]
        protected StateTransitionProperties m_StoreTransitionProperties = new StateTransitionProperties();

        public override BaseTransitionProperties TransitionProperties => m_StoreTransitionProperties;

        public override TransitionType TransitionType => TransitionType.GlobalTransition;

        public override void DuplicateTransitionProperties(BaseTransitionModel sourceTransition)
        {
            m_StoreTransitionProperties = (StateTransitionProperties)sourceTransition.TransitionProperties.Clone();
        }
    }
}
