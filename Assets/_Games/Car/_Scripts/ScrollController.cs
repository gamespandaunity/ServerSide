namespace CarRace 
{
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollController : MonoBehaviour, IBeginDragHandler
{
    [SerializeField] float smoothTime = 0.5f; // Adjust the speed of the scroll
    private ScrollRect horizontalScrollView;

    private float targetNormalizedPosition = 0f;
    private bool isScrolling = false;
    private bool isUserInteracting = false; // Flag to track user interaction
    private void Awake()
    {
        horizontalScrollView = GetComponent<ScrollRect>();
    }
    private void OnEnable()
    {
        isUserInteracting = false;
        horizontalScrollView.horizontalNormalizedPosition = 0.25f;
        SmoothScroll(0f);
    }
    void Update()
    {
        if (isUserInteracting)
        {
            isScrolling = false; // Stop smooth scrolling when user interacts
        }
        if (isScrolling)
        {
            // Smoothly interpolate between the current and target normalized position
            horizontalScrollView.horizontalNormalizedPosition = Mathf.Lerp(horizontalScrollView.horizontalNormalizedPosition, targetNormalizedPosition, smoothTime * Time.deltaTime);

            // Check if we are close enough to the target position
           // if (Mathf.Abs(horizontalScrollView.horizontalNormalizedPosition - targetNormalizedPosition) < 0.005f)
            if (Mathf.Abs(horizontalScrollView.horizontalNormalizedPosition - targetNormalizedPosition) < 0.00002f)
            {
                isScrolling = false; // Stop scrolling when close enough
            }
        }
    }

    // Call this function to initiate a smooth scroll
    public void SmoothScroll(float targetNormalizedPosition)
    {
        this.targetNormalizedPosition = Mathf.Clamp01(targetNormalizedPosition);
        isScrolling = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isUserInteracting = true;
    }
}

}