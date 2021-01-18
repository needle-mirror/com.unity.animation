using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using Unity.Animation.Model;

namespace Unity.Animation.Editor.Tests
{
    class ComponentBindingTests : BaseGraphFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void CreatingBinding_CreateDeclarations_WithValidFieldHandles()
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            Assert.AreEqual(2, GraphModel.VariableDeclarations.Count);
            var field1Decl = GraphModel.VariableDeclarations[0] as InputComponentFieldVariableDeclarationModel;
            Assert.IsNotNull(field1Decl);
            Assert.AreEqual("Field1", field1Decl.FieldHandle.Resolve());
            var field5Decl = GraphModel.VariableDeclarations[1] as InputComponentFieldVariableDeclarationModel;
            Assert.IsNotNull(field1Decl);
            Assert.AreEqual("Field5", field5Decl.FieldHandle.Resolve());
        }

        [Test]
        public void RemovingBinding_DeletesDeclarations()
        {
            Assert.AreEqual(0, GraphModel.VariableDeclarations.Count);
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            Assert.AreEqual(2, GraphModel.VariableDeclarations.Count);
            GraphModel.RemoveComponentBinding(binding);
            Assert.AreEqual(0, GraphModel.VariableDeclarations.Count);
        }

        [Test]
        public void CreatingVariableNode_FromInputDeclaration_ContainsValidName()
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));

            var varNode = Stencil.CreateVariableModelForDeclaration(
                GraphModel, GraphModel.VariableDeclarations[0], new UnityEngine.Vector2(0, 0)) as InputComponentFieldVariableModel;
            Assert.IsNotNull(varNode);
            Assert.AreEqual("Test", varNode.ComponentName);
        }

        [Test]
        public void CreatingVariableNode_Duplication_SameGraph()
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            var varNode = Stencil.CreateVariableModelForDeclaration(
                GraphModel, GraphModel.VariableDeclarations[0], new UnityEngine.Vector2(0, 0)) as InputComponentFieldVariableModel;
            Assert.IsNotNull(varNode);

            var duplicatedNode = GraphModel.DuplicateNode(varNode, Vector2.zero) as InputComponentFieldVariableModel;
            Assert.IsNotNull(duplicatedNode);
            Assert.AreEqual(varNode.DeclarationModel, duplicatedNode.DeclarationModel);
            Assert.AreEqual(varNode.ComponentName, duplicatedNode.ComponentName);
        }

        [Test]
        public void CreatingVariableNode_Duplication_OtherGraphWithMatchingDeclaration()
        {
            GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            var varNode = Stencil.CreateVariableModelForDeclaration(
                GraphModel, GraphModel.VariableDeclarations[0], new UnityEngine.Vector2(0, 0)) as InputComponentFieldVariableModel;
            Assert.IsNotNull(varNode);

            var newGraphAsset = GraphAssetCreationHelpers<BaseGraphAssetModel>.CreateInMemoryGraphAsset(CreatedStencilType, "TargetGraph", $"{k_GraphPath}TargetGraph.asset");
            Assert.IsNotNull(newGraphAsset);
            (newGraphAsset.GraphModel as BaseModel).AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            Assert.IsTrue(Stencil.CanPasteNode(varNode, newGraphAsset.GraphModel));

            var duplicatedNode = newGraphAsset.GraphModel.DuplicateNode(varNode, Vector2.zero) as InputComponentFieldVariableModel;
            Assert.IsNotNull(duplicatedNode);
            Assert.AreNotEqual(varNode.DeclarationModel, duplicatedNode.DeclarationModel);
            Assert.AreEqual(varNode.ComponentName, duplicatedNode.ComponentName);

            var sourceVariableDeclaration = varNode.DeclarationModel as InputComponentFieldVariableDeclarationModel;
            var targetVariableDeclaration = duplicatedNode.DeclarationModel as InputComponentFieldVariableDeclarationModel;
            Assert.AreEqual(sourceVariableDeclaration.Identifier, targetVariableDeclaration.Identifier);
            Assert.AreEqual(sourceVariableDeclaration.FieldHandle, targetVariableDeclaration.FieldHandle);
        }

        [Test]
        public void CreatingVariableNode_Duplication_OtherGraphWithoutMatchingDeclaration()
        {
            GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            var varNode = Stencil.CreateVariableModelForDeclaration(
                GraphModel, GraphModel.VariableDeclarations[0], new UnityEngine.Vector2(0, 0)) as InputComponentFieldVariableModel;
            Assert.IsNotNull(varNode);

            var newGraphAsset = GraphAssetCreationHelpers<BaseGraphAssetModel>.CreateInMemoryGraphAsset(CreatedStencilType, "TargetGraph", $"{k_GraphPath}TargetGraph.asset");
            Assert.IsNotNull(newGraphAsset);
            Assert.IsFalse(Stencil.CanPasteNode(varNode, newGraphAsset.GraphModel));
        }

        [Test]
        public void RenamingBinding_ChangesName_OnVariableNodes()
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            var varNode = Stencil.CreateVariableModelForDeclaration(
                GraphModel, GraphModel.VariableDeclarations[0], new UnityEngine.Vector2(0, 0)) as InputComponentFieldVariableModel;
            Assert.IsNotNull(varNode);
            Assert.AreEqual("Test", varNode.ComponentName);
            m_Store.Dispatch(new UpdateComponentNameAction(binding.Identifier, "OtherTest"));
            Assert.AreEqual("OtherTest", varNode.ComponentName);
        }

        [Test]
        public void CreatingBinding_WithValidComponentType_ReturnsValidBinding()
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            Assert.IsNotNull(binding);
            Assert.AreEqual("Test", binding.Name);
            Assert.AreEqual(typeof(DummyAuthoringComponent), binding.Identifier.Type.Resolve());
        }

        [Test]
        public void CreatingBinding_WithNullComponentType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => GraphModel.AddComponentBinding("Test", null));
        }

        struct NotAComponent
        {}

        [Test]
        public void CreatingBinding_WithInvalidComponentType_Throws()
        {
            Assert.Throws<ArgumentException>(() => GraphModel.AddComponentBinding("Test", typeof(NotAComponent)));
        }

        [Test]
        public void CreatingDuplicateBinding_Throws()
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            Assert.Throws<ArgumentException>(() => GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent)));
        }

        [Test]
        public void RemovingBinding_ByBinding_DeletesEntry()
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            Assert.IsNotNull(GraphModel.GetComponentBinding("Test"));
            GraphModel.RemoveComponentBinding(binding);
            Assert.IsNull(GraphModel.GetComponentBinding("Test"));
        }

        [Test]
        public void RemovingBinding_ByIdentifier_DeletesEntry()
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            var id = binding.Identifier;
            Assert.IsTrue(GraphModel.TryGetComponentBinding(id, out _));
            GraphModel.RemoveComponentBinding(id);
            Assert.IsFalse(GraphModel.TryGetComponentBinding(id, out _));
        }

        [Test]
        public void RemovingBinding_DeletesVariablesNodes()
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            Assert.IsNotNull(GraphModel.GetComponentBinding("Test"));
            var varNode = Stencil.CreateVariableModelForDeclaration(
                GraphModel, GraphModel.VariableDeclarations[0], new UnityEngine.Vector2(0, 0));
            Assert.AreEqual(1, GraphModel.NodeModels.Count);
            GraphModel.RemoveComponentBinding(binding);
            Assert.AreEqual(0, GraphModel.NodeModels.Count);
        }

        [Test]
        public void Dispatching_CreateInputBindingAction_CreatesValidBinding()
        {
            m_Store.Dispatch(new CreateInputComponentAction(
                new[] { new CreateInputComponentAction.InputData("Test", typeof(DummyAuthoringComponent)) }));
            Assert.IsNotNull(GraphModel.GetComponentBinding(typeof(DummyAuthoringComponent)));
        }

        [Test]
        public void Dispatching_RenameInputBindingAction_ChangesName()
        {
            m_Store.Dispatch(new CreateInputComponentAction(
                new[] { new CreateInputComponentAction.InputData("Test", typeof(DummyAuthoringComponent)) }));
            var binding = GraphModel.GetComponentBinding(typeof(DummyAuthoringComponent));
            Assert.AreEqual("Test", binding.Name);
            m_Store.Dispatch(new UpdateComponentNameAction(binding.Identifier, "NewName"));
            Assert.AreEqual("NewName", binding.Name);
        }

        [Test]
        public void Dispatching_RenameInputBindingAction_WithConflictingName_IgnoresRename()
        {
            m_Store.Dispatch(new CreateInputComponentAction(
                new[] { new CreateInputComponentAction.InputData("Test", typeof(DummyAuthoringComponent)) }));
            m_Store.Dispatch(new CreateInputComponentAction(
                new[] { new CreateInputComponentAction.InputData("OtherTest", typeof(OtherDummyAuthoringComponent)) }));
            var binding = GraphModel.GetComponentBinding(typeof(DummyAuthoringComponent));
            Assert.AreEqual("Test", binding.Name);
            m_Store.Dispatch(new UpdateComponentNameAction(binding.Identifier, "OtherTest"));
            Assert.AreNotEqual("OtherTest", binding.Name);
        }

        [Test]
        public void Dispatching_RemovingInputBindingAction_DeletesEntry()
        {
            var binding = GraphModel.AddComponentBinding("Test", typeof(DummyAuthoringComponent));
            var id = binding.Identifier;
            m_Store.Dispatch(new RemoveComponentAction(id));
            Assert.IsFalse(GraphModel.TryGetComponentBinding(id, out _));
        }

        [Test]
        public void Dispatching_RemovingInputBindingAction_WithInvalidID_IgnoresAction()
        {
            var id = new ComponentBindingIdentifier() { Type = typeof(NotAComponent).GenerateTypeHandle() };
            m_Store.Dispatch(new RemoveComponentAction(id));
        }
    }
}
