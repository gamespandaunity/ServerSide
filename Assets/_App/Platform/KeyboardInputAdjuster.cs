//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections;
//using TMPro;

//public class KeyboardInputAdjuster : MonoBehaviour
//{
//    public TMP_InputField inputField;
//    public RectTransform inputFieldTransform;
//    public Canvas canvas;
//    public float padding = 10f;

//    private Vector2 originalAnchoredPosition;
//    private Coroutine adjustCoroutine;
//    private KeyboardGameManager keyboard;

//    private void Start()
//    {
//        originalAnchoredPosition = inputFieldTransform.anchoredPosition;

//        inputField.onEndEdit.AddListener(_ => StopAdjustment());
//        inputField.onSelect.AddListener(_ => StartAdjustment());
//    }

//    private void StartAdjustment()
//    {
//        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
//        adjustCoroutine = StartCoroutine(AdjustInputPosition());
//    }

//    private void StopAdjustment()
//    {
//        if (adjustCoroutine != null)
//        {
//            StopCoroutine(adjustCoroutine);
//            adjustCoroutine = null;
//        }
//        inputFieldTransform.anchoredPosition = originalAnchoredPosition;
//    }

//    private IEnumerator AdjustInputPosition()
//    {
//        // Wait for keyboard to initialize
//        yield return new WaitForSeconds(0.1f);

//        while (keyboard != null && keyboard.active)
//        {
//            // Get keyboard area in screen coordinates
//            Rect keyboardRect = keyboard.area;

//            if (keyboardRect.height > 0)
//            {
//                // Convert keyboard position to canvas space
//                Vector2 keyboardPosition = new Vector2(0, keyboardRect.height);
//                RectTransformUtility.ScreenPointToLocalPointInRectangle(
//                    canvas.GetComponent<RectTransform>(),
//                    keyboardPosition,
//                    null,
//                    out Vector2 localPoint
//                );

//                // Adjust input field position with padding
//                inputFieldTransform.anchoredPosition = new Vector2(
//                    originalAnchoredPosition.x,
//                    -localPoint.y + padding
//                );
//            }
//            else
//            {
//                // Fallback for devices that don't report keyboard height
//                inputFieldTransform.anchoredPosition = new Vector2(
//                    originalAnchoredPosition.x,
//                    Screen.height * 0.25f
//                );
//            }

//            yield return null;
//        }

//        // Reset position when keyboard closes
//        inputFieldTransform.anchoredPosition = originalAnchoredPosition;
//    }
//}