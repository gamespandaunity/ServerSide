using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MultiInfoPanel : MonoBehaviour {
    [FormerlySerializedAs("msgTextView")] public Text messageText;
    [FormerlySerializedAs("cancelBtn")] public GameObject cancelButton;


  //  public void ShowMessage(string msg)
    public void DisplayMessage(string msg)
    {
        messageText.text = msg;
        gameObject.SetActive(true);
        CancelInvoke();
        cancelButton.SetActive(false);
        Invoke("showCrossBtn",10);
    }
  //  void showCrossBtn()
    void ShowCloseButton()
    {
        cancelButton.SetActive(true);

    }
    //public void HideInfo()
    public void HideInfoPanel()
    {
        gameObject.SetActive(false);
    }
}
