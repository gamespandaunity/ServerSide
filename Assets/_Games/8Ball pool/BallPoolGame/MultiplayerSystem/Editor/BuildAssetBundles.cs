using UnityEngine;
using UnityEditor;
using System.IO;

public class BuildAssetBundles
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        if (!Directory.Exists(Application.dataPath + "/AssetBundlesForUpload"))
        {
            Directory.CreateDirectory(Application.dataPath + "/AssetBundlesForUpload");
        }
        BuildForTargetPlatform(BuildTarget.StandaloneWindows);
        BuildForTargetPlatform(BuildTarget.StandaloneOSX);
        BuildForTargetPlatform(BuildTarget.WebGL);
        BuildForTargetPlatform(BuildTarget.Android);
        BuildForTargetPlatform(BuildTarget.iOS);
    }
    static void BuildForTargetPlatform(BuildTarget target)
    {
        string localPath = "AssetBundlesForUpload/" + target.ToString();
        if (!Directory.Exists(Application.dataPath + "/" + localPath))
        {
            Directory.CreateDirectory(Application.dataPath + "/" + localPath);
        }
        BuildPipeline.BuildAssetBundles("Assets/" + localPath, BuildAssetBundleOptions.None, target);
    }
}
