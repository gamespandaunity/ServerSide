using System.Net.Mime;
using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.UI;

public class PopupManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelReference;
    [SerializeField] private Text textReference;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float slideDistance = 100f;

    private RectTransform panelRectTransform;
    private Vector2 originalPosition;
    private Coroutine currentPopupCoroutine;

    public static PopupManager instance;

    private void Awake()
    {
        instance = this;
        if (panelReference != null)
        {
            panelRectTransform = panelReference.GetComponent<RectTransform>();

            // Add CanvasGroup if not assigned
            if (canvasGroup == null)
            {
                canvasGroup = panelReference.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = panelReference.AddComponent<CanvasGroup>();
                }
            }

            // Store original position immediately
            if (panelRectTransform != null)
            {
                originalPosition = panelRectTransform.anchoredPosition;
            }

            // Hide panel initially
            panelReference.SetActive(false);
        }
    }

    // Show popup with message and callback
    public void ShowPopup(string message, Action onComplete = null)
    {
        // Stop any existing popup animation
        if (currentPopupCoroutine != null)
        {
            StopCoroutine(currentPopupCoroutine);
        }

        if (textReference != null)
        {
            textReference.text = message;
        }

        currentPopupCoroutine = StartCoroutine(PopupSequence(onComplete));
    }

    // Show popup without changing text
    public void ShowPopup(Action onComplete = null)
    {
        if (currentPopupCoroutine != null)
        {
            StopCoroutine(currentPopupCoroutine);
        }

        currentPopupCoroutine = StartCoroutine(PopupSequence(onComplete));
    }

    private IEnumerator PopupSequence(Action onComplete)
    {
        // Store positions locally to ensure consistency throughout this animation
        Vector2 targetPosition = originalPosition;
        Vector2 hiddenPosition = originalPosition - new Vector2(0, slideDistance);

        // Reset to original position first
        panelRectTransform.anchoredPosition = targetPosition;

        // Activate panel
        panelReference.SetActive(true);

        // === SLIDE UP AND FADE IN ===
        panelRectTransform.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;

        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

            panelRectTransform.anchoredPosition = Vector2.Lerp(hiddenPosition, targetPosition, smoothProgress);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothProgress);

            yield return null;
        }

        // Ensure final values
        panelRectTransform.anchoredPosition = targetPosition;
        canvasGroup.alpha = 1f;

        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);

        // === SLIDE DOWN AND FADE OUT (SAME DISTANCE) ===
        elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float smoothProgress = Mathf.Pow(progress, 3f);

            panelRectTransform.anchoredPosition = Vector2.Lerp(targetPosition, hiddenPosition, smoothProgress);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothProgress);

            yield return null;
        }

        // Ensure final values
        panelRectTransform.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;

        // Deactivate panel
        panelReference.SetActive(false);

        // Reset to original position for next use
        panelRectTransform.anchoredPosition = originalPosition;

        // Wait one frame to ensure deactivation is fully processed
        yield return null;

        // NOW invoke callback - panel is completely off
        onComplete?.Invoke();

        currentPopupCoroutine = null;
    }
    // Force hide popup immediately
    public void HidePopupImmediately()
    {
        if (currentPopupCoroutine != null)
        {
            StopCoroutine(currentPopupCoroutine);
            currentPopupCoroutine = null;
        }

        if (panelReference != null)
        {
            panelReference.SetActive(false);
            panelRectTransform.anchoredPosition = originalPosition;
            canvasGroup.alpha = 0f;
        }
    }

    [ContextMenu("Test Popup Message")]
    public void TestMesssage()
    {
        ShowPopup("This is a test popup message!");
    }
}