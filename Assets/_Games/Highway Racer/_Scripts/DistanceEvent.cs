using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class DistanceEvent : MonoBehaviour
{
    public Transform target;
    public Transform secondaryTarget;
    public float triggerDistance = 5f;
    public UnityEvent onDistanceReached;
    public TMP_Text distanceText;

    private void Update()
    {
        if (target == null || secondaryTarget == null) return;

        float distanceToTarget = Vector3.Distance(secondaryTarget.position, target.position);

        if (distanceText != null)
        {
            distanceText.text = "Distance to Target: " + distanceToTarget.ToString("F2");
        }

        if (distanceToTarget >= triggerDistance)
        {
            onDistanceReached.Invoke();
            enabled = false; // Disable after triggering once, optional
        }
    }
    public void SetTarget(Transform tranform)
    {

        "Set Target".Show();
        target = tranform;


    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, triggerDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, target.position);

        if (secondaryTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(secondaryTarget.position, target.position);
        }
    }
}