using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuickShopManager : MonoBehaviour
{
    public TMP_Text chipText;


    private void OnEnable()
    {
        chipText.text= LocalSettings.Rs(LocalSettings.GetTotalChips().ToString());
    }

    public void addcahsBtn()
    {
        // LocalSettings.Add_ChipsFrom();
        SceneManager.LoadSceneAsync("Home");
    }
}
