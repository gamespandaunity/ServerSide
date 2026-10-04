using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraResultionScript : MonoBehaviour
{
    public float referenceWidth = 720f;  // The width of the reference resolution
    public float referenceHeight = 1280f; // The height of the reference resolution
    public float pixelsPerUnit = 100f;    // Pixels per unit

    private Camera camera;

    void Start()
    {
        camera = GetComponent<Camera>();
        UpdateOrthographicSize();
    }
    private void Update()
    {
        UpdateOrthographicSize();
    }
    void UpdateOrthographicSize()
    {
        float targetAspect = referenceWidth / referenceHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            camera.orthographicSize = referenceHeight / 2.0f / pixelsPerUnit;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            camera.orthographicSize = referenceHeight / 2.0f / pixelsPerUnit * scaleWidth;
        }
    }

    void OnValidate()
    {
        if (camera == null) camera = GetComponent<Camera>();
        UpdateOrthographicSize();
    }
}
