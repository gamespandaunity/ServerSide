using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageInfo : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Message;
    public Image ImageToShare;
    void Start()
    {
        //Destroy(gameObject,5);
    }

   
    public void imageShouldShow()
    {
        ImageShowPanel.instance.ShowImage(ImageToShare);
    }

    public void destroyThisGameobject()
    {
        Destroy(gameObject);
    }
    
}
