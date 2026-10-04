using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class GUIDChanger : EditorWindow
{
    private Object targetAsset;
    private string newGUID = "";
    private bool showAdvanced = false;
    private Vector2 scrollPosition;
    private int referenceCount = -1;
    private List<string> referenceFiles = new List<string>();
    private bool showReferences = false;

    [MenuItem("Nasmo Studio/GUID Changer")]
    public static void ShowWindow()
    {
        GetWindow<GUIDChanger>("GUID Changer");
    }

    void OnGUI()
    {
        GUILayout.Label("GUID Changer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("Warning: This operation modifies project files. Make sure to backup your project first!", MessageType.Warning);
        GUILayout.Space(10);

        // Asset selection
        targetAsset = EditorGUILayout.ObjectField("Target Asset", targetAsset, typeof(Object), false);

        if (targetAsset != null)
        {
            string currentPath = AssetDatabase.GetAssetPath(targetAsset);
            string currentGUID = AssetDatabase.AssetPathToGUID(currentPath);

            EditorGUILayout.LabelField("Current Path:", currentPath);
            EditorGUILayout.LabelField("Current GUID:", currentGUID);

            // Show reference count
            if (referenceCount == -1)
            {
                if (GUILayout.Button("Scan for References"))
                {
                    ScanForReferences(currentGUID);
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("References Found:", referenceCount.ToString());
                if (GUILayout.Button("Rescan", GUILayout.Width(60)))
                {
                    ScanForReferences(currentGUID);
                }
                GUILayout.EndHorizontal();

                if (referenceCount > 0)
                {
                    showReferences = EditorGUILayout.Foldout(showReferences, $"Show Reference Files ({referenceCount})");
                    if (showReferences)
                    {
                        EditorGUI.indentLevel++;
                        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(150));

                        foreach (string file in referenceFiles)
                        {
                            string relativePath = file.StartsWith(Application.dataPath) ?
                                "Assets" + file.Substring(Application.dataPath.Length) : file;

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("• " + relativePath, EditorStyles.miniLabel);
                            if (GUILayout.Button("Ping", GUILayout.Width(40)))
                            {
                                Object obj = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
                                if (obj != null)
                                {
                                    EditorGUIUtility.PingObject(obj);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }

                        EditorGUILayout.EndScrollView();
                        EditorGUI.indentLevel--;
                    }
                }
            }

            GUILayout.Space(10);

            newGUID = EditorGUILayout.TextField("New GUID:", newGUID);

            if (string.IsNullOrEmpty(newGUID))
            {
                if (GUILayout.Button("Generate New GUID"))
                {
                    newGUID = System.Guid.NewGuid().ToString("N");
                }
            }

            GUILayout.Space(10);

            // Validation
            bool isValidGUID = IsValidGUID(newGUID);
            bool isUnique = isValidGUID && IsGUIDUnique(newGUID);

            if (!string.IsNullOrEmpty(newGUID))
            {
                if (!isValidGUID)
                {
                    EditorGUILayout.HelpBox("Invalid GUID format. Should be 32 hexadecimal characters.", MessageType.Error);
                }
                else if (!isUnique)
                {
                    EditorGUILayout.HelpBox("GUID already exists in project!", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("GUID is valid and unique.", MessageType.Info);
                }
            }

            GUILayout.Space(10);

            // Advanced options
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Options");
            if (showAdvanced)
            {
                EditorGUILayout.HelpBox("The script will automatically:\n• Update the .meta file\n• Find and update all references in scenes, prefabs, and other assets\n• Refresh the AssetDatabase", MessageType.Info);
            }

            GUILayout.Space(10);

            GUI.enabled = isValidGUID && isUnique && !string.IsNullOrEmpty(newGUID);
            if (GUILayout.Button("Change GUID", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm GUID Change",
                    $"Are you sure you want to change the GUID of '{targetAsset.name}'?\n\nThis operation cannot be undone easily. Make sure you have a backup!",
                    "Yes, Change GUID", "Cancel"))
                {
                    ChangeAssetGUID(currentPath, currentGUID, newGUID);
                }
            }
            GUI.enabled = true;
        }
        else
        {
            EditorGUILayout.HelpBox("Select an asset to change its GUID.", MessageType.Info);
            referenceCount = -1;
            referenceFiles.Clear();
        }
    }

    private void ScanForReferences(string guid)
    {
        referenceFiles = FindFilesWithGUIDReferences(guid);
        referenceCount = referenceFiles.Count;
        Debug.Log($"Found {referenceCount} references to GUID {guid}");
    }

    private bool IsValidGUID(string guid)
    {
        if (string.IsNullOrEmpty(guid) || guid.Length != 32)
            return false;

        return Regex.IsMatch(guid, @"^[a-fA-F0-9]{32}$");
    }

    private bool IsGUIDUnique(string guid)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path);
    }

    private void ChangeAssetGUID(string assetPath, string oldGUID, string newGUID)
    {
        try
        {
            EditorUtility.DisplayProgressBar("Changing GUID", "Starting process...", 0f);

            // Step 1: Update the .meta file
            string metaPath = assetPath + ".meta";
            if (File.Exists(metaPath))
            {
                EditorUtility.DisplayProgressBar("Changing GUID", "Updating .meta file...", 0.2f);
                string metaContent = File.ReadAllText(metaPath);
                metaContent = metaContent.Replace($"guid: {oldGUID}", $"guid: {newGUID}");
                File.WriteAllText(metaPath, metaContent);
            }

            // Step 2: Find all files that might reference this GUID
            EditorUtility.DisplayProgressBar("Changing GUID", "Scanning project files...", 0.4f);
            List<string> filesToUpdate = FindFilesWithGUIDReferences(oldGUID);

            // Step 3: Update references
            float progress = 0.4f;
            float progressStep = 0.4f / Mathf.Max(filesToUpdate.Count, 1);

            foreach (string filePath in filesToUpdate)
            {
                EditorUtility.DisplayProgressBar("Changing GUID", $"Updating references in {Path.GetFileName(filePath)}...", progress);
                UpdateGUIDReferencesInFile(filePath, oldGUID, newGUID);
                progress += progressStep;
            }

            // Step 4: Refresh AssetDatabase
            EditorUtility.DisplayProgressBar("Changing GUID", "Refreshing Asset Database...", 0.9f);
            AssetDatabase.Refresh();

            EditorUtility.DisplayProgressBar("Changing GUID", "Complete!", 1f);

            Debug.Log($"Successfully changed GUID from {oldGUID} to {newGUID}. Updated {filesToUpdate.Count} files.");

            // Clear the fields
            targetAsset = null;
            newGUID = "";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error changing GUID: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to change GUID: {e.Message}", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private List<string> FindFilesWithGUIDReferences(string guid)
    {
        List<string> matchingFiles = new List<string>();

        // Search in common Unity file types
        string[] searchPatterns = { "*.unity", "*.prefab", "*.asset", "*.mat", "*.controller", "*.anim", "*.cs", "*.js" };

        foreach (string pattern in searchPatterns)
        {
            string[] files = Directory.GetFiles(Application.dataPath, pattern, SearchOption.AllDirectories);

            foreach (string file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    if (content.Contains(guid))
                    {
                        matchingFiles.Add(file);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not read file {file}: {e.Message}");
                }
            }
        }

        return matchingFiles;
    }

    private void UpdateGUIDReferencesInFile(string filePath, string oldGUID, string newGUID)
    {
        try
        {
            string content = File.ReadAllText(filePath);

            // Common GUID reference patterns in Unity files
            string[] patterns = {
                $"guid: {oldGUID}",           // YAML format
                $"\"guid\":\"{oldGUID}\"",    // JSON format
                $"guid=\"{oldGUID}\"",        // XML format
                oldGUID                       // Direct reference
            };

            string[] replacements = {
                $"guid: {newGUID}",
                $"\"guid\":\"{newGUID}\"",
                $"guid=\"{newGUID}\"",
                newGUID
            };

            bool modified = false;
            for (int i = 0; i < patterns.Length; i++)
            {
                if (content.Contains(patterns[i]))
                {
                    content = content.Replace(patterns[i], replacements[i]);
                    modified = true;
                }
            }

            if (modified)
            {
                File.WriteAllText(filePath, content);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating file {filePath}: {e.Message}");
        }
    }
}