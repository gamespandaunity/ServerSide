using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Server project restructure — split into 6 small phases.
/// Commit + push after EACH phase to stay under git push limits.
///
/// Phase 1 — ThirdParty packages
/// Phase 2 — Script→_App  /  Scripts→_Network  (core renames)
/// Phase 3 — Games  (00.All GAMES + root game folders + game scripts)
/// Phase 4 — UI + Fonts + Prefabs
/// Phase 5 — Art folders
/// Phase 6 — Dev folders + root loose scripts
/// Phase 7 — (ServerAppOrganizer)   Tools/Structure/7. App Organizer
/// Phase 8 — (ServerNetworkOrganizer) Tools/Structure/8. Network Organizer
/// </summary>
public class ServerRestructureAll : EditorWindow
{
    // ── Phase 1 ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Structure/Phase 1 - ThirdParty")]
    public static void Phase1()
    {
        if (!Confirm("Phase 1 — ThirdParty packages",
            "Moves to ThirdParty/:\n" +
            "Agora-RTC-Plugin, Chat, GoogleSignIn,\n" +
            "GeneratedLocalRepo, Project→Photon_Project,\n" +
            "DailyRewards, Provided Assets")) return;

        var r = new Runner();
        r.EnsureFolder("Assets/ThirdParty");
        r.Pairs(new[]{
            ("Assets/Agora-RTC-Plugin",   "Assets/ThirdParty/Agora-RTC-Plugin"),
            ("Assets/Chat",               "Assets/ThirdParty/Chat"),
            ("Assets/GoogleSignIn",       "Assets/ThirdParty/GoogleSignIn"),
            ("Assets/GeneratedLocalRepo", "Assets/ThirdParty/GeneratedLocalRepo"),
            ("Assets/Project",            "Assets/ThirdParty/Photon_Project"),
            ("Assets/DailyRewards",       "Assets/ThirdParty/DailyRewards"),
            ("Assets/Provided Assets",    "Assets/ThirdParty/Provided Assets"),
        });
        r.Finish("Phase 1 done — commit & push, then run Phase 2.");
    }

    // ── Phase 2 ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Structure/Phase 2 - Core Renames")]
    public static void Phase2()
    {
        if (!Confirm("Phase 2 — Core renames",
            "Script/  → _App/\n" +
            "Scripts/ → _Network/\n\n" +
            "CRITICAL: run before Phase 3–6.")) return;

        var r = new Runner();
        r.FolderMove("Assets/Script",  "Assets/_App");
        r.FolderMove("Assets/Scripts", "Assets/_Network");
        r.Finish("Phase 2 done — commit & push, then run Phase 3.");
    }

