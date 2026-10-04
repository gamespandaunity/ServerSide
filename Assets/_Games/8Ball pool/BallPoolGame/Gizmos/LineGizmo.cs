using UnityEngine;

public class LineGizmo : MonoBehaviour
{
	void OnDrawGizmosSelected ()
	{
		Gizmos.color = Color.blue;
		RenderGizmosQuad(transform);
	}
	public static void RenderGizmosQuad (Transform quad)
	{
		Vector3 pos = quad.position;
		
        Vector3 localPos1 = 0.5f * quad.lossyScale.x * quad.right;
        Vector3 localPos2 = -0.5f * quad.lossyScale.x * quad.right;
		
		Gizmos.DrawLine (pos + localPos1, pos + localPos2);

		Gizmos.DrawLine(quad.position, quad.position - quad.lossyScale.z * quad.forward);
	}
}
