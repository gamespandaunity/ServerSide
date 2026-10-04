using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutOfGoldMenu : MonoBehaviour {

    public Text totalCoins;
    public Text requiredCoins;
    PlayerDataSerializeable playerData;
    void OnEnable()
    {
        playerData = PlayerDataController.instance.playerStats;
        
        requiredCoins.text = (playerData.CarsList[playerData.CurrentSelectedVehicle - 1].UpgradePrice - playerData.PlayerCash).ToString();

        
        totalCoins.text = playerData.PlayerCash.ToString();
    }
}
