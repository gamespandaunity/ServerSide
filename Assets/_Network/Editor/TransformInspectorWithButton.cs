using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(Transform), true)]
[CanEditMultipleObjects]
public class TransformInspectorWithButton : Editor
{
    private Editor defaultEditor;

    // Store transform values instead of reference
    private static Vector3 copiedPosition;
    private static Quaternion copiedRotation;
    private static Vector3 copiedScale;
    private static string copiedObjectName;
    private static bool hasValidCopy = false;

    void OnEnable()
    {
        // Get Unity's built-in transform inspector
        System.Type transformType = typeof(Transform);
        defaultEditor = CreateEditor(targets, System.Type.GetType("UnityEditor.TransformInspector, UnityEditor"));
    }

    void OnDisable()
    {
        // Properly dispose of the default editor
        if (defaultEditor != null)
        {
            DestroyImmediate(defaultEditor);
        }
    }

    public override void OnInspectorGUI()
    {
        // Draw the default transform inspector
        if (defaultEditor != null)
        {
            defaultEditor.OnInspectorGUI();
        }

        EditorGUILayout.Space();

        // Custom Copy/Paste Component buttons
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Copy Component", GUILayout.Height(25)))
        {
            HandleCopyComponent();
        }

        GUI.backgroundColor = hasValidCopy ? Color.green : Color.gray;
        GUI.enabled = hasValidCopy;
        if (GUILayout.Button("Paste Component", GUILayout.Height(25)))
        {
            HandlePasteComponent();
        }
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        if (hasValidCopy)
        {
            EditorGUILayout.HelpBox($"Copied values from: {copiedObjectName}", MessageType.Info);
        }
    }

    private void HandleCopyComponent()
    {
        if (targets.Length == 1)
        {
            Transform selectedTransform = (Transform)target;

            // Store the actual values instead of the reference
            copiedPosition = selectedTransform.position;
            copiedRotation = selectedTransform.rotation;
            copiedScale = selectedTransform.localScale;
            copiedObjectName = selectedTransform.name;
            hasValidCopy = true;

            Debug.Log($"Copied Transform component from: {selectedTransform.name}");
            Debug.Log($"Position: {copiedPosition}");
            Debug.Log($"Rotation: {copiedRotation.eulerAngles}");
            Debug.Log($"Scale: {copiedScale}");
        }
        else
        {
            Debug.LogWarning("Can only copy from one Transform at a time. Please select a single object.");
        }
    }

    private void HandlePasteComponent()
    {
        if (!hasValidCopy)
        {
            Debug.LogWarning("No Transform component copied yet!");
            return;
        }

        Undo.RecordObjects(targets, "Paste Transform Component");

        foreach (Object targetObj in targets)
        {
            Transform targetTransform = (Transform)targetObj;

            // Apply the stored values
            targetTransform.position = copiedPosition;
            targetTransform.rotation = copiedRotation;
            targetTransform.localScale = copiedScale;

            // Mark the object as dirty for saving
            EditorUtility.SetDirty(targetTransform);
        }

        Debug.Log($"Pasted Transform component to {targets.Length} object(s)");
    }
}