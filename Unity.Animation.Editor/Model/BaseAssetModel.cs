using System;
using System.IO;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Hybrid;

namespace Unity.Animation.Model
{
    [Serializable]
    internal abstract class BaseAssetModel : GraphAssetModel, ICompiledGraphProvider
    {
        [SerializeField, HideInInspector]
        CompiledGraph m_CompiledGraph = new CompiledGraph();

        public CompiledGraph CompiledGraph => m_CompiledGraph;

        public string AssetId
        {
            get
            {
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out var guid, out long localId))
                    return string.Empty;
                return guid;
            }
        }

        public override string SourceFilePath => Path.Combine(Path.Combine(Environment.CurrentDirectory, Path.Combine("Assets", "Runtime", "Animation")), Name + ".cs");

        public override IBlackboardGraphModel BlackboardGraphModel { get; }

        protected BaseAssetModel()
        {
            BlackboardGraphModel = new BlackboardGraphModel { AssetModel = this };
        }
    }
}
