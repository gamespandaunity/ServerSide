using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class uiAnimatorSpinControl : MonoBehaviour
{
    public RectTransform rectTransform;
    private CanvasGroup canvasGroupComponent;
    private float animValue;
    private float animTarget = 1f;
    private float animVel;

    public mainScript _mainScript;
    [HideInInspector]
    public HAND_MODE handModeSpinControl;

    public Camera uiCamera;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroupComponent = GetComponent<CanvasGroup>();
     //   uiCamera = Camera.main; // Change this if using a different UI camera
    }

    public void showSpinControl()
    {
        base.transform.parent.gameObject.SetActive(true);
        animValue = 0f;
        animTarget = 1f;
        canvasGroupComponent.alpha = 0f;
    }

    public void hideSpinControl(bool val)
    {
        animValue = 1f;
        animTarget = 0f;
        _mainScript.showPowerMeter(val);
    }

    private void Update()
    {
        // Animate
        canvasGroupComponent.alpha = animValue;
        animValue = Mathf.SmoothDamp(animValue, animTarget, ref animVel, 0.14f);
        rectTransform.localScale = new Vector3(animValue, animValue, 1f);

        if (handModeSpinControl == HAND_MODE.Right)
        {
            rectTransform.anchoredPosition = new Vector2(50f, 245f) + new Vector2(240f, 40f) * animValue;
        }
        else if (handModeSpinControl == HAND_MODE.Left)
        {
            rectTransform.anchoredPosition = new Vector2(-50f, 245f) + new Vector2(-240f, 40f) * animValue;
        }

        if (animVel < 0f && animValue < 0.02f && animTarget == 0f)
        {
            mainScript.spinSetOn = false;
            
            base.transform.parent.gameObject.SetActive(false);
        }

        // Handle outside click
        CheckOutsideClick();
    }

    private void CheckOutsideClick()
    {
        if (!gameObject.activeInHierarchy || animTarget == 0f)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            Debug.Log($"Raycast hit {results.Count} objects");

            foreach (var result in results)
            {
                Debug.Log("Hit UI Object: " + result.gameObject.name);

                if (result.gameObject != null && result.gameObject.transform.IsChildOf(rectTransform))
                {
                    Debug.Log("Click is inside the spin group UI.");
                    return;
                }
            }

            Debug.Log("Click is outside . hiding spin control");
            hideSpinControl(!_mainScript.aiPlaying);
        }
    }




    private bool IsPointerOverUIObject()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}
