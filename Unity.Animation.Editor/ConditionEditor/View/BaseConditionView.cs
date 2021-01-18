using System;
using UnityEngine.UIElements;

namespace Unity.Animation.Editor
{
    internal class BaseConditionView : VisualElement
    {
        public enum QueryType
        {
            IsSet,
            IsNotSet
        }
        public BaseConditionViewModel ViewModel { get; }

        public BaseConditionView(BaseConditionViewModel viewModel)
        {
            ViewModel = viewModel;
            pickingMode = PickingMode.Position;
            UpdateConditionPositionAndSizeFromModel();
        }

        public void UpdateConditionPositionAndSizeFromModel()
        {
            style.left = ViewModel.posX;
            style.top = ViewModel.posY;
            style.width = ViewModel.width;
            style.height = ViewModel.height;
        }

        public virtual Tuple<float, float> GetAutoArrangeDesiredSize()
        {
            return new Tuple<float, float>(0f, 0f);
        }

        public virtual void Arrange(float posX, float posY, float width, float height)
        {
            ViewModel.SetItemBoundary(posX, posY, width, height);
            UpdateConditionPositionAndSizeFromModel();
        }

        bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;
                _isSelected = value;
                if (_isSelected)
                    AddToClassList("compositor-condition-fragment--selected");
                else
                    RemoveFromClassList("compositor-condition-fragment--selected");
            }
        }

        bool _isDebugValid;
        public bool IsDebugValid
        {
            get => _isDebugValid;
            set
            {
                if (_isDebugValid == value)
                    return;
                _isDebugValid = value;
                if (_isDebugValid)
                    AddToClassList("compositor-condition-fragment--valid");
                else
                    RemoveFromClassList("compositor-condition-fragment--valid");
            }
        }

        bool _isDebugInvalid;
        public bool IsDebugInvalid
        {
            get => _isDebugInvalid;
            set
            {
                if (_isDebugInvalid == value)
                    return;
                _isDebugInvalid = value;
                if (_isDebugInvalid)
                    AddToClassList("compositor-condition-fragment--invalid");
                else
                    RemoveFromClassList("compositor-condition-fragment--invalid");
            }
        }
    }
}
