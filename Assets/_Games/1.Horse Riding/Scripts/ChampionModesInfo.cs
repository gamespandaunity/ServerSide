using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChampionModesInfo : MonoBehaviour
{
    public int ModeId;
    public bool IsUnlocked;
    [TextArea]
    public string UnlockInfo;

    public GameObject lockImage;

    void OnEnable()
    {
        switch (ModeId)
        {
            case 0:
                IsUnlocked = PlayerDataController.instance.playerStats.UnlockedDubaiChampion;
                break;
            case 1:
                PlayerDataController.instance.playerStats.UnlockedBritishChampion =
                    PlayerDataController.instance.playerStats.LastUnlockedDubaiChampionLevel > 7;
                PlayerDataController.instance.SaveData();
                IsUnlocked = PlayerDataController.instance.playerStats.UnlockedBritishChampion;
                break;
            case 2:
                PlayerDataController.instance.playerStats.UnlockedKentuckyChampion =
                    PlayerDataController.instance.playerStats.LastUnlockedBritishChampionLevel > 10;
                PlayerDataController.instance.SaveData();
                IsUnlocked = PlayerDataController.instance.playerStats.UnlockedKentuckyChampion;
                break;
            case 3:
                PlayerDataController.instance.playerStats.UnlockedPegasusChampion =
                    PlayerDataController.instance.playerStats.LastUnlockedKentuckyChampionLevel > 10;
                PlayerDataController.instance.SaveData();
                IsUnlocked = PlayerDataController.instance.playerStats.UnlockedPegasusChampion;
                break;
        }

        lockImage.SetActive(!IsUnlocked);
    }
}
