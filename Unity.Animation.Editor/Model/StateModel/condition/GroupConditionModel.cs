using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Animation.Model
{
    [Serializable]
    internal class GroupConditionModel : BaseConditionModel
    {
        public static float DefaultWidth = 300f;
        public static float DefaultHeight = 130f;
        public static float TitleHeight = 40.0f;

        public GroupConditionModel()
        {
            GroupOperation = Operation.And;
            ListSubConditions = new List<BaseConditionModel>();
        }

        public enum Operation
        {
            And,
            Or
        }

        [SerializeField]
        public Operation GroupOperation;

        public enum DisplayMode
        {
            Math,
            Human
        }

        public static DisplayMode DefaultDisplayMode = DisplayMode.Math;

        public string GetLabel()
        {
            if (DefaultDisplayMode == DisplayMode.Math)
            {
                return GroupOperation == Operation.And ? "AND" : "OR";
            }
            return GroupOperation == Operation.And ? "All conditions need to be true" : "One condition needs to be true";
        }

        [SerializeReference]
        public List<BaseConditionModel> ListSubConditions;

        public void InsertCondition(BaseConditionModel condition, int position = -1)
        {
            condition.Parent = this;
            if (position == -1 || position > ListSubConditions.Count)
            {
                condition.IndexInParent = ListSubConditions.Count;
                ListSubConditions.Add(condition);
                return;
            }
            ListSubConditions.Insert(position, condition);
            for (int i = position; i < ListSubConditions.Count; ++i)
            {
                ListSubConditions[i].IndexInParent = i;
            }
        }

        public void RemoveCondition(BaseConditionModel condition)
        {
            Assert.IsTrue(condition.Parent == this);
            Assert.IsTrue(condition.IndexInParent < ListSubConditions.Count);
            Assert.IsTrue(condition == ListSubConditions[condition.IndexInParent]);
            ListSubConditions.RemoveAt(condition.IndexInParent);
            for (int i = condition.IndexInParent; i < ListSubConditions.Count; ++i)
                ListSubConditions[i].IndexInParent = i;
        }

        private void MoveCondition(BaseConditionModel condition, int position)
        {
            if (condition is GroupConditionModel)
            {
                BaseConditionModel target = this;
                while (target != condition && target != null)
                {
                    target = target.Parent;
                }
                if (target == condition)
                    return;
            }
            var originalGroup = condition.Parent as GroupConditionModel;
            Assert.IsTrue(originalGroup != null);
            int originalIndexInGroup = condition.IndexInParent;
            // if moving to the same group, we're removing the condition first so the position we asked for might be different as well
            if (originalGroup == this && position != -1 && originalIndexInGroup < position)
            {
                --position;
            }
            originalGroup.RemoveCondition(condition);
            InsertCondition(condition, position);
        }

        public void MoveConditions(List<BaseConditionModel> conditions, int position = -1)
        {
            int absolutePosition = position;
            foreach (var condition in conditions)
            {
                MoveCondition(condition, position == -1 ? -1 : absolutePosition);
                ++absolutePosition;
            }
        }

        public override string ToString(int indentLevel = 0)
        {
            string indentLevelStr = GetIndentationString(indentLevel);
            StringBuilder sb = new StringBuilder();
            sb.Append(indentLevelStr);
            sb.Append(GetLabel());
            sb.AppendLine();
            foreach (var condition in ListSubConditions)
            {
                sb.Append(condition.ToString(indentLevel + 1));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        protected override void OnClone(ref BaseConditionModel clone)
        {
            GroupConditionModel groupConditionClone = (GroupConditionModel)clone;
            groupConditionClone.ListSubConditions = new List<BaseConditionModel>();
            foreach (var subCondition in ListSubConditions)
            {
                groupConditionClone.InsertCondition((BaseConditionModel)subCondition.Clone());
            }
        }
    }
}