    // ── Phase 3 ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Structure/Phase 3 - Games")]
    public static void Phase3()
    {
        if (!Confirm("Phase 3 — Games",
            "00.All GAMES → _Games/\n" +
            "Root game folders → _Games/\n" +
            "Root ResultManager scripts → _Games/")) return;

        var r = new Runner();

        // 00.All GAMES children → _Games/
        foreach (string g in new[]{ "1.Horse Riding","12 Beads","8Ball pool","Carrom",
                                    "Highway Racer","Ludo","POKER 1","Snake aur Ladder","Snokker" })
            r.MOM($"Assets/00.All GAMES/{g}", $"Assets/_Games/{g}");

        // Loose .cs in 00.All GAMES
        r.FM("Assets/00.All GAMES/FusionManagmentRpcs.cs", "Assets/_Network/FusionManagmentRpcs.cs");
        r.FM("Assets/00.All GAMES/FusionNetwork.cs",       "Assets/_Network/FusionNetwork.cs");
        if (AssetDatabase.IsValidFolder("Assets/00.All GAMES") &&
            AssetDatabase.FindAssets("", new[]{"Assets/00.All GAMES"}).Length == 0)
            AssetDatabase.DeleteAsset("Assets/00.All GAMES");

        // Root game folders
        foreach (string g in new[]{ "BEDRILL","CRICKET","LUDO","TeenPati" })
            r.MOM($"Assets/{g}", $"Assets/_Games/{g}");
        r.MOM("Assets/Car",            "Assets/_Games/Car");
        r.MOM("Assets/Highway Racer",  "Assets/_Games/Highway Racer");
        r.MOM("Assets/1.Horse Riding", "Assets/_Games/1.Horse Riding");

        // Root ResultManager scripts
        foreach (string g in new[]{ "8Ball pool","Car","CRICKET","Highway Racer","1.Horse Riding" })
            r.EnsureFolder($"Assets/_Games/{g}");
        r.Pairs(new[]{
            ("Assets/ResultManagerFor8Ball.cs",         "Assets/_Games/8Ball pool/ResultManagerFor8Ball.cs"),
            ("Assets/ResultManagerForCar.cs",           "Assets/_Games/Car/ResultManagerForCar.cs"),
            ("Assets/ResultManagerForCricket.cs",       "Assets/_Games/CRICKET/ResultManagerForCricket.cs"),
            ("Assets/ResultManagerForHighwayRacing.cs", "Assets/_Games/Highway Racer/ResultManagerForHighwayRacing.cs"),
            ("Assets/ResultManagerForHorse.cs",         "Assets/_Games/1.Horse Riding/ResultManagerForHorse.cs"),
            ("Assets/HighwayCarNetwork.cs",             "Assets/_Games/Highway Racer/HighwayCarNetwork.cs"),
        });

        r.Finish("Phase 3 done — commit & push, then run Phase 4.");
    }

    // ── Phase 4 ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Structure/Phase 4 - UI Fonts Prefabs")]
    public static void Phase4()
    {
        if (!Confirm("Phase 4 — UI, Fonts, Prefabs",
            "0. UI + UI  → _UI/\n" +
            "Fonts + Font → _Fonts/\n" +
            "Prefab + Prefabs + Panels Prefab → _Prefabs/\n" +
            "Loose prefabs at root → _Prefabs/ / _Games/")) return;

        var r = new Runner();

        // UI
        r.MOM("Assets/0. UI", "Assets/_UI");
        r.MOM("Assets/UI",    "Assets/_UI");

        // Fonts
        r.EnsureFolder("Assets/_Fonts");
        r.Merge("Assets/Fonts", "Assets/_Fonts");
        r.Merge("Assets/Font",  "Assets/_Fonts");

        // Prefabs
        r.EnsureFolder("Assets/_Prefabs");
        r.Merge("Assets/Prefab",        "Assets/_Prefabs");
        r.Merge("Assets/Prefabs",       "Assets/_Prefabs");
        r.Merge("Assets/Panels Prefab", "Assets/_Prefabs/Panels");
        r.FM("Assets/Delete.prefab",       "Assets/_Prefabs/Delete.prefab");
        r.FM("Assets/SceneFolder.prefab",  "Assets/_Prefabs/SceneFolder.prefab");
        r.FM("Assets/SnookerArena.prefab", "Assets/_Games/Snokker/SnookerArena.prefab");

        r.Finish("Phase 4 done — commit & push, then run Phase 5.");
    }

    // ── Phase 5 ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Structure/Phase 5 - Art")]
    public static void Phase5()
    {
        if (!Confirm("Phase 5 — Art folders",
            "Moves all art/asset folders into _Art/:\n" +
            "3D Models, ANIMATION, Animations, AnimationClip,\n" +
            "AnimatorController, AudioClip, AudioMixer*,\n" +
            "Json Animations, Material, Mesh, Cubemap, Shader,\n" +
            "Sound, Sprites, Texture2D, models, old Animations,\n" +
            "NewGamesLogo")) return;

        var r = new Runner();
        r.EnsureFolder("Assets/_Art");
        foreach (string a in new[]{
            "3D Models","ANIMATION","AnimationClip","Animations",
            "AnimatorController","AudioClip",
            "AudioMixerController","AudioMixerGroupController","AudioMixerSnapshotController",
            "Json Animations","Material","Mesh","Cubemap",
            "Shader","Sound","Sprites","Texture2D",
            "models","old Animations","NewGamesLogo",
        })
            r.MOM($"Assets/{a}", $"Assets/_Art/{a}");

        r.Finish("Phase 5 done — commit & push, then run Phase 6.");
    }

