using System;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;

using UnityEngine;
using ICloneable = System.ICloneable;

namespace Unity.Animation.Model
{
    internal enum TransitionType
    {
        GlobalTransition,
        SelfTransition,
        OnEnterSelector,
        StateToStateTransition
    }

    internal interface ITransitionPropertiesModel
    {
        BaseTransitionProperties TransitionProperties { get; }
        TransitionType TransitionType { get; }
    }

    [Serializable]
    internal class BaseTransitionProperties : ICloneable
    {
        public BaseTransitionProperties()
        {
            if (Condition == null)
            {
                Condition = new GroupConditionModel() { width = GroupConditionModel.DefaultWidth, height = GroupConditionModel.DefaultHeight };
            }
        }

        [SerializeReference]
        public GroupConditionModel Condition;

        [SerializeField]
        public bool Enable = true;

        [SerializeField]
        public SerializableGUID TransitionId = new SerializableGUID(){GUID = GUID.Generate()};

        public object Clone()
        {
            var clone = (BaseTransitionProperties)MemberwiseClone();
            clone.Condition = new GroupConditionModel() { width = GroupConditionModel.DefaultWidth, height = GroupConditionModel.DefaultHeight };
            foreach (var sourceCondition in Condition.ListSubConditions)
            {
                clone.Condition.InsertCondition((BaseConditionModel)sourceCondition.Clone());
            }
            return clone;
        }
    }
}
