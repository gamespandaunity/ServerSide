using UnityEngine;

public class SlowDownTrigger : MonoBehaviour
{
    public float slowDownFactor = 0.5f; // Factor to slow down the speed (0.5 means half the speed)

    private void OnTriggerEnter(Collider other)
    {
        // Check if the other object has a Rigidbody component
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Slow down the object by reducing its velocity
            rb.linearVelocity *= slowDownFactor;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Optionally, continue to slow down the object while it stays in the trigger
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity *= slowDownFactor;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Optionally, restore the speed or perform other actions when the object exits the trigger
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Example: restore original speed
            // Note: You might need to store the original speed when the object enters the trigger
        }
    }
}
