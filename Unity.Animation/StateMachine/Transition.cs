using System;
using Unity.Entities;

namespace Unity.Animation.StateMachine
{
    internal enum TransitionSynchronization
    {
        None,
        Ratio,
        Proportional,
        InverseProportional,
        EntryPoint,
        Tag
    }

    [Serializable]
    internal struct TransitionDefinition
    {
        public int                          ID;
        public float                        Duration;
        public int                          SourceStateIndex;
        public int                          TargetStateIndex;
        public int                          AdvanceSourceDuringTransition;
        public TransitionSynchronization    SynchronizationMode;
        public float                        SyncTargetRatio;
        //public StringHash                   SyncTagType;
        public int                          SyncEntryPoint;
        public int                          RootConditionIndex;
    }

    internal enum ConditionFragmentType
    {
        Empty,
        GroupAnd,
        GroupOr,
        BlackboardValue,
        Markup,
        ElapsedTime,
        StateTag,
        EndOfDominantAnimation,
        EvaluationRatio
    }

    internal enum ComparisonOperation
    {
        Equal,
        NotEqual,
        LessThan,
        LessOrEqual,
        GreaterThan,
        GreaterOrEqual
    }

    [Serializable]
    internal struct TransitionConditionFragment
    {
        public ConditionFragmentType Type;
        public int NextSiblingConditionIndex;

        //group
        public int FirstChildConditionIndex;
        //gameplay property
        public GraphVariant CompareValue;
        public ComparisonOperation Operation;
        public int BlackboardValueComponentDataTypeIndex;
        public int BlackboardValueOffset;

        //state tag
        public Hash128 ReferencedStateTagHash;
        //markup
        public Hash128 MarkupHash;
        public bool IsSet;            //@TODO int
        //elapsed time, EndOfDominantAnimation, EvaluationRatio
        public float FloatValue;
    }

    internal struct TransitionPropertiesOverride
    {
        [Flags]
        enum Override
        {
            None = 0,
            Duration = 1 << 0,
            AdvanceSourceDuringTransition = 1 << 1,
            SynchronizationMode = 1 << 2,
            SyncTargetRatio = 1 << 3,
            SyncTagType = 1 << 4,
            SyncEntryPoint = 1 << 5,
        }
        Override m_Flags;

        public bool OverrideDuration
        {
            get => (m_Flags & Override.Duration) == Override.Duration;
            set
            {
                if (value)
                    m_Flags |= Override.Duration;
                else
                {
                    m_Flags &= ~Override.Duration;
                }
            }
        }

        public bool OverrideAdvanceSourceDuringTransition
        {
            get => (m_Flags & Override.AdvanceSourceDuringTransition) == Override.AdvanceSourceDuringTransition;
            set
            {
                if (value)
                    m_Flags |= Override.AdvanceSourceDuringTransition;
                else
                {
                    m_Flags &= ~Override.AdvanceSourceDuringTransition;
                }
            }
        }

        public bool OverrideSynchronizationMode
        {
            get => (m_Flags & Override.SynchronizationMode) == Override.SynchronizationMode;
            set
            {
                if (value)
                    m_Flags |= Override.SynchronizationMode;
                else
                {
                    m_Flags &= ~Override.SynchronizationMode;
                }
            }
        }

        public bool OverrideSyncTargetRatio
        {
            get => (m_Flags & Override.SyncTargetRatio) == Override.SyncTargetRatio;
            set
            {
                if (value)
                    m_Flags |= Override.SyncTargetRatio;
                else
                {
                    m_Flags &= ~Override.SyncTargetRatio;
                }
            }
        }

        public bool OverrideSyncTagType
        {
            get => (m_Flags & Override.SyncTagType) == Override.SyncTagType;
            set
            {
                if (value)
                    m_Flags |= Override.SyncTagType;
                else
                {
                    m_Flags &= ~Override.SyncTagType;
                }
            }
        }

        public bool OverrideSyncEntryPoint
        {
            get => (m_Flags & Override.SyncEntryPoint) == Override.SyncEntryPoint;
            set
            {
                if (value)
                    m_Flags |= Override.SyncEntryPoint;
                else
                {
                    m_Flags &= ~Override.SyncEntryPoint;
                }
            }
        }

        // this can lead to the wrong result if the override is trying to reset the value to default while the global above has been setting it to something else
        public TransitionPropertiesOverride(TransitionDefinition transitionDefinition)
        {
            m_Flags = Override.None;
        }
    }


    internal struct EnterSelectorTransition : IBufferElementData
    {
        public TransitionDefinition         TransitionDefinition;
        public TransitionPropertiesOverride PropertiesOverride;

        internal void UpdateTransitionProperties(ref TransitionDefinition transitionDefinition)
        {
            if (PropertiesOverride.OverrideDuration)
                transitionDefinition.Duration = TransitionDefinition.Duration;
            if (PropertiesOverride.OverrideSynchronizationMode)
                transitionDefinition.SynchronizationMode = TransitionDefinition.SynchronizationMode;
            if (PropertiesOverride.OverrideAdvanceSourceDuringTransition)
                transitionDefinition.AdvanceSourceDuringTransition = TransitionDefinition.AdvanceSourceDuringTransition;
            if (PropertiesOverride.OverrideSyncTargetRatio)
                transitionDefinition.SyncTargetRatio = TransitionDefinition.SyncTargetRatio;
            if (PropertiesOverride.OverrideSyncEntryPoint)
                transitionDefinition.SyncEntryPoint = TransitionDefinition.SyncEntryPoint;
            //if (PropertiesOverride.OverrideSyncTagType)
            //    transitionDefinition.SyncTagType = TransitionDefinition.SyncTagType;
        }
    }

    internal struct GlobalTransition
    {
        public TransitionDefinition TransitionDefinition;
    }

    internal struct OutgoingTransition
    {
        public TransitionDefinition TransitionDefinition;
    }

    internal interface ICondition
    {
    }

    internal struct EmptyCondition : ICondition
    {
    }

    internal struct ElapsedTimeCondition : ICondition
    {
        public float ElapsedTime;
    }
}
