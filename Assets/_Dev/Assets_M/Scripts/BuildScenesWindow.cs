#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

public class BuildScenesWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showSettings = false;
    private int selectedSceneIndex = -1; // Track selected scene

    // Settings
    private int headerFontSize = 12;
    private int sceneNameFontSize = 11;
    private float buttonWidth = 80f;
    private FontStyle headerFontStyle = FontStyle.Bold;
    private FontStyle sceneNameFontStyle = FontStyle.Normal;
    private Font headerFont = null;
    private Font sceneNameFont = null;

    // Temporary settings (for editing)
    private int tempHeaderFontSize;
    private int tempSceneNameFontSize;
    private float tempButtonWidth;
    private FontStyle tempHeaderFontStyle;
    private FontStyle tempSceneNameFontStyle;
    private Font tempHeaderFont;
    private Font tempSceneNameFont;

    // Track if settings have changed
    private bool settingsChanged = false;

    [MenuItem("Nasmo Studio/Build Scenes")]
    public static void ShowWindow()
    {
        GetWindow<BuildScenesWindow>("Build Scenes");
    }

    private void OnEnable()
    {
        LoadSettings();
        showSettings = false; // Always start with main panel
    }

    private void LoadSettings()
    {
        headerFontSize = EditorPrefs.GetInt("BuildScenes_HeaderFontSize", 12);
        sceneNameFontSize = EditorPrefs.GetInt("BuildScenes_SceneNameFontSize", 11);
        buttonWidth = EditorPrefs.GetFloat("BuildScenes_ButtonWidth", 80f);
        headerFontStyle = (FontStyle)EditorPrefs.GetInt("BuildScenes_HeaderFontStyle", (int)FontStyle.Bold);
        sceneNameFontStyle = (FontStyle)EditorPrefs.GetInt("BuildScenes_SceneNameFontStyle", (int)FontStyle.Normal);

        // Load fonts from paths
        string headerFontPath = EditorPrefs.GetString("BuildScenes_HeaderFontPath", "");
        if (!string.IsNullOrEmpty(headerFontPath))
        {
            headerFont = AssetDatabase.LoadAssetAtPath<Font>(headerFontPath);
        }

        string sceneNameFontPath = EditorPrefs.GetString("BuildScenes_SceneNameFontPath", "");
        if (!string.IsNullOrEmpty(sceneNameFontPath))
        {
            sceneNameFont = AssetDatabase.LoadAssetAtPath<Font>(sceneNameFontPath);
        }
    }

    private void SaveSettings()
    {
        EditorPrefs.SetInt("BuildScenes_HeaderFontSize", headerFontSize);
        EditorPrefs.SetInt("BuildScenes_SceneNameFontSize", sceneNameFontSize);
        EditorPrefs.SetFloat("BuildScenes_ButtonWidth", buttonWidth);
        EditorPrefs.SetInt("BuildScenes_HeaderFontStyle", (int)headerFontStyle);
        EditorPrefs.SetInt("BuildScenes_SceneNameFontStyle", (int)sceneNameFontStyle);

        // Save font paths
        if (headerFont != null)
        {
            string path = AssetDatabase.GetAssetPath(headerFont);
            EditorPrefs.SetString("BuildScenes_HeaderFontPath", path);
        }
        else
        {
            EditorPrefs.SetString("BuildScenes_HeaderFontPath", "");
        }

        if (sceneNameFont != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneNameFont);
            EditorPrefs.SetString("BuildScenes_SceneNameFontPath", path);
        }
        else
        {
            EditorPrefs.SetString("BuildScenes_SceneNameFontPath", "");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        // Header with Settings button
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = headerFontSize;
        headerStyle.fontStyle = headerFontStyle;
        if (headerFont != null)
        {
            headerStyle.font = headerFont;
        }
        GUILayout.Label("Build Scenes", headerStyle);

        GUILayout.FlexibleSpace();

        // Settings button in top right
        if (GUILayout.Button("⚙", GUILayout.Width(30), GUILayout.Height(25)))
        {
            showSettings = !showSettings;
            if (showSettings)
            {
                // Initialize temp settings
                tempHeaderFontSize = headerFontSize;
                tempSceneNameFontSize = sceneNameFontSize;
                tempButtonWidth = buttonWidth;
                tempHeaderFontStyle = headerFontStyle;
                tempSceneNameFontStyle = sceneNameFontStyle;
                tempHeaderFont = headerFont;
                tempSceneNameFont = sceneNameFont;
                settingsChanged = false;
            }
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        // Show settings panel if enabled
        if (showSettings)
        {
            DrawSettingsPanel();
            return; // Don't show the main content when settings are open
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        if (scenes.Length == 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("No scenes in build settings", MessageType.Info);
        }

        List<EditorBuildSettingsScene> scenesList = new List<EditorBuildSettingsScene>(scenes);

        for (int i = 0; i < scenesList.Count; i++)
        {
            EditorBuildSettingsScene scene = scenesList[i];
            if (scene == null) continue;

            string scenePath = scene.path;
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            bool sceneExists = File.Exists(scenePath);

            DrawSceneItem(i, sceneName, scenePath, sceneExists, scenesList);
        }

        EditorGUILayout.EndScrollView();

        // Draw scene details panel if a scene is selected
        if (selectedSceneIndex >= 0 && selectedSceneIndex < scenesList.Count)
        {
            DrawSceneDetails(scenesList[selectedSceneIndex], selectedSceneIndex);
        }

        EditorGUILayout.Space(5);
        DrawLine();
        EditorGUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("Build", GUILayout.Width(100), GUILayout.Height(25)))
        {
            BuildGame();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        if (GUILayout.Button("Open Build Settings", GUILayout.Width(150), GUILayout.Height(25)))
        {
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // Detect clicks outside scene items to deselect
        if (Event.current.type == EventType.MouseDown)
        {
            selectedSceneIndex = -1;
            Repaint();
        }
    }

    private void DrawSceneDetails(EditorBuildSettingsScene scene, int index)
    {
        EditorGUILayout.Space(5);
        DrawLine();
        EditorGUILayout.Space(5);

        // Scene details panel
        Color bgColor = new Color(0.3f, 0.5f, 0.8f, 0.15f);
        GUI.backgroundColor = bgColor;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = Color.white;

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 11;
        GUILayout.Label("Scene Details", titleStyle);

        EditorGUILayout.Space(3);

        // Get scene GUID
        string guid = AssetDatabase.AssetPathToGUID(scene.path);
        FileInfo fileInfo = File.Exists(scene.path) ? new FileInfo(scene.path) : null;

        GUIStyle detailStyle = new GUIStyle(EditorStyles.label);
        detailStyle.fontSize = 10;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Build Index:", detailStyle, GUILayout.Width(100));
        GUILayout.Label(index.ToString(), detailStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Scene Name:", detailStyle, GUILayout.Width(100));
        GUILayout.Label(System.IO.Path.GetFileNameWithoutExtension(scene.path), detailStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("GUID:", detailStyle, GUILayout.Width(100));
        EditorGUILayout.SelectableLabel(guid, detailStyle, GUILayout.Height(16));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Path:", detailStyle, GUILayout.Width(100));
        EditorGUILayout.SelectableLabel(scene.path, detailStyle, GUILayout.Height(16));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Enabled:", detailStyle, GUILayout.Width(100));
        GUILayout.Label(scene.enabled ? "Yes" : "No", detailStyle);
        EditorGUILayout.EndHorizontal();

        if (fileInfo != null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("File Size:", detailStyle, GUILayout.Width(100));
            GUILayout.Label(FormatFileSize(fileInfo.Length), detailStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Last Modified:", detailStyle, GUILayout.Width(100));
            GUILayout.Label(fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"), detailStyle);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private void DrawSettingsPanel()
    {
        EditorGUILayout.Space(10);

        // Settings box
        Color bgColor = new Color(0, 0, 0, 0.1f);
        GUI.backgroundColor = bgColor;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // Header Font Size
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Header Font Size:", GUILayout.Width(150));
        tempHeaderFontSize = EditorGUILayout.IntSlider(tempHeaderFontSize, 10, 20);
        EditorGUILayout.EndHorizontal();

        // Header Font Style
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Header Font Style:", GUILayout.Width(150));
        tempHeaderFontStyle = (FontStyle)EditorGUILayout.EnumPopup(tempHeaderFontStyle);
        EditorGUILayout.EndHorizontal();

        // Header Font
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Header Font:", GUILayout.Width(150));
        tempHeaderFont = (Font)EditorGUILayout.ObjectField(tempHeaderFont, typeof(Font), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Scene Name Font Size
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Scene Name Font Size:", GUILayout.Width(150));
        tempSceneNameFontSize = EditorGUILayout.IntSlider(tempSceneNameFontSize, 9, 16);
        EditorGUILayout.EndHorizontal();

        // Scene Name Font Style
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Scene Name Font Style:", GUILayout.Width(150));
        tempSceneNameFontStyle = (FontStyle)EditorGUILayout.EnumPopup(tempSceneNameFontStyle);
        EditorGUILayout.EndHorizontal();

        // Scene Name Font
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Scene Name Font:", GUILayout.Width(150));
        tempSceneNameFont = (Font)EditorGUILayout.ObjectField(tempSceneNameFont, typeof(Font), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Button Width
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Button Width:", GUILayout.Width(150));
        tempButtonWidth = EditorGUILayout.Slider(tempButtonWidth, 50f, 150f);
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            settingsChanged = true;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.EndVertical();

        // Preview Section
        EditorGUILayout.Space(10);
        GUILayout.Label("Preview", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Preview Header
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUIStyle previewHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
        previewHeaderStyle.fontSize = tempHeaderFontSize;
        previewHeaderStyle.fontStyle = tempHeaderFontStyle;
        if (tempHeaderFont != null)
        {
            previewHeaderStyle.font = tempHeaderFont;
        }
        GUILayout.Label("Build Scenes", previewHeaderStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Preview scene item
        bgColor = new Color(0, 0, 0, 0.1f);
        GUI.backgroundColor = bgColor;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = Color.white;

        EditorGUILayout.BeginHorizontal(GUILayout.Height(20));

        // Focus button
        if (GUILayout.Button("→", GUILayout.Width(35), GUILayout.Height(25)))
        {
            // Preview only
        }

        // Index
        GUIStyle indexStyle = new GUIStyle(GUI.skin.label);
        indexStyle.alignment = TextAnchor.MiddleCenter;
        indexStyle.fontStyle = FontStyle.Bold;
        indexStyle.fontSize = 11;
        GUILayout.Label("0", indexStyle, GUILayout.Width(22));

        // Scene name with temp settings
        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = tempSceneNameFontSize;
        nameStyle.fontStyle = tempSceneNameFontStyle;
        if (tempSceneNameFont != null)
        {
            nameStyle.font = tempSceneNameFont;
        }
        GUILayout.Label("ExampleScene", nameStyle, GUILayout.MinWidth(80), GUILayout.MaxWidth(300));

        GUILayout.FlexibleSpace();

        // Open button with temp width
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("Open", GUILayout.Width(tempButtonWidth), GUILayout.Height(25)))
        {
            // Preview only
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // Save and Cancel buttons
        EditorGUILayout.Space(15);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // Save button
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("Save", GUILayout.Width(100), GUILayout.Height(30)))
        {
            headerFontSize = tempHeaderFontSize;
            sceneNameFontSize = tempSceneNameFontSize;
            buttonWidth = tempButtonWidth;
            headerFontStyle = tempHeaderFontStyle;
            sceneNameFontStyle = tempSceneNameFontStyle;
            headerFont = tempHeaderFont;
            sceneNameFont = tempSceneNameFont;
            SaveSettings();
            showSettings = false;
            settingsChanged = false;
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        // Cancel/Close button
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("✕ Close", GUILayout.Width(100), GUILayout.Height(30)))
        {
            if (settingsChanged)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    "You have unsaved changes. Are you sure you want to close without saving?",
                    "Discard", "Cancel"))
                {
                    showSettings = false;
                    settingsChanged = false;
                }
            }
            else
            {
                showSettings = false;
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
    }

    private void DrawSceneItem(int index, string sceneName, string scenePath, bool sceneExists, List<EditorBuildSettingsScene> scenesList)
    {
        // Check if this scene is selected
        bool isSelected = (selectedSceneIndex == index);

        // Subtle background for each scene
        Color bgColor;
        if (isSelected)
        {
            bgColor = new Color(0.3f, 0.5f, 0.8f, 0.3f); // Blue highlight when selected
        }
        else if (!sceneExists)
        {
            bgColor = new Color(1, 0, 0, 0.1f);
        }
        else
        {
            bgColor = new Color(0, 0, 0, 0.1f);
        }

        GUI.backgroundColor = bgColor;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Get the rect of the vertical container for click detection
        Rect containerRect = GUILayoutUtility.GetLastRect();

        GUI.backgroundColor = Color.white;

        EditorGUILayout.BeginHorizontal(GUILayout.Height(25));

        // Focus button (before index)
        if (sceneExists)
        {
            if (GUILayout.Button("→", GUILayout.Width(35), GUILayout.Height(25)))
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(scenePath);
            }
        }
        else
        {
            GUILayout.Space(38);
        }

        // Index with subtle styling
        GUIStyle indexStyle = new GUIStyle(GUI.skin.label);
        indexStyle.alignment = TextAnchor.MiddleCenter;
        indexStyle.fontStyle = FontStyle.Bold;
        indexStyle.fontSize = 11;
        GUILayout.Label($"{index}", indexStyle, GUILayout.Width(22));

        // Scene name with settings - make it clickable
        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = sceneNameFontSize;
        nameStyle.fontStyle = sceneNameFontStyle;
        if (sceneNameFont != null)
        {
            nameStyle.font = sceneNameFont;
        }

        if (!sceneExists)
        {
            GUI.contentColor = new Color(1f, 0.3f, 0.3f);
            nameStyle.fontStyle = FontStyle.Bold;
        }

        // Make the scene name area clickable for selection
        if (GUILayout.Button(sceneName, nameStyle, GUILayout.MinWidth(80), GUILayout.MaxWidth(300)))
        {
            selectedSceneIndex = index;
            Repaint();
        }

        if (!sceneExists)
        {
            GUI.contentColor = new Color(1f, 0.5f, 0.5f);
            GUILayout.Label("(Missing)", EditorStyles.miniLabel, GUILayout.Width(50));
        }

        GUI.contentColor = Color.white;

        GUILayout.FlexibleSpace();

        // Action Buttons (always visible)
        if (sceneExists)
        {
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("Open", GUILayout.Width(buttonWidth), GUILayout.Height(25)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath);
                }
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕ Remove", GUILayout.Width(65), GUILayout.Height(18)))
            {
                if (EditorUtility.DisplayDialog("Remove Missing Scene",
                    $"Remove '{sceneName}' from build settings?",
                    "Remove", "Cancel"))
                {
                    scenesList.RemoveAt(index);
                    EditorBuildSettings.scenes = scenesList.ToArray();
                    selectedSceneIndex = -1;
                    GUIUtility.ExitGUI();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // Handle click on the entire row area for selection (except buttons)
        if (Event.current.type == EventType.MouseDown)
        {
            Rect lastRect = GUILayoutUtility.GetLastRect();
            if (lastRect.Contains(Event.current.mousePosition))
            {
                selectedSceneIndex = index;
                Event.current.Use();
                Repaint();
            }
        }
    }

    private void DrawLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    private void BuildGame()
    {
        string extension = "";
        switch (EditorUserBuildSettings.activeBuildTarget)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                extension = "exe";
                break;
            case BuildTarget.StandaloneOSX:
                extension = "app";
                break;
            case BuildTarget.StandaloneLinux64:
                extension = "x86_64";
                break;
            case BuildTarget.Android:
                extension = "apk";
                break;
            default:
                extension = "";
                break;
        }

        string path = EditorUtility.SaveFilePanel(
            "Build Game",
            "",
            PlayerSettings.productName,
            extension
        );

        if (!string.IsNullOrEmpty(path))
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = GetScenePaths();
            buildPlayerOptions.locationPathName = path;
            buildPlayerOptions.target = EditorUserBuildSettings.activeBuildTarget;
            buildPlayerOptions.options = BuildOptions.None;

            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
    }

    private string[] GetScenePaths()
    {
        List<string> paths = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && File.Exists(scene.path))
            {
                paths.Add(scene.path);
            }
        }
        return paths.ToArray();
    }
}
#endif