using UnityEngine;

public class CircleGizmo : MonoBehaviour
{
    public int nodeCounts = 12;
    void OnDrawGizmosSelected ()
    {
        Gizmos.color = Color.cyan;
        float radius = 0.5f * transform.lossyScale.x;
        Vector3 position = transform.position;
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        float step = 2.0f * Mathf.PI / (float)nodeCounts;
        float max = 2.0f * Mathf.PI;

        for (float f = 0.0f; f < max; f += step)
        {
            float x1 = radius * Mathf.Sin(f);
            float z1 = radius * Mathf.Cos(f);
            float x2 = radius * Mathf.Sin(f + step);
            float z2 = radius * Mathf.Cos(f + step);
            Vector3 position1 = position + x1 * right + z1 * forward;
            Vector3 position2 = position + x2 * right + z2 * forward;
            Gizmos.DrawLine(position1, position2);
        }
    }
}
