using System;
using UnityEngine;

namespace Unity.Animation.Model
{
    [Serializable]
    abstract class BaseConditionModel : ICloneable
    {
        public abstract string ToString(int indentLevel = 0);

        [SerializeField]
        internal int indexInParent;

        [SerializeReference]
        internal BaseConditionModel parent;

        public int IndexInParent
        {
            get => indexInParent;
            set => indexInParent = value;
        }

        public BaseConditionModel Parent
        {
            get => parent;
            set => parent = value;
        }

        [SerializeField]
        internal float posX;
        [SerializeField]
        internal float posY;
        [SerializeField]
        internal float width;
        [SerializeField]
        internal float height;

        protected string GetIndentationString(int indentLevel)
        {
            string result = string.Empty;
            if (indentLevel > 0)
                result = new string('\t', indentLevel);
            return result;
        }

        public object Clone()
        {
            var clone = (BaseConditionModel)MemberwiseClone();
            OnClone(ref clone);
            return clone;
        }

        protected virtual void OnClone(ref BaseConditionModel clone) {}
    }
}
