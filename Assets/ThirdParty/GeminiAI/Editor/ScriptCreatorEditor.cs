#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

public class ScriptCreatorEditor : EditorWindow
{
    private string scriptContent = "using UnityEngine;\n\npublic class NewScript : MonoBehaviour\n{\n    // Start is called before the first frame update\n    void Start()\n    {\n        \n    }\n\n    // Update is called once per frame\n    void Update()\n    {\n        \n    }\n}";
    private Vector2 scrollPosition;
    private GUIStyle textAreaStyle;

    // Variables to track script compilation
    private bool waitingForCompilation = false;
    private string pendingScriptName = null;
    private GameObject targetGameObject = null;
    private string pendingScriptPath = null;

    // Hard-coded font size
    private const int fontSize = 12;

    // Save path variables
    private string saveFolderPath = "Assets/Scripts";
    private bool attachToSelected = true;

    [MenuItem("Nasmo Studio/Script Creator")]
    public static void ShowWindow()
    {
        GetWindow<ScriptCreatorEditor>("Script Creator");
    }

    private void OnEnable()
    {
        textAreaStyle = new GUIStyle();
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnGUI()
    {
        // Main Title
        EditorGUILayout.Space();
        GUILayout.Label("Unity Script Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Help box
        EditorGUILayout.HelpBox("Write your script below and choose save location. Use buttons to create or create+attach.", MessageType.Info);
        EditorGUILayout.Space();

        // Save path selection
        EditorGUILayout.BeginHorizontal();
        saveFolderPath = EditorGUILayout.TextField("Save Location", saveFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.SaveFolderPanel("Select Script Save Location", saveFolderPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Convert to relative path if within Assets
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    saveFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Location", "Scripts must be saved within the Assets folder.", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // Set up the text area style with monospace font
        textAreaStyle = new GUIStyle(EditorStyles.textArea);
        textAreaStyle.font = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;
        if (textAreaStyle.font == null)
        {
            textAreaStyle = EditorStyles.textArea;
        }
        textAreaStyle.wordWrap = true;
        textAreaStyle.fontSize = fontSize;

        // Editor title
        EditorGUILayout.LabelField("Script Editor:");

        // Create a scroll view for the text area
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        scriptContent = EditorGUILayout.TextArea(scriptContent, textAreaStyle, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Action buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Script", GUILayout.Height(30)))
        {
            CreateScript(false);
        }
        if (GUILayout.Button("Create & Attach", GUILayout.Height(30)))
        {
            CreateScript(true);
        }
        EditorGUILayout.EndHorizontal();

        // Display status if waiting for compilation
        if (waitingForCompilation)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Waiting for script compilation...", MessageType.Info);
        }
    }

    private void OnEditorUpdate()
    {
        // Check if we're waiting for compilation and if it's done
        if (waitingForCompilation && !EditorApplication.isCompiling)
        {
            waitingForCompilation = false;

            // Try to attach the script to the GameObject
            TryAttachScript();
        }
    }

    private void CreateScript(bool attachToGameObject)
    {
        string className = ExtractClassName(scriptContent);

        if (string.IsNullOrEmpty(className))
        {
            EditorUtility.DisplayDialog("Invalid Script", "Could not extract class name from the script. Make sure your script contains a valid class declaration.", "OK");
            return;
        }

        // Validate save path
        if (string.IsNullOrEmpty(saveFolderPath))
        {
            saveFolderPath = "Assets/Scripts";
        }

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(saveFolderPath))
        {
            string[] folderPath = saveFolderPath.Split('/');
            string parentFolder = folderPath[0];

            for (int i = 1; i < folderPath.Length; i++)
            {
                string folder = folderPath[i];
                string currentPath = parentFolder + "/" + folder;

                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(parentFolder, folder);
                }

                parentFolder = currentPath;
            }
        }

        // Set file path
        string filePath = Path.Combine(Application.dataPath, saveFolderPath.Substring(7), className + ".cs");
        string relativePath = Path.Combine(saveFolderPath, className + ".cs");

        // Check if file already exists
        if (File.Exists(filePath))
        {
            bool overwrite = EditorUtility.DisplayDialog("File Already Exists",
                $"A script named '{className}.cs' already exists at the target location. Do you want to overwrite it?",
                "Overwrite", "Cancel");

            if (!overwrite)
            {
                return;
            }
        }

        // Write the script to the file
        File.WriteAllText(filePath, scriptContent);
        AssetDatabase.Refresh();

        if (attachToGameObject)
        {
            if (Selection.activeGameObject != null)
            {
                // Store the variables for attachment after compilation
                targetGameObject = Selection.activeGameObject;
                pendingScriptName = className;
                pendingScriptPath = relativePath;

                // Set flag to wait for compilation
                waitingForCompilation = true;
            }
            else
            {
                EditorUtility.DisplayDialog("No GameObject Selected", "Please select a GameObject in the hierarchy to attach the script to.", "OK");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Script Created", $"Script '{className}.cs' was successfully created at '{saveFolderPath}'.", "OK");
        }
    }

    private void TryAttachScript()
    {
        if (targetGameObject == null || string.IsNullOrEmpty(pendingScriptName))
        {
            return;
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
                    .FirstOrDefault(t => t.Name == pendingScriptName && typeof(MonoBehaviour).IsAssignableFrom(t));
            }

            // If not found, try other assemblies
            if (scriptType == null)
            {
                foreach (var asm in assemblies)
                {
                    try
                    {
                        var types = asm.GetTypes();
                        scriptType = types.FirstOrDefault(t => t.Name == pendingScriptName && typeof(MonoBehaviour).IsAssignableFrom(t));
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
                EditorUtility.DisplayDialog("Success", $"Script '{pendingScriptName}' created and attached to '{targetGameObject.name}'.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Warning",
                    $"Script '{pendingScriptName}' was created but couldn't be automatically attached. Please add it manually through the Add Component menu.",
                    "OK");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error attaching script: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("Error",
                $"An error occurred while attaching the script. The script was created but you may need to add it manually.",
                "OK");
        }
        finally
        {
            // Clear the pending script variables
            pendingScriptName = null;
            targetGameObject = null;
            pendingScriptPath = null;
        }
    }

    private string ExtractClassName(string scriptContent)
    {
        // Regular expression to find class declaration
        // This looks for: public/private/internal/etc class ClassName : inheritance
        // Or just: class ClassName
        string pattern = @"(?:public|private|protected|internal)?\s+class\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*(?::\s*[a-zA-Z_][a-zA-Z0-9_.,<>\s]*)?";

        Match match = Regex.Match(scriptContent, pattern);

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value;
        }

        return string.Empty;
    }
}
#endif