using UnityEngine;

public class SphereGizmo : MonoBehaviour 
{
	void OnDrawGizmosSelected ()
	{
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.5f * transform.lossyScale.x);
	}
}
