using UnityEngine;

public class portateToLandscape : MonoBehaviour
{
    public ScreenOrientation targetOrientation = ScreenOrientation.LandscapeLeft;
    public float targetOrthographicSize = 5f;
    public float targetAspect = 1080 / 1920;

    void Start()
    {
        // Set the screen orientation
        Screen.orientation = targetOrientation;
        //Debug.Log("Screen orientation: " + Screen.orientation);

        // Adjust the camera aspect ratio
        //Camera mainCamera = Camera.main;
        //if (mainCamera != null)
        //{
        //    mainCamera.orthographicSize = targetOrthographicSize;
        //    mainCamera.aspect = targetAspect;
        //}
    }
}
