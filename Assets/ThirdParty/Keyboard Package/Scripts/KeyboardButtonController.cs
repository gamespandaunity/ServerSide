using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class KeyboardButtonController : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image containerBorderImage;
    [SerializeField] Image containerFillImage;
    [SerializeField] Image containerIcon;
    [SerializeField] TextMeshProUGUI containerText;
    [SerializeField] TextMeshProUGUI containerActionText;

    private void Start() {
        SetContainerBorderColor(ColorDataStore.GetKeyboardBorderColor());
        SetContainerFillColor(ColorDataStore.GetKeyboardFillColor());
        SetContainerTextColor(ColorDataStore.GetKeyboardTextColor());
        SetContainerActionTextColor(ColorDataStore.GetKeyboardActionTextColor());
    }

    public void SetContainerBorderColor(Color color) => containerBorderImage.color = color;
    public void SetContainerFillColor(Color color) => containerFillImage.color = color;
    public void SetContainerTextColor(Color color) => containerText.color = color;
    public void SetContainerActionTextColor(Color color) { 
        containerActionText.color = color;
        containerIcon.color = color;
    }

    public void AddLetter() {
        if(KeyboardGameManager.Instance != null) {
            KeyboardGameManager.Instance.AddLetter(containerText.text);
        } else {
            Debug.Log(containerText.text + " is pressed");
        }
    }
    public void DeleteLetter() { 
        if(KeyboardGameManager.Instance != null) {
            KeyboardGameManager.Instance.DeleteLetter();
        } else {
            Debug.Log("Last char deleted");
        }
    }
    public void SubmitWord() {
        if(KeyboardGameManager.Instance != null) {
            KeyboardGameManager.Instance.SubmitWord();
        } else {
            Debug.Log("Submitted successfully!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        KeyboardGameManager.Instance._keyboardController.ispointerdown = true;
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
}