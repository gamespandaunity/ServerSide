using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace UnityEditor
{
    public class StaticVariableViewer : EditorWindow
    {
        private Vector2 scrollPosition;
        private Dictionary<string, ScriptInfo> staticVariablesByScript = new Dictionary<string, ScriptInfo>();
        private bool hasData = false;
        private string currentScenePath = "";
        private Dictionary<string, double> lastClickTime = new Dictionary<string, double>();
        private Dictionary<string, bool> scriptFoldouts = new Dictionary<string, bool>();

        [MenuItem("Nasmo Studio/Static Variable Viewer")]
        public static void ShowWindow()
        {
            GetWindow<StaticVariableViewer>("Static Variable Viewer");
        }

        private void OnEnable()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            ClearData();
        }

        private void OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            ClearData();
        }

        private void ClearData()
        {
            staticVariablesByScript.Clear();
            scriptFoldouts.Clear();
            hasData = false;
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Show Static Variables", GUILayout.Width(200), GUILayout.Height(30)))
            {
                SearchStaticVariables();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (hasData && staticVariablesByScript.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Expand All", GUILayout.Width(95)))
                {
                    ExpandCollapseAll(true);
                }

                if (GUILayout.Button("Collapse All", GUILayout.Width(95)))
                {
                    ExpandCollapseAll(false);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            if (hasData)
            {
                DrawStaticVariables();
            }
            else if (staticVariablesByScript.Count == 0 && hasData)
            {
                EditorGUILayout.HelpBox("No static variables found in the current scene.", MessageType.Info);
            }
        }

        private void ExpandCollapseAll(bool expand)
        {
            List<string> keys = new List<string>(scriptFoldouts.Keys);
            foreach (string key in keys)
            {
                scriptFoldouts[key] = expand;
            }
            Repaint();
        }

        private void SearchStaticVariables()
        {
            staticVariablesByScript.Clear();
            hasData = false;

            // Get all GameObjects in the scene
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            Dictionary<Type, GameObject> typeToGameObject = new Dictionary<Type, GameObject>();

            foreach (GameObject obj in allObjects)
            {
                // Get all MonoBehaviour components
                MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();

                foreach (MonoBehaviour component in components)
                {
                    if (component == null) continue;

                    Type componentType = component.GetType();

                    // Store the first GameObject we find with this type
                    if (!typeToGameObject.ContainsKey(componentType))
                    {
                        typeToGameObject[componentType] = obj;
                    }
                }
            }

            // Process each unique type
            foreach (var kvp in typeToGameObject)
            {
                Type componentType = kvp.Key;
                GameObject gameObject = kvp.Value;

                // Get all static fields and properties
                List<StaticVariableInfo> variables = GetStaticVariables(componentType);

                if (variables.Count > 0)
                {
                    staticVariablesByScript[componentType.Name] = new ScriptInfo
                    {
                        type = componentType,
                        gameObject = gameObject,
                        variables = variables
                    };

                    // Initialize foldout state (expanded by default)
                    if (!scriptFoldouts.ContainsKey(componentType.Name))
                    {
                        scriptFoldouts[componentType.Name] = true;
                    }
                }
            }

            hasData = true;
            Repaint();
        }

        private List<StaticVariableInfo> GetStaticVariables(Type type)
        {
            List<StaticVariableInfo> variables = new List<StaticVariableInfo>();

            // Get static fields
            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                // Skip compiler-generated fields
                if (field.Name.Contains("<") || field.Name.Contains(">"))
                    continue;

                try
                {
                    object value = field.GetValue(null);
                    variables.Add(new StaticVariableInfo
                    {
                        name = field.Name,
                        value = value,
                        valueType = field.FieldType,
                        isPublic = field.IsPublic,
                        isField = true,
                        fieldInfo = field,
                        propertyInfo = null,
                        isReadOnly = field.IsInitOnly
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error reading field {field.Name}: {e.Message}");
                }
            }

            // Get static properties
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (PropertyInfo property in properties)
            {
                // Skip properties without getter
                if (!property.CanRead)
                    continue;

                try
                {
                    object value = property.GetValue(null);
                    variables.Add(new StaticVariableInfo
                    {
                        name = property.Name,
                        value = value,
                        valueType = property.PropertyType,
                        isPublic = property.GetGetMethod(true).IsPublic,
                        isField = false,
                        fieldInfo = null,
                        propertyInfo = property,
                        isReadOnly = !property.CanWrite
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error reading property {property.Name}: {e.Message}");
                }
            }

            return variables;
        }

        private void DrawStaticVariables()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (staticVariablesByScript.Count == 0)
            {
                EditorGUILayout.HelpBox("No static variables found in the current scene.", MessageType.Info);
            }
            else
            {
                foreach (var kvp in staticVariablesByScript.OrderBy(x => x.Key))
                {
                    DrawScriptSection(kvp.Key, kvp.Value);
                    EditorGUILayout.Space(10);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawScriptSection(string scriptName, ScriptInfo scriptInfo)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(25));

            // Foldout arrow
            bool isExpanded = scriptFoldouts.ContainsKey(scriptName) ? scriptFoldouts[scriptName] : true;

            // Custom foldout style to match the original header look
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
            foldoutStyle.fontSize = 14;
            foldoutStyle.margin = new RectOffset(0, 0, 4, 0);

            // Set all text colors to the blue header color
            Color headerColor = new Color(0.3f, 0.7f, 1f);
            foldoutStyle.normal.textColor = headerColor;
            foldoutStyle.onNormal.textColor = headerColor;
            foldoutStyle.hover.textColor = new Color(0.4f, 0.8f, 1f);
            foldoutStyle.onHover.textColor = new Color(0.4f, 0.8f, 1f);
            foldoutStyle.focused.textColor = headerColor;
            foldoutStyle.onFocused.textColor = headerColor;
            foldoutStyle.active.textColor = headerColor;
            foldoutStyle.onActive.textColor = headerColor;

            scriptFoldouts[scriptName] = EditorGUILayout.Foldout(isExpanded, scriptName, true, foldoutStyle);

            GUILayout.FlexibleSpace();

            // Navigate button
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;

            if (GUILayout.Button("→", buttonStyle, GUILayout.Width(30), GUILayout.Height(22)))
            {
                NavigateToGameObject(scriptInfo.gameObject);
            }

            // Open script button
            if (GUILayout.Button("📄", buttonStyle, GUILayout.Width(30), GUILayout.Height(22)))
            {
                OpenScript(scriptInfo.type);
            }

            EditorGUILayout.EndHorizontal();

            // Only draw variables if expanded
            if (scriptFoldouts[scriptName])
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                foreach (var variable in scriptInfo.variables.OrderBy(v => v.name))
                {
                    DrawVariable(variable, scriptInfo.type);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void NavigateToGameObject(GameObject gameObject)
        {
            if (gameObject != null)
            {
                Selection.activeGameObject = gameObject;
                EditorGUIUtility.PingObject(gameObject);
                SceneView.FrameLastActiveSceneView();
            }
            else
            {
                Debug.LogWarning("GameObject reference is null.");
            }
        }

        private void OpenScript(Type type)
        {
            // Find the MonoScript asset for this type
            MonoScript[] scripts = Resources.FindObjectsOfTypeAll<MonoScript>();
            MonoScript targetScript = scripts.FirstOrDefault(s => s.GetClass() == type);

            if (targetScript != null)
            {
                AssetDatabase.OpenAsset(targetScript);
            }
            else
            {
                Debug.LogWarning($"Could not find script for type: {type.Name}");
            }
        }

        private void OpenScriptAtLine(Type type, string memberName, bool isField)
        {
            // Find the MonoScript asset for this type
            MonoScript[] scripts = Resources.FindObjectsOfTypeAll<MonoScript>();
            MonoScript targetScript = scripts.FirstOrDefault(s => s.GetClass() == type);

            if (targetScript != null)
            {
                // Get the script path
                string scriptPath = AssetDatabase.GetAssetPath(targetScript);

                // Try to find the line number
                int lineNumber = FindMemberLineNumber(scriptPath, memberName, isField);

                if (lineNumber > 0)
                {
                    // Open at specific line
                    AssetDatabase.OpenAsset(targetScript, lineNumber);
                }
                else
                {
                    // Fallback: just open the script
                    AssetDatabase.OpenAsset(targetScript);
                }
            }
            else
            {
                Debug.LogWarning($"Could not find script for type: {type.Name}");
            }
        }

        private int FindMemberLineNumber(string scriptPath, string memberName, bool isField)
        {
            try
            {
                string[] lines = System.IO.File.ReadAllLines(scriptPath);
                string searchPattern = isField ?
                    $"static.*{memberName}" :
                    $"static.*{memberName}.*{{";

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    // Simple pattern matching for static field/property
                    if (line.Contains("static") && line.Contains(memberName))
                    {
                        // Make sure it's not in a comment
                        if (!line.TrimStart().StartsWith("//"))
                        {
                            return i + 1; // Line numbers are 1-based
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading script file: {e.Message}");
            }

            return -1;
        }

        private void DrawVariable(StaticVariableInfo variable, Type scriptType)
        {
            EditorGUILayout.BeginHorizontal();

            // Variable name with access modifier color (clickable)
            GUIStyle nameStyle = new GUIStyle(EditorStyles.label);
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.normal.textColor = variable.isPublic ? Color.green : Color.yellow;
            nameStyle.hover.textColor = Color.cyan;

            string accessModifier = variable.isPublic ? "public" : "private";
            string readOnlyText = variable.isReadOnly ? " (readonly)" : "";
            string labelText = $"{accessModifier} {variable.name}{readOnlyText}";

            Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(labelText), nameStyle, GUILayout.Width(250));

            // Draw clickable label
            if (GUI.Button(labelRect, labelText, nameStyle))
            {
                string key = $"{scriptType.Name}_{variable.name}";
                double currentTime = EditorApplication.timeSinceStartup;

                if (lastClickTime.ContainsKey(key) && (currentTime - lastClickTime[key]) < 0.3)
                {
                    // Double click detected
                    OpenScriptAtLine(scriptType, variable.name, variable.isField);
                    lastClickTime[key] = 0; // Reset
                }
                else
                {
                    // First click
                    lastClickTime[key] = currentTime;
                }
            }

            // Change cursor on hover
            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);

            // Type
            EditorGUILayout.LabelField($"({variable.valueType.Name})", EditorStyles.miniLabel, GUILayout.Width(100));

            // Value field (editable or read-only based on type)
            EditorGUI.BeginDisabledGroup(variable.isReadOnly);
            DrawEditableField(variable);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEditableField(StaticVariableInfo variable)
        {
            Type type = variable.valueType;
            object currentValue = variable.value;

            try
            {
                object newValue = currentValue;

                // Handle different types with appropriate input fields
                if (type == typeof(int))
                {
                    newValue = EditorGUILayout.IntField(currentValue != null ? (int)currentValue : 0);
                }
                else if (type == typeof(float))
                {
                    newValue = EditorGUILayout.FloatField(currentValue != null ? (float)currentValue : 0f);
                }
                else if (type == typeof(double))
                {
                    newValue = EditorGUILayout.DoubleField(currentValue != null ? (double)currentValue : 0.0);
                }
                else if (type == typeof(long))
                {
                    newValue = EditorGUILayout.LongField(currentValue != null ? (long)currentValue : 0L);
                }
                else if (type == typeof(string))
                {
                    newValue = EditorGUILayout.TextField(currentValue != null ? (string)currentValue : "");
                }
                else if (type == typeof(bool))
                {
                    newValue = EditorGUILayout.Toggle(currentValue != null && (bool)currentValue);
                }
                else if (type.IsEnum)
                {
                    if (currentValue != null)
                    {
                        newValue = EditorGUILayout.EnumPopup((Enum)currentValue);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("null");
                    }
                }
                else if (type == typeof(Vector2))
                {
                    newValue = EditorGUILayout.Vector2Field("", currentValue != null ? (Vector2)currentValue : Vector2.zero);
                }
                else if (type == typeof(Vector3))
                {
                    newValue = EditorGUILayout.Vector3Field("", currentValue != null ? (Vector3)currentValue : Vector3.zero);
                }
                else if (type == typeof(Vector4))
                {
                    newValue = EditorGUILayout.Vector4Field("", currentValue != null ? (Vector4)currentValue : Vector4.zero);
                }
                else if (type == typeof(Color))
                {
                    newValue = EditorGUILayout.ColorField(currentValue != null ? (Color)currentValue : Color.white);
                }
                else if (type == typeof(Rect))
                {
                    newValue = EditorGUILayout.RectField(currentValue != null ? (Rect)currentValue : new Rect());
                }
                else if (type == typeof(Bounds))
                {
                    newValue = EditorGUILayout.BoundsField(currentValue != null ? (Bounds)currentValue : new Bounds());
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    newValue = EditorGUILayout.ObjectField(currentValue as UnityEngine.Object, type, true);
                }
                else
                {
                    // For complex types, show as read-only text field
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(FormatComplexValue(currentValue));
                    EditorGUI.EndDisabledGroup();
                }

                // If value changed and not readonly, update the static variable
                if (!variable.isReadOnly && !Equals(newValue, currentValue))
                {
                    SetStaticValue(variable, newValue);
                    variable.value = newValue;
                }
            }
            catch (Exception e)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField($"Error: {e.Message}");
                EditorGUI.EndDisabledGroup();
            }
        }

        private void SetStaticValue(StaticVariableInfo variable, object newValue)
        {
            try
            {
                if (variable.isField && variable.fieldInfo != null)
                {
                    variable.fieldInfo.SetValue(null, newValue);
                }
                else if (!variable.isField && variable.propertyInfo != null && variable.propertyInfo.CanWrite)
                {
                    variable.propertyInfo.SetValue(null, newValue);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to set value for {variable.name}: {e.Message}");
            }
        }

        private string FormatComplexValue(object value)
        {
            if (value == null)
                return "null";

            if (value is UnityEngine.Object unityObj)
            {
                if (unityObj == null)
                    return "null (destroyed)";
                return unityObj.name;
            }

            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var items = enumerable.Cast<object>().Take(5).Select(x => FormatComplexValue(x));
                string preview = string.Join(", ", items);
                return $"[{preview}...]";
            }

            return value.ToString();
        }

        private class ScriptInfo
        {
            public Type type;
            public GameObject gameObject;
            public List<StaticVariableInfo> variables;
        }

        private class StaticVariableInfo
        {
            public string name;
            public object value;
            public Type valueType;
            public bool isPublic;
            public bool isField;
            public FieldInfo fieldInfo;
            public PropertyInfo propertyInfo;
            public bool isReadOnly;
        }
    }
}