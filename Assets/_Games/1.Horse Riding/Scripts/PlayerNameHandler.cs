 
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameHandler : MonoBehaviour {
    [SerializeField]
    //Internal reference to the InputField used to enter the server name.
    protected InputField m_NameInput;
    public GameObject okButton;
    // Use this for initialization
    // Use this for initialization

    public Toggle toggleAutoSelect;

    void OnEnable()
    {
        if (!PlayerDataController.instance.playerStats.playerName.Equals("Player"))
        {
            m_NameInput.text = PlayerDataController.instance.playerStats.playerName;

        }
        else
        {
            m_NameInput.text = string.Empty;

        }
        okButton.SetActive(false);
        Invoke("showOkBtn", 2);
        toggleAutoSelect.isOn = PlayerDataController.instance.playerStats.autoSelectCountry;

       
    }

    public void OnToggleAutoselect()
    {
       
        PlayerDataController.instance.playerStats.autoSelectCountry = toggleAutoSelect.isOn;

        
    }
    void showOkBtn()
    {
        okButton.SetActive(true);
    }

    public void OnNameChanged()
    {

        if (String.IsNullOrEmpty(m_NameInput.text))
        {
            if (RivalsCountryController.Instance.countryData!=null)
            {
                PlayerDataController.instance.playerStats.playerName = RivalsCountryController.Instance.countryData.GetRandomPlayerName();
                //Debug.Log("Player Name "+ PlayerDataController.Instance.playerData.playerName);
            }
            else
            {
                PlayerDataController.instance.playerStats.playerName = MultiPlayerGame.getDefaultPlayerName();

            }

        }
        else
        {
            PlayerDataController.instance.playerStats.playerName = m_NameInput.text;

        }
        PlayerDataController.instance.playerStats.isPlayerNameSet = true;
        PlayerDataController.instance.SaveData();
        //Photon Removal    PhotonNetwork.LocalPlayer.NickName = MultiPlayerGame.getPlayerName();

        if (PlayerDataController.instance.playerStats.autoSelectCountry && (PlayerDataController.instance.playerStats.countryCode == null || PlayerDataController.instance.playerStats.countryCode.Equals("") ||  PlayerDataController.instance.playerStats.countryCode.Equals("EW")))
        {
            //Debug.Log("Set country");
            MainMenuManager.Instance.SetCountryCode();
        }
    }
}
