using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

public class ConfirmAlert : MonoBehaviour
{
    public TMP_Text Title;
    public TMP_Text Description;
    public Button YesBtn, NoBtn;
    public void Init(string title, string Des, UnityAction yesbtncall, UnityAction Nobtncall = null)
    {
        Title.text = title;
        Description.text = Des;
        gameObject.SetActive(true);
        YesBtn.onClick.AddListener(() => { yesbtncall.Invoke(); DestroyPanel(); });
        if (Nobtncall != null)
            NoBtn.onClick.AddListener(() => { Nobtncall.Invoke(); DestroyPanel(); });
        else
            NoBtn.onClick.AddListener(DestroyPanel);

    }
    void DestroyPanel()
    {
        Destroy(gameObject);
    }
}
