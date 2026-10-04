using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallCollisionChecker : MonoBehaviour
{
    public Transform targetObject; // The object whose position we want to match

    private void OnEnable()
    {
        if (targetObject != null)
        {
            // Change the position of the current object to match the position of the target object
            Vector3 targetPosition = targetObject.position;
            transform.position = new Vector3(targetPosition.x, 0.1085f, targetPosition.z);
        }
        else
        {
            ConstantsData_M.LogInfo("Target object is not assigned!");
        }
    }
}
