using System;
using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class StateTransitionProperties : BaseTransitionProperties
    {
        public static readonly float kDefaultTransitionDuration = 1.5f;
        [SerializeField]
        public float TransitionDuration = kDefaultTransitionDuration;

        public enum TransitionSynchronization
        {
            None,
            Ratio, // ratio jumps at a specific ratio
            Proportional, // proportional jumps to the source's ratio
            InverseProportional, // jumps to 1-sourceRatio
            EntryPoint,
            Tag
        }

        [SerializeField]
        public TransitionSynchronization SynchronizationMode;

        [SerializeField]
        public float SyncTargetRatio;

        [SerializeField]
        public string SyncTagType;

        [SerializeField]
        public int SyncEntryPoint;

        [SerializeField]
        public bool AdvanceSourceDuringTransition = true;
    }
}
