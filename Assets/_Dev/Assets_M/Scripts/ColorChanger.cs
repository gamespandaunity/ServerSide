using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ColorChanger : MonoBehaviour
{
    public Color[] colors; // Array of colors to cycle through
    public float transitionSpeed = 1f; // Speed of color transition
    private Image objectRenderer;
    private int currentColorIndex = 0;

    void Start()
    {
        if (colors.Length == 0)
        {
            ConstantsData_M.Log("No colors assigned to the ColorChanger script!");
            return;
        }

        objectRenderer = GetComponent<Image>();
        if (objectRenderer == null)
        {
            ConstantsData_M.Log("No Renderer found on the GameObject!");
            return;
        }

        StartCoroutine(ChangeColorSmoothly());
    }

    private IEnumerator ChangeColorSmoothly()
    {
        while (true)
        {
            Color startColor = objectRenderer.color;
            Color targetColor = colors[currentColorIndex];

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * transitionSpeed;
                objectRenderer.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            // Move to the next color in the array
            currentColorIndex = (currentColorIndex + 1) % colors.Length;
        }
    }
    private void OnDestroy()
    {
        StopAllCoroutines();
    }
    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
