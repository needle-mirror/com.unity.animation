using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Animation.Hybrid
{
    [DisallowMultipleComponent]
    public class AnimationGraph : MonoBehaviour
    {
        [Serializable]
        internal class ObjectBindingEntry
        {
            public PortTargetGUID TargetGUID;
            public UnityEngine.Object Value;
        }

        [Serializable]
        internal class InputBindingEntry
        {
            public string Identification;
            public Component Value;
        }

        AnimationGraph()
        {
            var phases = ConversionService.GetPhases();
            if (phases.Any())
                PhaseIdentification = phases[0].Type.AssemblyQualifiedName;
        }

        [SerializeField]
        internal UnityEngine.Object Graph;
        [SerializeField]
        internal Component Context;
        [SerializeField]
        internal List<ObjectBindingEntry> ExposedObjects = new List<ObjectBindingEntry>();
        [SerializeField]
        internal string PhaseIdentification;
        [SerializeField]
        internal bool ShowBindings = false;
        [SerializeField]
        internal List<InputBindingEntry> Inputs = new List<InputBindingEntry>();
    }
}
