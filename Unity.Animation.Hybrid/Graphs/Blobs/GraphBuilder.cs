using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Animation.Hybrid
{
    internal static class GraphBuilder
    {
        internal static unsafe uint ComputeHash<T>(ref BlobArray<T> array, uint seed = 0)
            where T : struct
        {
            return math.hash(array.GetUnsafePtr(), array.Length * UnsafeUtility.SizeOf<T>(), seed);
        }

        internal static uint ComputeHash(ref BlobAssetReference<Graph> graph)
        {
            unchecked
            {
                uint hashCode = ComputeHash(ref graph.Value.CreateCommands);
                hashCode = ComputeHash(ref graph.Value.ConnectCommands, hashCode);
                hashCode = ComputeHash(ref graph.Value.ConnectAssetCommands, hashCode);
                hashCode = ComputeHash(ref graph.Value.ResizeCommands, hashCode);
                hashCode = ComputeHash(ref graph.Value.GraphInputs, hashCode);
                hashCode = ComputeHash(ref graph.Value.GraphOutputs, hashCode);
                hashCode = ComputeHash(ref graph.Value.CreateStateCommands, hashCode);
                hashCode = ComputeHash(ref graph.Value.CreateTransitionCommands, hashCode);
                hashCode = ComputeHash(ref graph.Value.CreateConditionFragmentCommands, hashCode);
                hashCode = ComputeHash(ref graph.Value.TypesUsed, hashCode);
                hashCode = ComputeHash(ref graph.Value.InputTargets, hashCode);
                return ComputeHash(ref graph.Value.SetValueCommands, hashCode);
            }
        }

        internal static uint ComputeHash(ref BlobAssetReference<GraphInstanceParameters> graph)
        {
            unchecked
            {
                uint hashCode = ComputeHash(ref graph.Value.Values);
                return hashCode;
            }
        }

        public static BlobAssetReference<Graph> Build(CompiledGraph compiledGraph)
        {
            if (compiledGraph == null)
                return new BlobAssetReference<Graph>();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var graph = ref blobBuilder.ConstructRoot<Graph>();

            blobBuilder.Construct(ref graph.CreateCommands, compiledGraph.Definition.TopologyDefinition.NodeCreations.ToArray());
            blobBuilder.Construct(ref graph.ConnectCommands, compiledGraph.Definition.TopologyDefinition.Connections.ToArray());
            blobBuilder.Construct(ref graph.GraphInputs, compiledGraph.Definition.TopologyDefinition.Inputs.ToArray());
            blobBuilder.Construct(ref graph.GraphOutputs, compiledGraph.Definition.TopologyDefinition.Outputs.ToArray());
            blobBuilder.Construct(ref graph.ResizeCommands, compiledGraph.Definition.TopologyDefinition.PortArrays.ToArray());
            blobBuilder.Construct(ref graph.CreateStateCommands, compiledGraph.Definition.TopologyDefinition.States.ToArray());
            blobBuilder.Construct(ref graph.CreateTransitionCommands, compiledGraph.Definition.TopologyDefinition.Transitions.ToArray());
            blobBuilder.Construct(ref graph.CreateConditionFragmentCommands, compiledGraph.Definition.TopologyDefinition.ConditionFragments.ToArray());

            blobBuilder.Construct(ref graph.SetValueCommands, compiledGraph.Definition.TopologyDefinition.Values.ToArray());
            blobBuilder.Construct(ref graph.InputTargets, compiledGraph.Definition.InputTargets.ToArray());

            FillTypesUsedBuffer(compiledGraph.Definition.TypesUsed, ref graph, ref blobBuilder);
            FillAssetBuffer(compiledGraph.Definition.Assets, ref graph, ref blobBuilder);

            var outputGraph = blobBuilder.CreateBlobAssetReference<Graph>(Allocator.Persistent);
            outputGraph.Value.m_HashCode = (int)ComputeHash(ref outputGraph);
            outputGraph.Value.IsStateMachine = compiledGraph.Definition.TopologyDefinition.IsStateMachine;
            blobBuilder.Dispose();

            return outputGraph;
        }

        internal static BlobAssetReference<GraphInstanceParameters> BuildInstanceSpecificData(CompiledGraph compiledGraph, AnimationGraph graph)
        {
            if (compiledGraph == null)
                return new BlobAssetReference<GraphInstanceParameters>();

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var graphInstance = ref blobBuilder.ConstructRoot<GraphInstanceParameters>();

            //Stuff that is specific to each instance of the Graph?
            FillParameterBuffer(compiledGraph.Definition, graph.Context, graph.ExposedObjects, ref graphInstance, ref blobBuilder);

            var outputInstance = blobBuilder.CreateBlobAssetReference<GraphInstanceParameters>(Allocator.Persistent);
            outputInstance.Value.m_HashCode = (int)ComputeHash(ref outputInstance);
            blobBuilder.Dispose();

            return outputInstance;
        }

        public static Entities.Hash128 GetAssetHash(UnityEngine.Object asset)
        {
            if (asset == null)
                return default;

            var result = new Entities.Hash128();
            if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long fileID))
            {
                result = new Entities.Hash128(guid);
                result.Value.w ^= (uint)fileID;
                result.Value.z ^= (uint)(fileID >> 32);
            }
            else
            {
                result.Value.x = (uint)asset.GetInstanceID();
                result.Value.y = (uint)asset.GetType().GetHashCode();
                result.Value.z = 0xABCD;
                result.Value.w = 0xEF01;
            }

            return result;
        }

        static void FillAssetBuffer(IReadOnlyList<GraphAssetReference> assets, ref Graph graph, ref BlobBuilder blobBuilder)
        {
            var arrayBuilder = blobBuilder.Allocate(ref graph.ConnectAssetCommands, assets.Count);
            for (var i = 0; i != assets.Count; ++i)
            {
                var hash = GetAssetHash(assets[i].Asset);
                arrayBuilder[i] = new ConnectAssetCommand
                {
                    DestinationNodeID = assets[i].NodeID,
                    DestinationPortID = assets[i].PortID,
                    AssetID = hash,
                    AssetType = assets[i].TypeHash,
                    ClipDuration = GetClipDurationFromAsset(assets[i].Asset)
                };
            }
        }

        static float GetClipDurationFromAsset(Object asset)
        {
            if (asset is AnimationClip clip)
            {
                return clip.length;
            }

            return 0.0f;
        }

        static void FillTypesUsedBuffer(IReadOnlyList<string> typeNames, ref Graph graph, ref BlobBuilder blobBuilder)
        {
            var strings = blobBuilder.Allocate(ref graph.TypesUsed, typeNames.Count);
            for (var i = 0; i != typeNames.Count; ++i)
            {
                blobBuilder.AllocateString(ref strings[i], typeNames[i]);
            }
        }

        static void FillParameterBuffer(
            GraphDefinition definition,
            UnityEngine.Component context,
            IReadOnlyList<AnimationGraph.ObjectBindingEntry> parameters,
            ref GraphInstanceParameters graph, ref BlobBuilder blobBuilder)
        {
            var defaultValues = definition.TopologyDefinition.Values;
            var validParameters = parameters.Where(p => p.Value != null).ToList();
            var arrayBuilder = blobBuilder.Allocate(ref graph.Values, validParameters.Count);

            var bindingsToProcess = definition.ExposedObjects.ToDictionary(v => v.TargetGUID, v => v);
            for (var i = 0; i != validParameters.Count; ++i)
            {
                var p = validParameters[i];
                if (p.Value == null)
                    continue;
                if (!bindingsToProcess.TryGetValue(p.TargetGUID, out var valueBinding))
                    continue;

                bindingsToProcess.Remove(p.TargetGUID);

                var resolvedValue = ObjectResolverService.ResolveObjectReference(valueBinding.TypeHash, p.Value, context);

                var variant = resolvedValue;
                variant.Type = GraphVariant.ValueType.Int4;
                arrayBuilder[i] =
                    new SetValueCommand
                {
                    Type = resolvedValue.Type,
                    Value = variant.Int4,
                    Node = valueBinding.NodeID,
                    Port = valueBinding.PortID
                };
            }
        }
    }
}
#endif
