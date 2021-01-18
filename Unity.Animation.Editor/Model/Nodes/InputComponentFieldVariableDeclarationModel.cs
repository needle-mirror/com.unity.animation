using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.Assertions;
using System.Reflection;
using Unity.Animation.Editor;

namespace Unity.Animation.Model
{
    internal class InputComponentFieldVariableDeclarationModel : VariableDeclarationModel
    {
        [SerializeField, HideInInspector]
        internal ComponentBindingIdentifier Identifier;
        [SerializeField, HideInInspector]
        internal FieldHandle FieldHandle;

        public override string DisplayTitle => FieldHandle.Resolve();

        public static InputComponentFieldVariableDeclarationModel Create(FieldInfo field, ComponentBinding componentBinding, GraphModel graph)
        {
            Assert.IsNotNull(graph);
            Assert.IsNotNull(graph.AssetModel);

            var decl = new InputComponentFieldVariableDeclarationModel();

            decl.FieldHandle = field.GenerateFieldHandle();
            decl.VariableType = VariableType.GraphVariable;
            decl.AssetModel = graph.AssetModel;
            decl.VariableName = field.Name;
            decl.DataType = field.FieldType.GenerateTypeHandle();
            decl.Identifier = componentBinding.Identifier;
            decl.Modifiers = ModifierFlags.ReadOnly;
            decl.IsExposed = true;

            decl.m_Capabilities.Remove(UnityEditor.GraphToolsFoundation.Overdrive.Capabilities.Renamable);

            return decl;
        }
    }
}
