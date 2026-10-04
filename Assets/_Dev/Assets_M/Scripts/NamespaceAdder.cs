#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class NamespaceAdder : EditorWindow
{
    private string newNamespace = "YourNamespace";
    private List<MonoScript> selectedScripts = new List<MonoScript>();
    private Vector2 scrollPosition;

    [MenuItem("Nasmo Studio/Namespace Adder")]
    public static void ShowWindow()
    {
        GetWindow<NamespaceAdder>("Namespace Adder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Namespace Adder", EditorStyles.boldLabel);
        newNamespace = EditorGUILayout.TextField("Namespace:", newNamespace);

        EditorGUILayout.Space();
        GUILayout.Label("Drag and Drop Scripts Below", EditorStyles.boldLabel);

        DragAndDropArea();

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Namespace to Selected Scripts"))
        {
            if (selectedScripts.Count > 0 && !string.IsNullOrEmpty(newNamespace))
            {
                AddNamespaceToSelectedScripts(newNamespace, selectedScripts);
            }
            else
            {
                ConstantsData_M.LogInfo("Please drag and drop scripts and provide a namespace.");
            }
        }
    }

    private void DragAndDropArea()
    {
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag & Drop Scripts Here", EditorStyles.helpBox);

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

                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is MonoScript)
                        {
                            selectedScripts.Add((MonoScript)draggedObject);
                        }
                    }
                }
                break;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        // Display selected scripts with remove buttons
        for (int i = selectedScripts.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();

            // Script field
            EditorGUILayout.ObjectField(selectedScripts[i], typeof(MonoScript), false);

            // Remove button
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                selectedScripts.RemoveAt(i);
                GUIUtility.ExitGUI(); // Prevent GUI errors when modifying list
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Clear List"))
        {
            selectedScripts.Clear();
        }
    }

    private static void AddNamespaceToSelectedScripts(string newNamespace, List<MonoScript> selectedScripts)
    {
        foreach (var script in selectedScripts)
        {
            if (script != null)
            {
                string scriptPath = AssetDatabase.GetAssetPath(script);
                string scriptContent = File.ReadAllText(scriptPath);

                scriptContent = RemoveExistingNamespace(scriptContent);
                scriptContent = AddNewNamespace(scriptContent, newNamespace);

                File.WriteAllText(scriptPath, scriptContent);
                //Debug.Log($"Updated namespace in: {scriptPath}");
            }
        }

        AssetDatabase.Refresh();
    }

    private static string RemoveExistingNamespace(string scriptContent)
    {
        string pattern = @"\s*namespace\s+\w+\s*\{";
        Regex regex = new Regex(pattern, RegexOptions.Singleline);

        if (regex.IsMatch(scriptContent))
        {
            scriptContent = regex.Replace(scriptContent, "");
            scriptContent = scriptContent.Replace("}", "");
        }

        return scriptContent;
    }

    private static string AddNewNamespace(string scriptContent, string newNamespace)
    {
        int insertIndex = scriptContent.LastIndexOf("using");
        if (insertIndex == -1)
        {
            insertIndex = 0;
        }
        else
        {
            insertIndex = scriptContent.IndexOf("\n", insertIndex) + 1;
        }

        return $"namespace {newNamespace} \n{{\n{scriptContent}\n}}";
    }
}
#endif