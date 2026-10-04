using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[InitializeOnLoad]
public class SystemStart 
{
    static SystemStart()
    {
        //Debug.Log("Up and running");
    }
//	[PostProcessBuild]
//	public static void ReplaceAndroidManifest(BuildTarget buildTarget, string pathToBuiltProject){
//		if (buildTarget == BuildTarget.Android) {
//			string manifestPath = Application.dataPath.Replace("Assets","") +  "Temp/StagingArea/AndroidManifest.xml";
//			Debug.Log (manifestPath);
//			string customManifestPath = Application.dataPath + "/Plugins/Android/AndroidManifest.xml";
//
//			FileUtil.ReplaceFile(customManifestPath, manifestPath);
//		}
//	}
}


