#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ScriptUtils
{
    public static string ExtractClassName(string scriptContent)
    {
        // Regular expression to find class declaration
        // This looks for: public/private/protected/internal/? class ClassName : inheritance
        // Or just: class ClassName
        string pattern = @"(?:public|private|protected|internal)?\s+class\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*(?::\s*[a-zA-Z_][a-zA-Z0-9_.,<>\s]*)?";
        
        Match match = Regex.Match(scriptContent, pattern);
        
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }
        
        return string.Empty;
    }
    
    public static void CreateDirectoryIfNotExists(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] folders = path.Split('/');
        string currentPath = folders[0]; // Start with "Assets"

        for (int i = 1; i < folders.Length; i++)
        {
            string nextPath = currentPath + "/" + folders[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            }

            currentPath = nextPath;
        }
    }
    
    public static bool TryAttachScriptToGameObject(string scriptName, GameObject targetGameObject)
    {
        if (targetGameObject == null || string.IsNullOrEmpty(scriptName))
        {
            return false;
        }
        
        try
        {
            // Try to find the type by name in all loaded assemblies
            System.Type scriptType = null;
            
            // First check in Assembly-CSharp which is where most Unity scripts go
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            var assembly = assemblies.FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            
            if (assembly != null)
            {
                // Try to find the type with or without namespace
                scriptType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == scriptName && typeof(MonoBehaviour).IsAssignableFrom(t));
            }
            
            // If not found, try other assemblies
            if (scriptType == null)
            {
                foreach (var asm in assemblies)
                {
                    try
                    {
                        var types = asm.GetTypes();
                        scriptType = types.FirstOrDefault(t => t.Name == scriptName && typeof(MonoBehaviour).IsAssignableFrom(t));
                        if (scriptType != null)
                            break;
                    }
                    catch
                    {
                        // Ignore errors from assemblies we can't read
                    }
                }
            }
            
            if (scriptType != null)
            {
                targetGameObject.AddComponent(scriptType);
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error attaching script: {e.Message}\n{e.StackTrace}");
        }
        
        return false;
    }
}
#endif