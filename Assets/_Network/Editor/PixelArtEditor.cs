using UnityEngine;
using UnityEditor;
using System.IO;

public class PixelArtEditor : EditorWindow
{
    private int gridWidth = 20;
    private int gridHeight = 20;
    private Color[,] pixelGrid;
    private Color selectedColor = Color.white;
    private bool brushMode = true;
    private bool gridCreated = false;
    private string savePath = "Assets/PixelArt/";
    private string fileName = "pixel_art";

    // Grid display settings
    private float cellSize = 12f;
    private bool isDragging = false;
    private Rect gridContainerRect;
    private Vector2 gridOffset = Vector2.zero;

    [MenuItem("Tools/Pixel Art Editor")]
    public static void ShowWindow()
    {
        GetWindow<PixelArtEditor>("Pixel Art Editor");
    }

    private void OnEnable()
    {
        CreateGrid();
    }

    private void OnGUI()
    {
        // Main layout
        EditorGUILayout.BeginVertical();

        // Header
        DrawHeader();

        // Settings panel
        DrawSettingsPanel();

        // Main grid area - this will take remaining space
        DrawGridArea();

        // Bottom controls
        DrawBottomControls();

        EditorGUILayout.EndVertical();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Pixel Art Editor", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
    }

    private void DrawSettingsPanel()
    {
        // Create a horizontal layout for settings
        EditorGUILayout.BeginHorizontal();

        // Left side - Grid settings
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200));
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

        int newWidth = EditorGUILayout.IntField("Width:", gridWidth);
        int newHeight = EditorGUILayout.IntField("Height:", gridHeight);

        newWidth = Mathf.Clamp(newWidth, 1, 100);
        newHeight = Mathf.Clamp(newHeight, 1, 100);

        if (newWidth != gridWidth || newHeight != gridHeight)
        {
            gridWidth = newWidth;
            gridHeight = newHeight;
            CreateGrid();
        }

        EditorGUILayout.Space(10);

        // Tools
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
        brushMode = EditorGUILayout.Toggle("Brush Mode", brushMode);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Selected Color:");
        selectedColor = EditorGUILayout.ColorField(selectedColor);

        EditorGUILayout.EndVertical();

        // Middle - Color palette
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(150));
        EditorGUILayout.LabelField("Color Palette", EditorStyles.boldLabel);

        // Color palette in grid format
        EditorGUILayout.BeginHorizontal();
        if (ColorButton(Color.black, 25)) selectedColor = Color.black;
        if (ColorButton(Color.white, 25)) selectedColor = Color.white;
        if (ColorButton(Color.red, 25)) selectedColor = Color.red;
        if (ColorButton(Color.green, 25)) selectedColor = Color.green;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (ColorButton(Color.blue, 25)) selectedColor = Color.blue;
        if (ColorButton(Color.yellow, 25)) selectedColor = Color.yellow;
        if (ColorButton(Color.cyan, 25)) selectedColor = Color.cyan;
        if (ColorButton(Color.magenta, 25)) selectedColor = Color.magenta;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        // Right side - Save settings
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Save Settings", EditorStyles.boldLabel);

        savePath = EditorGUILayout.TextField("Path:", savePath);
        fileName = EditorGUILayout.TextField("Name:", fileName);

        if (GUILayout.Button("Choose Location"))
        {
            string path = EditorUtility.OpenFolderPanel("Choose Save Location", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    savePath = "Assets" + path.Substring(Application.dataPath.Length) + "/";
                }
            }
        }

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Save PNG", GUILayout.Height(30)))
        {
            SavePixelArt();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
    }

    private void DrawGridArea()
    {
        // Create a dedicated area for the grid that takes remaining space
        EditorGUILayout.LabelField("Canvas", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Left Click/Drag: Paint | Right Click/Drag: Erase", MessageType.Info);

        // Calculate available space for grid
        Rect windowRect = position;
        float availableHeight = windowRect.height - 200; // Reserve space for other UI elements
        float availableWidth = windowRect.width - 20; // Some padding

        // Calculate optimal cell size to fit the grid in available space
        float maxCellWidth = availableWidth / gridWidth;
        float maxCellHeight = availableHeight / gridHeight;
        cellSize = Mathf.Min(maxCellWidth, maxCellHeight, 20f); // Cap at 20px max
        cellSize = Mathf.Max(cellSize, 4f); // Minimum 4px

        // Calculate actual grid size
        float gridPixelWidth = gridWidth * cellSize;
        float gridPixelHeight = gridHeight * cellSize;

        // Create container rect for the grid
        gridContainerRect = GUILayoutUtility.GetRect(availableWidth, availableHeight);

        // Draw container background (dull gray)
        EditorGUI.DrawRect(gridContainerRect, new Color(0.4f, 0.4f, 0.4f, 1f));

        // Center the grid within the container
        float offsetX = (gridContainerRect.width - gridPixelWidth) * 0.5f;
        float offsetY = (gridContainerRect.height - gridPixelHeight) * 0.5f;

        Rect gridRect = new Rect(
            gridContainerRect.x + offsetX,
            gridContainerRect.y + offsetY,
            gridPixelWidth,
            gridPixelHeight
        );

        // Draw grid background (lighter gray for actual grid area)
        EditorGUI.DrawRect(gridRect, new Color(0.6f, 0.6f, 0.6f, 1f));

        // Handle mouse input
        HandleMouseInput(Event.current, gridRect);

        // Draw the pixel grid
        DrawPixelGrid(gridRect);
    }

    private void DrawPixelGrid(Rect gridRect)
    {
        if (pixelGrid == null) return;

        // Draw individual pixels and grid lines
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Rect cellRect = new Rect(
                    gridRect.x + x * cellSize,
                    gridRect.y + y * cellSize,
                    cellSize,
                    cellSize
                );

                // Draw pixel color
                Color pixelColor = pixelGrid[x, y];
                if (pixelColor.a > 0) // Only draw if not transparent
                {
                    EditorGUI.DrawRect(cellRect, pixelColor);
                }

                // Draw grid lines (subtle)
                Color gridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);

                // Horizontal line (top of cell)
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellSize, 1), gridLineColor);
                // Vertical line (left of cell) 
                EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellSize), gridLineColor);
            }
        }

        // Draw final border lines (right and bottom)
        Color borderColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        EditorGUI.DrawRect(new Rect(gridRect.x + gridWidth * cellSize, gridRect.y, 1, gridHeight * cellSize + 1), borderColor);
        EditorGUI.DrawRect(new Rect(gridRect.x, gridRect.y + gridHeight * cellSize, gridWidth * cellSize + 1, 1), borderColor);
    }

    private void DrawBottomControls()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Clear Grid", GUILayout.Height(25)))
        {
            ClearGrid();
        }

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Reset to 20x20", GUILayout.Height(25)))
        {
            gridWidth = 20;
            gridHeight = 20;
            CreateGrid();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();

        // Grid info
        EditorGUILayout.LabelField($"Grid: {gridWidth}x{gridHeight} | Cell Size: {cellSize:F1}px", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    private void HandleMouseInput(Event e, Rect gridRect)
    {
        Vector2 mousePos = e.mousePosition;

        // Check if mouse is within grid bounds
        if (!gridRect.Contains(mousePos))
        {
            if (e.type == EventType.MouseUp)
            {
                isDragging = false;
            }
            return;
        }

        // Calculate grid coordinates
        int gridX = Mathf.FloorToInt((mousePos.x - gridRect.x) / cellSize);
        int gridY = Mathf.FloorToInt((mousePos.y - gridRect.y) / cellSize);

        // Clamp to grid bounds
        gridX = Mathf.Clamp(gridX, 0, gridWidth - 1);
        gridY = Mathf.Clamp(gridY, 0, gridHeight - 1);

        bool shouldPaint = false;
        Color colorToPaint = Color.clear;

        // Handle different mouse events
        switch (e.type)
        {
            case EventType.MouseDown:
                isDragging = true;
                shouldPaint = true;
                if (e.button == 0) // Left mouse button - paint
                {
                    colorToPaint = selectedColor;
                }
                else if (e.button == 1) // Right mouse button - erase
                {
                    colorToPaint = Color.clear;
                }
                e.Use();
                break;

            case EventType.MouseDrag:
                if (isDragging && brushMode)
                {
                    shouldPaint = true;
                    if (e.button == 0) // Left mouse button - paint
                    {
                        colorToPaint = selectedColor;
                    }
                    else if (e.button == 1) // Right mouse button - erase
                    {
                        colorToPaint = Color.clear;
                    }
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                isDragging = false;
                e.Use();
                break;
        }

        // Apply the painting
        if (shouldPaint)
        {
            pixelGrid[gridX, gridY] = colorToPaint;
            Repaint();
        }
    }

    private void CreateGrid()
    {
        pixelGrid = new Color[gridWidth, gridHeight];

        // Initialize with transparent color
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                pixelGrid[x, y] = Color.clear;
            }
        }

        gridCreated = true;
        Repaint();
    }

    private bool ColorButton(Color color, float size = 30)
    {
        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        bool clicked = GUILayout.Button("", GUILayout.Width(size), GUILayout.Height(size));
        GUI.backgroundColor = oldColor;
        return clicked;
    }

    private void ClearGrid()
    {
        if (pixelGrid != null)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    pixelGrid[x, y] = Color.clear;
                }
            }
            Repaint();
        }
    }

    private void SavePixelArt()
    {
        if (pixelGrid == null)
        {
            EditorUtility.DisplayDialog("Error", "No pixel art to save. Create a grid first.", "OK");
            return;
        }

        // Create texture
        Texture2D texture = new Texture2D(gridWidth, gridHeight, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point; // Pixel perfect

        // Set pixels
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Unity texture coordinates are flipped vertically
                texture.SetPixel(x, gridHeight - 1 - y, pixelGrid[x, y]);
            }
        }

        texture.Apply();

        // Convert to PNG
        byte[] pngData = texture.EncodeToPNG();

        // Ensure directory exists
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // Save file
        string fullPath = Path.Combine(savePath, fileName + ".png");
        File.WriteAllBytes(fullPath, pngData);

        // Cleanup
        DestroyImmediate(texture);

        // Refresh asset database
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", $"Pixel art saved to: {fullPath}", "OK");

        Debug.Log($"Pixel art saved to: {fullPath}");
    }
}