    // ── Phase 6 ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Structure/Phase 6 - Dev and Root Scripts")]
    public static void Phase6()
    {
        if (!Confirm("Phase 6 — Dev folders + root loose scripts",
            "Dev/personal → _Dev/\n" +
            "Root app scripts → _App/ sub-folders\n" +
            "Network infra → _Network/\n" +
            "Delete Temp/")) return;

        var r = new Runner();
        r.EnsureFolder("Assets/_Dev");

        // Dev / personal
        r.Pairs(new[]{
            ("Assets/1 Mohsin",        "Assets/_Dev/Mohsin"),
            ("Assets/1.Assets_M",      "Assets/_Dev/Assets_M"),
            ("Assets/_DevelopAqib",    "Assets/_Dev/Aqib"),
            ("Assets/WasiData",        "Assets/_Dev/WasiData"),
            ("Assets/BuildReport",     "Assets/_Dev/BuildReport"),
            ("Assets/ScriptGenerator", "Assets/_Dev/ScriptGenerator"),
            ("Assets/New Scene",       "Assets/_Dev/New Scene"),
            ("Assets/_Recovery",       "Assets/_Dev/Recovery"),
        });
        r.FM("Assets/New Scene.unity", "Assets/_Dev/New Scene.unity");
        if (AssetDatabase.IsValidFolder("Assets/Temp"))
            AssetDatabase.DeleteAsset("Assets/Temp");

        // Network infra
        r.MOM("Assets/EdgegapServerBootstrap", "Assets/_Network/EdgegapServerBootstrap");
        r.MOM("Assets/NetworkObjectSpawner",   "Assets/_Network/NetworkObjectSpawner");

        // Root app scripts → _App/ sub-folders
        foreach (string s in new[]{ "Platform","Audio","Core","Shop","UI","History","Auth","Notifications" })
            r.EnsureFolder($"Assets/_App/{s}");
        r.Pairs(new[]{
            ("Assets/APKUpdater.cs",             "Assets/_App/Platform/APKUpdater.cs"),
            ("Assets/AgoraSpeaker.cs",           "Assets/_App/Audio/AgoraSpeaker.cs"),
            ("Assets/AllScriptsManager.cs",      "Assets/_App/Core/AllScriptsManager.cs"),
            ("Assets/BundlePurchase.cs",         "Assets/_App/Shop/BundlePurchase.cs"),
            ("Assets/ChallengeItemPrefab.cs",    "Assets/_App/UI/ChallengeItemPrefab.cs"),
            ("Assets/ClientHisory.cs",           "Assets/_App/History/ClientHisory.cs"),
            ("Assets/ConstantsData_M.cs",        "Assets/_App/Core/ConstantsData_M.cs"),
            ("Assets/GuestDataManager.cs",       "Assets/_App/Auth/GuestDataManager.cs"),
            ("Assets/ImageShowPanel.cs",         "Assets/_App/UI/ImageShowPanel.cs"),
            ("Assets/KeyPadInput.cs",            "Assets/_App/Platform/KeyPadInput.cs"),
            ("Assets/KeyboardAnimation.cs",      "Assets/_App/Platform/KeyboardAnimation.cs"),
            ("Assets/KeyboardHeightProvider.cs", "Assets/_App/Platform/KeyboardHeightProvider.cs"),
            ("Assets/KeyboardInputAdjuster.cs",  "Assets/_App/Platform/KeyboardInputAdjuster.cs"),
            ("Assets/MessageInfo.cs",            "Assets/_App/Core/MessageInfo.cs"),
            ("Assets/Microphone.cs",             "Assets/_App/Audio/Microphone.cs"),
            ("Assets/OptionPrefab.cs",           "Assets/_App/UI/OptionPrefab.cs"),
            ("Assets/PopupMessageManager.cs",    "Assets/_App/Core/PopupMessageManager.cs"),
            ("Assets/RulesDetail.cs",            "Assets/_App/Core/RulesDetail.cs"),
            ("Assets/SelectOver.cs",             "Assets/_App/UI/SelectOver.cs"),
            ("Assets/SimpleNotification.cs",     "Assets/_App/Notifications/SimpleNotification.cs"),
            ("Assets/SpriteAnimator.cs",         "Assets/_App/UI/SpriteAnimator.cs"),
            ("Assets/SpritesManager.cs",         "Assets/_App/UI/SpritesManager.cs"),
            ("Assets/TwelveBeadSoundManager.cs", "Assets/_App/Audio/TwelveBeadSoundManager.cs"),
            ("Assets/DummyHorse.cs",             "Assets/_Dev/DummyHorse.cs"),
        });

        r.Finish("Phase 6 done — commit & push.\n\nThen run:\n7. App Organizer\n8. Network Organizer");
    }

