using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class RemoveMissingScripts : EditorWindow
{
    private Vector2 scrollPosition;
    private List<GameObject> objectsWithMissingScripts = new List<GameObject>();
    private bool showResults = false;
    private int totalMissingScripts = 0;

    [MenuItem("Tools/Remove Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow<RemoveMissingScripts>("Remove Missing Scripts");
    }

    void OnGUI()
    {
        GUILayout.Label("Remove Missing Script Components", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("This tool will help you find and remove missing script components from GameObjects in your project.", MessageType.Info);
        GUILayout.Space(10);

        // Scan buttons
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Scan Current Scene", GUILayout.Height(30)))
        {
            ScanCurrentScene();
        }
        
        if (GUILayout.Button("Scan All Scenes", GUILayout.Height(30)))
        {
            ScanAllScenes();
        }
        
        if (GUILayout.Button("Scan Project Assets", GUILayout.Height(30)))
        {
            ScanProjectAssets();
        }
        
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Results section
        if (showResults)
        {
            GUILayout.Label($"Found {totalMissingScripts} missing script(s) on {objectsWithMissingScripts.Count} GameObject(s)", EditorStyles.boldLabel);
            
            if (objectsWithMissingScripts.Count > 0)
            {
                GUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Remove All Missing Scripts", GUILayout.Height(25)))
                {
                    RemoveAllMissingScripts();
                }
                
                if (GUILayout.Button("Clear Results", GUILayout.Height(25)))
                {
                    ClearResults();
                }
                
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                
                // Scrollable list of objects
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
                
                foreach (GameObject obj in objectsWithMissingScripts)
                {
                    if (obj != null)
                    {
                        GUILayout.BeginHorizontal();
                        
                        // Object field (clickable)
                        EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                        
                        // Individual remove button
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            RemoveMissingScriptsFromObject(obj);
                            RefreshResults();
                        }
                        
                        GUILayout.EndHorizontal();
                    }
                }
                
                GUILayout.EndScrollView();
            }
        }
    }

    private void ScanCurrentScene()
    {
        ClearResults();
        
        // Find all GameObjects in current scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (HasMissingScripts(obj))
            {
                objectsWithMissingScripts.Add(obj);
                totalMissingScripts += GetMissingScriptCount(obj);
            }
        }
        
        showResults = true;
        Debug.Log($"Scene scan complete. Found {totalMissingScripts} missing scripts on {objectsWithMissingScripts.Count} objects.");
    }

    private void ScanAllScenes()
    {
        ClearResults();
        
        // Get all scenes in build settings
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        
        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            EditorUtility.DisplayProgressBar("Scanning Scenes", $"Scanning: {scenePath}", 0.5f);
            
            // Open scene additively to scan
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            
            GameObject[] sceneObjects = scene.GetRootGameObjects();
            foreach (GameObject rootObj in sceneObjects)
            {
                ScanGameObjectRecursively(rootObj);
            }
            
            EditorSceneManager.CloseScene(scene, true);
        }
        
        EditorUtility.ClearProgressBar();
        showResults = true;
        Debug.Log($"All scenes scan complete. Found {totalMissingScripts} missing scripts on {objectsWithMissingScripts.Count} objects.");
    }

    private void ScanProjectAssets()
    {
        ClearResults();
        
        // Find all prefabs in project
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            EditorUtility.DisplayProgressBar("Scanning Prefabs", $"Scanning: {prefabPath}", (float)i / prefabGuids.Length);
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                ScanGameObjectRecursively(prefab);
            }
        }
        
        EditorUtility.ClearProgressBar();
        showResults = true;
        Debug.Log($"Project assets scan complete. Found {totalMissingScripts} missing scripts on {objectsWithMissingScripts.Count} objects.");
    }

    private void ScanGameObjectRecursively(GameObject obj)
    {
        if (HasMissingScripts(obj))
        {
            objectsWithMissingScripts.Add(obj);
            totalMissingScripts += GetMissingScriptCount(obj);
        }
        
        // Check children
        foreach (Transform child in obj.transform)
        {
            ScanGameObjectRecursively(child.gameObject);
        }
    }

    private bool HasMissingScripts(GameObject obj)
    {
        Component[] components = obj.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
                return true;
        }
        return false;
    }

    private int GetMissingScriptCount(GameObject obj)
    {
        int count = 0;
        Component[] components = obj.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
                count++;
        }
        return count;
    }

    private void RemoveMissingScriptsFromObject(GameObject obj)
    {
        if (obj == null) return;
        
        // Record for undo
        Undo.RegisterCompleteObjectUndo(obj, "Remove Missing Scripts");
        
        // Use GameObjectUtility to remove missing scripts (Unity 2019.1+)
        int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
        
        if (removedCount > 0)
        {
            Debug.Log($"Removed {removedCount} missing script(s) from {obj.name}");
            EditorUtility.SetDirty(obj);
        }
    }

    private void RemoveAllMissingScripts()
    {
        if (objectsWithMissingScripts.Count == 0) return;
        
        bool confirm = EditorUtility.DisplayDialog(
            "Remove All Missing Scripts",
            $"Are you sure you want to remove all {totalMissingScripts} missing script(s) from {objectsWithMissingScripts.Count} GameObject(s)?",
            "Yes", "Cancel"
        );
        
        if (!confirm) return;
        
        for (int i = 0; i < objectsWithMissingScripts.Count; i++)
        {
            GameObject obj = objectsWithMissingScripts[i];
            if (obj != null)
            {
                EditorUtility.DisplayProgressBar("Removing Missing Scripts", $"Processing: {obj.name}", (float)i / objectsWithMissingScripts.Count);
                RemoveMissingScriptsFromObject(obj);
            }
        }
        
        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Successfully removed {totalMissingScripts} missing scripts from {objectsWithMissingScripts.Count} objects.");
        ClearResults();
    }

    private void RefreshResults()
    {
        // Remove objects that no longer have missing scripts
        objectsWithMissingScripts.RemoveAll(obj => obj == null || !HasMissingScripts(obj));
        
        // Recalculate total
        totalMissingScripts = 0;
        foreach (GameObject obj in objectsWithMissingScripts)
        {
            if (obj != null)
            {
                totalMissingScripts += GetMissingScriptCount(obj);
            }
        }
        
        if (objectsWithMissingScripts.Count == 0)
        {
            showResults = false;
        }
    }

    private void ClearResults()
    {
        objectsWithMissingScripts.Clear();
        totalMissingScripts = 0;
        showResults = false;
    }
}

// Additional utility class for context menu operations
public class MissingScriptUtility
{
    [MenuItem("GameObject/Remove Missing Scripts", false, 0)]
    public static void RemoveMissingScriptsFromSelection()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected.");
            return;
        }
        
        int totalRemoved = 0;
        
        foreach (GameObject obj in selectedObjects)
        {
            Undo.RegisterCompleteObjectUndo(obj, "Remove Missing Scripts");
            
            // Use GameObjectUtility to remove missing scripts
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            totalRemoved += removedCount;
            
            if (removedCount > 0)
            {
                EditorUtility.SetDirty(obj);
            }
        }
        
        if (totalRemoved > 0)
        {
            Debug.Log($"Removed {totalRemoved} missing script(s) from {selectedObjects.Length} selected object(s).");
        }
        else
        {
            Debug.Log("No missing scripts found on selected objects.");
        }
    }
    
    [MenuItem("GameObject/Remove Missing Scripts", true)]
    public static bool ValidateRemoveMissingScriptsFromSelection()
    {
        return Selection.gameObjects.Length > 0;
    }
}