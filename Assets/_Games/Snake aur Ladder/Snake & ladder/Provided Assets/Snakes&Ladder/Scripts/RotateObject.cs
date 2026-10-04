using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public Vector3 rotationVec3 = new Vector3(0, 5, 1);

    void Update()
    {
        // Get the current rotation
        Vector3 currentRotation = transform.localRotation.eulerAngles;

        // Increment each component of the rotation vector by its respective speed
        currentRotation.x += rotationVec3.x * Time.deltaTime;
        currentRotation.y += rotationVec3.y * Time.deltaTime;
        currentRotation.z += rotationVec3.z * Time.deltaTime;

        // Apply the new rotation
        transform.localRotation = Quaternion.Euler(currentRotation);
    }
}