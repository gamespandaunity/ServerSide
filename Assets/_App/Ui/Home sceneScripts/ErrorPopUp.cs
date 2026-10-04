using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class ErrorPopUp : MonoBehaviour
{
    public TMP_Text Titletxt;
    public TMP_Text Messagetext;
    // Start is called before the first frame update
    public void ClosePopUp()
    {
        Destroy(this.gameObject);
    }
    public void InitMessage(string msg,string title="")
    {
        Messagetext.text = msg;
        Titletxt.text = string.IsNullOrEmpty(title) ? Titletxt.text : title;
    }
    
}
