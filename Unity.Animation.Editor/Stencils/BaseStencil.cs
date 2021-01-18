using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    internal abstract class BaseStencil : Stencil, IAuthoringContextProvider, IBuilderProvider
    {
        public abstract IAuthoringContext Context { get; }

        public abstract IBuilder Builder { get; }

        public virtual IVariableNodeModel CreateVariableModelForDeclaration(IGraphModel graphModel, IVariableDeclarationModel declarationModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null)
        {
            throw new ArgumentException("Invalid variable declaration model");
        }
    }
}
