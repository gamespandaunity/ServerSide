
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ToolbarWindow : EditorWindow
{
    private bool isButton1Pressed = false;
    private bool isButton2Pressed = false;

    private float variable1 = 0f; // Example Variable 1
    private string variable2 = "Default"; // Example Variable 2

    [MenuItem("Tools/Custom Toolbar")]
    public static void ShowWindow()
    {
        ToolbarWindow window = GetWindow<ToolbarWindow>("Custom Toolbar");
        window.minSize = new Vector2(50, 200); // Minimum size of the toolbar
        window.maxSize = new Vector2(600, 600); // Maximum width of the toolbar
    }

    private void OnGUI()
    {
        // Make the toolbar vertically oriented
        GUILayout.BeginVertical();

        // Button 1
        if (GUILayout.Button(new GUIContent("Get Values", "Coins Value"), GUILayout.Height(50)))
        {
            isButton1Pressed = true;
            //Debug.Log("Action 1 triggered!");
            variable1 = EditorGUILayout.FloatField(float.Parse(ApiAndRoomManager.LastFetchedCoins.data.silver_balance));
        }

        // Button 2
        //if (GUILayout.Button(new GUIContent("🔄", "Action 2"), GUILayout.Height(50)))
        //{
        //    isButton2Pressed = true;
        //    //Debug.Log("Action 2 triggered!");
        //}

        //// Button 3
        //if (GUILayout.Button(new GUIContent("📏", "Action 3"), GUILayout.Height(50)))
        //{
        //    //Debug.Log("Action 3 triggered!");
        //}

        GUILayout.Space(10); // Add space between buttons

        // Display the two example variables
        GUILayout.Label("Silver", EditorStyles.boldLabel);
        variable1 = EditorGUILayout.FloatField(float.Parse(ApiAndRoomManager.LastFetchedCoins.data.silver_balance));

        GUILayout.Label("Variable 2", EditorStyles.boldLabel);
        variable2 = EditorGUILayout.TextField(variable2);

        GUILayout.EndVertical();
    }
}
#endif