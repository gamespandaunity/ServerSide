#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Animator), true)]
[CanEditMultipleObjects]
public class AnimatorInspectorWithButtons : Editor
{
    private Editor defaultEditor;

    void OnEnable()
    {
        // Load Unity's default Animator inspector
        defaultEditor = CreateEditor(targets, System.Type.GetType("UnityEditor.AnimatorInspector, UnityEditor"));
    }

    void OnDisable()
    {
        if (defaultEditor != null)
        {
            DestroyImmediate(defaultEditor);
        }
    }

    public override void OnInspectorGUI()
    {
        // Draw Unity's default Animator inspector
        if (defaultEditor != null)
        {
            defaultEditor.OnInspectorGUI();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Open Animator Window", GUILayout.Height(25)))
        {
            OpenAnimatorWindow();
        }

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Open Animation Window", GUILayout.Height(25)))
        {
            OpenAnimationWindow();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

    }

    private void OpenAnimatorWindow()
    {
        // Opens Animator tab reliably
        EditorApplication.ExecuteMenuItem("Window/Animation/Animator");

        // Focus selected controller (if any)
        if (Selection.activeGameObject != null)
        {
            Animator animator = Selection.activeGameObject.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                Selection.activeObject = animator.runtimeAnimatorController;
                Debug.Log($"Opened Animator window for: {animator.name}");
            }
            else
            {
                Debug.LogWarning("Selected GameObject doesn't have a valid Animator Controller.");
            }
        }
    }

    private void OpenAnimationWindow()
    {
        // Opens Animation tab reliably
        EditorApplication.ExecuteMenuItem("Window/Animation/Animation");

        if (targets.Length == 1)
        {
            Animator animator = (Animator)target;
            Selection.activeGameObject = animator.gameObject;
            Debug.Log($"Opened Animation window for: {animator.name}");
        }
        else
        {
            Debug.Log("Opened Animation window");
        }
    }
}
#endif