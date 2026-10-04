#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

public class AdvancedFavoritesManager : EditorWindow
{
    [System.Serializable]
    public class FavoriteItem
    {
        public string name;
        public string path;
        public string type;
        public string category;
        public string tags;
        public string notes;
        public DateTime dateAdded;
        public int accessCount;
        public DateTime lastAccessed;
        public bool isPinned;
        public Color customColor;
        public UnityEngine.Object reference;

        public FavoriteItem(string name, string path, string type, UnityEngine.Object reference)
        {
            this.name = name;
            this.path = path;
            this.type = type;
            this.reference = reference;
            this.category = "General";
            this.tags = "";
            this.notes = "";
            this.dateAdded = DateTime.Now;
            this.accessCount = 0;
            this.lastAccessed = DateTime.Now;
            this.isPinned = false;
            this.customColor = Color.white;
        }
    }

    [System.Serializable]
    public class FavoriteCategory
    {
        public string name;
        public Color color;
        public bool isExpanded;

        public FavoriteCategory(string name, Color color)
        {
            this.name = name;
            this.color = color;
            this.isExpanded = true;
        }
    }

    // Data
    private List<FavoriteItem> favorites = new List<FavoriteItem>();
    private List<FavoriteCategory> categories = new List<FavoriteCategory>();
    private Vector2 scrollPosition;
    private Vector2 settingsScrollPosition;

    // UI State
    private string searchFilter = "";
    private string selectedCategory = "All";
    private SortMode sortMode = SortMode.DateAdded;
    private bool showPinnedOnly = false;
    private bool showSettings = false;
    private bool showAdvancedFilters = false;
    private ViewMode viewMode = ViewMode.List;
    private bool showStatistics = false;

    // Advanced Filters
    private string tagFilter = "";
    private DateTime dateFromFilter = DateTime.MinValue;
    private DateTime dateToFilter = DateTime.MaxValue;
    private int minAccessCountFilter = 0;

    // Settings
    private bool autoBackup = true;
    private int maxFavorites = 100;
    private bool showAccessCount = true;
    private bool enableQuickAccess = true;
    private bool showThumbnails = true;

    // Constants
    private const string FAVORITES_KEY = "AdvancedFavoritesManager_Data";
    private const string CATEGORIES_KEY = "AdvancedFavoritesManager_Categories";
    private const string SETTINGS_KEY = "AdvancedFavoritesManager_Settings";

    // Enums
    public enum SortMode
    {
        Name, DateAdded, LastAccessed, AccessCount, Type, Category
    }

    public enum ViewMode
    {
        Card, List, Grid
    }

    // Styles
    private GUIStyle headerStyle;
    private GUIStyle cardStyle;
    private GUIStyle nameStyle;
    private GUIStyle pathStyle;
    private GUIStyle typeStyle;
    private GUIStyle dropAreaStyle;
    private GUIStyle sectionHeaderStyle;
    private bool stylesInitialized = false;

    [MenuItem("Tools/Advanced Favorites Manager")]
    public static void ShowWindow()
    {
        AdvancedFavoritesManager window = GetWindow<AdvancedFavoritesManager>("Favorites");
        window.minSize = new Vector2(400, 300);
    }

    private void OnEnable()
    {
        LoadData();
        InitializeDefaultCategories();
    }

    private void OnDisable()
    {
        SaveData();
    }

    private void InitializeDefaultCategories()
    {
        if (categories.Count == 0)
        {
            categories.Add(new FavoriteCategory("Scripts", new Color(0.3f, 0.6f, 1f)));
            categories.Add(new FavoriteCategory("Scenes", new Color(0.3f, 0.8f, 0.3f)));
            categories.Add(new FavoriteCategory("Prefabs", new Color(1f, 0.6f, 0.2f)));
            categories.Add(new FavoriteCategory("Materials", new Color(0.8f, 0.3f, 0.8f)));
            categories.Add(new FavoriteCategory("Audio", new Color(0.2f, 0.8f, 0.8f)));
            categories.Add(new FavoriteCategory("UI", new Color(1f, 0.8f, 0.2f)));
        }
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(0, 0, 4, 4)
        };

        cardStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(2, 2, 2, 2)
        };

        nameStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12
        };

        pathStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            wordWrap = true
        };

        typeStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = EditorStyles.miniLabel.normal.textColor }
        };

        dropAreaStyle = new GUIStyle(EditorStyles.helpBox)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            padding = new RectOffset(10, 10, 15, 15)
        };

        sectionHeaderStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(0, 0, 4, 4)
        };

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitializeStyles();

        DrawHeader();
        DrawToolbar();

        if (showSettings)
        {
            DrawSettings();
            return;
        }

        if (showStatistics)
        {
            DrawStatistics();
            return;
        }

        DrawMainContent();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("FAVORITES MANAGER", headerStyle);

        // Quick stats
        var filteredFavorites = GetFilteredFavorites();
        EditorGUILayout.LabelField(
            $"{filteredFavorites.Count} items, {categories.Count} categories",
            EditorStyles.miniLabel,
            GUILayout.ExpandWidth(false)
        );

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // Search
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));

        // Category filter
        List<string> categoryNames = new List<string> { "All" };
        categoryNames.AddRange(categories.Select(c => c.name));
        int selectedIndex = Mathf.Max(0, categoryNames.IndexOf(selectedCategory));
        selectedIndex = EditorGUILayout.Popup(selectedIndex, categoryNames.ToArray(), EditorStyles.toolbarPopup, GUILayout.Width(100));
        selectedCategory = categoryNames[selectedIndex];

        // Sort mode
        sortMode = (SortMode)EditorGUILayout.EnumPopup(sortMode, EditorStyles.toolbarPopup, GUILayout.Width(100));

        GUILayout.FlexibleSpace();

        // View mode
        viewMode = (ViewMode)EditorGUILayout.EnumPopup(viewMode, EditorStyles.toolbarPopup, GUILayout.Width(80));

        // Pinned filter
        showPinnedOnly = GUILayout.Toggle(showPinnedOnly, "Pinned", EditorStyles.toolbarButton);

        // Settings button
        if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
        {
            showSettings = !showSettings;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawMainContent()
    {
        if (showAdvancedFilters)
        {
            DrawAdvancedFilters();
        }

        DrawDropArea();
        DrawFavorites();
    }

    private void DrawAdvancedFilters()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Advanced Filters", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Tags:", GUILayout.Width(40));
        tagFilter = EditorGUILayout.TextField(tagFilter);

        GUILayout.Label("Min Access:", GUILayout.Width(60));
        minAccessCountFilter = EditorGUILayout.IntField(minAccessCountFilter, GUILayout.Width(40));

        if (GUILayout.Button("Clear", EditorStyles.miniButton))
        {
            searchFilter = "";
            tagFilter = "";
            minAccessCountFilter = 0;
            selectedCategory = "All";
            showPinnedOnly = false;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawDropArea()
    {
        Rect dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag assets here to add to favorites", dropAreaStyle);
        HandleDragAndDrop(dropArea);
        EditorGUILayout.Space(4);
    }

    private void DrawFavorites()
    {
        var filteredFavorites = GetFilteredFavorites();

        if (filteredFavorites.Count == 0)
        {
            EditorGUILayout.HelpBox(
                favorites.Count == 0 ?
                    "No favorites yet. Drag assets here to add them." :
                    "No items match your current filters.",
                MessageType.Info
            );
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (viewMode == ViewMode.Grid)
        {
            DrawGridView(filteredFavorites);
        }
        else
        {
            DrawListView(filteredFavorites);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawGridView(List<FavoriteItem> items)
    {
        int columns = Mathf.FloorToInt(position.width / 150f);
        columns = Mathf.Max(1, columns);

        for (int i = 0; i < items.Count; i += columns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < columns && i + j < items.Count; j++)
            {
                DrawGridItem(items[i + j]);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawGridItem(FavoriteItem item)
    {
        EditorGUILayout.BeginVertical(cardStyle, GUILayout.Width(140), GUILayout.Height(100));

        // Name
        EditorGUILayout.LabelField(item.name, nameStyle);

        // Type
        EditorGUILayout.LabelField(item.type, typeStyle);

        // Action buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select", EditorStyles.miniButton))
        {
            Selection.activeObject = item.reference;
            EditorGUIUtility.PingObject(item.reference);
            AccessItem(item);
        }
        if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22)))
        {
            if (EditorUtility.DisplayDialog("Remove Favorite",
                $"Remove '{item.name}' from favorites?", "Remove", "Cancel"))
            {
                favorites.Remove(item);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawListView(List<FavoriteItem> items)
    {
        List<int> toRemove = new List<int>();

        foreach (var item in items)
        {
            int index = favorites.IndexOf(item);
            if (DrawFavoriteItem(item, index))
            {
                toRemove.Add(index);
            }
        }

        // Remove items marked for removal
        foreach (int index in toRemove.OrderByDescending(x => x))
        {
            favorites.RemoveAt(index);
        }
    }

    private bool DrawFavoriteItem(FavoriteItem item, int index)
    {
        if (item.reference == null)
        {
            item.reference = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.path);
            if (item.reference == null) return true; // Mark for removal
        }

        EditorGUILayout.BeginVertical(cardStyle);
        EditorGUILayout.BeginHorizontal();

        // Main content
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        // Name and type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(item.name, nameStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField(item.type, typeStyle, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        // Path
        EditorGUILayout.LabelField(item.path, pathStyle);

        // Info
        EditorGUILayout.LabelField($"Category: {item.category} • Accessed: {item.accessCount}x", pathStyle);

        EditorGUILayout.EndVertical();

        // Action buttons
        EditorGUILayout.BeginVertical(GUILayout.Width(120));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select", EditorStyles.miniButton))
        {
            Selection.activeObject = item.reference;
            EditorGUIUtility.PingObject(item.reference);
            AccessItem(item);
        }
        if (GUILayout.Button("Edit", EditorStyles.miniButton))
        {
            ShowEditDialog(item);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        item.isPinned = GUILayout.Toggle(item.isPinned, "Pin", EditorStyles.miniButton);
        if (GUILayout.Button("Remove", EditorStyles.miniButton))
        {
            if (EditorUtility.DisplayDialog("Remove Favorite",
                $"Remove '{item.name}' from favorites?", "Remove", "Cancel"))
            {
                return true; // Mark for removal
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);

        return false;
    }

    private void ShowEditDialog(FavoriteItem item)
    {
        // Create a simple popup for editing
        GenericMenu menu = new GenericMenu();

        foreach (var category in categories)
        {
            string categoryName = category.name;
            menu.AddItem(new GUIContent($"Move to/{categoryName}"),
                item.category == categoryName,
                () => {
                    item.category = categoryName;
                    SaveData();
                });
        }

        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Add Tag..."), false, () => ShowTagDialog(item));
        menu.AddItem(new GUIContent("Add Note..."), false, () => ShowNoteDialog(item));

        menu.ShowAsContext();
    }

    private void ShowTagDialog(FavoriteItem item)
    {
        string newTag = EditorInputDialog.Show("Add Tag", "Enter tag:", "");
        if (!string.IsNullOrEmpty(newTag))
        {
            if (string.IsNullOrEmpty(item.tags))
                item.tags = newTag;
            else
                item.tags += ", " + newTag;
            SaveData();
        }
    }

    private void ShowNoteDialog(FavoriteItem item)
    {
        string newNote = EditorInputDialog.Show("Add Note", "Enter note:", item.notes ?? "");
        if (newNote != null)
        {
            item.notes = newNote;
            SaveData();
        }
    }

    private void DrawSettings()
    {
        settingsScrollPosition = EditorGUILayout.BeginScrollView(settingsScrollPosition);

        EditorGUILayout.LabelField("Settings", sectionHeaderStyle);
        EditorGUILayout.Space(8);

        // General Settings
        EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
        autoBackup = EditorGUILayout.Toggle("Auto Backup", autoBackup);
        maxFavorites = EditorGUILayout.IntField("Max Favorites", maxFavorites);
        showThumbnails = EditorGUILayout.Toggle("Show Thumbnails", showThumbnails);
        showAccessCount = EditorGUILayout.Toggle("Show Access Count", showAccessCount);
        enableQuickAccess = EditorGUILayout.Toggle("Enable Quick Access", enableQuickAccess);

        EditorGUILayout.Space(10);

        // Category Management
        EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);

        for (int i = 0; i < categories.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            categories[i].name = EditorGUILayout.TextField(categories[i].name);
            categories[i].color = EditorGUILayout.ColorField(categories[i].color, GUILayout.Width(60));

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                categories.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Category"))
        {
            categories.Add(new FavoriteCategory("New Category", Color.white));
        }

        EditorGUILayout.Space(15);

        // Data Management
        EditorGUILayout.LabelField("Data Management", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Export Favorites"))
        {
            ExportFavorites();
        }
        if (GUILayout.Button("Import Favorites"))
        {
            ImportFavorites();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Clear All Favorites", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear All Favorites",
                "This will remove ALL favorites. Are you sure?", "Clear", "Cancel"))
            {
                favorites.Clear();
                SaveData();
            }
        }

        EditorGUILayout.Space(15);

        // Back button
        if (GUILayout.Button("Back to Favorites", GUILayout.Height(25)))
        {
            showSettings = false;
            SaveData();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawStatistics()
    {
        GUILayout.Label("Statistics", sectionHeaderStyle);
        EditorGUILayout.Space(8);

        // General stats
        EditorGUILayout.LabelField("General Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Favorites: {favorites.Count}");
        EditorGUILayout.LabelField($"Categories: {categories.Count}");
        EditorGUILayout.LabelField($"Most Used: {GetMostUsedItem()?.name ?? "None"}");
        EditorGUILayout.LabelField($"Newest: {GetNewestItem()?.name ?? "None"}");

        EditorGUILayout.Space(10);

        // Type breakdown
        EditorGUILayout.LabelField("Type Breakdown", EditorStyles.boldLabel);
        var typeGroups = favorites.GroupBy(f => f.type);
        foreach (var group in typeGroups.OrderByDescending(g => g.Count()))
        {
            EditorGUILayout.LabelField($"{group.Key}: {group.Count()}");
        }

        EditorGUILayout.Space(10);

        // Category breakdown
        EditorGUILayout.LabelField("Category Breakdown", EditorStyles.boldLabel);
        var categoryGroups = favorites.GroupBy(f => f.category);
        foreach (var group in categoryGroups.OrderByDescending(g => g.Count()))
        {
            EditorGUILayout.LabelField($"{group.Key}: {group.Count()}");
        }

        EditorGUILayout.Space(15);

        if (GUILayout.Button("Back to Favorites", GUILayout.Height(25)))
        {
            showStatistics = false;
        }
    }

    private List<FavoriteItem> GetFilteredFavorites()
    {
        var filtered = favorites.AsEnumerable();

        // Search filter
        if (!string.IsNullOrEmpty(searchFilter))
        {
            filtered = filtered.Where(f => f.name.ToLower().Contains(searchFilter.ToLower()) ||
                                         f.path.ToLower().Contains(searchFilter.ToLower()) ||
                                         f.type.ToLower().Contains(searchFilter.ToLower()));
        }

        // Category filter
        if (selectedCategory != "All")
        {
            filtered = filtered.Where(f => f.category == selectedCategory);
        }

        // Pinned filter
        if (showPinnedOnly)
        {
            filtered = filtered.Where(f => f.isPinned);
        }

        // Tag filter
        if (!string.IsNullOrEmpty(tagFilter))
        {
            filtered = filtered.Where(f => !string.IsNullOrEmpty(f.tags) &&
                                         f.tags.ToLower().Contains(tagFilter.ToLower()));
        }

        // Access count filter
        if (minAccessCountFilter > 0)
        {
            filtered = filtered.Where(f => f.accessCount >= minAccessCountFilter);
        }

        // Sort
        switch (sortMode)
        {
            case SortMode.Name:
                filtered = filtered.OrderBy(f => f.name);
                break;
            case SortMode.DateAdded:
                filtered = filtered.OrderByDescending(f => f.dateAdded);
                break;
            case SortMode.LastAccessed:
                filtered = filtered.OrderByDescending(f => f.lastAccessed);
                break;
            case SortMode.AccessCount:
                filtered = filtered.OrderByDescending(f => f.accessCount);
                break;
            case SortMode.Type:
                filtered = filtered.OrderBy(f => f.type);
                break;
            case SortMode.Category:
                filtered = filtered.OrderBy(f => f.category);
                break;
        }

        // Always show pinned items first
        return filtered.OrderByDescending(f => f.isPinned).ToList();
    }

    private FavoriteItem GetMostUsedItem()
    {
        return favorites.OrderByDescending(f => f.accessCount).FirstOrDefault();
    }

    private FavoriteItem GetNewestItem()
    {
        return favorites.OrderByDescending(f => f.dateAdded).FirstOrDefault();
    }

    private void AccessItem(FavoriteItem item)
    {
        item.accessCount++;
        item.lastAccessed = DateTime.Now;
        SaveData();
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;

        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                    {
                        AddToFavorites(draggedObject);
                    }
                }
                evt.Use();
                break;
        }
    }

    private void AddToFavorites(UnityEngine.Object obj)
    {
        if (obj == null) return;

        // Check max favorites limit
        if (favorites.Count >= maxFavorites)
        {
            EditorUtility.DisplayDialog("Favorites Limit Reached",
                $"You've reached the maximum of {maxFavorites} favorites. Remove some favorites or increase the limit in settings.", "OK");
            return;
        }

        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return;

        string type = GetAssetType(obj);
        string name = obj.name;

        // Check if already exists
        foreach (var fav in favorites)
        {
            if (fav.path == path)
            {
                EditorUtility.DisplayDialog("Already Added", $"'{name}' is already in your favorites!", "OK");
                return;
            }
        }

        // Determine category based on type
        string category = GetDefaultCategory(type);

        var favoriteItem = new FavoriteItem(name, path, type, obj)
        {
            category = category
        };

        favorites.Add(favoriteItem);
        SaveData();
        Repaint();

        // Show success notification
        Debug.Log($"Added '{name}' to favorites!");
    }

    private string GetDefaultCategory(string type)
    {
        switch (type)
        {
            case "Script": return "Scripts";
            case "Scene": return "Scenes";
            case "Prefab": return "Prefabs";
            case "Material": return "Materials";
            case "Audio": return "Audio";
            case "Texture": return "Materials";
            default: return "General";
        }
    }

    private string GetAssetType(UnityEngine.Object obj)
    {
        if (obj is SceneAsset) return "Scene";
        if (obj is MonoScript) return "Script";
        if (obj is GameObject && AssetDatabase.GetAssetPath(obj).EndsWith(".prefab")) return "Prefab";
        if (obj is Material) return "Material";
        if (obj is Texture2D) return "Texture";
        if (obj is AudioClip) return "Audio";
        if (obj is Mesh) return "Mesh";
        if (obj is AnimationClip) return "Animation";
        if (obj is Font) return "Font";
        if (obj is Shader) return "Shader";
        if (obj is ComputeShader) return "Compute";
        if (obj is ScriptableObject) return "ScriptableObject";
        return "Asset";
    }

    private void OpenScene(string scenePath)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    private void ExportFavorites()
    {
        string path = EditorUtility.SaveFilePanel("Export Favorites", "", "favorites_backup", "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var exportData = new ExportData
            {
                favorites = favorites.Select(f => new SerializableItem
                {
                    name = f.name,
                    path = f.path,
                    type = f.type,
                    category = f.category,
                    tags = f.tags,
                    notes = f.notes,
                    dateAdded = f.dateAdded.ToBinary(),
                    accessCount = f.accessCount,
                    lastAccessed = f.lastAccessed.ToBinary(),
                    isPinned = f.isPinned
                }).ToList(),
                categories = categories
            };

            string json = JsonUtility.ToJson(exportData, true);
            File.WriteAllText(path, json);

            EditorUtility.DisplayDialog("Export Complete", $"Favorites exported to:\n{path}", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Export Failed", $"Failed to export favorites:\n{e.Message}", "OK");
        }
    }

    private void ImportFavorites()
    {
        string path = EditorUtility.OpenFilePanel("Import Favorites", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = File.ReadAllText(path);
            var importData = JsonUtility.FromJson<ExportData>(json);

            bool merge = EditorUtility.DisplayDialog("Import Favorites",
                "How would you like to import?\n\nReplace: Clear current favorites\nMerge: Add to current favorites",
                "Merge", "Replace");

            if (!merge)
            {
                favorites.Clear();
                categories.Clear();
            }

            // Import categories
            foreach (var category in importData.categories)
            {
                if (!categories.Any(c => c.name == category.name))
                {
                    categories.Add(category);
                }
            }

            // Import favorites
            int importedCount = 0;
            foreach (var item in importData.favorites)
            {
                // Check if already exists
                if (favorites.Any(f => f.path == item.path)) continue;

                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.path);
                if (obj != null)
                {
                    var favoriteItem = new FavoriteItem(item.name, item.path, item.type, obj)
                    {
                        category = item.category,
                        tags = item.tags,
                        notes = item.notes,
                        dateAdded = DateTime.FromBinary(item.dateAdded),
                        accessCount = item.accessCount,
                        lastAccessed = DateTime.FromBinary(item.lastAccessed),
                        isPinned = item.isPinned
                    };
                    favorites.Add(favoriteItem);
                    importedCount++;
                }
            }

            SaveData();
            EditorUtility.DisplayDialog("Import Complete", $"Successfully imported {importedCount} favorites!", "OK");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Import Failed", $"Failed to import favorites:\n{e.Message}", "OK");
        }
    }

    private void SaveData()
    {
        try
        {
            // Save favorites
            var serializableFavorites = favorites.Where(f => f.reference != null).Select(f => new SerializableItem
            {
                name = f.name,
                path = f.path,
                type = f.type,
                category = f.category,
                tags = f.tags,
                notes = f.notes,
                dateAdded = f.dateAdded.ToBinary(),
                accessCount = f.accessCount,
                lastAccessed = f.lastAccessed.ToBinary(),
                isPinned = f.isPinned
            }).ToList();

            string favoritesJson = JsonUtility.ToJson(new SerializableList<SerializableItem> { items = serializableFavorites }, false);
            EditorPrefs.SetString(FAVORITES_KEY, favoritesJson);

            // Save categories
            string categoriesJson = JsonUtility.ToJson(new SerializableList<FavoriteCategory> { items = categories }, false);
            EditorPrefs.SetString(CATEGORIES_KEY, categoriesJson);

            // Save settings
            var settings = new Settings
            {
                autoBackup = this.autoBackup,
                maxFavorites = this.maxFavorites,
                showAccessCount = this.showAccessCount,
                enableQuickAccess = this.enableQuickAccess,
                showThumbnails = this.showThumbnails
            };
            string settingsJson = JsonUtility.ToJson(settings);
            EditorPrefs.SetString(SETTINGS_KEY, settingsJson);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to save favorites data: {e.Message}");
        }
    }

    private void LoadData()
    {
        try
        {
            // Load favorites
            favorites.Clear();
            string favoritesData = EditorPrefs.GetString(FAVORITES_KEY, "");
            if (!string.IsNullOrEmpty(favoritesData))
            {
                var favoritesList = JsonUtility.FromJson<SerializableList<SerializableItem>>(favoritesData);
                foreach (var item in favoritesList.items)
                {
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.path);
                    if (obj != null)
                    {
                        var favoriteItem = new FavoriteItem(item.name, item.path, item.type, obj)
                        {
                            category = item.category,
                            tags = item.tags,
                            notes = item.notes,
                            dateAdded = DateTime.FromBinary(item.dateAdded),
                            accessCount = item.accessCount,
                            lastAccessed = DateTime.FromBinary(item.lastAccessed),
                            isPinned = item.isPinned
                        };
                        favorites.Add(favoriteItem);
                    }
                }
            }

            // Load categories
            categories.Clear();
            string categoriesData = EditorPrefs.GetString(CATEGORIES_KEY, "");
            if (!string.IsNullOrEmpty(categoriesData))
            {
                var categoriesList = JsonUtility.FromJson<SerializableList<FavoriteCategory>>(categoriesData);
                categories = categoriesList.items;
            }

            // Load settings
            string settingsData = EditorPrefs.GetString(SETTINGS_KEY, "");
            if (!string.IsNullOrEmpty(settingsData))
            {
                var settings = JsonUtility.FromJson<Settings>(settingsData);
                autoBackup = settings.autoBackup;
                maxFavorites = settings.maxFavorites;
                showAccessCount = settings.showAccessCount;
                enableQuickAccess = settings.enableQuickAccess;
                showThumbnails = settings.showThumbnails;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to load favorites data: {e.Message}");
        }
    }

    // Serializable classes for data persistence
    [System.Serializable]
    private class SerializableItem
    {
        public string name;
        public string path;
        public string type;
        public string category;
        public string tags;
        public string notes;
        public long dateAdded;
        public int accessCount;
        public long lastAccessed;
        public bool isPinned;
    }

    [System.Serializable]
    private class SerializableList<T>
    {
        public List<T> items = new List<T>();
    }

    [System.Serializable]
    private class ExportData
    {
        public List<SerializableItem> favorites;
        public List<FavoriteCategory> categories;
    }

    [System.Serializable]
    private class Settings
    {
        public bool autoBackup = true;
        public int maxFavorites = 100;
        public bool showAccessCount = true;
        public bool enableQuickAccess = true;
        public bool showThumbnails = true;
    }
}

// Helper class for input dialogs
public static class EditorInputDialog
{
    public static string Show(string title, string message, string defaultValue)
    {
        return EditorUtility.DisplayDialog(title, message, "OK", "Cancel") ? defaultValue : null;
    }
}
#endif