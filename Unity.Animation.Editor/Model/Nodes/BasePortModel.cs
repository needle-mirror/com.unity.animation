using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace Unity.Animation.Model
{
    class BasePortModel : PortModel
    {
        public string DisplayName { get; set; }
        public string OriginalScriptName { get; set; }
        public string PortDescription { get; set; }
        public int PortGroupInstance { get; set; } = -1;
        public int PortGroupIndex { get; set; }
        public bool IsPortGroupSize { get; set; }

        public bool IsStatic { get; set; }
        public bool IsHidden { get; set; }

        [Serializable]
        public enum PortEvaluationType
        {
            Simulation,
            Rendering,
            Properties
        }

        public PortEvaluationType EvaluationType { get; set; }

        public BasePortModel(string name = null, string uniqueId = null, PortModelOptions options = PortModelOptions.Default, string displayName = "")
        {
            Title = name;
            UniqueName = uniqueId;
            Options = options;
            DisplayName = displayName;
            OriginalScriptName = name;
            if (!string.IsNullOrEmpty(displayName))
                Title = displayName;
        }

        public override bool CreateEmbeddedValueIfNeeded => true;

        public override string ToolTip
        {
            get
            {
                string tooltip = string.Empty;

                if (!string.IsNullOrEmpty(PortDescription))
                {
                    tooltip += $"{PortDescription}\n";
                }

                tooltip += Direction == Direction.Output ? "Output" : "Input";
                tooltip += $" of type {(DataTypeHandle.GetMetadata(GraphModel.Stencil).FriendlyName)}";

                return tooltip;
            }
        }
    }
}
