using System.Collections.Generic;
using Mirror;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// Only edits the build's scene copy. Source scenes and client builds stay intact.
public sealed class DedicatedServerSceneStripper : IProcessSceneWithReport
{
    public int callbackOrder => 1000;

    private static readonly HashSet<string> BootRoots = new HashSet<string>
    {
        "LoadingManager", "Mirror Network", "APIManager", "SpritesManager",
        "SoundManagerMain", "LocalizationForCricket", "ServerBootSettings"
    };

    private static readonly HashSet<string> LobbyRoots = new HashSet<string>
    {
        "DataManager", "GameManagerMainMenu"
    };

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || report.summary.platform != BuildTarget.StandaloneLinux64 ||
            EditorUserBuildSettings.standaloneBuildSubtarget != StandaloneBuildSubtarget.Server ||
            ServerMemoryBuild.KeepClientUI)
            return;

        HashSet<string> keep;
        if (scene.path == "Assets/Scenes/LoadingScene.unity")
            keep = BootRoots;
        else if (scene.path == "Assets/_Games/8Ball pool/BallPoolGame/MultiplayerSystem/Scenes/Home.unity")
            keep = LobbyRoots;
        else
            return; // Gameplay hierarchies can contain server logic inside UI objects.

        if (scene.path == "Assets/Scenes/LoadingScene.unity")
            PrepareBoot(scene);

        int objects = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // Never prune a network prefab or transport because its root was renamed.
            if (keep.Contains(root.name) || root.GetComponentInChildren<NetworkIdentity>(true) != null ||
                root.GetComponentInChildren<NetworkManager>(true) != null ||
                root.GetComponentInChildren<Transport>(true) != null ||
                root.GetComponentInChildren<BEKStudio.GameManager>(true) != null ||
                root.GetComponentInChildren<Snake_Ladder.GameManager>(true) != null)
                continue;

            objects += root.GetComponentsInChildren<Transform>(true).Length;
            Object.DestroyImmediate(root);
        }

        Debug.Log($"[ServerStrip] {scene.name}: removed {objects} client objects; kept {scene.rootCount} roots.");
    }

    private static void PrepareBoot(Scene scene)
    {
        // RCC is still present in the car game scene and can also be created lazily
        // by RCC_Singleton. Only its eager load in LoadingScene is removed.
        var settings = AssetDatabase.LoadAssetAtPath<CarRace.RCC_Settings>(
            "Assets/ThirdParty/RealisticCarControllerV3/Resources/RCC Assets/RCC_Settings.asset");
        if (settings == null)
            throw new BuildFailedException("Cannot preserve server physics timing: RCC_Settings is missing.");

        var boot = new GameObject("ServerBootSettings");
        SceneManager.MoveGameObjectToScene(boot, scene);
        var timing = boot.AddComponent<ServerBootSettings>();
        timing.overrideFixedTimeStep = settings.overrideFixedTimeStep;
        timing.fixedTimeStep = settings.fixedTimeStep;
        timing.overrideFrameRate = settings.overrideFPS;
        timing.frameRate = settings.maxFPS;

        // Keep the holder and array indices for legacy scripts, but sever its
        // cosmetic asset dependencies in the build copy only.
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (SpritesManager manager in root.GetComponentsInChildren<SpritesManager>(true))
        {
            if (manager.spritesScriptable == null) continue;
            var holder = Object.Instantiate(manager.spritesScriptable);
            holder.name = "Server cosmetic placeholders";
            var serialized = new SerializedObject(holder);
            var property = serialized.GetIterator();
            int cleared = 0;
            while (property.Next(true))
            {
                if (property.propertyPath == "m_Script" ||
                    property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;
                if (property.objectReferenceValue != null) cleared++;
                property.objectReferenceValue = null;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            manager.spritesScriptable = holder;
            Debug.Log($"[ServerStrip] Cleared {cleared} cosmetic asset references from server sprite catalog.");
        }
    }
}
