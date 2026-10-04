using UnityEngine;

public class GizmosNode : MonoBehaviour 
{
	void OnDrawGizmosSelected ()
	{
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f * transform.lossyScale.x);
	}
}
