using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using Unity.Animation.Model;

namespace Unity.Animation.Editor.Tests
{
    class StencilTests : BaseGraphFixture
    {
        [Test]
        public void CreateVariableModelForDeclaration_WithInvalidDecl_ThrowsException()
        {
            var declVar = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "msg", ModifierFlags.None, false);
            Assert.Throws<System.ArgumentException>(
                () => Stencil.CreateVariableModelForDeclaration(GraphModel, declVar, new Vector2()),
                "Only supported variable declarations are permitted.");
        }

        [Test]
        public void ValidateEdgeConnection_WithInvalidInputPortModel_ThrowsException()
        {
            var inputModel = new PortModel();
            var outputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float
            };

            Assert.Throws<System.ArgumentException>(
                () => GraphModel.IsValidEdge(inputModel, outputModel),
                "Only supported port models are permitted.");
        }

        [Test]
        public void ValidateEdgeConnection_WithInvalidOutputPortModel_ThrowsException()
        {
            var inputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float
            };
            var outputModel = new PortModel();

            Assert.Throws<System.ArgumentException>(
                () => GraphModel.IsValidEdge(inputModel, outputModel),
                "Only supported port models are permitted.");
        }

        [Test]
        public void ValidateEdgeConnection_WithInvalidInputDataType_ReturnsFalse()
        {
            var inputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float
            };
            var outputModel = new BasePortModel();

            Assert.False(GraphModel.IsValidEdge(inputModel, outputModel));
        }

        [Test]
        public void ValidateEdgeConnection_WithInvalidOutputDataType_ReturnsFalse()
        {
            var inputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float
            };
            var outputModel = new BasePortModel();

            Assert.False(GraphModel.IsValidEdge(inputModel, outputModel));
        }

        [Test]
        public void ValidateEdgeConnection_WithValidPorts_ReturnsTrue()
        {
            var inputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float,
                IsStatic = false,
                EvaluationType = BasePortModel.PortEvaluationType.Simulation
            };
            var outputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float,
                IsStatic = false,
                EvaluationType = BasePortModel.PortEvaluationType.Simulation
            };

            Assert.True(GraphModel.IsValidEdge(inputModel, outputModel));
        }

        [Test]
        public void ValidateEdgeConnection_WithStaticInputPort_ReturnsFalse()
        {
            var inputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float,
                IsStatic = true
            };
            var outputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float
            };

            Assert.False(GraphModel.IsValidEdge(inputModel, outputModel));
        }

        [Test]
        public void ValidateEdgeConnection_WithStaticOutputPort_ReturnsFalse()
        {
            var inputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float
            };
            var outputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float,
                IsStatic = true
            };

            Assert.False(GraphModel.IsValidEdge(inputModel, outputModel));
        }

        [Test]
        public void ValidateEdgeConnection_WithDifferentDataType_ReturnsFalse()
        {
            var inputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float
            };
            var outputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Int
            };

            Assert.False(GraphModel.IsValidEdge(inputModel, outputModel));
        }

        [Test]
        public void ValidateEdgeConnection_WithDifferentEvaluationType_ReturnsFalse()
        {
            var inputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float,
                EvaluationType = BasePortModel.PortEvaluationType.Simulation
            };
            var outputModel = new BasePortModel()
            {
                DataTypeHandle = TypeHandle.Float,
                EvaluationType = BasePortModel.PortEvaluationType.Rendering
            };

            Assert.False(GraphModel.IsValidEdge(inputModel, outputModel));
        }
    }
}
