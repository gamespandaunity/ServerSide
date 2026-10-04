using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TMPInputSelectListener : MonoBehaviour, ISelectHandler,IDeselectHandler
{
    public void OnDeselect(BaseEventData eventData)
    {
      //  if (EventSystem.current.gameObject.GetComponent<TMP_InputField>() != null) return;
       KeyboardGameManager.Instance._keyboardController.ClosePanelButton();
    }

    public void OnSelect(BaseEventData eventData)
    {
        var input = GetComponent<TMP_InputField>();
        if (input != KeyboardGameManager.Instance.textBox)
        {
         KeyboardGameManager.Instance.OnTMPInputSelected(input);
        }
    }

   

   
}
