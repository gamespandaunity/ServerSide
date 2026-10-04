using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ServerNetworkOrganizer : EditorWindow
{
    static readonly (string f, string t)[] Moves = {
        // 8 Ball Pool
        ("Assets/_Network/EightBallNetwork.cs",         "Assets/_Games/8Ball pool/EightBallNetwork.cs"),
        ("Assets/_Network/cueBallNetwork.cs",           "Assets/_Games/8Ball pool/cueBallNetwork.cs"),
        // Snooker
        ("Assets/_Network/ResultManagerForSnooker.cs",  "Assets/_Games/Snokker/ResultManagerForSnooker.cs"),
        ("Assets/_Network/SnookerCamera.cs",            "Assets/_Games/Snokker/SnookerCamera.cs"),
        // Ludo
        ("Assets/_Network/ResultManagerLudo.cs",        "Assets/_Games/Ludo/ResultManagerLudo.cs"),
        // Car
        ("Assets/_Network/CarController.cs",            "Assets/_Games/Car/CarController.cs"),
        // Highway Racer
        ("Assets/_Network/DistanceEvent.cs",            "Assets/_Games/Highway Racer/DistanceEvent.cs"),
        ("Assets/_Network/DistanceTracker.cs",          "Assets/_Games/Highway Racer/DistanceTracker.cs"),
        ("Assets/_Network/RCCGrassSlowdown.cs",         "Assets/_Games/Highway Racer/RCCGrassSlowdown.cs"),
        ("Assets/_Network/RaceProgressUI.cs",           "Assets/_Games/Highway Racer/RaceProgressUI.cs"),
        ("Assets/_Network/MouseFollowCamera.cs",        "Assets/_Games/Highway Racer/MouseFollowCamera.cs"),
        // Dev / junk
        ("Assets/_Network/TestScript.cs",               "Assets/_Dev/TestScript.cs"),
        ("Assets/_Network/cubemobing.cs",               "Assets/_Dev/cubemobing.cs"),
        ("Assets/_Network/unity_extensions_docs.md",    "Assets/_Dev/unity_extensions_docs.md"),
        ("Assets/_Network/CubeController.cs",           "Assets/_Dev/CubeController.cs"),
    };

    [MenuItem("Tools/Structure/Phase 8 - Network Organizer")]
    public static void Run()
    {
        if (!EditorUtility.DisplayDialog("Server Network Organizer",
            "Moves game-specific scripts from _Network/ to _Games/.\nContinue?", "Yes", "Cancel")) return;

        int mv = 0, sk = 0, fl = 0;
        var lg = new List<string>();

        foreach (string f in new[]{
            "Assets/_Games/8Ball pool","Assets/_Games/Snokker",
            "Assets/_Games/Ludo","Assets/_Games/Car",
            "Assets/_Games/Highway Racer","Assets/_Dev",
        }) EF(f);

        int total = Moves.Length, p = 0;
        foreach (var (from, to) in Moves)
        {
            EditorUtility.DisplayProgressBar("Network Organizer", Path.GetFileName(from), (float)p++ / total);
            Move(from, to, ref mv, ref sk, ref fl, lg);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        foreach (var e in lg) Debug.Log("[NetOrg] " + e);
        EditorUtility.DisplayDialog("Network Organizer Done",
            $"Moved:{mv}  Skipped:{sk}  Failed:{fl}\n\nServer restructure complete!", "OK");
    }

    static void EF(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int s = path.LastIndexOf('/');
        EF(path.Substring(0, s));
        AssetDatabase.CreateFolder(path.Substring(0, s), path.Substring(s + 1));
    }

    static void Move(string from, string to, ref int mv, ref int sk, ref int fl, List<string> lg)
    {
        bool src = AssetDatabase.IsValidFolder(from) || AssetDatabase.LoadAssetAtPath<Object>(from) != null;
        if (!src) { sk++; return; }
        bool dst = AssetDatabase.IsValidFolder(to)   || AssetDatabase.LoadAssetAtPath<Object>(to) != null;
        if (dst) { lg.Add($"⚠ exists:{to}"); sk++; return; }
        EF(to.Substring(0, to.LastIndexOf('/')));
        string err = AssetDatabase.MoveAsset(from, to);
        if (string.IsNullOrEmpty(err)) { lg.Add($"✓ {Path.GetFileName(from)}"); mv++; }
        else { lg.Add($"✗ {from}: {err}"); fl++; }
    }
}
