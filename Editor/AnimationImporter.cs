using System.IO;
using Unity.Animation.Hybrid;
using UnityEditor;
using UnityEngine;
using UnityEditor.VersionControl;

namespace Unity.Animation.Editor
{
    public class AnimationImporter
    {
        [MenuItem("Animation/Import Rigs", false, 10)]
        static void ImportRig()
        {
            if (Selection.objects == null)
                return;

            foreach (var obj in Selection.objects)
            {
                if (!PrefabUtility.IsPartOfPrefabAsset(obj))
                {
                    Debug.LogError(string.Format("Object {0} is not a prefab", obj.name));
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(obj);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                var skeleton = asset.GetComponent<Unity.Animation.Hybrid.Skeleton>();
                if (skeleton == null)
                {
                    Debug.LogError(string.Format("Asset {0} doesn't have a skeleton", obj.name));
                    continue;
                }


                var skeletonNodes = RigGenerator.ExtractSkeletonNodesFromTransforms(skeleton.transform, skeleton.Bones);
                var rigDefinition = RigBuilder.CreateRigDefinition(skeletonNodes);

                var blobPath = PrepareBlobAssetPath(AssetDatabase.AssetPathToGUID(path));
                BlobFile.WriteBlobAsset(ref rigDefinition, blobPath);

                Debug.Log(string.Format("Imported rig for asset {0}, skeleton has {1} nodes", path, skeletonNodes.Length));
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("Animation/Import Animation Clips", false, 11)]
        static void ImportAnim()
        {
            if (Selection.objects == null)
                return;

            foreach (var obj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsMainAsset(obj))
                {
                    //Import sub assets
                    Debug.Log(string.Format("Importing clips from main asset {0}", path));
                    var assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(path);

                    if (assetsAtPath.Length == 0)
                    {
                        Debug.LogWarning(string.Format("No clips to import"));
                        continue;
                    }

                    // We can only import one clip from a FBX because they are stored with
                    // the orignal file guid
                    int count = 0;
                    foreach (var asset in assetsAtPath)
                    {
                        var animationClip = asset as AnimationClip;
                        if (animationClip == null)
                            continue;

                        // Do not extract preview clips
                        if(animationClip.hideFlags.HasFlag(HideFlags.HideInHierarchy))
                            continue;

                        ImportAnimationClip(animationClip, path);
                        count++;
                        break;
                    }

                    Debug.Log(string.Format("Imported {0} clips", count));
                }
                else
                {
                    var clip = obj as AnimationClip;
                    if (!clip)
                    {
                        Debug.Log(string.Format("Could not import asset because it is not a clip {0}", path));
                        continue;
                    }

                    ImportAnimationClip(clip, path);
                }
            }

            AssetDatabase.Refresh();
        }

        static void ImportAnimationClip(AnimationClip clip, string path)
        {
            var denseClipBlob = ClipBuilder.AnimationClipToDenseClip(clip);
            var blobPath = PrepareBlobAssetPath(AssetDatabase.AssetPathToGUID(path));
            BlobFile.WriteBlobAsset(ref denseClipBlob, blobPath);
            Debug.Log(string.Format("Imported clip '{0}' from path '{1}', stored blob at '{2}'", clip.name, path, blobPath));
        }

        public static string PrepareBlobAssetPath(string guid)
        {
            var blobFile = guid + ".blob";
            var blobPath = "BlobAssets/" + blobFile;

            var fullBlobPath = Application.streamingAssetsPath + '/' + blobPath;

            var fullBlobDir = Path.GetDirectoryName(fullBlobPath);
            Unity.Animation.Debug.Log("fullBlobPath:" + fullBlobPath);
            if (!Directory.Exists(fullBlobDir))
                Directory.CreateDirectory(fullBlobDir);

            var assetPath = fullBlobPath.Remove(0, Application.dataPath.Length+1);

            VersionControlCheckout(assetPath);

            return blobFile;
        }

        internal static bool VersionControlCheckout(string assetPath)
        {
            if(!Provider.isActive)
                return false;

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (File.Exists(projectRoot + "/Assets/" + assetPath))
            {
                var asset = Provider.GetAssetByPath("Assets/" + assetPath);
                if (asset == null)
                    return false;

                UnityEditor.VersionControl.Task statusTask = UnityEditor.VersionControl.Provider.Status(asset);
                statusTask.Wait();

                if (Provider.AddIsValid(statusTask.assetList))
                {
                    var task = Provider.Add(asset, false);
                    task.Wait();
                    if (!task.success)
                    {
                        Unity.Animation.Debug.Log("Version Control: Failed to add" + assetPath);
                        return false;
                    }

                    Unity.Animation.Debug.Log("Version Control: Added " + assetPath);
                }

                if (Provider.CheckoutIsValid(statusTask.assetList))
                {
                    var task = Provider.Checkout(asset, CheckoutMode.Asset);
                    task.Wait();
                    if (!task.success)
                    {
                        Unity.Animation.Debug.Log("Version Control: Failed to check out " + assetPath);
                        return false;
                    }
                    Unity.Animation.Debug.Log("Version Control: Check out " + assetPath);
                }
            }
            return true;
        }
    }
}
