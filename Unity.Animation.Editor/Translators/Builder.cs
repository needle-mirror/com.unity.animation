using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using Unity.Animation.Model;

namespace Unity.Animation.Editor
{
    internal interface IBuilderProvider
    {
        IBuilder Builder { get; }
    }

    internal interface IBuilder
    {
        void Build(IEnumerable<BaseAssetModel> graphAssetModels);
    }

    class Builder : IBuilder
    {
        public static IBuilder Instance = new Builder();

        public void Build(IEnumerable<BaseAssetModel> graphAssetModels)
        {
            foreach (var assetModel in graphAssetModels)
            {
                var graphModel = (GraphModel)assetModel.GraphModel as BaseModel;
                var t = graphModel.Stencil.CreateTranslator();

                try
                {
                    var result = t.TranslateAndCompile(graphModel);

                    foreach (var error in result.errors)
                        UnityEngine.Debug.LogError(error, assetModel);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"Exception occured in {graphModel.Name}\n{e}");
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
