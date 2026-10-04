using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Node : MonoBehaviour
{
    public int nodeIndex;
    [SerializeField] Node NextNode;
    private Color lineColor;

    void Start()
    {
        // Generate a random color at the start
        lineColor = new Color(Random.value, Random.value, Random.value);
    }

    public Node GetNextNode()
    {
        return NextNode;
    }

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // Set the label color and draw the label
        GUIStyle style = new GUIStyle();
        style.fontSize = 16; // Increase font size
        style.normal.textColor = Color.red; // Set text color

        Handles.Label(transform.position, transform.name, style);

        // Check if there is a next node
        if (NextNode != null)
        {
            // Use the stored random color
            Handles.color = lineColor;

            // Draw a thicker line from this node to the next node
            Handles.DrawAAPolyLine(20f, new Vector3[] { transform.position, NextNode.transform.position });
        }
#endif
    }
}
