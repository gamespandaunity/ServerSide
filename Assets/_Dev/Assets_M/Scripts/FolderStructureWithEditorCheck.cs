
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.WSA;

public class FolderStructure : Editor
{
    [MenuItem("Nasmo Studio/Create Default Folders")]
    public static void CreateDefaultFolders()
    {
        if (!AssetDatabase.IsValidFolder("1.Mohsin"))
        {
            AssetDatabase.CreateFolder("Assets", "1.Mohsin");
            //Debug.Log($"Created folder: {"1.Mohsin"}");
        }
        else
        {
            //Debug.Log($"Folder already exists: {"1.Mohsin"}");
        }
        // Define the folder structure
        string[] folders = {
            "Scenes",
            "Scripts",
            "Prefabs",
            "Materials",
            "Textures",
            "Audio",
            "Animations",
            "UI",
            "Resources",
            "Editor"
        };

        // Create each folder if it doesn't exist
        foreach (string folder in folders)
        {
            string folderPath = "Assets/1.Mohsin/" + folder;
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/1.Mohsin", folder);
                //Debug.Log($"Created folder: {folderPath}");
            }
            else
            {
                //Debug.Log($"Folder already exists: {folderPath}");
            }
        }

        // Refresh the AssetDatabase to reflect changes
        AssetDatabase.Refresh();
    }
}
#endif
