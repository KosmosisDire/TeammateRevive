using UnityEditor;
using System.IO;
using UnityEngine;

public class CreateAssetBundles
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = "Assets/AssetBundles";
        if(!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        Debug.Log("Bundles finished building successfully.");
    }

    [MenuItem("Assets/Move Asset Bundles")]
    static void MoveAllAssetBundles()
    {
        string assetBundleDirectory = "Assets/AssetBundles";
        string moveToPath = @"C:\ClonedRepos\TeammateRevive\TeammateRevive\Resources";
        File.Copy(Path.Combine(assetBundleDirectory, "customresources"), Path.Combine(moveToPath, "customresources"), true);
        File.Copy(Path.Combine(assetBundleDirectory, "customresources.manifest"), Path.Combine(moveToPath, "customresources.manifest"), true);
        Debug.Log("Assets moved to " + moveToPath);
    }

    [MenuItem("Assets/Build and Move AssetBundles")]
    static void BuildAndMoveAllAssetBundles()
    {
        BuildAllAssetBundles();
        MoveAllAssetBundles();
    }
}