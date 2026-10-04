using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Linq;

// Only applies to classes that explicitly implement IHasStaticInspector
[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class OptimizedStaticVariableEditor : Editor
{
    // Cache for reflection data - persists across inspector refreshes
    private static Dictionary<Type, CachedTypeData> typeCache = new Dictionary<Type, CachedTypeData>();
    
    private class CachedTypeData
    {
        public List<CachedFieldData> Fields = new List<CachedFieldData>();
        public bool HasStaticFields = false;
        public long LastModified = 0;
    }
    
    private class CachedFieldData
    {
        public FieldInfo Field;
        public bool IsReadOnly;
        public string DisplayName;
        public Type FieldType;
    }
    
    private CachedTypeData cachedData;
    private bool shouldShowStaticFields;
    
    void OnEnable()
    {
        Type targetType = target.GetType();
        
        // Quick check - only process if implements IHasStaticInspector
        shouldShowStaticFields = typeof(IHasStaticInspector).IsAssignableFrom(targetType);
        
        if (!shouldShowStaticFields) 
            return;
        
        // Use cached data if available and still valid
        if (!typeCache.TryGetValue(targetType, out cachedData) || IsCacheInvalid(targetType))
        {
            cachedData = BuildCache(targetType);
            typeCache[targetType] = cachedData;
        }
    }
    
    private bool IsCacheInvalid(Type type)
    {
        // Invalidate cache if script has been modified (optional, for development)
        #if UNITY_EDITOR
        var script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
        if (script != null)
        {
            string path = AssetDatabase.GetAssetPath(script);
            long lastModified = System.IO.File.GetLastWriteTime(path).Ticks;
            return cachedData.LastModified != lastModified;
        }
        #endif
        return false;
    }
    
    private CachedTypeData BuildCache(Type targetType)
    {
        var data = new CachedTypeData();
        
        // Only get static fields, not all fields
        FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<ShowStaticInInspectorAttribute>();
            if (attribute != null)
            {
                var fieldData = new CachedFieldData
                {
                    Field = field,
                    IsReadOnly = attribute.ReadOnly,
                    DisplayName = ObjectNames.NicifyVariableName(field.Name),
                    FieldType = field.FieldType
                };
                data.Fields.Add(fieldData);
            }
        }
        
        data.HasStaticFields = data.Fields.Count > 0;
        
        #if UNITY_EDITOR
        var script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
        if (script != null)
        {
            string path = AssetDatabase.GetAssetPath(script);
            data.LastModified = System.IO.File.GetLastWriteTime(path).Ticks;
        }
        #endif
        
        return data;
    }
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        // Skip if this class doesn't implement IHasStaticInspector
        if (!shouldShowStaticFields || cachedData == null || !cachedData.HasStaticFields)
            return;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Static Variables", EditorStyles.boldLabel);
        
        EditorGUI.indentLevel++;
        
        foreach (var fieldData in cachedData.Fields)
        {
            DrawStaticField(fieldData);
        }
        
        EditorGUI.indentLevel--;
    }
    
    private void DrawStaticField(CachedFieldData fieldData)
    {
        EditorGUI.BeginChangeCheck();
        
        object currentValue = fieldData.Field.GetValue(null);
        object newValue = currentValue;
        
        using (new EditorGUI.DisabledScope(fieldData.IsReadOnly))
        {
            // Use a switch expression for better performance (C# 8+)
            newValue = fieldData.FieldType switch
            {
                Type t when t == typeof(int) => EditorGUILayout.IntField(fieldData.DisplayName, (int)currentValue),
                Type t when t == typeof(float) => EditorGUILayout.FloatField(fieldData.DisplayName, (float)currentValue),
                Type t when t == typeof(double) => EditorGUILayout.DoubleField(fieldData.DisplayName, (double)currentValue),
                Type t when t == typeof(string) => EditorGUILayout.TextField(fieldData.DisplayName, (string)currentValue),
                Type t when t == typeof(bool) => EditorGUILayout.Toggle(fieldData.DisplayName, (bool)currentValue),
                Type t when t == typeof(Vector2) => EditorGUILayout.Vector2Field(fieldData.DisplayName, (Vector2)currentValue),
                Type t when t == typeof(Vector3) => EditorGUILayout.Vector3Field(fieldData.DisplayName, (Vector3)currentValue),
                Type t when t == typeof(Vector4) => EditorGUILayout.Vector4Field(fieldData.DisplayName, (Vector4)currentValue),
                Type t when t == typeof(Color) => EditorGUILayout.ColorField(fieldData.DisplayName, (Color)currentValue),
                Type t when t == typeof(AnimationCurve) => EditorGUILayout.CurveField(fieldData.DisplayName, (AnimationCurve)currentValue),
                Type t when t == typeof(Rect) => EditorGUILayout.RectField(fieldData.DisplayName, (Rect)currentValue),
                Type t when t == typeof(Bounds) => EditorGUILayout.BoundsField(fieldData.DisplayName, (Bounds)currentValue),
                Type t when t.IsEnum => EditorGUILayout.EnumPopup(fieldData.DisplayName, (Enum)currentValue),
                Type t when typeof(UnityEngine.Object).IsAssignableFrom(t) => 
                    EditorGUILayout.ObjectField(fieldData.DisplayName, (UnityEngine.Object)currentValue, fieldData.FieldType, true),
                _ => currentValue // Return unchanged for unsupported types
            };
            
            // For unsupported types, show as label
            if (newValue == currentValue && !IsSupportedType(fieldData.FieldType))
            {
                EditorGUILayout.LabelField(fieldData.DisplayName, currentValue?.ToString() ?? "null");
            }
        }
        
        if (EditorGUI.EndChangeCheck() && !fieldData.IsReadOnly)
        {
            Undo.RecordObject(target, $"Change Static {fieldData.Field.Name}");
            fieldData.Field.SetValue(null, newValue);
            EditorUtility.SetDirty(target);
        }
    }
    
    private bool IsSupportedType(Type type)
    {
        return type == typeof(int) || type == typeof(float) || type == typeof(double) ||
               type == typeof(string) || type == typeof(bool) || type == typeof(Vector2) ||
               type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(Color) ||
               type == typeof(AnimationCurve) || type == typeof(Rect) || type == typeof(Bounds) ||
               type.IsEnum || typeof(UnityEngine.Object).IsAssignableFrom(type);
    }
    
    // Clear cache when scripts recompile
    [InitializeOnLoadMethod]
    static void ClearCacheOnRecompile()
    {
        typeCache.Clear();
    }
}