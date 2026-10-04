using UnityEngine;

public class ChangeOrientationAndAspectRatio : MonoBehaviour
{
    public ScreenOrientation targetOrientation = ScreenOrientation.Portrait;
    public float targetOrthographicSize = 5.6f;
   // public float targetAspect = 9 / 16f;

    void Start()
    {
        // Set the screen orientation
        Screen.orientation = targetOrientation;
        //Debug.Log("Screen orientation: " + Screen.orientation);

        // Adjust the camera aspect ratio
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.orthographicSize = targetOrthographicSize;
           // mainCamera.aspect = targetAspect;
        }
    }
}
