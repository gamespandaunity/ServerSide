using System.Collections;
using UnityEngine;

public class eachHolesForce : MonoBehaviour
{
    private Vector3 thisPosition;
    private int thisTriggerNumber;
    private Rigidbody targetRigidbody;
   public mainScript _mainScript;
    private void Start()
    {
        thisPosition = transform.position;
        thisTriggerNumber = int.Parse(gameObject.name.Replace("Capsule", string.Empty));
        if(_mainScript == null)
            _mainScript = FindAnyObjectByType<mainScript>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (!collision.CompareTag("ballTag") && collision.name != "cueBall")
            return;

        targetRigidbody = collision.GetComponent<Rigidbody>();
        targetRigidbody.constraints &= (RigidbodyConstraints)(-5);

        Vector3 pocketCenter = thisPosition;
        pocketCenter.y = _mainScript.CUEBALL_START_SNOOKER_POS.y; // Fix syntax error

        float velocityThreshold = 6f / 18f;
        float minForce = 2.5f / 18f;
        float maxForce = 3.1f / 18f;

        // Add threshold for "too fast" balls that might get stuck
        float maxSafeVelocity = 12f / 18f; // Adjust this value based on testing
        float currentVelocity = targetRigidbody.linearVelocity.magnitude;

        Vector3 directionToPocket = (pocketCenter - targetRigidbody.position).normalized;


        // Check if ball is moving too fast (likely to get stuck)
        if (currentVelocity > maxSafeVelocity)
        {
            // Reduce velocity to prevent getting stuck on pocket edges
            targetRigidbody.linearVelocity = targetRigidbody.linearVelocity.normalized * maxSafeVelocity;

            // Apply a stronger downward/inward force to help the ball fall into the pocket
            Vector3 pullForce = directionToPocket * maxForce * 2f; // Stronger pull
            pullForce.y = -0.5f; // Add downward component
            targetRigidbody.AddForce(pullForce, ForceMode.Impulse);

        }
        else if (thisTriggerNumber == 1 || thisTriggerNumber == 4)
        {
            if (currentVelocity < velocityThreshold)
            {
                float forceAmount = Mathf.Clamp(currentVelocity, minForce, maxForce);
                targetRigidbody.AddForce(directionToPocket * forceAmount, ForceMode.Impulse);
            }
            else
            {
                targetRigidbody.linearVelocity = directionToPocket * currentVelocity;
            }
        }
        else if (currentVelocity < velocityThreshold)
        {
            float forceAmount = Mathf.Clamp(currentVelocity, minForce, maxForce);
            targetRigidbody.AddForce(directionToPocket * forceAmount, ForceMode.Impulse);
        }

        // Check if ball might be stuck and needs help
        StartCoroutine(CheckIfStuck(collision.gameObject));

        Invoke("ChangeTurnHardly", 2f);
    }

    // Coroutine to check if ball is stuck on pocket edge
    private IEnumerator CheckIfStuck(GameObject ball)
    {
        yield return new WaitForSeconds(0.5f); // Wait a bit

        if (ball != null && targetRigidbody != null)
        {
            float velocity = targetRigidbody.linearVelocity.magnitude;

            // If ball is still in trigger area but barely moving, it might be stuck
            if (velocity < 0.1f && Vector3.Distance(ball.transform.position, thisPosition) < 0.3f)
            {

                // Apply a corrective force to unstick the ball
                Vector3 unstickForce = (thisPosition - ball.transform.position).normalized;
                unstickForce.y = -0.3f; // Downward component to fall into pocket
                targetRigidbody.AddForce(unstickForce * 0.5f, ForceMode.Impulse);

                // Optionally, slightly move the ball toward pocket center
                ball.transform.position = Vector3.Lerp(ball.transform.position, thisPosition, 0.1f);
            }
        }
    }

    public void ChangeTurnHardly()
    {
        if (targetRigidbody != null && targetRigidbody.constraints == RigidbodyConstraints.FreezePositionY)
        {
            Vector3 newPosition = thisPosition;
            newPosition.y = _mainScript.CUEBALL_START_SNOOKER_POS.y - 0.1f; // Fix syntax error
            targetRigidbody.transform.position = newPosition;
        }
    }

    // Alternative: OnTriggerStay to continuously handle stuck balls
    private void OnTriggerStay(Collider collision)
    {
        if (!collision.CompareTag("ballTag") && collision.name != "cueBall")
            return;

        Rigidbody rb = collision.GetComponent<Rigidbody>();

        // If ball is in pocket area but moving very slowly (stuck)
        if (rb != null && rb.linearVelocity.magnitude < 0.05f)
        {
            // Gently pull toward pocket center
            Vector3 pullDirection = (thisPosition - rb.position).normalized;
            pullDirection.y = -0.2f; // Add downward component
            rb.AddForce(pullDirection * 0.1f, ForceMode.Force);
        }
    }
}
