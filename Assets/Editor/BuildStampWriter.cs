using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Stamps every player build with a machine-comparable build number (yyyyMMddHHmm, e.g.
// 202609130315) in Assets/Resources/build_stamp.txt, read back at runtime by BuildStamp.
//
// On the DEDICATED SERVER this exists for one reason: incident attribution. An Edgegap deployment is
// ephemeral — delete it and its logs go with it — so a bug report can only be tied to a build if the
// build says who it is while the match is running. MyEightBallNetwork.OnStartServer logs
// BuildStamp.Timestamp for exactly that. Without this hook the server repo has the runtime reader but
// no stamp file at all, so BuildStamp.Timestamp returns 0 and every report stays unattributable.
//
// Do NOT try to reconstruct this from git afterwards: Unity builds the WORKING TREE, not git HEAD, so
// a commit date proves nothing about what is inside a build.
public class BuildStampWriter : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        string dir = "Assets/Resources";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string stamp = DateTime.Now.ToString("yyyyMMddHHmm");
        File.WriteAllText(Path.Combine(dir, "build_stamp.txt"), stamp);
        AssetDatabase.ImportAsset("Assets/Resources/build_stamp.txt");
        Debug.Log($"[BuildStamp] Stamped build with {stamp}");
    }
}
