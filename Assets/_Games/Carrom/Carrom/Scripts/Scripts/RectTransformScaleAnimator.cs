using UnityEngine;
namespace BEKStudio
{
    public class RectTransformScaleAnimator : MonoBehaviour
{
    public RectTransform targetRect; // Reference to the RectTransform to animate
    public Vector3 startScale = Vector3.one; // Starting scale of the RectTransform
    public Vector3 targetScale = Vector3.one * 2; // Target scale of the RectTransform
    public float delay = 0.5f; // Delay before the animation starts
    public float duration = 1.0f; // Duration of the animation
    public LeanTweenType tweenType = LeanTweenType.easeOutQuad; // Tween type for animation

    private void OnEnable()
    {
        // Set the starting scale of the RectTransform
        targetRect.localScale = startScale;

        // Animate to the target scale using LeanTween with specified tween type
        LeanTween.scale(targetRect, targetScale, duration)
            .setDelay(delay)
            .setEase(tweenType);
    }

    private void OnDestroy()
    {
        LeanTween.cancel(gameObject);
    }
}
}
