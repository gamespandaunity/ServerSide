using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MathCustom : MonoBehaviour, IPointerClickHandler
{
    public float duration = 0.5f; // Time to move from posA to posB
    public float Tranparentduration = 0.5f; // Time to move from posA to posB
    public Vector2 posA;
    public Vector2 posB;
    public Image TransparentLayer;
    public bool withpos;
    public bool RemainActive;
    private RectTransform _rectTransform;

    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (TransparentLayer != null)
        {
            Color color = TransparentLayer.color;
            color.a = 0f; // Start fully transparent
            TransparentLayer.color = color;
        }
    }

    public bool ispointerdown;
    public void OnPointerClick(PointerEventData eventData)
    {
        ispointerdown = true;
        // Only prevent deselection if there's an active input and we are clicking inside the keyboard layout
        if (KeyboardGameManager.Instance.textBox != null)
        {
            // Set the selected GameObject to the input field to keep focus
            //  EventSystem.current.SetSelectedGameObject(KeyboardGameManager.Instance.textBox.gameObject);

            // Activate the input field to ensure caret focus is restored
            KeyboardGameManager.Instance.textBox.ActivateInputField();
            KeyboardGameManager.Instance.textBox.MoveTextEnd(false); // false means smooth move

            KeyboardGameManager.Instance.textBox.caretPosition = KeyboardGameManager.Instance.textBox.text.Length;

        }
    }

    // Call this method to clear focus manually when you want the input to lose focus (like on "Done" or "Submit" button press)
    public void ClearFocus()
    {
        KeyboardGameManager.Instance.textBox = null;
        EventSystem.current.SetSelectedGameObject(null);
    }

    // Method to set a new focused input (in case you need to update focus manually)
    public void SetFocusedInput(TMP_InputField input)
    {
        KeyboardGameManager.Instance.textBox = input;
        KeyboardGameManager.Instance.textBox.MoveTextEnd(false);
        KeyboardGameManager.Instance.textBox.caretPosition = KeyboardGameManager.Instance.textBox.text.Length;

    }
    private void OnEnable()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        posA = withpos ? _rectTransform.position : _rectTransform.anchoredPosition;
        StartCoroutine(OpenPanel());
    }
    public void ClosePanelButton()
    {
        StartCoroutine(ClosePanel());
    }
    public void OpenPanelButton(TMP_InputField input)
    {
        SetFocusedInput(input);
        gameObject.SetActive(true);
        StartCoroutine(OpenPanel());
    }
    public IEnumerator OpenPanel()
    {
        float elapsedTime = 0f;

        while (elapsedTime < Tranparentduration)
        {
            float t = elapsedTime / duration; // Normalized time (0 to 1)
            float timg = elapsedTime / Tranparentduration; // Normalized time (0 to 1)
            if (!withpos)
                _rectTransform.anchoredPosition = Vector2.Lerp(posA, posB, t);
            else
                _rectTransform.position = Vector2.Lerp(posA, posB, t);

            if (TransparentLayer != null)
            {
                Color color = TransparentLayer.color;
                color.a = Mathf.Lerp(1f, 0f, timg); // Gradually increase alpha
                TransparentLayer.color = color;
            }

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!withpos)
            _rectTransform.anchoredPosition = posB;
        else _rectTransform.position = posB;
        if (TransparentLayer != null)
        {
            Color finalColor = TransparentLayer.color;
            finalColor.a = 0f; // Ensure it's fully visible at the end
            TransparentLayer.color = finalColor;
            TransparentLayer.enabled = false;
        }
        ispointerdown = false;
        KeyboardGameManager.Instance.textBox.MoveTextEnd(false);
        KeyboardGameManager.Instance.textBox.caretPosition = KeyboardGameManager.Instance.textBox.text.Length;

        // StartCoroutine(ClosePanel());
    }
    public IEnumerator ClosePanel()
    {
        Debug.Log("Closing");
        yield return new WaitForSecondsRealtime(0.1f);
        if (ispointerdown)
        {
            if (KeyboardGameManager.Instance.textBox)
            {
                KeyboardGameManager.Instance.textBox.MoveTextEnd(false);
                KeyboardGameManager.Instance.textBox.caretPosition = KeyboardGameManager.Instance.textBox.text.Length;
            }
            ispointerdown = false;
            yield return null;
        }
        else
        {
            ispointerdown = false;
            float elapsedTime = 0f;
            TransparentLayer.enabled = true;

            while (elapsedTime < Tranparentduration)
            {
                float t = elapsedTime / duration; // Normalized time (0 to 1)
                float timg = elapsedTime / Tranparentduration; // Normalized time (0 to 1)
                if (!withpos)
                    _rectTransform.anchoredPosition = Vector2.Lerp(posB, posA, t);
                else _rectTransform.position = Vector2.Lerp(posB, posA, t);

                if (TransparentLayer != null)
                {
                    Color color = TransparentLayer.color;
                    color.a = Mathf.Lerp(0f, 1f, timg); // Gradually increase alpha
                    TransparentLayer.color = color;
                }

                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!withpos)
                _rectTransform.anchoredPosition = posA;
            else _rectTransform.position = posA;
            if (TransparentLayer != null)
            {
                Color finalColor = TransparentLayer.color;
                finalColor.a = 1f; // Ensure it's fully visible at the end
                TransparentLayer.color = finalColor;
            }
            if (withpos)
                Destroy(gameObject.transform.parent.gameObject, 0.1f);
            KeyboardAnimation anim = KeyboardGameManager.Instance.textBox.GetComponentInParent<KeyboardAnimation>();
            if (anim != null && anim.MoveArea)
                anim.MoveArea.anchoredPosition = anim.originalChatUIPos;
            "FULLY CLOSE".Show(anim!=null);
            KeyboardGameManager.Instance.textBox = null;

            if (!RemainActive) gameObject.SetActive(false);
        }
    }
  
}