using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using Unity.Animation.Model;

namespace Unity.Animation.Editor.Tests
{
    internal class AnimationGraphFixture : BaseGraphFixture<AnimationGraphStencil>
    {
        protected override IGraphAssetModel CreateGraphAsset(string name, string path)
        {
            return GraphAssetCreationHelpers<BaseGraphAssetModel>.CreateInMemoryGraphAsset(
                CreatedStencilType, name, path, new CreateGraphTemplate(CreatedStencilType));
        }
    }

    class AnimationIRBuilderTests : AnimationGraphFixture
    {
        [Test]
        public void BuildAnimationClipGraph()
        {
            var clipNode = (AnimationClipNodeModel)GraphModel.CreateNode(typeof(AnimationClipNodeModel), "MyClip", default);
            clipNode.DefineNode();
            clipNode.SetClip(new UnityEngine.AnimationClip());

            var outputNode = GraphModel.NodeModels.OfType<AnimationOutputNodeModel>().SingleOrDefault();

            Assert.IsNotNull(outputNode);

            GraphModel.CreateEdge(
                outputNode.InputsById[AnimationOutputNodeModel.k_PoseResultPortName],
                clipNode.OutputsById[AnimationClipNodeModel.k_PosePortName]);
            var ir = IRBuilder.BuildBlendTreeIR(GraphModel);

            Assert.AreEqual(1, ir.AssetReferences.Count);

            // 1x DeltaTime Node
            // 1x Simple Clip Node
            // 1x Passthrough EntityManager
            // 1x Passthrough InputReference
            // 1x Passthrough Rig
            // 1x Passthrough Asset Reference
            // 1x Passthrough Output
            // 1x Object Converter for Root Motion
            Assert.AreEqual(8, ir.Nodes.Count);

            // 1x Asset Reference -> Clip
            // 1x DeltaTime -> Clip
            // 1x Object Converter -> Clip
            // 1x Rig -> Clip
            Assert.AreEqual(4, ir.SimulationConnections.Count);
            Assert.AreEqual(0, ir.SimulationToDataConnections.Count);

            // 1x Clip -> Output
            Assert.AreEqual(1, ir.DataConnections.Count);
        }

        [Test]
        public void BuildMixerGraph()
        {
            var clip1 = (AnimationClipNodeModel)GraphModel.CreateNode(typeof(AnimationClipNodeModel), "MyClip1", default);
            var clip2 = (AnimationClipNodeModel)GraphModel.CreateNode(typeof(AnimationClipNodeModel), "MyClip2", default);

            clip1.DefineNode();
            clip2.DefineNode();

            clip1.SetClip(new UnityEngine.AnimationClip());
            clip2.SetClip(new UnityEngine.AnimationClip());

            var mixer = (MixerNodeModel)GraphModel.CreateNode(typeof(MixerNodeModel), "Mixer", default);
            mixer.DefineNode();

            GraphModel.CreateEdge(
                mixer.InputsById[MixerNodeModel.k_Input1PortName],
                clip1.OutputsById[AnimationClipNodeModel.k_PosePortName]);
            GraphModel.CreateEdge(
                mixer.InputsById[MixerNodeModel.k_Input2PortName],
                clip2.OutputsById[AnimationClipNodeModel.k_PosePortName]);

            var outputNode = GraphModel.NodeModels.OfType<AnimationOutputNodeModel>().SingleOrDefault();

            Assert.IsNotNull(outputNode);

            GraphModel.CreateEdge(
                outputNode.InputsById[AnimationOutputNodeModel.k_PoseResultPortName],
                mixer.OutputsById[MixerNodeModel.k_PosePortName]);

            var ir = IRBuilder.BuildBlendTreeIR(GraphModel);

            Assert.AreEqual(1, ir.AssetReferences.Count);

            // 2x DeltaTime Node
            // 2x Simple Clip Node
            // 1x Passthrough EntityManager
            // 1x Passthrough InputReference
            // 1x Passthrough Rig
            // 2x Passthrough Asset Reference
            // 1x Passthrough Output
            // 2x Object Converter for Root Motion
            Assert.AreEqual(12, ir.Nodes.Count);
        }
    }
}
