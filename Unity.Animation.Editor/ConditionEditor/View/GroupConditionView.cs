using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal class GroupConditionView : BaseConditionView
    {
        public GroupConditionViewModel GroupViewModel => (GroupConditionViewModel)ViewModel;

        internal List<BaseConditionView> SubConditions { get; } = new List<BaseConditionView>();

        VisualElement SurfaceElement { get; }
        Label TitleLabel { get; }
        VisualElement TitleElement { get; }
        VisualElement InsertionPointElement { get; }

        public GroupConditionView(GroupConditionViewModel viewmodel)
            : base(viewmodel)
        {
            AddToClassList("condition-editor-group-fragment");
            AddToClassList(viewmodel.IsAndGroup ? "condition-editor-group-fragment--and" : "condition-editor-group-fragment--or");
            TitleElement = new VisualElement();
            TitleElement.name = "title";
            TitleElement.AddToClassList("condition-editor-group-fragment__title");
            Add(TitleElement);
            TitleLabel = new Label();
            TitleLabel.name = "label";
            TitleLabel.AddToClassList("condition-editor-group-fragment__title-label");
            if (viewmodel.IsAndGroup)
            {
                TitleLabel.text = "And";
            }
            else
            {
                TitleLabel.text = "Or";
            }
            TitleElement.Add(TitleLabel);
            SurfaceElement = new VisualElement();
            SurfaceElement.name = "surface";
            SurfaceElement.AddToClassList("condition-editor-group-fragment__surface");
            Add(SurfaceElement);
            InsertionPointElement = new VisualElement();
            InsertionPointElement.name = "insertion-point";
            InsertionPointElement.AddToClassList("condition-editor-group-fragment__insertion-point");
            SurfaceElement.Add(InsertionPointElement);
        }

        public void ClearInsertionTarget()
        {
            InsertionPointElement.RemoveFromClassList("condition-editor-group-fragment__insertion-point--visible");
            InsertionIndex = -1;
        }

        public Vector2 WorldToSurfaceLocal(Vector2 pos)
        {
            return SurfaceElement.WorldToLocal(pos);
        }

        internal int InsertionIndex { get; private set; }
        public void SetInsertionTarget(Vector2 ptOnSurface)
        {
            InsertionPointElement.style.left = layout.width / 2 - 10;

            int subConditionsCount = SubConditions.Count;
            int insertIndex = 0;

//            if (ptOnSurface.y < 0.0f)
//            {
//                insertIndex = subConditionsCount;
//            }
//            else
            {
                foreach (var subcondition in SubConditions)
                {
                    var middlePoint = subcondition.ViewModel.posY + subcondition.ViewModel.height / 2.0f;
                    if (ptOnSurface.y < middlePoint)
                    {
                        InsertionPointElement.style.top = subcondition.ViewModel.posY - MarginBetweenItems / 2.0f - (insertIndex == 0 ? 1.0f : 2.0f);
                        break;
                    }

                    ++insertIndex;
                }
            }

            if (subConditionsCount == 0)
            {
                InsertionPointElement.style.top = (layout.height - GroupConditionModel.TitleHeight) / 2;
            }
            else if (insertIndex == subConditionsCount)
            {
                InsertionPointElement.style.top = SubConditions[subConditionsCount - 1].ViewModel.posY + SubConditions[subConditionsCount - 1].ViewModel.height + MarginBetweenItems / 2.0f - 1.0f;
            }

            InsertionIndex = insertIndex;

            InsertionPointElement.AddToClassList("condition-editor-group-fragment__insertion-point--visible");
        }

        public void UpdateOperationFromModel()
        {
            if (GroupViewModel.IsAndGroup)
            {
                AddToClassList("condition-editor-group-fragment--and");
                RemoveFromClassList("condition-editor-group-fragment--or");
                TitleLabel.text = "And";
            }
            else
            {
                AddToClassList("condition-editor-group-fragment--or");
                RemoveFromClassList("condition-editor-group-fragment--and");
                TitleLabel.text = "Or";
            }
        }

        public void Insert(BaseConditionView childCondition, int position = -1)
        {
            SurfaceElement.Add(childCondition);
            SubConditions.Insert(position == -1 ? SubConditions.Count : position, childCondition);
        }

        public void Remove(BaseConditionView childCondition)
        {
            SurfaceElement.Remove(childCondition);
            SubConditions.Remove(childCondition);
        }

        static float TopMargin = 10.0f;
        static float BottomMargin = 10.0f;
        static float LeftMargin = 10.0f;
        static float RightMargin = 10.0f;
        static float MarginBetweenItems = 10.0f;
        static float GroupBorder = 4.0f;

        public override Tuple<float, float> GetAutoArrangeDesiredSize()
        {
            float desiredWidth = 0.0f;
            float desiredHeight = 0.0f;

            bool first = true;
            foreach (var condition in SubConditions)
            {
                if (!first)
                    desiredHeight += MarginBetweenItems;
                var conditionSize = condition.GetAutoArrangeDesiredSize();
                desiredWidth = Math.Max(desiredWidth, conditionSize.Item1);
                desiredHeight += conditionSize.Item2;
                first = false;
            }

            desiredWidth += LeftMargin + RightMargin + GroupBorder;
            desiredHeight += TopMargin + BottomMargin + GroupConditionModel.TitleHeight + GroupBorder;
            return new Tuple<float, float>(Math.Max(GroupConditionModel.DefaultWidth, desiredWidth), Math.Max(GroupConditionModel.DefaultHeight, desiredHeight));
        }

        public override void Arrange(float posX, float posY, float width, float height)
        {
            base.Arrange(posX, posY, width - GroupBorder, height);
            float currentY = TopMargin;
            foreach (var condition in SubConditions)
            {
                var itemDesiredSize = condition.GetAutoArrangeDesiredSize();
                condition.Arrange(LeftMargin, currentY, width - LeftMargin - RightMargin - GroupBorder, itemDesiredSize.Item2 - GroupBorder);
                currentY += itemDesiredSize.Item2;
                currentY += MarginBetweenItems;
            }
        }
    }
}
