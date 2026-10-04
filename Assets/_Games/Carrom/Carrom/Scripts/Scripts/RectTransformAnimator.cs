using UnityEngine;
namespace BEKStudio
{
    public class RectTransformAnimator : MonoBehaviour
{
    public RectTransform targetRect; // Reference to the RectTransform to animate
    public Vector2 startPosition;    // Starting position of the RectTransform
    public Vector2 targetPosition;   // Target position of the RectTransform
    public float delay = 0.5f;       // Delay before the animation starts
    public float duration = 1.0f;    // Duration of the animation
    public LeanTweenType tweenType = LeanTweenType.easeOutQuad;

    private void OnEnable()
    {
        // Set the starting position of the RectTransform
        targetRect.anchoredPosition = startPosition;

        // Animate to the target position using LeanTween
        LeanTween.move(targetRect, targetPosition, duration).setDelay(delay);
    }

    private void OnDestroy()
    {
        LeanTween.cancel(gameObject);
    }
}
}