    // ── Shared ───────────────────────────────────────────────────────────────

    static bool Confirm(string title, string body) =>
        EditorUtility.DisplayDialog(title, body + "\n\nContinue?", "Yes", "Cancel");

    // Inner runner to keep per-phase state isolated
    class Runner
    {
        int moved, skipped, failed;
        List<string> log = new List<string>();

        public void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int s = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, s));
            AssetDatabase.CreateFolder(path.Substring(0, s), path.Substring(s + 1));
        }

        public void Pairs((string f, string t)[] pairs) { foreach (var (f, t) in pairs) MOM(f, t); }

        public void FolderMove(string from, string to)
        {
            if (!AssetDatabase.IsValidFolder(from)) { skipped++; return; }
            if (AssetDatabase.IsValidFolder(to))    { Merge(from, to); return; }
            string err = AssetDatabase.MoveAsset(from, to);
            if (string.IsNullOrEmpty(err)) { log.Add($"✓ {from}→{to}"); moved++; }
            else { log.Add($"✗ {from}: {err}"); failed++; }
        }

        public void FM(string from, string to)
        {
            bool src = AssetDatabase.IsValidFolder(from) || AssetDatabase.LoadAssetAtPath<Object>(from) != null;
            if (!src) { skipped++; return; }
            bool dst = AssetDatabase.IsValidFolder(to)   || AssetDatabase.LoadAssetAtPath<Object>(to) != null;
            if (dst) { log.Add($"⚠ exists:{to}"); skipped++; return; }
            EnsureFolder(to.Substring(0, to.LastIndexOf('/')));
            string err = AssetDatabase.MoveAsset(from, to);
            if (string.IsNullOrEmpty(err)) { log.Add($"✓ {Path.GetFileName(from)}"); moved++; }
            else { log.Add($"✗ {from}: {err}"); failed++; }
        }

        public void MOM(string from, string to)
        {
            if (!AssetDatabase.IsValidFolder(from)) { FM(from, to); return; }
            if (!AssetDatabase.IsValidFolder(to))   FolderMove(from, to);
            else                                     Merge(from, to);
        }

        public void Merge(string from, string to)
        {
            if (!AssetDatabase.IsValidFolder(from)) return;
            EnsureFolder(to);
            foreach (string sub in AssetDatabase.GetSubFolders(from))
                MOM(sub, $"{to}/{Path.GetFileName(sub)}");
            foreach (string guid in AssetDatabase.FindAssets("", new[]{ from }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(p).Replace('\\','/') == from)
                    FM(p, $"{to}/{Path.GetFileName(p)}");
            }
            if (AssetDatabase.FindAssets("", new[]{ from }).Length == 0)
                AssetDatabase.DeleteAsset(from);
        }

        public void Finish(string nextStep)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            foreach (var e in log) Debug.Log("[Restructure] " + e);
            string sum = $"Moved:{moved}  Skipped:{skipped}  Failed:{failed}\n\n{nextStep}";
            Debug.Log("[Restructure] " + sum);
            EditorUtility.DisplayDialog("Done", sum, "OK");
        }
    }
}
