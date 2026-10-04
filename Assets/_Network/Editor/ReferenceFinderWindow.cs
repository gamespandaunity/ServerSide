using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.IMGUI.Controls;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class ReferenceFinderWindow : EditorWindow
{
    // Cache System
    [Serializable]
    public class ReferenceDatabase
    {
        // Key: Asset Path, Value: List of assets it references
        public Dictionary<string, List<string>> assetToReferences = new Dictionary<string, List<string>>();

        // Key: Asset Path, Value: List of assets that reference it (reverse lookup)
        public Dictionary<string, List<string>> assetToReferencedBy = new Dictionary<string, List<string>>();

        // Asset metadata cache
        public Dictionary<string, AssetMetadata> assetMetadata = new Dictionary<string, AssetMetadata>();

        public DateTime lastFullScanTime;
        public int totalAssetsScanned;
        public bool isDatabaseBuilt = false;
    }

    [Serializable]
    public class AssetMetadata
    {
        public string path;
        public string name;
        public string type;
        public long fileSize;
        public DateTime lastModified;
        public string guid;
    }

    // Database instance
    private static ReferenceDatabase database = new ReferenceDatabase();

    // You can change this path to save in Assets folder for version control
    // Option 1: Library folder (default - not in version control)
    private static string DatabasePath => "Library/ReferenceFinderCache.json";

    // Option 2: ProjectSettings folder (included in version control)
    // private static string DatabasePath => "ProjectSettings/ReferenceFinderCache.json";

    // Option 3: Assets folder (included in version control, visible in Unity)
    // private static string DatabasePath => "Assets/Editor/ReferenceFinderCache.json";

    // Target
    private Object targetObject;
    private string targetInfo = "";
    private Texture2D targetPreview;

    // Results (now pulled from cache)
    private List<SceneReference> sceneReferences = new List<SceneReference>();
    private List<AssetReference> cachedProjectReferences = new List<AssetReference>();

    // UI State
    private Vector2 sceneScrollPos;
    private Vector2 projectScrollPos;
    private string searchFilter = "";
    private int selectedTab = 0;
    private bool isSearching = false;
    private string searchStatus = "";
    private bool isDatabaseBuilding = false;
    private float databaseBuildProgress = 0f;

    // Settings
    private bool searchInAllScenes = false;
    private bool includeDisabled = true;
    private bool showFullPaths = false;
    private bool autoUpdateDatabase = true;
    private bool showDatabaseStats = false;

    // Styling
    private GUIStyle titleStyle;
    private GUIStyle tabStyle;
    private GUIStyle selectedTabStyle;
    private GUIStyle resultItemStyle;
    private GUIStyle resultItemAltStyle;
    private GUIStyle iconStyle;
    private GUIStyle statsStyle;
    private GUIStyle searchFieldStyle;
    private GUIStyle warningStyle;

    // Colors
    private readonly Color primaryColor = new Color(0.2f, 0.6f, 1f);
    private readonly Color successColor = new Color(0.2f, 0.8f, 0.4f);
    private readonly Color warningColor = new Color(1f, 0.7f, 0.2f);
    private readonly Color dangerColor = new Color(1f, 0.3f, 0.3f);
    private readonly Color bgDark = new Color(0.18f, 0.18f, 0.18f);
    private readonly Color bgLight = new Color(0.22f, 0.22f, 0.22f);

    private class SceneReference
    {
        public GameObject GameObject;
        public Component Component;
        public string PropertyPath;
        public string PropertyName;
        public string SceneName;
        public string FullPath;
    }

    private class AssetReference
    {
        public string AssetPath;
        public string AssetName;
        public string AssetType;
        public Object Asset;
        public long FileSize;
    }

    [MenuItem("Tools/Reference Finder Pro ⚡")]
    public static void ShowWindow()
    {
        var window = GetWindow<ReferenceFinderWindow>();
        window.titleContent = new GUIContent("Reference Finder Pro ⚡", EditorGUIUtility.IconContent("d_ViewToolZoom").image);
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnEnable()
    {
        InitializeStyles();
        LoadDatabase();

        // Subscribe to asset changes
        EditorApplication.projectChanged += OnProjectChanged;
        AssemblyReloadEvents.beforeAssemblyReload += SaveDatabase;

        // Subscribe to selection changes for auto-update
        Selection.selectionChanged += OnSelectionChanged;

        // Set initial selection
        if (Selection.activeObject != null)
        {
            targetObject = Selection.activeObject;
            UpdateTargetInfo();
            if (database.isDatabaseBuilt)
            {
                PerformAutoSearch();
            }
        }
    }

    private void OnDisable()
    {
        SaveDatabase();
        EditorApplication.projectChanged -= OnProjectChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= SaveDatabase;
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged()
    {
        // Auto-update when selection changes
        if (Selection.activeObject != targetObject)
        {
            targetObject = Selection.activeObject;
            UpdateTargetInfo();

            if (targetObject != null && database.isDatabaseBuilt)
            {
                PerformAutoSearch();
            }
            else
            {
                // Clear results if nothing selected
                sceneReferences.Clear();
                cachedProjectReferences.Clear();
                searchStatus = targetObject == null ? "Select an object to see its references" : "Build database to see project references";
            }

            Repaint();
        }
    }

    private void PerformAutoSearch()
    {
        // Automatic search without progress bars for better UX
        try
        {
            // Search cache for project references
            SearchInCache();

            // Search current scene for scene references
            FindSceneReferencesQuick();

            searchStatus = $"Found {sceneReferences.Count} scene refs, {cachedProjectReferences.Count} project refs • Auto-updated {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception e)
        {
            Debug.LogError($"[Reference Finder] Auto-search error: {e.Message}");
        }
    }

    private void FindSceneReferencesQuick()
    {
        // Quick scene search without progress bars
        sceneReferences.Clear();

        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                SearchInScene(scene);
            }
        }
    }

    private void OnProjectChanged()
    {
        if (autoUpdateDatabase && database.isDatabaseBuilt)
        {
            // Mark database as potentially stale
            searchStatus = "Project changed - Consider rebuilding database";
        }
    }

    // Methods to handle asset changes (called from the separate processor class)
    public static void HandleAssetCreation(string assetPath)
    {
        if (database.isDatabaseBuilt)
        {
            EditorApplication.delayCall += () => UpdateAssetInDatabase(assetPath);
        }
    }

    public static void HandleAssetDeletion(string assetPath)
    {
        if (database.isDatabaseBuilt)
        {
            RemoveAssetFromDatabase(assetPath);
        }
    }

    public static void HandleAssetMove(string sourcePath, string destinationPath)
    {
        if (database.isDatabaseBuilt)
        {
            EditorApplication.delayCall += () =>
            {
                RemoveAssetFromDatabase(sourcePath);
                UpdateAssetInDatabase(destinationPath);
            };
        }
    }

    private static void UpdateAssetInDatabase(string assetPath)
    {
        if (!assetPath.StartsWith("Assets/")) return;

        // Remove old references
        RemoveAssetFromDatabase(assetPath);

        // Add new references
        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (asset != null)
        {
            // Get dependencies
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);
            database.assetToReferences[assetPath] = dependencies.ToList();

            // Update reverse lookup
            foreach (var dep in dependencies)
            {
                if (!database.assetToReferencedBy.ContainsKey(dep))
                    database.assetToReferencedBy[dep] = new List<string>();

                if (!database.assetToReferencedBy[dep].Contains(assetPath))
                    database.assetToReferencedBy[dep].Add(assetPath);
            }

            // Update metadata
            UpdateAssetMetadata(assetPath);
        }
    }

    private static void RemoveAssetFromDatabase(string assetPath)
    {
        // Remove from forward lookup
        if (database.assetToReferences.ContainsKey(assetPath))
        {
            var references = database.assetToReferences[assetPath];
            foreach (var refPath in references)
            {
                // Remove from reverse lookup
                if (database.assetToReferencedBy.ContainsKey(refPath))
                {
                    database.assetToReferencedBy[refPath].Remove(assetPath);
                    if (database.assetToReferencedBy[refPath].Count == 0)
                        database.assetToReferencedBy.Remove(refPath);
                }
            }
            database.assetToReferences.Remove(assetPath);
        }

        // Remove from reverse lookup (if this asset was referenced)
        if (database.assetToReferencedBy.ContainsKey(assetPath))
        {
            database.assetToReferencedBy.Remove(assetPath);
        }

        // Remove metadata
        if (database.assetMetadata.ContainsKey(assetPath))
        {
            database.assetMetadata.Remove(assetPath);
        }
    }

    private static void UpdateAssetMetadata(string assetPath)
    {
        var metadata = new AssetMetadata
        {
            path = assetPath,
            name = System.IO.Path.GetFileName(assetPath),
            guid = AssetDatabase.AssetPathToGUID(assetPath)
        };

        var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        if (asset != null)
        {
            metadata.type = asset.GetType().Name;
        }

        var fileInfo = new System.IO.FileInfo(assetPath);
        if (fileInfo.Exists)
        {
            metadata.fileSize = fileInfo.Length;
            metadata.lastModified = fileInfo.LastWriteTime;
        }

        database.assetMetadata[assetPath] = metadata;
    }

    private void LoadDatabase()
    {
        if (System.IO.File.Exists(DatabasePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(DatabasePath);
                database = JsonUtility.FromJson<ReferenceDatabase>(json);

                if (database == null)
                    database = new ReferenceDatabase();

                searchStatus = $"Database loaded • {database.totalAssetsScanned} assets • Last scan: {database.lastFullScanTime:HH:mm:ss}";
            }
            catch (Exception e)
            {
                Debug.LogError($"[Reference Finder] Failed to load database: {e.Message}");
                database = new ReferenceDatabase();
            }
        }
    }

    private void SaveDatabase()
    {
        try
        {
            string json = JsonUtility.ToJson(database, true);
            System.IO.File.WriteAllText(DatabasePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Reference Finder] Failed to save database: {e.Message}");
        }
    }

    private void InitializeStyles()
    {
        // Styles will be initialized in OnGUI
    }

    private void OnGUI()
    {
        if (titleStyle == null) SetupStyles();

        DrawBackground();
        DrawHeader();

        // Show database status/warning if not built
        if (!database.isDatabaseBuilt)
        {
            DrawDatabaseWarning();
        }

        DrawTargetSection();

        if (targetObject != null && database.isDatabaseBuilt)
        {
            DrawSearchControls();
            DrawTabs();
            DrawResults();
            DrawStatusBar();
        }
        else if (targetObject == null)
        {
            DrawEmptyState();
        }

        HandleDragAndDrop();
    }

    private void DrawDatabaseWarning()
    {
        EditorGUILayout.BeginVertical("HelpBox");

        EditorGUILayout.BeginHorizontal();
        var warningIcon = EditorGUIUtility.IconContent("console.warnicon");
        GUILayout.Label(warningIcon, GUILayout.Width(30), GUILayout.Height(30));

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Database Not Built", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Click 'Build Database' to scan all project assets for the first time.", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        GUI.backgroundColor = warningColor;
        if (GUILayout.Button("Build Database", GUILayout.Width(120), GUILayout.Height(30)))
        {
            BuildFullDatabase();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        if (isDatabaseBuilding)
        {
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                databaseBuildProgress,
                $"Building database... {(int)(databaseBuildProgress * 100)}%");
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void SetupStyles()
    {
        titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white },
            padding = new RectOffset(10, 10, 10, 10)
        };

        tabStyle = new GUIStyle(EditorStyles.toolbarButton)
        {
            fontSize = 12,
            fixedHeight = 30,
            normal = { textColor = Color.gray }
        };

        selectedTabStyle = new GUIStyle(tabStyle)
        {
            normal = {
                textColor = primaryColor,
                background = MakeColorTexture(new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.1f))
            },
            fontStyle = FontStyle.Bold
        };

        resultItemStyle = new GUIStyle(EditorStyles.label)
        {
            padding = new RectOffset(10, 10, 5, 5),
            normal = { background = MakeColorTexture(bgDark) }
        };

        resultItemAltStyle = new GUIStyle(resultItemStyle)
        {
            normal = { background = MakeColorTexture(bgLight) }
        };

        iconStyle = new GUIStyle(EditorStyles.label)
        {
            fixedWidth = 20,
            fixedHeight = 20,
            alignment = TextAnchor.MiddleCenter
        };

        statsStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField)
        {
            fontSize = 12,
            fixedHeight = 25,
            margin = new RectOffset(10, 10, 5, 5)
        };

        warningStyle = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = 12,
            padding = new RectOffset(10, 10, 10, 10)
        };
    }

    private void DrawBackground()
    {
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.15f, 0.15f, 0.15f));
    }

    private void DrawHeader()
    {
        var headerRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(50));
        EditorGUI.DrawRect(headerRect, bgDark);

        GUILayout.Space(10);

        // Icon
        var icon = EditorGUIUtility.IconContent("d_SceneViewTools");
        if (icon != null && icon.image != null)
        {
            GUILayout.Label(icon, GUILayout.Width(40), GUILayout.Height(40));
        }

        // Title
        GUILayout.Label("Reference Finder Pro", titleStyle);

        GUILayout.FlexibleSpace();

        // Database controls
        if (database.isDatabaseBuilt)
        {
            GUI.backgroundColor = successColor;
            GUILayout.Label($"✓ Database Ready ({database.totalAssetsScanned} assets)",
                EditorStyles.miniLabel, GUILayout.Height(30));
            GUI.backgroundColor = Color.white;
        }

        // Rebuild Database button
        GUI.backgroundColor = database.isDatabaseBuilt ? Color.white : warningColor;
        if (GUILayout.Button(new GUIContent("🔄", database.isDatabaseBuilt ? "Rebuild Database" : "Build Database"),
            EditorStyles.toolbarButton, GUILayout.Width(30), GUILayout.Height(30)))
        {
            BuildFullDatabase();
        }
        GUI.backgroundColor = Color.white;

        // Quick Actions
        if (GUILayout.Button(new GUIContent("⚙", "Settings"), EditorStyles.toolbarButton, GUILayout.Width(30), GUILayout.Height(30)))
        {
            ShowSettingsMenu();
        }

        if (GUILayout.Button(new GUIContent("?", "Help"), EditorStyles.toolbarButton, GUILayout.Width(30), GUILayout.Height(30)))
        {
            ShowHelpMenu();
        }

        GUILayout.Space(10);
        EditorGUILayout.EndHorizontal();

        DrawSeparator(primaryColor);
    }

    private void DrawTargetSection()
    {
        EditorGUILayout.Space(10);

        var sectionRect = EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        // Preview
        var previewRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64));
        GUI.Box(previewRect, GUIContent.none, "HelpBox");

        if (targetObject != null)
        {
            var preview = AssetPreview.GetAssetPreview(targetObject);
            if (preview == null) preview = AssetPreview.GetMiniThumbnail(targetObject);
            if (preview != null)
            {
                GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
            }
        }
        else
        {
            var selectIcon = EditorGUIUtility.IconContent("d_Search Icon");
            if (selectIcon != null && selectIcon.image != null)
            {
                GUI.DrawTexture(new Rect(previewRect.x + 20, previewRect.y + 20, 24, 24), selectIcon.image);
            }
        }

        GUILayout.Space(10);

        // Object Field and Info
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Currently Selected", EditorStyles.boldLabel);

        // Auto-sync indicator
        GUI.color = successColor;
        EditorGUILayout.LabelField("● Auto-Tracking", EditorStyles.miniLabel, GUILayout.Width(100));
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        targetObject = EditorGUILayout.ObjectField(targetObject, typeof(Object), true, GUILayout.Height(25));
        if (EditorGUI.EndChangeCheck())
        {
            UpdateTargetInfo();
            if (database.isDatabaseBuilt)
            {
                PerformAutoSearch();
            }

            // Also update Unity's selection to match
            if (targetObject != null)
            {
                Selection.activeObject = targetObject;
            }
        }

        if (!string.IsNullOrEmpty(targetInfo))
        {
            EditorGUILayout.LabelField(targetInfo, EditorStyles.miniLabel);
        }
        else if (targetObject == null)
        {
            EditorGUILayout.LabelField("Select any object in Project or Hierarchy to see its references", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
    }

    private void DrawSearchControls()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        // Refresh Button (for manual refresh if needed)
        if (GUILayout.Button(new GUIContent("🔄 Refresh", "Manually refresh references"),
            GUILayout.Height(30), GUILayout.Width(100)))
        {
            if (targetObject != null)
            {
                PerformAutoSearch();

                // For all scenes search
                if (searchInAllScenes)
                {
                    FindSceneReferences();
                }
            }
        }

        // Settings toggles
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();

        var prevSearchAll = searchInAllScenes;
        searchInAllScenes = GUILayout.Toggle(searchInAllScenes, " All Scenes", GUILayout.Width(100));
        if (searchInAllScenes != prevSearchAll && targetObject != null)
        {
            // Re-search when toggled
            if (searchInAllScenes)
            {
                FindSceneReferences();
            }
            else
            {
                FindSceneReferencesQuick();
            }
        }

        includeDisabled = GUILayout.Toggle(includeDisabled, " Include Disabled", GUILayout.Width(120));
        showFullPaths = GUILayout.Toggle(showFullPaths, " Show Full Paths", GUILayout.Width(120));
        autoUpdateDatabase = GUILayout.Toggle(autoUpdateDatabase, " Auto Update DB", GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        GUILayout.Space(10);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Search Filter
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
        searchFilter = EditorGUILayout.TextField(searchFilter, searchFieldStyle, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("✕", GUILayout.Width(25), GUILayout.Height(25)))
        {
            searchFilter = "";
            GUI.FocusControl(null);
        }
        GUILayout.Space(10);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
    }

    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(10);

        var sceneCount = GetFilteredSceneReferences().Count;
        var projectCount = GetFilteredProjectReferences().Count;

        if (GUILayout.Toggle(selectedTab == 0, $"Scene ({sceneCount})", selectedTab == 0 ? selectedTabStyle : tabStyle))
        {
            selectedTab = 0;
        }

        if (GUILayout.Toggle(selectedTab == 1, $"Project ({projectCount})", selectedTab == 1 ? selectedTabStyle : tabStyle))
        {
            selectedTab = 1;
        }

        if (GUILayout.Toggle(selectedTab == 2, $"Statistics", selectedTab == 2 ? selectedTabStyle : tabStyle))
        {
            selectedTab = 2;
        }

        if (GUILayout.Toggle(selectedTab == 3, $"Database", selectedTab == 3 ? selectedTabStyle : tabStyle))
        {
            selectedTab = 3;
        }

        GUILayout.FlexibleSpace();
        GUILayout.Space(10);
        EditorGUILayout.EndHorizontal();

        DrawSeparator(Color.gray * 0.3f);
    }

    private void DrawResults()
    {
        var resultsRect = EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

        switch (selectedTab)
        {
            case 0:
                DrawSceneResults();
                break;
            case 1:
                DrawProjectResults();
                break;
            case 2:
                DrawStatistics();
                break;
            case 3:
                DrawDatabaseInfo();
                break;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDatabaseInfo()
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("HelpBox");
        EditorGUILayout.LabelField("Database Information", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField($"Status: {(database.isDatabaseBuilt ? "Built" : "Not Built")}");
        EditorGUILayout.LabelField($"Total Assets: {database.totalAssetsScanned}");
        EditorGUILayout.LabelField($"Cached References: {database.assetToReferences.Count}");
        EditorGUILayout.LabelField($"Reverse Lookups: {database.assetToReferencedBy.Count}");
        EditorGUILayout.LabelField($"Last Full Scan: {database.lastFullScanTime:yyyy-MM-dd HH:mm:ss}");

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Database", GUILayout.Width(120)))
        {
            if (EditorUtility.DisplayDialog("Clear Database",
                "This will clear all cached data. You'll need to rebuild the database.",
                "Clear", "Cancel"))
            {
                ClearDatabase();
            }
        }

        if (GUILayout.Button("Export Database", GUILayout.Width(120)))
        {
            ExportDatabase();
        }

        if (GUILayout.Button("Verify Integrity", GUILayout.Width(120)))
        {
            VerifyDatabaseIntegrity();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        // Memory usage estimate
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical("HelpBox");
        EditorGUILayout.LabelField("Memory Usage (Estimate)", EditorStyles.boldLabel);

        long memoryUsage = EstimateMemoryUsage();
        EditorGUILayout.LabelField($"Database Size: {FormatFileSize(memoryUsage)}");

        if (System.IO.File.Exists(DatabasePath))
        {
            var fileInfo = new System.IO.FileInfo(DatabasePath);
            EditorGUILayout.LabelField($"Cache File Size: {FormatFileSize(fileInfo.Length)}");
        }

        EditorGUILayout.EndVertical();
    }

    private long EstimateMemoryUsage()
    {
        // Rough estimate of memory usage
        long bytes = 0;

        foreach (var kvp in database.assetToReferences)
        {
            bytes += kvp.Key.Length * 2; // Unicode strings
            bytes += kvp.Value.Sum(s => s.Length * 2);
        }

        foreach (var kvp in database.assetToReferencedBy)
        {
            bytes += kvp.Key.Length * 2;
            bytes += kvp.Value.Sum(s => s.Length * 2);
        }

        foreach (var kvp in database.assetMetadata)
        {
            bytes += kvp.Key.Length * 2;
            bytes += 100; // Rough estimate for metadata object
        }

        return bytes;
    }

    private void ClearDatabase()
    {
        database = new ReferenceDatabase();
        SaveDatabase();
        searchStatus = "Database cleared";
    }

    private void ExportDatabase()
    {
        string exportPath = EditorUtility.SaveFilePanel("Export Database", "", "ReferenceDatabase", "json");
        if (!string.IsNullOrEmpty(exportPath))
        {
            string json = JsonUtility.ToJson(database, true);
            System.IO.File.WriteAllText(exportPath, json);
            EditorUtility.DisplayDialog("Export Complete", $"Database exported to:\n{exportPath}", "OK");
        }
    }

    private void VerifyDatabaseIntegrity()
    {
        int missingAssets = 0;
        int invalidReferences = 0;

        foreach (var kvp in database.assetToReferences)
        {
            if (!System.IO.File.Exists(kvp.Key))
            {
                missingAssets++;
            }

            foreach (var refPath in kvp.Value)
            {
                if (!System.IO.File.Exists(refPath))
                {
                    invalidReferences++;
                }
            }
        }

        string message = $"Database Integrity Check:\n" +
                        $"Missing Assets: {missingAssets}\n" +
                        $"Invalid References: {invalidReferences}\n\n" +
                        $"{(missingAssets + invalidReferences == 0 ? "Database is healthy!" : "Consider rebuilding the database.")}";

        EditorUtility.DisplayDialog("Database Integrity", message, "OK");
    }

    private void BuildFullDatabase()
    {
        isDatabaseBuilding = true;
        databaseBuildProgress = 0f;

        try
        {
            EditorUtility.DisplayProgressBar("Building Reference Database", "Initializing...", 0f);

            // Clear existing database
            database = new ReferenceDatabase();

            // Get all assets
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths()
                .Where(p => !p.StartsWith("Packages/") && !p.StartsWith("Library/") && p.StartsWith("Assets/"))
                .ToArray();

            Debug.Log($"[Reference Finder] Building database for {allAssetPaths.Length} assets...");

            for (int i = 0; i < allAssetPaths.Length; i++)
            {
                databaseBuildProgress = (float)i / allAssetPaths.Length;

                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("Building Reference Database",
                        $"Processing assets... {i}/{allAssetPaths.Length}",
                        databaseBuildProgress);
                }

                string assetPath = allAssetPaths[i];

                try
                {
                    // Get dependencies for this asset
                    string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

                    // Store in forward lookup
                    database.assetToReferences[assetPath] = dependencies.ToList();

                    // Build reverse lookup
                    foreach (var dep in dependencies)
                    {
                        if (!database.assetToReferencedBy.ContainsKey(dep))
                            database.assetToReferencedBy[dep] = new List<string>();

                        if (!database.assetToReferencedBy[dep].Contains(assetPath))
                            database.assetToReferencedBy[dep].Add(assetPath);
                    }

                    // Store metadata
                    UpdateAssetMetadata(assetPath);
                }
                catch (Exception e)
                {
                    // Skip corrupted assets
                    Debug.LogWarning($"[Reference Finder] Skipped asset {assetPath}: {e.Message}");
                }
            }

            database.totalAssetsScanned = allAssetPaths.Length;
            database.lastFullScanTime = DateTime.Now;
            database.isDatabaseBuilt = true;

            SaveDatabase();

            searchStatus = $"Database built • {database.totalAssetsScanned} assets processed • {DateTime.Now:HH:mm:ss}";
            Debug.Log($"[Reference Finder] Database build complete: {database.totalAssetsScanned} assets processed");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Reference Finder] Database build failed: {e.Message}\n{e.StackTrace}");
            searchStatus = "Database build failed - check console";
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            isDatabaseBuilding = false;
            databaseBuildProgress = 0f;
            Repaint();
        }
    }

    private void SearchInCache()
    {
        if (targetObject == null) return;
        if (!database.isDatabaseBuilt)
        {
            EditorUtility.DisplayDialog("Database Not Built",
                "Please build the database first by clicking the 'Build Database' button.", "OK");
            return;
        }

        cachedProjectReferences.Clear();

        string targetPath = AssetDatabase.GetAssetPath(targetObject);
        if (string.IsNullOrEmpty(targetPath)) return;

        // Look up in reverse index
        if (database.assetToReferencedBy.ContainsKey(targetPath))
        {
            var referencingAssets = database.assetToReferencedBy[targetPath];

            foreach (var assetPath in referencingAssets)
            {
                // Get metadata if available
                AssetMetadata metadata = null;
                if (database.assetMetadata.ContainsKey(assetPath))
                {
                    metadata = database.assetMetadata[assetPath];
                }

                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset != null)
                {
                    cachedProjectReferences.Add(new AssetReference
                    {
                        AssetPath = assetPath,
                        AssetName = metadata?.name ?? System.IO.Path.GetFileName(assetPath),
                        AssetType = metadata?.type ?? asset.GetType().Name,
                        Asset = asset,
                        FileSize = metadata?.fileSize ?? 0
                    });
                }
            }

            searchStatus = $"Found {cachedProjectReferences.Count} project references (from cache) • {DateTime.Now:HH:mm:ss}";
        }
        else
        {
            searchStatus = $"No references found in cache • {DateTime.Now:HH:mm:ss}";
        }

        Repaint();
    }

    private void DrawSceneResults()
    {
        var filtered = GetFilteredSceneReferences();

        if (filtered.Count == 0)
        {
            DrawEmptyResults("No scene references found");
            return;
        }

        sceneScrollPos = EditorGUILayout.BeginScrollView(sceneScrollPos);

        for (int i = 0; i < filtered.Count; i++)
        {
            DrawSceneReferenceItem(filtered[i], i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSceneReferenceItem(SceneReference reference, int index)
    {
        var style = index % 2 == 0 ? resultItemStyle : resultItemAltStyle;

        EditorGUILayout.BeginVertical(style);
        EditorGUILayout.BeginHorizontal();

        // Icon
        var goIcon = EditorGUIUtility.IconContent("GameObject Icon");
        GUILayout.Label(goIcon, iconStyle);

        // GameObject Name
        if (GUILayout.Button(reference.GameObject.name, EditorStyles.label, GUILayout.Width(200)))
        {
            Selection.activeGameObject = reference.GameObject;
            EditorGUIUtility.PingObject(reference.GameObject);
        }

        // Component
        GUILayout.Label($"[{reference.Component.GetType().Name}]", EditorStyles.miniLabel, GUILayout.Width(150));

        // Property
        GUILayout.Label(reference.PropertyName, EditorStyles.label);

        GUILayout.FlexibleSpace();

        // Actions
        if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
        {
            Selection.activeGameObject = reference.GameObject;
        }

        if (GUILayout.Button("Frame", EditorStyles.miniButton, GUILayout.Width(50)))
        {
            Selection.activeGameObject = reference.GameObject;
            SceneView.FrameLastActiveSceneView();
        }

        EditorGUILayout.EndHorizontal();

        if (showFullPaths)
        {
            EditorGUILayout.LabelField($"  Path: {reference.FullPath}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawProjectResults()
    {
        var filtered = GetFilteredProjectReferences();

        if (filtered.Count == 0)
        {
            DrawEmptyResults("No project references found");
            return;
        }

        projectScrollPos = EditorGUILayout.BeginScrollView(projectScrollPos);

        for (int i = 0; i < filtered.Count; i++)
        {
            DrawProjectReferenceItem(filtered[i], i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawProjectReferenceItem(AssetReference reference, int index)
    {
        var style = index % 2 == 0 ? resultItemStyle : resultItemAltStyle;

        EditorGUILayout.BeginVertical(style);
        EditorGUILayout.BeginHorizontal();

        // Icon based on asset type
        var icon = AssetDatabase.GetCachedIcon(reference.AssetPath);
        if (icon != null)
        {
            GUILayout.Label(new GUIContent(icon), iconStyle);
        }

        // Asset Name
        if (GUILayout.Button(reference.AssetName, EditorStyles.label, GUILayout.Width(250)))
        {
            Selection.activeObject = reference.Asset;
            EditorGUIUtility.PingObject(reference.Asset);
        }

        // Type
        GUILayout.Label($"[{reference.AssetType}]", EditorStyles.miniLabel, GUILayout.Width(100));

        GUILayout.FlexibleSpace();

        // File size
        if (reference.FileSize > 0)
        {
            GUILayout.Label(FormatFileSize(reference.FileSize), statsStyle, GUILayout.Width(80));
        }

        // Actions
        if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(50)))
        {
            AssetDatabase.OpenAsset(reference.Asset);
        }

        EditorGUILayout.EndHorizontal();

        if (showFullPaths)
        {
            EditorGUILayout.LabelField($"  {reference.AssetPath}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStatistics()
    {
        EditorGUILayout.BeginVertical();
        GUILayout.Space(20);

        // Summary Card
        DrawStatsCard("Summary", new Dictionary<string, string>
        {
            { "Total References Found", (sceneReferences.Count + cachedProjectReferences.Count).ToString() },
            { "Scene References", sceneReferences.Count.ToString() },
            { "Project References (Cached)", cachedProjectReferences.Count.ToString() },
            { "Database Assets", database.totalAssetsScanned.ToString() },
            { "Cache Age", GetCacheAge() }
        });

        GUILayout.Space(20);

        // Reference Types
        if (sceneReferences.Count > 0)
        {
            var componentGroups = sceneReferences.GroupBy(r => r.Component.GetType().Name);
            var componentStats = new Dictionary<string, string>();
            foreach (var group in componentGroups.OrderByDescending(g => g.Count()).Take(5))
            {
                componentStats[group.Key] = group.Count().ToString();
            }

            if (componentStats.Count > 0)
            {
                DrawStatsCard("Top Components", componentStats);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private string GetCacheAge()
    {
        if (!database.isDatabaseBuilt) return "Not built";

        var age = DateTime.Now - database.lastFullScanTime;
        if (age.TotalMinutes < 60)
            return $"{(int)age.TotalMinutes} minutes";
        else if (age.TotalHours < 24)
            return $"{(int)age.TotalHours} hours";
        else
            return $"{(int)age.TotalDays} days";
    }

    private void DrawStatsCard(string title, Dictionary<string, string> stats)
    {
        EditorGUILayout.BeginVertical("HelpBox");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        foreach (var stat in stats)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(stat.Key, GUILayout.Width(200));
            EditorGUILayout.LabelField(stat.Value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawStatusBar()
    {
        var statusRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(25));
        EditorGUI.DrawRect(statusRect, bgDark);

        GUILayout.Space(10);

        // Status text
        if (isDatabaseBuilding)
        {
            GUILayout.Label($"⚡ Building database... {(int)(databaseBuildProgress * 100)}%", EditorStyles.miniLabel);
        }
        else if (isSearching)
        {
            GUILayout.Label("⚡ Searching...", EditorStyles.miniLabel);
        }
        else if (!string.IsNullOrEmpty(searchStatus))
        {
            GUILayout.Label(searchStatus, EditorStyles.miniLabel);
        }

        GUILayout.FlexibleSpace();

        // Results summary
        GUILayout.Label($"Scene: {sceneReferences.Count} | Project (Cached): {cachedProjectReferences.Count}", statsStyle);

        GUILayout.Space(10);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEmptyState()
    {
        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginVertical();

        GUILayout.FlexibleSpace();

        var centerStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14
        };

        if (!database.isDatabaseBuilt)
        {
            EditorGUILayout.LabelField("Build the database first to enable fast reference finding", centerStyle);

            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = warningColor;
            if (GUILayout.Button("Build Database Now", GUILayout.Width(200), GUILayout.Height(40)))
            {
                BuildFullDatabase();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            var iconStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 32
            };

            EditorGUILayout.LabelField("👆", iconStyle);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Select any object to instantly see its references", centerStyle);
            EditorGUILayout.LabelField("Works with Project assets and Hierarchy objects", centerStyle);
        }

        GUILayout.Space(20);

        // Feature list
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical(GUILayout.Width(300));

        DrawFeature("✓ Auto-tracks selection changes");
        DrawFeature("✓ Instant results from cached database");
        DrawFeature("✓ Real-time scene reference updates");
        DrawFeature("✓ No manual searching needed");
        DrawFeature("✓ Filter and sort results");
        DrawFeature("✓ Works in any Unity window");

        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
    }

    private void DrawEmptyResults(string message)
    {
        GUILayout.FlexibleSpace();

        var centerStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            normal = { textColor = Color.gray }
        };

        EditorGUILayout.LabelField(message, centerStyle);

        if (!string.IsNullOrEmpty(searchFilter))
        {
            EditorGUILayout.LabelField($"Filter: \"{searchFilter}\"", centerStyle);
        }

        GUILayout.FlexibleSpace();
    }

    private void DrawFeature(string feature)
    {
        EditorGUILayout.LabelField(feature, EditorStyles.miniLabel);
    }

    private void DrawSeparator(Color color)
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, color);
    }

    private void HandleDragAndDrop()
    {
        Event evt = Event.current;
        Rect dropArea = new Rect(0, 0, position.width, position.height);

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition)) return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        targetObject = DragAndDrop.objectReferences[0];
                        Selection.activeObject = targetObject; // Also update selection
                        UpdateTargetInfo();
                        if (database.isDatabaseBuilt)
                        {
                            PerformAutoSearch();
                        }
                    }
                }
                break;
        }
    }

    private void UpdateTargetInfo()
    {
        if (targetObject == null)
        {
            targetInfo = "";
            return;
        }

        string type = targetObject.GetType().Name;
        string path = AssetDatabase.GetAssetPath(targetObject);

        if (!string.IsNullOrEmpty(path))
        {
            // Try to get from cache first
            if (database.assetMetadata.ContainsKey(path))
            {
                var metadata = database.assetMetadata[path];
                targetInfo = $"{type} • {FormatFileSize(metadata.fileSize)} • {path}";
            }
            else
            {
                var fileInfo = new System.IO.FileInfo(path);
                if (fileInfo.Exists)
                {
                    targetInfo = $"{type} • {FormatFileSize(fileInfo.Length)} • {path}";
                }
                else
                {
                    targetInfo = $"{type} • {path}";
                }
            }
        }
        else
        {
            targetInfo = $"{type} • Scene Object";
        }
    }

    private void FindSceneReferences()
    {
        sceneReferences.Clear();

        if (searchInAllScenes)
        {
            // Search all scenes in build settings
            var currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

            foreach (var sceneAsset in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                try
                {
                    EditorUtility.DisplayProgressBar("Finding References",
                        $"Searching scene: {System.IO.Path.GetFileNameWithoutExtension(sceneAsset.path)}", 0.3f);

                    // Open scene additively if not already open
                    var scene = EditorSceneManager.OpenScene(sceneAsset.path, OpenSceneMode.Additive);
                    SearchInScene(scene);

                    // Close the scene if it wasn't the active scene
                    if (scene.path != currentScenePath && EditorSceneManager.sceneCount > 1)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Reference Finder] Failed to search scene {sceneAsset.path}: {e.Message}");
                }
            }
        }
        else
        {
            // Search only currently loaded scenes
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    EditorUtility.DisplayProgressBar("Finding References", $"Searching scene: {scene.name}", 0.3f);
                    SearchInScene(scene);
                }
            }
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"[Reference Finder] Found {sceneReferences.Count} scene references");
    }

    private void SearchInScene(UnityEngine.SceneManagement.Scene scene)
    {
        var allObjects = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(includeDisabled))
            .Select(t => t.gameObject)
            .Distinct()
            .ToList();

        foreach (var go in allObjects)
        {
            // Check if this GameObject is a prefab instance of our target
            if (targetObject is GameObject targetPrefab)
            {
                var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(go);
                var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);

                if (prefabAsset == targetPrefab ||
                    (prefabRoot != null && PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot) == targetPrefab))
                {
                    // Found a prefab instance
                    sceneReferences.Add(new SceneReference
                    {
                        GameObject = go,
                        Component = go.transform,
                        PropertyPath = "Prefab Instance",
                        PropertyName = "Prefab Instance",
                        SceneName = scene.name,
                        FullPath = GetGameObjectPath(go)
                    });
                    continue;
                }
            }

            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;

                try
                {
                    using (var so = new SerializedObject(comp))
                    {
                        var prop = so.GetIterator();
                        while (prop.Next(true))
                        {
                            if (prop.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                var referencedObject = prop.objectReferenceValue;
                                if (referencedObject != null && IsReferenceToTarget(referencedObject))
                                {
                                    sceneReferences.Add(new SceneReference
                                    {
                                        GameObject = go,
                                        Component = comp,
                                        PropertyPath = prop.propertyPath,
                                        PropertyName = prop.displayName,
                                        SceneName = scene.name,
                                        FullPath = GetGameObjectPath(go)
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error checking {comp.GetType().Name} on {go.name}: {e.Message}");
                }
            }
        }
    }

    private bool IsReferenceToTarget(Object referencedObject)
    {
        // Direct reference
        if (referencedObject == targetObject)
            return true;

        // Check if it's a reference to the same asset
        string targetPath = AssetDatabase.GetAssetPath(targetObject);
        string referencedPath = AssetDatabase.GetAssetPath(referencedObject);

        if (!string.IsNullOrEmpty(targetPath) && targetPath == referencedPath)
        {
            // Same asset file, might be different sub-asset
            if (targetObject is Sprite targetSprite && referencedObject is Sprite referencedSprite)
            {
                return targetSprite == referencedSprite;
            }

            if (targetObject is Texture2D && referencedObject is Sprite sprite)
            {
                return sprite.texture == targetObject;
            }

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(targetPath);
            if (mainAsset == targetObject || mainAsset == referencedObject)
                return true;
        }

        // Check for prefab instances
        if (targetObject is GameObject && referencedObject is GameObject referencedGO)
        {
            var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(referencedGO);
            if (prefabAsset == targetObject)
                return true;
        }

        return false;
    }

    private List<SceneReference> GetFilteredSceneReferences()
    {
        if (string.IsNullOrEmpty(searchFilter))
            return sceneReferences;

        return sceneReferences.Where(r =>
            r.GameObject.name.ToLower().Contains(searchFilter.ToLower()) ||
            r.Component.GetType().Name.ToLower().Contains(searchFilter.ToLower()) ||
            r.PropertyName.ToLower().Contains(searchFilter.ToLower())
        ).ToList();
    }

    private List<AssetReference> GetFilteredProjectReferences()
    {
        if (string.IsNullOrEmpty(searchFilter))
            return cachedProjectReferences;

        return cachedProjectReferences.Where(r =>
            r.AssetPath.ToLower().Contains(searchFilter.ToLower()) ||
            r.AssetName.ToLower().Contains(searchFilter.ToLower()) ||
            r.AssetType.ToLower().Contains(searchFilter.ToLower())
        ).ToList();
    }

    private string GetGameObjectPath(GameObject go)
    {
        Transform t = go.transform;
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    private Texture2D MakeColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void ShowSettingsMenu()
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Auto Update Database"), autoUpdateDatabase, () => {
            autoUpdateDatabase = !autoUpdateDatabase;
        });

        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Clear Results"), false, () => {
            sceneReferences.Clear();
            cachedProjectReferences.Clear();
            searchStatus = "Results cleared";
        });

        menu.AddItem(new GUIContent("Clear Database"), false, () => {
            if (EditorUtility.DisplayDialog("Clear Database",
                "This will clear all cached data. You'll need to rebuild the database.",
                "Clear", "Cancel"))
            {
                ClearDatabase();
            }
        });

        menu.AddItem(new GUIContent("Reset Settings"), false, () => {
            searchInAllScenes = false;
            includeDisabled = true;
            showFullPaths = false;
            autoUpdateDatabase = true;
        });

        menu.ShowAsContext();
    }

    private void ShowHelpMenu()
    {
        EditorUtility.DisplayDialog("Reference Finder Pro - Auto-Tracking",
            "Automatically shows references for any selected object.\n\n" +
            "KEY FEATURES:\n" +
            "• Auto-tracks selection - just click any object!\n" +
            "• Instant results from cached database\n" +
            "• Real-time updates as you select different objects\n" +
            "• Works with Project assets and Hierarchy objects\n\n" +
            "HOW TO USE:\n" +
            "1. Click 'Build Database' first time (one-time setup)\n" +
            "2. Select ANY object in Project or Hierarchy\n" +
            "3. References appear instantly!\n" +
            "4. Change selection to see different references\n\n" +
            "OPTIONS:\n" +
            "• All Scenes: Search all scenes (slower) or current only\n" +
            "• Include Disabled: Show disabled objects\n" +
            "• Show Full Paths: Display complete hierarchy paths\n" +
            "• Auto Update DB: Keep database synced with changes\n\n" +
            "TIPS:\n" +
            "• Keep window docked for constant reference monitoring\n" +
            "• Use filter to find specific references quickly\n" +
            "• Click reference items to navigate to them",
            "OK");
    }
}

// Separate class to handle asset modifications
public class ReferenceFinderAssetProcessor : AssetModificationProcessor
{
    public static void OnWillCreateAsset(string assetPath)
    {
        ReferenceFinderWindow.HandleAssetCreation(assetPath);
    }

    public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
    {
        ReferenceFinderWindow.HandleAssetDeletion(assetPath);
        return AssetDeleteResult.DidNotDelete;
    }

    public static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
    {
        ReferenceFinderWindow.HandleAssetMove(sourcePath, destinationPath);
        return AssetMoveResult.DidNotMove;
    }
}