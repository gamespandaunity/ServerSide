using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class KeyPadInput : MonoBehaviour
{
    public TMP_InputField selectedField;
    public List<TMP_InputField> inputs;
    public List<TMP_InputField> editInputField;
    public void OnEnable()
    {
        if (editInputField.Count > 0)
        {
            selectedField = editInputField[0];
            selectedField.ActivateInputField();
            selectedField.caretPosition = selectedField.text.Length;
        }
        else
        {
            selectedField = inputs[0];
            selectedField.ActivateInputField();
            selectedField.caretPosition = selectedField.text.Length;
        }
    }

    public void EnterChar(string ch)
    {
        if (selectedField != null)
        {
            if (selectedField.characterLimit == 0 || selectedField.text.Length < selectedField.characterLimit)
            {
                selectedField.text += ch;
                selectedField.ActivateInputField();
                selectedField.caretPosition = selectedField.text.Length;

            }
        }
    }
    public void InputNumber()
    {
        EnterChar(EventSystem.current.currentSelectedGameObject.GetComponentInChildren<Text>().text);
    }
    public void SelectInput()
    {
        selectedField = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();

    }
    public void DeleteNumber()
    {
        if (selectedField != null && selectedField.text.Length > 0)
        {
            selectedField.text = selectedField.text.Substring(0, selectedField.text.Length - 1);
            selectedField.ActivateInputField();
            selectedField.caretPosition = selectedField.text.Length;
        }
    }
    public void MoveToNext()
    {

        if (editInputField.Contains(selectedField))
        {
            foreach (TMP_InputField input in editInputField)
            {
                if (input == selectedField)
                {
                    if (editInputField.IndexOf(input) + 1 < editInputField.Count)
                        selectedField = editInputField[editInputField.IndexOf(input) + 1];
                    else
                        selectedField = editInputField[0];
                    selectedField.ActivateInputField();
                    selectedField.caretPosition = selectedField.text.Length;

                    break;
                }

            }
        }
        else
        {
            foreach (TMP_InputField input in inputs)
            {
                if (input == selectedField)
                {
                    if (inputs.IndexOf(input) + 1 < inputs.Count)
                        selectedField = inputs[inputs.IndexOf(input) + 1];
                    else
                        selectedField = inputs[0];
                    selectedField.ActivateInputField();
                    selectedField.caretPosition = selectedField.text.Length;

                    break;
                }
            }
        }
    }


}
