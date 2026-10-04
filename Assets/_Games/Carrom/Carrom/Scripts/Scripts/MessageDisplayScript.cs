using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BEKStudio
{
    public class MessageDisplayScript : MonoBehaviour
{
    public static MessageDisplayScript instance;
    public GameObject displayObject;
    [SerializeField] private TextMeshProUGUI headingTxt;

    [SerializeField] private TextMeshProUGUI messageTxt;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowMessage(string heading,string msg)
    {
        headingTxt.text = heading;
        messageTxt.text = msg;
        displayObject.SetActive(true);
    }
}
}
