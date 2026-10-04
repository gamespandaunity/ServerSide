using UnityEngine;
using UnityEditor;

public class ThirdPartyMover : EditorWindow
{
    static readonly string[] ThirdPartyFolders = new string[]
    {
        "Mirror",
        "Mirror 1",
        "Photon",
        "Firebase",
        "TextMesh Pro",
        "NaughtyAttributes",
        "ExternalDependencyManager",
        "FacebookSDK",
        "UniWebView",
        "UnityCipher",
        "Thirdweb",
        "RealisticCarControllerV3",
        "edgegap-unity-plugin",
        "PlayAssetDelivery",
        "Epic Toon FX",
        "JMO Assets",
        "Dragon Arts",
        "AlmostEngine",
        "ShinyEffectForUGUI",
        "Simple Toon",
        "ULTIMATE PARTICLE PACK",
        "PlayerPrefsEditor",
        "FindReference2",
        "Parse",
        "ScreenshotUtility",
        "Samples",
        "GeminiAI",
        "MiniMap",
        "Image loader",
        "JSON",
        "Keyboard Package",
        "TSF",
        "PixelArt",
    };

    [MenuItem("Tools/Structure/Move ThirdParty Folders")]
    public static void MoveThirdPartyFolders()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Move ThirdParty Folders",
            "This will move all third-party folders into Assets/ThirdParty/.\n\nMake sure you have committed to git before proceeding.\n\nContinue?",
            "Yes, Move",
            "Cancel"
        );

        if (!confirm) return;

        if (!AssetDatabase.IsValidFolder("Assets/ThirdParty"))
            AssetDatabase.CreateFolder("Assets", "ThirdParty");

        int moved = 0;
        int skipped = 0;
        int failed = 0;

        for (int i = 0; i < ThirdPartyFolders.Length; i++)
        {
            string folder = ThirdPartyFolders[i];
            string sourcePath = "Assets/" + folder;
            string destPath = "Assets/ThirdParty/" + folder;

            EditorUtility.DisplayProgressBar(
                "Moving ThirdParty Folders",
                $"Moving: {folder}",
                (float)i / ThirdPartyFolders.Length
            );

            if (!AssetDatabase.IsValidFolder(sourcePath))
            {
                Debug.LogWarning($"[ThirdPartyMover] Skipped '{folder}' — not found");
                skipped++;
                continue;
            }

            if (AssetDatabase.IsValidFolder(destPath))
            {
                Debug.LogWarning($"[ThirdPartyMover] Skipped '{folder}' — already in ThirdParty/");
                skipped++;
                continue;
            }

            string error = AssetDatabase.MoveAsset(sourcePath, destPath);
            if (string.IsNullOrEmpty(error))
            {
                Debug.Log($"[ThirdPartyMover] Moved: {folder}");
                moved++;
            }
            else
            {
                Debug.LogError($"[ThirdPartyMover] Failed '{folder}': {error}");
                failed++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary = $"Done!\n\nMoved: {moved}\nSkipped: {skipped}\nFailed: {failed}";
        Debug.Log($"[ThirdPartyMover] {summary}");
        EditorUtility.DisplayDialog("ThirdParty Mover — Done", summary, "OK");
    }
}
