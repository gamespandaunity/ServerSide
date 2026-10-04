using System.Collections;
using System.Collections.Generic;
using BEKStudio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StrikerPositionAdjuster : MonoBehaviour
{
    [FormerlySerializedAs("pucks")] public GameObject[] puckObjects;
    [FormerlySerializedAs("adjustmentStep")] public float strikerMoveStep = 0.1f;
    [FormerlySerializedAs("minX")] public float strikerMinX = -4.5f;
    [FormerlySerializedAs("maxX")] public float strikerMaxX = 4.5f;
    [FormerlySerializedAs("strikerRadius")] public float strikerRadius = 0.5f;

    public void SetPosition()
    {
        // Find the best position to avoid collision
        if (SceneManager.GetActiveScene().name == "CarromOffline")
        {
            Vector3 bestPosition = FindBestPosition(PlayerPuckOffline.Instance.gameObject.transform.position);
            PlayerPuckOffline.Instance.gameObject.transform.position = bestPosition;
        }
        else
        {
            Vector3 bestPosition = FindBestPosition(PlayerPuck.Instance.gameObject.transform.position);
            PlayerPuck.Instance.gameObject.transform.position = bestPosition;

        }
        ////Debug.Log("Striker radiud "+ striker.GetComponent<CircleCollider2D>().radius * striker.transform.localScale.x);
        ////Debug.Log("Puck radiud " + pucks[0].GetComponent<CircleCollider2D>().radius * pucks[0].transform.localScale.x);

    }
    public void SetSliderPosition(Slider slider)
    {
        float currentX;
        if (SceneManager.GetActiveScene().name == "CarromOffline")
        {
            currentX = PlayerPuckOffline.Instance.gameObject.transform.localPosition.x;

        }
        else
        {
            currentX = PlayerPuck.Instance.gameObject.transform.localPosition.x;

        }
        // Calculate the slider value based on the current X position
        float sliderValue = CalculateSliderValue(currentX, strikerMinX, strikerMaxX);
        // Update the slider's value
        slider.value = sliderValue;
    }
    private float CalculateSliderValue(float currentX, float minX, float maxX)
    {
        // Ensure that currentX is clamped between minX and maxX
        currentX = Mathf.Clamp(currentX, minX, maxX);

        // Calculate and return the slider value
        return (currentX - minX) / (maxX - minX);
    }
    private Vector3 FindBestPosition(Vector3 initialPosition)
    {
        float startX = initialPosition.x;
        float y = initialPosition.y; // Assume we want to move along the X-axis only (along the line)

        // Check the current position first
        if (!IsColliding(startX, y))
        {
            return initialPosition; // If no collision, return the current position
        }

        // Try adjusting left and right within the defined boundaries
        for (int i = 1; i < 100; i++)  // Maximum 100 tries (can adjust this)
        {
            // Calculate new positions
            float leftX = Mathf.Clamp(startX - i * strikerMoveStep, strikerMinX, strikerMaxX);
            float rightX = Mathf.Clamp(startX + i * strikerMoveStep, strikerMinX, strikerMaxX);

            // Check if moving left avoids collision
            if (!IsColliding(leftX, y))
            {
                return new Vector3(leftX, y, initialPosition.z);
            }

            // Check if moving right avoids collision
            if (!IsColliding(rightX, y))
            {
                return new Vector3(rightX, y, initialPosition.z);
            }
        }

        ConstantsData_M.Log("Inital Erro...");
        // Return the original position if no suitable position is found
        return initialPosition;
    }
    private bool IsColliding(float x, float y)
    {
        Vector2 strikerPosition = new Vector2(x, y);
        foreach (GameObject puck in puckObjects)
        {
            Collider2D puckCollider = puck.GetComponent<Collider2D>();
            // Check the distance between striker and puck using bounds
            float puckRadius = puckCollider.bounds.extents.x; // Approximate puck radius
                                                              // //Debug.Log("Puck radiud 2" + puckRadius);

            float distance = Vector2.Distance(strikerPosition, puck.transform.position);

            // Check if the distance is less than the sum of the radii (indicating a collision)
            if (distance < strikerRadius + puckRadius)
            {
                return true; // A collision is detected
            }
        }
        return false; // No collisions detected
    }
}
