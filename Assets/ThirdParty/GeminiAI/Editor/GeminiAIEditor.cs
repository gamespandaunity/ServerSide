#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using GeminiScriptGenerator;
using System.Threading.Tasks;

public class GeminiAIEditor : EditorWindow
{
    private string scriptContent = "using UnityEngine;\n\npublic class NewScript : MonoBehaviour\n{\n    // Start is called before the first frame update\n    void Start()\n    {\n        \n    }\n\n    // Update is called once per frame\n    void Update()\n    {\n        \n    }\n}";
    private Vector2 scrollPosition;
    private Vector2 mainScrollPosition; // Added for main window scrolling
    private GUIStyle textAreaStyle;
    
    // Variables to track script compilation
    private bool waitingForCompilation = false;
    private string pendingScriptName = null;
    private GameObject targetGameObject = null;
    private string pendingScriptPath = null;
    
    // Gemini API variables
    private string promptInput = "";
    private bool isGenerating = false;
    private GeminiAPISettings apiSettings;
    
    // Hard-coded font size
    private const int fontSize = 12;
    
    // Variables for typewriter effect
    private string targetScriptContent;
    private string currentDisplayContent;
    private bool isTyping = false;
    private float lastTypeTime;
    private int currentTypeIndex = 0;
    
    // Script modification variables
    private bool isModificationMode = false;
    private string originalScriptPath = null;
    private string originalScriptName = null;
    private string modificationPrompt = "";
    private bool isModifying = false;
    private MonoScript selectedScript = null;

    // Path selection variables
    private string scriptSavePath = "Assets/Scripts";
    private bool showPathSelection = false;

    // Syntax highlighting variables
    private Dictionary<string, Color> keywordColors;
    private Dictionary<string, Color> typeColors;
    private Color commentColor = new Color(0.4f, 0.7f, 0.4f);
    private Color stringColor = new Color(0.8f, 0.6f, 0.4f);
    private bool syntaxHighlightingEnabled = true;
    
    [MenuItem("Nasmo Studio/Gemini Script Generator")]
    public static void ShowWindow()
    {
        GetWindow<GeminiAIEditor>("Gemini Script Generator");
    }

    private void OnEnable()
    {
        textAreaStyle = new GUIStyle();
        EditorApplication.update += OnEditorUpdate;

        // Initialize syntax highlighting colors
        InitializeSyntaxColors();

        // Initialize default script path
        if (string.IsNullOrEmpty(scriptSavePath))
        {
            scriptSavePath = "Assets/Scripts";
        }

        // Try to find the GeminiAPISettings asset
        string[] guids = AssetDatabase.FindAssets("t:GeminiAPISettings");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            apiSettings = AssetDatabase.LoadAssetAtPath<GeminiAPISettings>(path);
        }

