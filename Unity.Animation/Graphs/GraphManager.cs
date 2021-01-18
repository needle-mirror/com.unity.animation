using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Hash128 = Unity.Entities.Hash128;

namespace Unity.Animation
{
    public struct GraphID : IEquatable<GraphID>
    {
        public Hash128 Value;

        public bool IsValid() => Value.IsValid;

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is GraphID otherID)
                return Equals(otherID);
            return false;
        }

        public bool Equals(GraphID other)
        {
            return Value == other.Value && Value == other.Value;
        }

        public static bool operator==(GraphID lhs, GraphID rhs)
        {
            if (ReferenceEquals(lhs, null))
            {
                if (ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator!=(GraphID lhs, GraphID rhs)
        {
            return !(lhs == rhs);
        }
    }

    //Indicate that a Compositor graph needs to be added to the GraphManager
    public struct GraphRegister : IBufferElementData
    {
        public   GraphID ID;
        public   BlobAssetReference<Graph>                  Graph;
        internal BlobAssetReference<StateMachine.StateMachineDefinition> StateMachineDefinition;
    }

    //Indicate that the instance-specific parameters of a Compositor graph needs to be added to the graph manager
    public struct GraphParameterRegister : IAssetRegister<GraphInstanceParameters>
    {
        public Hash128 ID { get; set; }
        public BlobAssetReference<GraphInstanceParameters> Asset { get; set; }
    }

    public sealed class GenericAssetManager<TAsset, TRegister>
        where TAsset : struct
        where TRegister : IAssetRegister<TAsset>
    {
        private GenericAssetManager() {}
        static GenericAssetManager() {}

        public static GenericAssetManager<TAsset, TRegister> Instance { get; } = new GenericAssetManager<TAsset, TRegister>();

        private readonly Dictionary<Hash128, BlobAssetReference<TAsset>> m_AssetReferences =
            new Dictionary<Hash128, BlobAssetReference<TAsset>>();

        public void Clear() { m_AssetReferences.Clear(); }

        public void AddAsset(TRegister register)
        {
            if (!register.ID.IsValid)
                throw new ArgumentException($"Invalid asset ID");
            if (!register.Asset.IsCreated)
                throw new ArgumentException($"Invalid asset : {register.ID.Value}");

            if (!m_AssetReferences.ContainsKey(register.ID))
            {
                m_AssetReferences[register.ID] = register.Asset;
            }
            else
            {
                //perhaps we should log something about this, but domain reload makes it so these objects are not cleared, but the blobassets clearly are
                m_AssetReferences[register.ID] = register.Asset;
            }
        }

        public BlobAssetReference<TAsset> GetAsset(Hash128 id)
        {
            if (!m_AssetReferences.TryGetValue(id, out var asset))
                throw new System.ArgumentException($"Cannot find asset with ID {id.Value}.");
            return asset;
        }

        public bool TryGetAsset(Hash128 id, out BlobAssetReference<TAsset> asset)
        {
            return m_AssetReferences.TryGetValue(id, out asset);
        }
    }

    [BurstCompatible]
    public struct GraphManager : IComponentData, IDisposable
    {
        internal UnsafeHashMap<GraphID, GraphRegister> m_Graphs;

        public void Clear() => m_Graphs.Clear();

        // is there a way to mark this method not thread safe?
        [NotBurstCompatible]
        public void AddGraph(GraphRegister graph)
        {
            ValidateGraphManager();
            ValidateGraph(graph);

            if (!m_Graphs.ContainsKey(graph.ID))
            {
                m_Graphs[graph.ID] = graph;
            }
            else
            {
                m_Graphs[graph.ID] = graph;
            }
        }

        public GraphRegister GetGraph(GraphID id)
        {
            ValidateGraphManager();

            if (!m_Graphs.TryGetValue(id, out var graph))
                throw new System.ArgumentException($"Cannot find graph with ID {id.Value}.");
            return graph;
        }

        public bool TryGetGraph(GraphID id, out GraphRegister graphRegister)
        {
            ValidateGraphManager();

            return m_Graphs.TryGetValue(id, out graphRegister);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateGraph(GraphRegister graph)
        {
            if (!graph.ID.IsValid())
                throw new ArgumentException("Invalid Graph ID");
            if (!graph.Graph.IsCreated)
                throw new ArgumentException($"Invalid Graph : {graph.ID.Value}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void ValidateGraphManager()
        {
            if (!m_Graphs.IsCreated)
                throw new InvalidOperationException("Invalid GraphManager");
        }

        [NotBurstCompatible]
        public void Initialize()
        {
            m_Graphs = new UnsafeHashMap<GraphID, GraphRegister>(32, Allocator.Persistent);
        }

        [NotBurstCompatible]
        public void Dispose()
        {
            m_Graphs.Dispose();
            m_Graphs = default;
        }
    }
}
