using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
//
using POKER;
using UnityEngine.SceneManagement;
namespace POKER
{
    public class QuickShopManager : MonoBehaviour
    {
        public TMP_Text chipText;


        private void OnEnable()
        {
            if (LocalSettings.GetTotalChips() > 0)
                chipText.text = LocalSettings.Rs(LocalSettings.GetTotalChips().ToString());
            else
                chipText.text = "0";

        }

        public void addcahsBtn()
        {
            SceneManager.LoadScene("Home");
            //LocalSettings.Add_ChipsFrom();
            //chipText.text = LocalSettings.Rs(LocalSettings.GetTotalChips().ToString());
            //if (LocalSettings.IsMenuScene())
            //    Menu_Manager.Instance.TotalChips.text = LocalSettings.Rs(LocalSettings.GetTotalChips().ToString());
        }
    }
}