// NetworkSceneSaver.cs  — Editor only
// Before saving any scene, re-activates all GameObjects that have a NetworkIdentity
// so they are never baked into the scene file as "inactive" due to Mirror's runtime deactivation.
#if UNITY_EDITOR
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NetworkSceneSaver
{
    static NetworkSceneSaver()
    {
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }

    private static void OnSceneSaving(Scene scene, string path)
    {
        int count = 0;
        foreach (var ni in Object.FindObjectsOfType<NetworkIdentity>(includeInactive: true))
        {
            if (!ni.gameObject.activeSelf)
            {
                ni.gameObject.SetActive(true);
                count++;
            }
        }

        if (count > 0)
            Debug.Log($"[NetworkSceneSaver] Re-activated {count} NetworkIdentity object(s) before saving '{scene.name}'.");
    }
}
#endif
