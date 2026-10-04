using UnityEngine;

public class BoardCollider : MonoBehaviour 
{
    void Awake()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        Mesh newMesh = new Mesh();
        newMesh.vertices = mesh.vertices;
        newMesh.triangles = mesh.triangles;
        Vector3[] normals = new Vector3[mesh.normals.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.ProjectOnPlane(mesh.normals[i], Vector3.up).normalized;
        }
        newMesh.normals = normals;
        newMesh.RecalculateNormals();
        GetComponent<MeshCollider>().sharedMesh = newMesh;
    }
}
