using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
namespace Twelve
{
public class CameraResultionScript : MonoBehaviour
{
        [FormerlySerializedAs("referenceWidth")]  public float ReferenceWidth = 720f;  // The width of the reference resolution
        [FormerlySerializedAs("referenceHeight")] public float ReferenceHeight = 1280f; // The height of the reference resolution
        [FormerlySerializedAs("pixelsPerUnit")] public float PixelsPerUnit = 100f;    // Pixels per unit

    private Camera MainCamera;

    void Start()
    {
        MainCamera = GetComponent<Camera>();
        UpdateOrthographicSize();
    }
    private void Update()
    {
        UpdateOrthographicSize();
    }
    void UpdateOrthographicSize()
    {
        float targetAspect = ReferenceWidth / ReferenceHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            MainCamera.orthographicSize = ReferenceHeight / 2.0f / PixelsPerUnit;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            MainCamera.orthographicSize = ReferenceHeight / 2.0f / PixelsPerUnit * scaleWidth;
        }
    }

    void OnValidate()
    {
        if (MainCamera == null) MainCamera = GetComponent<Camera>();
        UpdateOrthographicSize();
    }
}}