        // Initialize typewriter content
        currentDisplayContent = scriptContent;
        targetScriptContent = scriptContent;
    }
    private void InitializeSyntaxColors()
    {
        keywordColors = new Dictionary<string, Color>
        {
            {"public", new Color(0.3f, 0.5f, 1f)},
            {"private", new Color(0.3f, 0.5f, 1f)},
            {"protected", new Color(0.3f, 0.5f, 1f)},
            {"static", new Color(0.3f, 0.5f, 1f)},
            {"void", new Color(0.3f, 0.5f, 1f)},
            {"int", new Color(0.3f, 0.5f, 1f)},
            {"string", new Color(0.3f, 0.5f, 1f)},
            {"bool", new Color(0.3f, 0.5f, 1f)},
            {"float", new Color(0.3f, 0.5f, 1f)},
            {"if", new Color(0.8f, 0.3f, 0.8f)},
            {"else", new Color(0.8f, 0.3f, 0.8f)},
            {"for", new Color(0.8f, 0.3f, 0.8f)},
            {"while", new Color(0.8f, 0.3f, 0.8f)},
            {"return", new Color(0.8f, 0.3f, 0.8f)},
            {"class", new Color(0.3f, 0.5f, 1f)},
            {"using", new Color(0.3f, 0.5f, 1f)}
        };

        typeColors = new Dictionary<string, Color>
        {
            {"MonoBehaviour", new Color(0.4f, 0.8f, 0.4f)},
            {"GameObject", new Color(0.4f, 0.8f, 0.4f)},
            {"Transform", new Color(0.4f, 0.8f, 0.4f)},
            {"Vector3", new Color(0.4f, 0.8f, 0.4f)},
            {"Vector2", new Color(0.4f, 0.8f, 0.4f)}
        };
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnGUI()
    {
        // Start main scroll view to ensure all content is accessible
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);

        // Main Title
        EditorGUILayout.Space();
        GUILayout.Label("Unity Script Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // API Settings section
        DrawAPISettingsSection();
        EditorGUILayout.Space();

        // Mode Selection
        DrawModeSelectionSection();
        EditorGUILayout.Space();

        if (isModificationMode)
        {
            DrawScriptModificationSection();
        }
        else
        {
            DrawScriptGenerationSection();
        }

        EditorGUILayout.Space();

        // Manual Editor Section
        DrawScriptEditorSection();
        EditorGUILayout.Space();

        // Path selection UI (only for new script mode)
        if (!isModificationMode)
        {
            DrawPathSelectionUI();
            EditorGUILayout.Space();
        }

        // Create or Save button based on mode
        DrawActionButtons();

        // Display status if waiting for compilation
        if (waitingForCompilation)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Waiting for script compilation...", MessageType.Info);
        }

        // End the main scroll view
        EditorGUILayout.EndScrollView();

        // Auto-scroll to make buttons visible if they're outside the window's visible area
        EnsureButtonsVisibility();

        // Update typewriter effect
        UpdateTypewriterEffect();
    }

    private void DrawAPISettingsSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("API Settings", EditorStyles.boldLabel);

        if (apiSettings == null)
        {
            EditorGUILayout.HelpBox("Gemini API Settings not found. Please create or select a settings asset.", MessageType.Warning);

            if (GUILayout.Button("Create API Settings Asset"))
            {
                CreateApiSettingsAsset();
            }
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Using API Settings:", EditorStyles.boldLabel);

            // Display reference to the settings asset
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(apiSettings, typeof(GeminiAPISettings), false);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Change", GUILayout.Width(60)))
            {
                SelectApiSettingsAsset();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawModeSelectionSection()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        isModificationMode = EditorGUILayout.ToggleLeft("Modify Existing Script", isModificationMode, EditorStyles.boldLabel, GUILayout.Width(200));
        if (EditorGUI.EndChangeCheck())
        {
            // Reset appropriate fields when switching modes
            if (isModificationMode)
            {
                selectedScript = null;
                originalScriptPath = null;
                originalScriptName = null;
                modificationPrompt = "";
            }
            else
            {
                // Reset to new script mode
                promptInput = "";
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawScriptModificationSection()
    {
        // Script Modification Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Script Modification", EditorStyles.boldLabel);

        // Script Selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Select Script:", GUILayout.Width(100));
        MonoScript prevScript = selectedScript;
        selectedScript = (MonoScript)EditorGUILayout.ObjectField(selectedScript, typeof(MonoScript), false);

        if (selectedScript != prevScript && selectedScript != null)
        {
            LoadScriptContent();
        }
        EditorGUILayout.EndHorizontal();

        // Script browse button
        if (GUILayout.Button("Browse Scripts"))
        {
            string path = EditorUtility.OpenFilePanel("Select C# Script", "Assets", "cs");
            if (!string.IsNullOrEmpty(path))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(relativePath);
                if (script != null)
                {
                    selectedScript = script;
                    LoadScriptContent();
                }
            }
        }

        EditorGUILayout.Space();

        // Modification prompt
        EditorGUILayout.LabelField("Modification Instructions:");
        modificationPrompt = EditorGUILayout.TextArea(modificationPrompt, GUILayout.Height(80));

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(modificationPrompt) || selectedScript == null || apiSettings == null || isModifying);
        if (GUILayout.Button("Modify Script", GUILayout.Height(30)))
        {
            ModifyScriptWithAI();
        }
        EditorGUI.EndDisabledGroup();

        if (isModifying)
        {
            EditorGUILayout.HelpBox("Modifying script... Please wait.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawScriptGenerationSection()
    {
        // Prompt Input Section for new script
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("AI Script Generation", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Enter a prompt describing the script you want to create, then click 'Generate Script' to use Gemini API.", MessageType.Info);

        EditorGUILayout.LabelField("Prompt:");
        promptInput = EditorGUILayout.TextArea(promptInput, GUILayout.Height(80));

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(promptInput) || apiSettings == null || isGenerating);
        if (GUILayout.Button("Generate Script", GUILayout.Height(30)))
        {
            GenerateScriptFromPrompt();
        }
        EditorGUI.EndDisabledGroup();

        if (isGenerating)
        {
            EditorGUILayout.HelpBox("Generating script... Please wait.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

  private void DrawScriptEditorSection()
{
    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
    
    // Header with syntax highlighting toggle
    EditorGUILayout.BeginHorizontal();
    GUILayout.Label("Script Editor", EditorStyles.boldLabel);
    GUILayout.FlexibleSpace();
    syntaxHighlightingEnabled = EditorGUILayout.ToggleLeft("Syntax Highlighting", syntaxHighlightingEnabled, GUILayout.Width(130));
    EditorGUILayout.EndHorizontal();

    // Set up the text area style with monospace font
    textAreaStyle = new GUIStyle(EditorStyles.textArea);
    textAreaStyle.font = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;
    if (textAreaStyle.font == null)
    {
        textAreaStyle = EditorStyles.textArea;
    }
    textAreaStyle.wordWrap = true;
    textAreaStyle.fontSize = fontSize;
    textAreaStyle.richText = syntaxHighlightingEnabled; // Enable rich text when highlighting is on

    // Create a scroll view for the text area
    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

    // Display the typewriter text (read-only during typing animation)
    EditorGUI.BeginDisabledGroup(isTyping);

    // Prepare content based on syntax highlighting setting
    string displayContent = syntaxHighlightingEnabled ? 
        ApplySyntaxHighlighting(currentDisplayContent) : 
        currentDisplayContent;

    // Single editable text area
    EditorGUI.BeginChangeCheck();
    
    if (syntaxHighlightingEnabled)
    {
        // Use a custom text area that supports rich text editing
        string newContent = EditorGUILayout.TextArea(displayContent, textAreaStyle, GUILayout.ExpandHeight(true));
        
        // Clean the content of rich text tags when it changes
        if (EditorGUI.EndChangeCheck() && !isTyping)
        {
            string cleanContent = StripRichTextTags(newContent);
            scriptContent = cleanContent;
            targetScriptContent = cleanContent;
            currentDisplayContent = cleanContent;
        }
    }
    else
    {
        // Regular text area without syntax highlighting
        string newContent = EditorGUILayout.TextArea(currentDisplayContent, textAreaStyle, GUILayout.ExpandHeight(true));
        if (EditorGUI.EndChangeCheck() && !isTyping)
        {
            scriptContent = newContent;
            targetScriptContent = newContent;
            currentDisplayContent = newContent;
        }
    }

    EditorGUI.EndDisabledGroup();
    EditorGUILayout.EndScrollView();

    EditorGUILayout.EndVertical();
}
private string StripRichTextTags(string richText)
{
    if (string.IsNullOrEmpty(richText)) return richText;
    
    // Remove color tags
    string result = System.Text.RegularExpressions.Regex.Replace(richText, @"<color=[^>]*>", "");
    result = System.Text.RegularExpressions.Regex.Replace(result, @"</color>", "");
    
    // Remove other common rich text tags
    result = System.Text.RegularExpressions.Regex.Replace(result, @"</?[bi]>", "");
    
    return result;
}
private string ApplySyntaxHighlighting(string code)
{
    if (string.IsNullOrEmpty(code)) return code;
    
    string result = code;
    
    // Apply keyword highlighting
    foreach (var keyword in keywordColors.Keys)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(keywordColors[keyword]);
        result = System.Text.RegularExpressions.Regex.Replace(result, 
            @"\b" + keyword + @"\b", 
            $"<color=#{colorHex}>{keyword}</color>");
    }
    
    // Apply type highlighting
    foreach (var type in typeColors.Keys)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(typeColors[type]);
        result = System.Text.RegularExpressions.Regex.Replace(result, 
            @"\b" + type + @"\b", 
            $"<color=#{colorHex}>{type}</color>");
    }
    
    // Apply comment highlighting (single line)
    string commentColorHex = ColorUtility.ToHtmlStringRGB(commentColor);
    result = System.Text.RegularExpressions.Regex.Replace(result, 
        @"//.*", 
        match => $"<color=#{commentColorHex}>{match.Value}</color>");
    
    // Apply string highlighting
    string stringColorHex = ColorUtility.ToHtmlStringRGB(stringColor);
    result = System.Text.RegularExpressions.Regex.Replace(result, 
        "\"([^\"\\\\]|\\\\.)*\"", 
        match => $"<color=#{stringColorHex}>{match.Value}</color>");
    
    return result;
}

    private void DrawPathSelectionUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Script Save Location", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Path:", GUILayout.Width(40));
        scriptSavePath = EditorGUILayout.TextField(scriptSavePath);

        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Script Save Location", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // Convert absolute path to relative path
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    scriptSavePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Path", "Please select a folder within the Assets directory.", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // Validate path
        if (!scriptSavePath.StartsWith("Assets"))
        {
            EditorGUILayout.HelpBox("Path must be within the Assets folder.", MessageType.Warning);
        }
        else if (!AssetDatabase.IsValidFolder(scriptSavePath))
        {
            EditorGUILayout.HelpBox("This folder doesn't exist. It will be created when you save the script.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        if (isModificationMode)
        {
            if (GUILayout.Button("Save Modified Script", GUILayout.Height(30)))
            {
                SaveModifiedScript();
            }
        }
        else
        {
            // Two buttons for new script mode
            EditorGUILayout.BeginHorizontal();

            // Create Script button (always enabled)
            if (GUILayout.Button("Create Script", GUILayout.Height(30)))
            {
                CreateScriptOnly();
            }

            // Create and Attach button (only enabled when GameObject is selected)
            EditorGUI.BeginDisabledGroup(Selection.activeGameObject == null);
            if (GUILayout.Button("Create & Attach Script", GUILayout.Height(30)))
            {
                CreateAndAttachScript();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Show selection hint
            if (Selection.activeGameObject == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject to enable 'Create & Attach Script' button.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Script will be attached to: {Selection.activeGameObject.name}", MessageType.Info);
            }
        }
    }

    private void UpdateTypewriterEffect()
    {
        if (isTyping)
        {
            // Check if it's time to update the typewriter effect
            float currentTime = (float)EditorApplication.timeSinceStartup;
            float deltaTime = currentTime - lastTypeTime;
            int charsToAdd = Mathf.FloorToInt(deltaTime * 500);
        
            if (charsToAdd > 0)
            {
                lastTypeTime = currentTime;
            
                // Add the next batch of characters
                int nextIndex = Mathf.Min(currentTypeIndex + charsToAdd, targetScriptContent.Length);
                currentDisplayContent = targetScriptContent.Substring(0, nextIndex);
                currentTypeIndex = nextIndex;
            
                // Check if we've reached the end
                if (currentTypeIndex >= targetScriptContent.Length)
                {
                    isTyping = false;
                    scriptContent = targetScriptContent; // Update the actual script content
                }
            
                // Request a repaint to update the UI
                Repaint();
            }
        }
    }

    private void StartTypewriterEffect(string newContent)
    {
        targetScriptContent = newContent;
        currentDisplayContent = "";
        currentTypeIndex = 0;
        lastTypeTime = (float)EditorApplication.timeSinceStartup;
        isTyping = true;
    }

    private void EnsureButtonsVisibility()
    {
        // Check if window is too small to display all controls
        Rect windowRect = position;
        
        // Calculate total content height by checking current scroll position and view size
        float totalContentHeight = mainScrollPosition.y + EditorGUIUtility.singleLineHeight;
        
        // If there's a scrollbar and we're in a state where buttons should be visible
        if (windowRect.height < totalContentHeight)
        {
            // If waiting for compilation or generating, scroll to bottom to show status
            if (waitingForCompilation || isGenerating || isModifying)
            {
                mainScrollPosition = new Vector2(mainScrollPosition.x, totalContentHeight - windowRect.height);
                Repaint();
            }
            // When user clicks Generate Script, make sure the script editor is visible
            else if (Event.current.type == EventType.Used && 
                    (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                // Scroll to make the script editor visible
                float scriptEditorPosition = mainScrollPosition.y + 300; // Approximate position
                mainScrollPosition = new Vector2(mainScrollPosition.x, scriptEditorPosition);
                Repaint();
            }
        }
    }

    private void CreateApiSettingsAsset()
    {
        // Create Assets/ScriptGenerator if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/ScriptGenerator"))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptGenerator");
        }
        
        // Create the scriptable object
        GeminiAPISettings newSettings = CreateInstance<GeminiAPISettings>();
        AssetDatabase.CreateAsset(newSettings, "Assets/ScriptGenerator/GeminiAPISettings.asset");
        AssetDatabase.SaveAssets();
        
        apiSettings = newSettings;
        
        // Ping the asset in the project window
        EditorGUIUtility.PingObject(newSettings);
        
        EditorUtility.DisplayDialog("API Settings Created", 
            "Please configure your Gemini API key in the created asset.", "OK");
    }

    private void SelectApiSettingsAsset()
    {
        string path = EditorUtility.OpenFilePanel("Select Gemini API Settings", "Assets", "asset");
        if (!string.IsNullOrEmpty(path))
        {
            // Convert to project relative path
            path = "Assets" + path.Substring(Application.dataPath.Length);
            GeminiAPISettings settings = AssetDatabase.LoadAssetAtPath<GeminiAPISettings>(path);
            
            if (settings != null)
            {
                apiSettings = settings;
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Asset", 
                    "The selected asset is not a GeminiAPISettings object.", "OK");
            }
        }
    }

    private async void GenerateScriptFromPrompt()
    {
        if (string.IsNullOrEmpty(promptInput) || apiSettings == null)
        {
            return;
        }
        
        isGenerating = true;
        
        try
        {
            string generatedScript = await GeminiAPIClient.SendPromptToGeminiAPI(promptInput, apiSettings);
            
            if (!string.IsNullOrEmpty(generatedScript))
            {
                // Start typewriter effect with the new script
                StartTypewriterEffect(generatedScript);
            }
            else
            {
                EditorUtility.DisplayDialog("Generation Failed", 
                    "Failed to generate script from prompt. Check console for more details.", "OK");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error generating script: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("Error", 
                $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            isGenerating = false;
            Repaint();
        }
    }

    private async void ModifyScriptWithAI()
    {
        if (string.IsNullOrEmpty(modificationPrompt) || selectedScript == null || apiSettings == null)
        {
            return;
        }
        
        isModifying = true;
        
        try
        {
            string fullPrompt = $"Modify the following C# Unity script according to these instructions: {modificationPrompt}\n\nOriginal script:\n```csharp\n{scriptContent}\n```\n\nProvide ONLY the complete modified script, no explanations, preserve Script name.";
            
            string modifiedScript = await GeminiAPIClient.SendPromptToGeminiAPI(fullPrompt, apiSettings);
            
            if (!string.IsNullOrEmpty(modifiedScript))
            {
                // Start typewriter effect with the modified script
                StartTypewriterEffect(modifiedScript);
            }
            else
            {
                EditorUtility.DisplayDialog("Modification Failed", 
                    "Failed to modify script. Check console for more details.", "OK");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error modifying script: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("Error", 
                $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            isModifying = false;
            Repaint();
        }
    }

    private void CreateScriptOnly()
    {
        string className = ScriptUtils.ExtractClassName(scriptContent);

        if (string.IsNullOrEmpty(className))
        {
            EditorUtility.DisplayDialog("Invalid Script", "Could not extract class name from the script. Make sure your script contains a valid class declaration.", "OK");
            return;
        }

        // Ensure the save path exists
        ScriptUtils.CreateDirectoryIfNotExists(scriptSavePath);

        // Set file path based on selected path
        string absolutePath = Path.Combine(Application.dataPath, scriptSavePath.Substring("Assets/".Length));
        string filePath = Path.Combine(absolutePath, className + ".cs");
        string relativePath = Path.Combine(scriptSavePath, className + ".cs").Replace('\\', '/');

        // Check if file already exists
        if (File.Exists(filePath))
        {
            bool overwrite = EditorUtility.DisplayDialog("File Already Exists",
                $"A script named '{className}.cs' already exists at '{relativePath}'. Do you want to overwrite it?",
                "Overwrite", "Cancel");

            if (!overwrite)
            {
                return;
            }
        }

        try
        {
            // Write the script to the file
            File.WriteAllText(filePath, scriptContent);
            AssetDatabase.Refresh();

            // Ping the created asset
            UnityEngine.Object createdAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
            if (createdAsset != null)
            {
                EditorGUIUtility.PingObject(createdAsset);
            }

            EditorUtility.DisplayDialog("Success", $"Script '{className}.cs' created successfully at '{relativePath}'.", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating script: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to create script: {e.Message}", "OK");
        }
    }

    private void CreateAndAttachScript()
    {
        string className = ScriptUtils.ExtractClassName(scriptContent);

        if (string.IsNullOrEmpty(className))
        {
            EditorUtility.DisplayDialog("Invalid Script", "Could not extract class name from the script. Make sure your script contains a valid class declaration.", "OK");
            return;
        }

        // Use the selected path instead of hardcoded "Assets/Scripts"
        string scriptsFolder = scriptSavePath;
        ScriptUtils.CreateDirectoryIfNotExists(scriptsFolder);

        // Set file path using the selected path
        string filePath = Path.Combine(Application.dataPath, scriptsFolder.Substring("Assets/".Length), className + ".cs");
        string relativePath = Path.Combine(scriptsFolder, className + ".cs").Replace('\\', '/');

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

        // Store the variables we need for later
        targetGameObject = Selection.activeGameObject;
        pendingScriptName = className;
        pendingScriptPath = relativePath;

        // Write the script to the file
        File.WriteAllText(filePath, scriptContent);
        AssetDatabase.Refresh();

        // Set flag to wait for compilation
        waitingForCompilation = true;
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
        
        // Update typewriter effect in the editor update loop
        if (isTyping)
        {
            Repaint(); // Force a repaint to update the typewriter effect
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
            bool success = ScriptUtils.TryAttachScriptToGameObject(pendingScriptName, targetGameObject);
            
            if (success)
            {
                EditorUtility.DisplayDialog("Success", $"Script '{pendingScriptName}' created and attached to '{targetGameObject.name}'.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Warning", 
                    $"Script '{pendingScriptName}' was created but couldn't be automatically attached. Please add it manually through the Add Component menu.", 
                    "OK");
            }
        }
        finally
        {
            // Clear the pending script variables
            pendingScriptName = null;
            targetGameObject = null;
            pendingScriptPath = null;
        }
    }

    private void LoadScriptContent()
    {
        if (selectedScript == null)
            return;
        
        try
        {
            // Get the path to the script asset
            string scriptPath = AssetDatabase.GetAssetPath(selectedScript);
            if (string.IsNullOrEmpty(scriptPath))
                return;
            
            originalScriptPath = scriptPath;
            originalScriptName = Path.GetFileNameWithoutExtension(scriptPath);
            
            // Load the content of the script
            scriptContent = File.ReadAllText(scriptPath);
            targetScriptContent = scriptContent;
            currentDisplayContent = scriptContent;
            
            Debug.Log($"Loaded script: {originalScriptName} from {originalScriptPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading script: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to load script: {e.Message}", "OK");
        }
    }
    
    private void SaveModifiedScript()
    {
        if (string.IsNullOrEmpty(originalScriptPath) || selectedScript == null)
        {
            EditorUtility.DisplayDialog("Error", "No script is selected or loaded. Cannot save modifications.", "OK");
            return;
        }
        
        try
        {
            // Confirm before overwriting
            bool shouldSave = EditorUtility.DisplayDialog("Save Changes",
                $"Are you sure you want to overwrite the script '{originalScriptName}'?",
                "Save", "Cancel");
                
            if (!shouldSave)
                return;
            
            // Save the modified content to the original file
            File.WriteAllText(originalScriptPath, scriptContent);
            
            // Refresh to update the asset in Unity
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Success", $"Script '{originalScriptName}' has been updated successfully.", "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving script: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to save script: {e.Message}", "OK");
        }
    }
}
#endif