using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereCast : MonoBehaviour
{
    public LayerMask collisionLayer; // Specify the layer mask for collision detection
    public float sphereRadius = 0.5f; // Radius of the sphere cast
    public float castDistance = 1f; // Distance of the sphere cast
    Vector3 newPosition;
    void Update()
    {

        // Cast a sphere downwards from the cue ball's eeweposition
        RaycastHit hit;
        Vector3 castDirection = Vector3.down; // Direction of the sphere cast
        if (Physics.SphereCast(transform.position, sphereRadius, castDirection, out hit, castDistance, collisionLayer))
        {
            // If a collision is detected, change the cue ball's position
             newPosition = hit.point + castDirection * sphereRadius; // Move slightly above the hit point
            transform.position = newPosition;
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition,sphereRadius);
    }
}
