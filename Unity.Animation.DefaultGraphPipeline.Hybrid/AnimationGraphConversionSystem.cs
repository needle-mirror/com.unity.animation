#if UNITY_EDITOR
using System;
using Unity.Entities;
using Unity.Profiling;
using UnityEditor;

namespace Unity.Animation.Hybrid
{
    [ConverterVersion(userName: "Unity.Animation.Hybrid.AnimationGraphConversionSystem", version: 2)]
    [UpdateAfter(systemType: typeof(BaseGraphConversionSystem))]
    public class AnimationGraphConversionSystem : GameObjectConversionSystem
    {
        ulong m_AnimationClipHash;
        ulong m_BlendTree1DHash;
        ulong m_BlendTree2DHash;

        static readonly ProfilerMarker k_ProfilerMarker = new ProfilerMarker("Unity.Animation.AnimationGraphConversionSystem");

        protected override void OnCreate()
        {
            base.OnCreate();

            m_AnimationClipHash = TypeHash.CalculateStableTypeHash(typeof(BlobAssetReference<Clip>));
            m_BlendTree1DHash = TypeHash.CalculateStableTypeHash(typeof(BlobAssetReference<BlendTree1D>));
            m_BlendTree2DHash = TypeHash.CalculateStableTypeHash(typeof(BlobAssetReference<BlendTree2DSimpleDirectional>));
        }

        void AddToBuffer<T>(Entity entity, T element)
            where T : struct, IBufferElementData
        {
            DynamicBuffer<T> buffer;
            if (!DstEntityManager.HasComponent<T>(entity))
                buffer = DstEntityManager.AddBuffer<T>(entity);
            else
                buffer = DstEntityManager.GetBuffer<T>(entity);
            buffer.Add(element);
        }

        private void AddToAssetStoreIfNecessary<T>(Hash128 id, BlobAssetReference<T> assetBlobRef)
            where T : struct
        {
            if (!BlobAssetStore.Contains<T>(id))
            {
                BlobAssetStore.TryAdd(id, assetBlobRef);
            }
        }

        private void RegisterAssets(UnityEngine.GameObject gameObject, CompiledGraph compiledGraph, Entity targetEntity, Entity primaryEntity)
        {
            foreach (var a in compiledGraph.Definition.Assets)
            {
                if (a.TypeHash == m_AnimationClipHash)
                {
                    var hash = GraphBuilder.GetAssetHash(a.Asset);
                    var assetBlobRef = ClipConversion.ToDenseClip((UnityEngine.AnimationClip)a.Asset);

                    AddToAssetStoreIfNecessary(hash, assetBlobRef);
                    AddToBuffer(targetEntity, new ClipRegister()
                    {
                        ID = hash,
                        Asset = assetBlobRef,
                    });
                }
                else if (a.TypeHash == m_BlendTree1DHash)
                {
                    AddToBuffer(targetEntity, new BlendTree1DAsset()
                    {
                        Index = BlendTreeConversion.Convert((UnityEditor.Animations.BlendTree)a.Asset, targetEntity, DstEntityManager),
                        Node = a.NodeID,
                        Port = a.PortID
                    });
                }
                else if (a.TypeHash == m_BlendTree2DHash)
                {
                    AddToBuffer(targetEntity, new BlendTree2DAsset()
                    {
                        Index = BlendTreeConversion.Convert((UnityEditor.Animations.BlendTree)a.Asset, targetEntity, DstEntityManager),
                        Node = a.NodeID,
                        Port = a.PortID
                    });
                }
                DeclareAssetDependency(gameObject, a.Asset);
            }
        }

        protected override void OnUpdate()
        {
            k_ProfilerMarker.Begin();
            Entities.ForEach((AnimationGraph component) =>
            {
                var graphProvider = component.Graph as ICompiledGraphProvider;
                if (graphProvider == null || graphProvider.CompiledGraph?.Definition == null)
                    return;

                var graphDefinition = graphProvider.CompiledGraph.Definition;
                var entity = TryGetPrimaryEntity(component);
                if (entity == Entity.Null)
                    throw new Exception($"Something went wrong while creating an Entity for the component : {component.name}");

                var contextEntity = TryGetPrimaryEntity(component.Context);

                if (contextEntity == Entity.Null)
                    throw new Exception($"Something went wrong while creating an Entity for the Context : {component.name}");

                DstEntityManager.AddComponent<ProcessDefaultAnimationGraph.AnimatedRootMotion>(contextEntity);

                RegisterAssets(component.gameObject, graphProvider.CompiledGraph, entity, contextEntity);
                foreach (var otherGraph in graphProvider.CompiledGraph.CompiledDependencies)
                {
                    var dependencyGraph = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                        AssetDatabase.GUIDToAssetPath(otherGraph.Guid));
                    var compiledGraph = (dependencyGraph as ICompiledGraphProvider)?.CompiledGraph;
                    if (compiledGraph != null)
                    {
                        RegisterAssets(component.gameObject, compiledGraph, entity, contextEntity);
                    }
                }
            });
            k_ProfilerMarker.End();
        }
    }
}
#endif
