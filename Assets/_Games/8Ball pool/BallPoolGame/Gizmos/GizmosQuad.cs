using UnityEngine;

public class GizmosQuad : MonoBehaviour 
{
	void OnDrawGizmosSelected ()
	{
		Gizmos.color = Color.green;
        Vector3 lossyScale = transform.lossyScale;
        Gizmos.DrawCube(transform.position, new Vector3(lossyScale.x, 0.0f, lossyScale.y));
		//DrawGizmosQuad(transform);
	}
	public static void DrawGizmosQuad (Transform quad)
	{
		Vector3 pos = quad.position;
		
		Vector3 localPos1 = 0.5f*quad.lossyScale.x * quad.right + 0.5f*quad.lossyScale.y*quad.up;
		Vector3 localPos2 = -0.5f*quad.lossyScale.x * quad.right + 0.5f*quad.lossyScale.y*quad.up;
		Vector3 localPos3 = -0.5f*quad.lossyScale.x * quad.right - 0.5f*quad.lossyScale.y*quad.up;
		Vector3 localPos4 = 0.5f*quad.lossyScale.x * quad.right - 0.5f*quad.lossyScale.y*quad.up;

        Gizmos.DrawLine (pos + localPos1, pos + localPos2);
		Gizmos.DrawLine (pos + localPos2, pos + localPos3);
		Gizmos.DrawLine (pos + localPos3, pos + localPos4);
		Gizmos.DrawLine (pos + localPos4, pos + localPos1);

		Gizmos.DrawLine(quad.position, quad.position - quad.lossyScale.z * quad.forward);
	}
}
